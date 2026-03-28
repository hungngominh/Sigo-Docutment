# Vehicle Detail Flow - Performance Optimization Design

**Date:** 2026-03-28
**Approach:** Incremental (Phase-by-phase with before/after measurement)
**Scope:** Application code + Database optimization
**Constraint:** API response format unchanged

---

## Context

### Current Flow

Endpoint: `POST /api/v1/SearchingRentalService/Detail`

Call chain:
```
SearchingRentalServiceController.GetPublicRentalServiceDetail()
  -> RentalService_PublicAsync.GetPublicRentalServiceDetailAsync()
    -> GetDetailAsync() [RentalService_SelfdriveCarRental_DetailServiceAsync.cs]
```

### Current Execution Pattern (Sequential)

```
SetRentalServiceItemAsync          ──────► (DB: eager load Vehicle, HostAddress, RentalSetting, Insurance)
BuildDictServiceItem_ImageAsync    ──────► (DB: ServiceItem_Image + nested Image_File)
GetTotalPriceModel                 ──────► (compute pricing)
GetRentalPriceDetails              ──────► (compute)
GetDeliveryInfo                    ──────► (compute)
BuildPublicData                    ──────► (assemble response)
GetCharacteristics                 ──────► (cached lookups)
GetOwnerInfo                       ──────► (cached lookups)
GetFeatures                        ──────► (DB: ServiceItem_Feature_Mapping)
GetDocumentAndSecurityAsync        ──────► (DB: ServiceItem_RentalRequiredItem_Mapping)
GetReviewAsync                     ──────► (DB: Order + Order_Rating + Order_Vehicle)
GetVouchers                        ──────► (DB: DiscountCode_Summary + OneTimeUse, sequential)
GetBusySchedules                   ──────► (DB: 3 sequential queries - Booked, Weekday, DateBusy)
GetDefaultNote                     ──────► (DB: Order table query)
CreateLogView                      ──────► (DB: insert log)
```

**Total estimated DB round-trips:** 8-10 sequential queries per request.

### Identified Bottlenecks

| # | Issue | Location | Severity |
|---|-------|----------|----------|
| 1 | 8+ sequential DB queries in one request | GetDetailAsync | HIGH |
| 2 | 3 sequential queries in GetBusySchedules | RentalServiceItemService_Detail.cs:282 | HIGH |
| 3 | GetVouchers has conditionally sequential DB queries (Summary + OneTimeUse) | DiscountCodeService.cs:254 | MEDIUM |
| 4 | `.GetAwaiter().GetResult()` blocking async in sync detail path | DetailService.cs (sync variant) | LOW |
| 5 | Missing database indexes on frequently queried tables | DB level | CRITICAL |
| 6 | Independent async operations run sequentially | GetDetailAsync | HIGH |

**Note on bottleneck #3:** GetVouchers loads discount codes from in-memory cache (`CachedDataManagement.DiscountCodes`), then runs 2 conditional DB queries: (1) `DiscountCode_Summary` to check `IsReachLimit`, (2) `Order_DiscountCode_Applied_OneTimeUse` for one-time-use codes. The second query depends on the filtered result of the first, so they **cannot** be parallelized internally.

**Note on bottleneck #4:** The sync `GetDetail` path (non-async variant) contains `.GetAwaiter().GetResult()` calls but the main public endpoint uses the async path (`GetDetailAsync`). Phase 3c targets the sync path as a lower priority cleanup.

**Note on timing estimates:** All ms values in this document (e.g., "120ms", "95ms") are **placeholders/estimates**. Actual values will be measured in Phase 1 baseline collection.

---

## Phase 1: Performance Logging & Monitoring (Baseline)

### Goal
Establish baseline performance data before any optimization.

### 1.1 Application-Side: DetailPerformanceTracker

Add a lightweight tracker class that wraps each step in `GetDetailAsync` and records timing.

