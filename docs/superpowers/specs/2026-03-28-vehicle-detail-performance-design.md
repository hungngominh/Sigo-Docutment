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
| 3 | 2 sequential queries in GetVouchers | DiscountCodeService.cs:254 | MEDIUM |
| 4 | `.GetAwaiter().GetResult()` blocking async | DetailService.cs:537 | MEDIUM |
| 5 | Missing database indexes on frequently queried tables | DB level | CRITICAL |
| 6 | Independent async operations run sequentially | GetDetailAsync | HIGH |

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
WHERE query ILIKE '%RentalService%'
   OR query ILIKE '%ServiceItem_Image%'
   OR query ILIKE '%Order_Rating%'
   OR query ILIKE '%BusyRentalSchedule%'
   OR query ILIKE '%BookedRentalSchedule%'
   OR query ILIKE '%DiscountCode%'
   OR query ILIKE '%Feature_Mapping%'
   OR query ILIKE '%ViewHistory%';
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
JOIN dbo."_perf_query_snapshot" a ON b.queryid = a.queryid
WHERE b.snapshot_id = 'baseline'
  AND a.snapshot_id = 'after_phase2a'
ORDER BY b.mean_exec_time DESC;
```

### 1.3 Baseline Collection Process

1. Reset `pg_stat_statements` on staging: `SELECT pg_stat_statements_reset();`
2. Deploy application with `DetailPerformanceTracker` to staging
3. Call `POST /api/v1/SearchingRentalService/Detail` 30 times with varied vehicle IDs
4. Take DB snapshot with `snapshot_id = 'baseline'`
5. Export application logs
6. Calculate: avg, p50, p95, max for total and each step

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
- `BuildImages`, `GetFeatures`, `GetDocumentSecurity`, `GetReview`, `GetBusySchedules`, `GetDefaultNote` are all independent - only need `RentalServiceItem`
- `GetTotalPriceModel` needs `RentalServiceItem` only - can run in parallel with above group
- `GetDeliveryInfo` needs `totalPriceModel` - must wait for GetTotalPrice
- `BuildPublicData` needs `totalPriceModel` + `avaImage` - must wait for both
- `GetVouchers` needs `RentalServiceItem.Id` only - can join the parallel group
- `BookingFeatureInfos` needs `Securities` from `GetDocumentSecurity` - must wait
- `CancelBookingPolicyInfo` needs `totalPriceModel` + `fromDate` - must wait

**Implementation approach:**
```csharp
// Group 1: Independent operations (parallel)
var imageTask = SearchingVehicleHelper.BuildDictServiceItem_ImageAsync(...);
var docSecurityTask = GetDocumentAndSecurityAsync(result, RentalServiceItem);
var reviewTask = GetReviewAsync(result);
var busyScheduleTask = GetBusySchedules(param);
var featuresTask = Task.Run(() => GetFeatures(RentalServiceItem));
var defaultNoteTask = GetDefaultNoteAsync(userLoginId);

// Group 2: Can also run in parallel with Group 1
var totalPriceModel = GetTotalPriceModel(param);

// Wait for all Group 1
await Task.WhenAll(imageTask, docSecurityTask, reviewTask, busyScheduleTask, featuresTask, defaultNoteTask);

// Assign results and continue with dependent operations
var (dictAvaImage, dictImages) = imageTask.Result;
result.Features = featuresTask.Result;
result.BusySchedules = busyScheduleTask.Result;
// ... etc
```

**Estimated improvement:** Total drops from ~850ms to ~350ms (120ms SetItem + 180ms parallel group + 50ms assembly)

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

**GetVouchers - 2 queries to parallel:**
```csharp
// Current: sequential
var summaries = repo1.GetQueryable(...).ToArray();
var oneTimeUse = repo2.GetQueryable(...).ToArray();

// After: parallel
var summariesTask = repo1.GetQueryable(...).ToArrayAsync();
var oneTimeUseTask = repo2.GetQueryable(...).ToArrayAsync();
await Task.WhenAll(summariesTask, oneTimeUseTask);
```

### 3c: Remove Blocking Calls

Find and replace `.GetAwaiter().GetResult()` patterns in the sync `DetailService` with proper async/await. Ensure the entire call chain from controller to service is async.

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
| `DiscountCodeService.cs` | 3b | Parallelize GetVouchers queries |
| `RentalService_SelfdriveCarRental_DetailService.cs` | 3c | Remove blocking async calls |
| New: `DetailPerformanceTracker.cs` | 1 | Performance tracking utility |
| New: `perf_snapshots.sql` | 1, 2 | DB snapshot scripts |
| New: `create_indexes.sql` | 2 | Index creation scripts |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Parallel DB queries increase connection pool pressure | Monitor connection pool on staging; set reasonable pool size |
| EF Core DbContext not thread-safe for parallel queries | Create separate repository instances per parallel task |
| Index creation locks tables | Use `CREATE INDEX CONCURRENTLY` |
| Parallel operations change error behavior | Ensure proper exception handling in `Task.WhenAll` |
| Measurement noise on staging | Run 30+ iterations per phase, use percentiles not averages |