**Integration point:** `GetDetailAsync()` in `RentalService_SelfdriveCarRental_DetailServiceAsync.cs`

**Log output format:**
```
[DetailPerf] VehicleId=1234 | Total=850ms
  SetRentalServiceItem: 120ms
  BuildImages: 95ms
  GetTotalPrice: 45ms
  GetDeliveryInfo: 30ms
  GetCharacteristics: 5ms (cached)
  GetOwnerInfo: 8ms (cached)
  GetFeatures: 65ms
  GetDocumentSecurity: 70ms
  GetReview: 180ms
  GetVouchers: 85ms
  GetBusySchedules: 110ms
  GetDefaultNote: 37ms
```

**Fields tracked per step:**
- Step name
- Duration (ms)
- Whether it hit cache or DB

**Fields tracked per request:**
- Vehicle ID / RentalServiceItem ID
- Total duration (ms)
- Timestamp
- Number of DB queries executed

### 1.2 Database-Side: pg_stat_statements Snapshot

SQL script to run on staging PostgreSQL:

```sql
-- Enable pg_stat_statements if not already
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Create snapshot table for before/after comparison
CREATE TABLE IF NOT EXISTS dbo."_perf_query_snapshot" (
    snapshot_id TEXT,
    captured_at TIMESTAMPTZ DEFAULT NOW(),
    queryid BIGINT,
    query TEXT,
    calls BIGINT,
    total_exec_time DOUBLE PRECISION,
    mean_exec_time DOUBLE PRECISION,
    min_exec_time DOUBLE PRECISION,
    max_exec_time DOUBLE PRECISION,
    rows BIGINT
);

-- Take a snapshot (run before and after each phase)
INSERT INTO dbo."_perf_query_snapshot"
SELECT
    'baseline' AS snapshot_id,  -- change per phase: 'after_phase2a', 'after_phase3a', etc.
    NOW(),
    queryid, query, calls, total_exec_time, mean_exec_time, min_exec_time, max_exec_time, rows
FROM pg_stat_statements
WHERE query ILIKE '%dbo%'
   AND (
       query ILIKE '%RentalService%'
       OR query ILIKE '%ServiceItem_Image%'
       OR query ILIKE '%Order_Rating%'
       OR query ILIKE '%BusyRentalSchedule%'
       OR query ILIKE '%BookedRentalSchedule%'
       OR query ILIKE '%DiscountCode%'
       OR query ILIKE '%Feature_Mapping%'
       OR query ILIKE '%ViewHistory%'
       OR query ILIKE '%Order%'
       OR query ILIKE '%RentalRequiredItem%'
   );
-- Note: EF Core generates parameterized queries with schema-qualified table names.
-- The '%dbo%' filter ensures we catch EF Core queries. Validate against actual
-- EF Core output on staging before relying on these filters.
```

**Comparison query:**
```sql
-- Compare two snapshots
SELECT
    b.query,
    b.mean_exec_time AS before_ms,
    a.mean_exec_time AS after_ms,
    ROUND(((b.mean_exec_time - a.mean_exec_time) / NULLIF(b.mean_exec_time, 0) * 100)::numeric, 1) AS improvement_pct
FROM dbo."_perf_query_snapshot" b
JOIN dbo."_perf_query_snapshot" a
  ON b.queryid = a.queryid
  OR (b.queryid != a.queryid AND LEFT(b.query, 100) = LEFT(a.query, 100))  -- fallback: match by normalized query text
WHERE b.snapshot_id = 'baseline'
  AND a.snapshot_id = 'after_phase2a'
ORDER BY b.mean_exec_time DESC;
-- Note: queryid can change if pg_stat_statements is reset between snapshots.
-- The LEFT(query, 100) fallback handles this case.
```

### 1.3 Baseline Collection Process

1. Reset `pg_stat_statements` on staging: `SELECT pg_stat_statements_reset();`
2. Deploy application with `DetailPerformanceTracker` to staging
3. Call `POST /api/v1/SearchingRentalService/Detail` 35 times with varied vehicle IDs
4. Discard first 5 requests as warmup (populates EF Core query plan cache + PostgreSQL buffer cache)
5. Use remaining 30 requests for measurement
6. Take DB snapshot with `snapshot_id = 'baseline'`
7. Export application logs
8. Calculate: avg, p50, p95, max for total and each step

### 1.4 Deliverable

Baseline report with:
- Per-step timing breakdown (avg/p50/p95/max)
- Total request time distribution
- Top slowest DB queries from pg_stat_statements
- Identification of which steps benefit most from optimization

---

## Phase 2: Database Index Optimization

### Goal
Reduce individual query execution time by adding targeted indexes.

### 2a: Indexes Directly Impacting Detail Flow

These indexes target tables queried during `GetDetailAsync`:

```sql
-- 1. ServiceItem_Image (BuildDictServiceItem_ImageAsync)
CREATE INDEX CONCURRENTLY idx_serviceitem_image_rentalserviceitemid
ON dbo."ServiceItem_Image" ("RentalServiceItemId")
WHERE "RentalServiceItemId" IS NOT NULL;

-- 2. ServiceItem_Feature_Mapping (GetFeatures)
CREATE INDEX CONCURRENTLY idx_feature_mapping_lookup
ON dbo."ServiceItem_Feature_Mapping" ("RentalServiceItemId", "IsDisable", "IsFeatured");

-- 3. ServiceItem_BookedRentalSchedule (GetBusySchedules)
CREATE INDEX CONCURRENTLY idx_booked_schedule_lookup
ON dbo."ServiceItem_BookedRentalSchedule" ("RentalServiceItemId", "IsBooked", "ToDate");

-- 4. ServiceItem_DateBusyRentalSchedule (GetBusySchedules)
CREATE INDEX CONCURRENTLY idx_busy_schedule_lookup
ON dbo."ServiceItem_DateBusyRentalSchedule" ("RentalServiceItemId", "IsBusy", "ToDate");

-- 5. ServiceItem_WeekdaysBusyRentalSchedule (GetBusySchedules)
CREATE INDEX CONCURRENTLY idx_weekday_busy_schedule_lookup
ON dbo."ServiceItem_WeekdaysBusyRentalSchedule" ("RentalServiceItemId", "IsBusy");

-- 6. Order - for GetReviewAsync
CREATE INDEX CONCURRENTLY idx_order_rentalserviceitemid
ON dbo."Order" ("RentalServiceItemId");

-- 7. Order - for GetDefaultNote
CREATE INDEX CONCURRENTLY idx_order_renter_status
ON dbo."Order" ("RenterId", "StatusCode");

-- 8. Order_DiscountCode_Applied_OneTimeUse - for GetVouchers
CREATE INDEX CONCURRENTLY idx_onetimeuse_discount_lookup
ON dbo."Order_DiscountCode_Applied_OneTimeUse" ("DiscountCodeId", "Order_UserId", "IsCanceled");
```

**Measurement process:**
1. Take DB snapshot: `snapshot_id = 'before_phase2a'`
2. Run Detail API 30 times, collect app logs
3. Apply indexes
4. Run Detail API 30 times, collect app logs
5. Take DB snapshot: `snapshot_id = 'after_phase2a'`
6. Compare snapshots + app log timings

### 2b: Indexes from DB_OPTIMIZATION Report (Indirect Impact)

```sql
-- 8. RentalServiceItemViewHistory (CreateLogView in Detail flow)
CREATE INDEX CONCURRENTLY idx_rental_view_history_main
ON dbo."RentalServiceItemViewHistory" (
    "RentalServiceItemId", "IsDeleted", "ViewIP", "ViewFrom", "ViewAt"
);

CREATE INDEX CONCURRENTLY idx_rental_view_history_viewat
ON dbo."RentalServiceItemViewHistory" ("ViewAt", "IsDeleted", "RentalServiceItemId")
WHERE "IsDeleted" = false;

-- 9. Order_ChangeStatus
CREATE INDEX CONCURRENTLY idx_order_changeStatus_orderId
ON dbo."Order_ChangeStatus" ("OrderId", "CancelReasonId", "ChangedDate");

-- 10. NotificationMessage
CREATE INDEX CONCURRENTLY idx_notification_entity_type
ON dbo."NotificationMessage" ("EntityId", "Type");
```

**Same measurement process as 2a.**

### 2 Deliverable

Per-batch report:
- Before/after query execution times (from pg_stat_statements)
- Before/after per-step timings (from app logs)
- Summary: "Database indexes reduced total response time by X%"

---

## Phase 3: Application Code Optimization

### Goal
Reduce total response time by parallelizing independent operations and removing blocking calls.

### 3a: Parallelize Top-Level Operations in GetDetailAsync

**Current (sequential):**
```
SetRentalServiceItem  [120ms]
BuildImages           [95ms]   ← independent
GetTotalPrice         [45ms]   ← needs RentalServiceItem
GetDeliveryInfo       [30ms]   ← needs totalPriceModel
BuildPublicData       [---]    ← needs above
GetCharacteristics    [5ms]    ← cached, independent
GetOwnerInfo          [8ms]    ← cached, independent
GetFeatures           [65ms]   ← independent
GetDocumentSecurity   [70ms]   ← independent
GetReview             [180ms]  ← independent
GetVouchers           [85ms]   ← independent
GetBusySchedules      [110ms]  ← independent
GetDefaultNote        [37ms]   ← independent
CreateLogView         [---]    ← fire-and-forget
```

**After (parallel where possible):**
```
SetRentalServiceItem  [120ms]
    │
    ├─► BuildImages           ─┐
    ├─► GetFeatures           ─┤
    ├─► GetDocumentSecurity   ─┤  await Task.WhenAll()  [~180ms = slowest]
    ├─► GetReview             ─┤
    ├─► GetBusySchedules      ─┤
    ├─► GetDefaultNote        ─┘
    │
    ├─► GetTotalPrice         [45ms]  (needs RentalServiceItem only)
    │   └─► GetDeliveryInfo   [30ms]  (needs totalPriceModel)
    │
    └─► BuildPublicData + GetVouchers + remaining assembly
```

**Dependency analysis:**
- `BuildImages`, `GetDocumentSecurity`, `GetReview`, `GetBusySchedules`, `GetDefaultNote` are all independent - only need `RentalServiceItem`
- `GetFeatures` is independent but synchronous (DB query) - must be converted to async (`GetFeaturesAsync`) instead of using `Task.Run` (which is an anti-pattern for IO-bound work in ASP.NET Core)
- `GetTotalPriceModel` needs `RentalServiceItem` only - can run in parallel with above group
- `GetDeliveryInfo` needs `totalPriceModel` - must wait for GetTotalPrice
- `BuildPublicData` needs `totalPriceModel` + `avaImage` - must wait for both
- `GetVouchers` runs AFTER parallel group - it mutates `param.Id` and uses `out sMessage`, making it unsafe for parallelization without refactoring
- `BookingFeatureInfos` needs `Securities` from `GetDocumentSecurity` - must wait
- `CancelBookingPolicyInfo` needs `SettingJson.CancelOrderSetting` + `fromDate` - must wait for price computation

**Critical safety constraints:**
1. **`param` is shared mutable state** - `GetVouchers` does `param.Id = RentalServiceItem.Id.ToString()` before calling. If parallelized, this creates a race condition. Solution: `GetVouchers` stays sequential OR receives a local copy of the ID.
2. **`GetVouchers` uses `out string sMessage`** - cannot be included in `Task.WhenAll` without refactoring the method signature. Keep sequential for now.
3. **`DC_CreateRepository<T>()`** creates a new DbContext per call - parallel DB queries are safe.

**Implementation approach:**
```csharp
// Capture values before parallel group to avoid shared-state issues
var rentalServiceItemId = RentalServiceItem.Id;
var userLoginId = ID_ConvertStringToPKId(UserLoginId);

// Group 1: Independent async operations (parallel)
var imageTask = SearchingVehicleHelper.BuildDictServiceItem_ImageAsync(new long[] { rentalServiceItemId });
var docSecurityTask = GetDocumentAndSecurityAsync(result, RentalServiceItem);
var reviewTask = GetReviewAsync(result);
var busyScheduleTask = GetBusySchedulesAsync(param);  // converted to async
var featuresTask = GetFeaturesAsync(RentalServiceItem);  // converted to async (NOT Task.Run)
var defaultNoteTask = GetDefaultNoteAsync(userLoginId);

// Group 2: Can run in parallel with Group 1 (only reads RentalServiceItem)
var totalPriceModel = GetTotalPriceModel(param);

// Wait for all Group 1
await Task.WhenAll(imageTask, docSecurityTask, reviewTask, busyScheduleTask, featuresTask, defaultNoteTask);

// Assign results from parallel group
var (dictAvaImage, dictImages) = imageTask.Result;
result.Features = featuresTask.Result;
result.BusySchedules = busyScheduleTask.Result;
// ... assign other results

// Sequential: operations with dependencies or shared-state mutation
param.Id = rentalServiceItemId.ToString();
result.Vouchers = GetVouchers(param, out sMessage);  // stays sequential (mutates param, uses out)
// BookingFeatureInfos (needs result.Securities from docSecurityTask)
// CancelBookingPolicyInfo (needs SettingJson + fromDate)
```

**Estimated improvement:** Placeholder - actual values from Phase 1 baseline. Expected ~50-70% reduction from parallelization.

### 3b: Parallelize Within Methods

**GetBusySchedules - 3 queries to parallel:**
```csharp
// Current: sequential
var booked = repo1.GetQueryable(...).ToArray();
var weekdays = repo2.GetQueryable(...).ToArray();
var dateBusy = repo3.GetQueryable(...).ToArray();

// After: parallel
var bookedTask = repo1.GetQueryable(...).ToArrayAsync();
var weekdaysTask = repo2.GetQueryable(...).ToArrayAsync();
var dateBusyTask = repo3.GetQueryable(...).ToArrayAsync();
await Task.WhenAll(bookedTask, weekdaysTask, dateBusyTask);
```

**GetVouchers - NOT parallelizable internally:**
The two DB queries in GetVouchers have a data dependency: the OneTimeUse query's input (`ids`) depends on the filtered output of the Summary query. These must remain sequential. The optimization for GetVouchers is limited to:
- Adding a missing index on `Order_DiscountCode_Applied_OneTimeUse` (see Phase 2a addendum)
- The main discount codes come from in-memory cache, which is already fast

**GetFeatures - convert to async:**
```csharp
// Current: synchronous DB call
private BaseIconCategoryItem[] GetFeatures(RentalService_SelfdriveCarRental rentalService)
{
    using (var repo = DC_CreateRepository<ServiceItem_Feature_Mapping>())
    {
        var mappings = repo.GetQueryable().Where(...).ToArray();  // blocking
    }
}

// After: async
private async Task<BaseIconCategoryItem[]> GetFeaturesAsync(RentalService_SelfdriveCarRental rentalService)
{
    using (var repo = DC_CreateRepository<ServiceItem_Feature_Mapping>())
    {
        var mappings = await repo.GetQueryable().Where(...).ToArrayAsync();  // non-blocking
    }
}
```

### 3c: Remove Blocking Calls (Lower Priority)

The sync `GetDetail` method in `RentalService_SelfdriveCarRental_DetailService.cs` contains `.GetAwaiter().GetResult()` blocking patterns. However, the main public endpoint (`/api/v1/SearchingRentalService/Detail`) uses the **async** path (`GetDetailAsync`), so this is a lower priority cleanup.

**Scope:** Find all `.GetAwaiter().GetResult()` calls in the sync detail path. If the sync path is still used by other endpoints (e.g., management APIs), convert those endpoints to use the async path instead.

### 3 Deliverable

Per-sub-phase report:
- Waterfall diagram: before (sequential) vs after (parallel)
- Per-step timing comparison
- Total response time comparison
- Summary: "Code optimization reduced total response time by Y%"

---

## Final Report Structure

```
# Vehicle Detail Performance Optimization Report

## Executive Summary
- Baseline: Xms average response time
- After optimization: Yms average response time
- Total improvement: Z%

## Phase 1: Baseline Measurement
- [table: per-step avg/p50/p95/max]
- [chart: response time distribution]

## Phase 2: Database Index Optimization
### 2a: Direct indexes
- [table: before/after per-step]
- Improvement: X1%
### 2b: Indirect indexes
- [table: before/after per-step]
- Improvement: X2%

## Phase 3: Code Optimization
### 3a: Top-level parallelization
- [waterfall diagram: before vs after]
- Improvement: Y1%
### 3b: Internal method parallelization
- [table: before/after per-method]
- Improvement: Y2%
### 3c: Blocking call removal
- [table: before/after]
- Improvement: Y3%

## Cumulative Results
- [chart: response time across all phases]
- [table: phase-by-phase improvement stacking]

## Database Query Statistics
- [table: pg_stat_statements comparison across snapshots]
```

---

## Files Modified

| File | Phase | Change |
|------|-------|--------|
| `RentalService_SelfdriveCarRental_DetailServiceAsync.cs` | 1, 3a | Add perf tracking, parallelize operations |
| `RentalServiceItemService_Detail.cs` | 3b | Parallelize GetBusySchedules queries |
| `RentalService_SelfdriveCarRental_DetailService.cs` (GetFeatures) | 3b | Convert GetFeatures to async |
| `RentalService_SelfdriveCarRental_DetailService.cs` (sync path) | 3c | Remove blocking async calls (lower priority) |
| New: `DetailPerformanceTracker.cs` | 1 | Performance tracking utility |
| New: `perf_snapshots.sql` | 1, 2 | DB snapshot scripts |
| New: `create_indexes.sql` | 2 | Index creation scripts |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Parallel DB queries increase connection pool pressure | Monitor connection pool on staging. With 6 parallel queries × N concurrent requests, need N×6 connections. Default Npgsql pool = 100, PostgreSQL max_connections = 100. Calculate and adjust if needed. |
| EF Core DbContext not thread-safe for parallel queries | Verified: `DC_CreateRepository<T>()` creates a new DbContext per call. Each parallel task gets its own DbContext instance. Safe for parallelization. |
| Index creation locks tables | Use `CREATE INDEX CONCURRENTLY` to avoid blocking writes |
| Parallel operations change error behavior | Wrap `Task.WhenAll` in try/catch, handle `AggregateException`. If any task fails, return error gracefully. |
| Measurement noise on staging | Run 35 iterations per phase, discard first 5 as warmup, use percentiles on remaining 30 |
| Shared mutable `param` object in parallel context | Capture needed values (IDs) into local variables before launching parallel tasks. Never mutate `param` inside parallel tasks. |

## Rollback Plan

| Phase | Rollback Procedure |
|-------|-------------------|
| Phase 1 (Logging) | Remove `DetailPerformanceTracker` calls. No risk - logging only. |
| Phase 2 (Indexes) | `DROP INDEX CONCURRENTLY idx_name;` for each index. Script provided alongside create script. |
| Phase 3 (Code) | Git revert to pre-Phase-3 commit. Each sub-phase (3a, 3b, 3c) is a separate commit for granular rollback. |
