# Vehicle Detail Performance Optimization - Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Incrementally optimize the vehicle detail API (`POST /api/v1/SearchingRentalService/Detail`) with before/after measurement at each phase.

**Architecture:** Add performance logging first (Phase 1), then database indexes (Phase 2), then parallelize independent async operations in `GetDetailAsync` (Phase 3). Each phase is measured independently for a cumulative performance report.

**Tech Stack:** ASP.NET Core, Entity Framework Core, PostgreSQL 16.2, C#, `System.Diagnostics.Stopwatch`

**Spec:** `docs/superpowers/specs/2026-03-28-vehicle-detail-performance-design.md`

---

## File Structure

| File | Purpose |
|------|---------|
| **New:** `SOURCE/AllianceMiddlemanWebAPI.Service/Helper/DetailPerformanceTracker.cs` | Lightweight stopwatch-based tracker for per-step timing in Detail flow |
| **New:** `SOURCE/DB_OPTIMIZATION/scripts/perf_snapshots.sql` | SQL scripts for pg_stat_statements snapshot/comparison |
| **New:** `SOURCE/DB_OPTIMIZATION/scripts/phase2a_create_indexes.sql` | Phase 2a index creation |
| **New:** `SOURCE/DB_OPTIMIZATION/scripts/phase2a_drop_indexes.sql` | Phase 2a rollback |
| **New:** `SOURCE/DB_OPTIMIZATION/scripts/phase2b_create_indexes.sql` | Phase 2b index creation |
| **New:** `SOURCE/DB_OPTIMIZATION/scripts/phase2b_drop_indexes.sql` | Phase 2b rollback |
| **Modify:** `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailServiceAsync.cs` | Phase 1: add tracking. Phase 3a: parallelize top-level operations |
| **Modify:** `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalServiceItem/RentalServiceItemService_Detail.cs` | Phase 3b: parallelize 3 queries inside GetBusySchedules |
| **Modify:** `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailService.cs` | Phase 3b: add `GetFeaturesAsync` method |

---

## Task 1: Create DetailPerformanceTracker utility

**Files:**
- Create: `SOURCE/AllianceMiddlemanWebAPI.Service/Helper/DetailPerformanceTracker.cs`

- [ ] **Step 1: Create the tracker class**

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AllianceMiddlemanWebAPI.Shared.Helper
{
    /// <summary>
    /// Lightweight per-step performance tracker for the Vehicle Detail flow.
    /// Usage:
    ///   var tracker = new DetailPerformanceTracker(vehicleId);
    ///   tracker.Start("StepName");
    ///   // ... do work ...
    ///   tracker.Stop("StepName");
    ///   tracker.LogResult();
    /// </summary>
    public class DetailPerformanceTracker
    {
        private readonly string _vehicleId;
        private readonly Stopwatch _totalStopwatch;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Stopwatch> _stepTimers = new();
        private readonly List<string> _stepOrder = new();

        public DetailPerformanceTracker(string vehicleId)
        {
            _vehicleId = vehicleId;
            _totalStopwatch = Stopwatch.StartNew();
        }

        public void Start(string stepName)
        {
            var sw = _stepTimers.GetOrAdd(stepName, _ =>
            {
                lock (_stepOrder) { _stepOrder.Add(stepName); }
                return new Stopwatch();
            });
            sw.Start();
        }

        public void Stop(string stepName)
        {
            if (_stepTimers.TryGetValue(stepName, out var sw))
                sw.Stop();
        }

        public long GetStepMs(string stepName)
        {
            return _stepTimers.TryGetValue(stepName, out var sw) ? sw.ElapsedMilliseconds : -1;
        }

        public string GetLogOutput()
        {
            _totalStopwatch.Stop();
            var sb = new StringBuilder();
            sb.AppendLine($"[DetailPerf] VehicleId={_vehicleId} | Total={_totalStopwatch.ElapsedMilliseconds}ms");
            foreach (var step in _stepOrder)
            {
                var ms = _stepTimers[step].ElapsedMilliseconds;
                sb.AppendLine($"  {step}: {ms}ms");
            }
            return sb.ToString();
        }

        public Dictionary<string, long> GetStepResults()
        {
            return _stepOrder.ToDictionary(s => s, s => _stepTimers[s].ElapsedMilliseconds);
        }

        public long TotalMs => _totalStopwatch.ElapsedMilliseconds;
    }
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build SOURCE/AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Service.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add SOURCE/AllianceMiddlemanWebAPI.Service/Helper/DetailPerformanceTracker.cs
git commit -m "feat: add DetailPerformanceTracker utility for vehicle detail flow measurement"
```

---

## Task 2: Integrate performance tracking into GetDetailAsync

**Files:**
- Modify: `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailServiceAsync.cs`

**Reference:** The current `GetDetailAsync` method is at lines 18-478. Each `#region` block represents a step to wrap with tracking.

- [ ] **Step 1: Verify using statements**

The file already has `using AllianceMiddlemanWebAPI.Shared.Helper;` at line 4 — no change needed. Verify it's present.

- [ ] **Step 2: Add tracker initialization after parameter validation**

After line 36 (`param.IsGetVehicle_InclueInsurance = true;`), before `await SetRentalServiceItemAsync(param);`, add:
```csharp
var perfTracker = new DetailPerformanceTracker(param.Id ?? "unknown");
```

- [ ] **Step 3: Wrap each major step with Start/Stop calls**

Wrap `SetRentalServiceItemAsync` (line 37):
```csharp
perfTracker.Start("SetRentalServiceItem");
await SetRentalServiceItemAsync(param);
perfTracker.Stop("SetRentalServiceItem");
```

Wrap `BuildDictServiceItem_ImageAsync` (lines 44-48):
```csharp
perfTracker.Start("BuildImages");
var (dictAvaImage, dictImages) = ...  // existing code
perfTracker.Stop("BuildImages");
```

Wrap `GetTotalPriceModel` (line 53):
```csharp
perfTracker.Start("GetTotalPrice");
var totalPriceModel = GetTotalPriceModel(param);
perfTracker.Stop("GetTotalPrice");
```

Wrap `GetRentalPriceDetails + GetDeliveryInfo` (lines 61-63):
```csharp
perfTracker.Start("GetDeliveryInfo");
var rentalPriceDetails = GetRentalPriceDetails(totalPriceModel, rentalDate);
rentalPriceDetails = GetDeliveryInfo(param, out sMessage, rentalPriceDetails, out RentalServiceDeliveryAddress deliveryInfo);
perfTracker.Stop("GetDeliveryInfo");
```

Wrap `GetCharacteristics` (line 110):
```csharp
perfTracker.Start("GetCharacteristics");
result.Characteristics = GetCharacteristics(RentalServiceItem);
perfTracker.Stop("GetCharacteristics");
```

Wrap Owner info block (lines 116-150):
```csharp
perfTracker.Start("GetOwnerInfo");
// ... existing owner code ...
perfTracker.Stop("GetOwnerInfo");
```

Wrap `GetFeatures` (line 156):
```csharp
perfTracker.Start("GetFeatures");
result.Features = GetFeatures(RentalServiceItem);
perfTracker.Stop("GetFeatures");
```

Wrap `GetDocumentAndSecurityAsync` (line 162):
```csharp
perfTracker.Start("GetDocumentSecurity");
await GetDocumentAndSecurityAsync(result, RentalServiceItem);
perfTracker.Stop("GetDocumentSecurity");
```

Wrap `GetReviewAsync` (line 271):
```csharp
perfTracker.Start("GetReview");
await GetReviewAsync(result);
perfTracker.Stop("GetReview");
```

Wrap `GetVouchers` (lines 277-278):
```csharp
perfTracker.Start("GetVouchers");
param.Id = RentalServiceItem.Id.ToString();
result.Vouchers = GetVouchers(param, out sMessage);
perfTracker.Stop("GetVouchers");
```

Wrap `GetBusySchedules` (line 431):
```csharp
perfTracker.Start("GetBusySchedules");
result.BusySchedules = await GetBusySchedules(param);
perfTracker.Stop("GetBusySchedules");
```

Wrap `GetDefaultNote` (lines 435-452):
```csharp
perfTracker.Start("GetDefaultNote");
using (var repoO = DC_CreateRepository<Order>())
{
    // ... existing code ...
}
perfTracker.Stop("GetDefaultNote");
```

- [ ] **Step 4: Add log output before return**

Before `return (result, sMessage);` (line 477), add:
```csharp
try
{
    var perfLog = perfTracker.GetLogOutput();
    System.Diagnostics.Debug.WriteLine(perfLog);
}
catch { /* don't let perf logging break the flow */ }
```

**Note:** `Debug.WriteLine` outputs to the debug console/attached debugger on staging. For production-grade logging, replace with `ILogger` injection if available in the base service.

- [ ] **Step 5: Verify it compiles**

Run: `dotnet build SOURCE/AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Service.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailServiceAsync.cs
git commit -m "feat: integrate performance tracking into GetDetailAsync for baseline measurement"
```

---

## Task 3: Create database snapshot and index SQL scripts

**Files:**
- Create: `SOURCE/DB_OPTIMIZATION/scripts/perf_snapshots.sql`
- Create: `SOURCE/DB_OPTIMIZATION/scripts/phase2a_create_indexes.sql`
- Create: `SOURCE/DB_OPTIMIZATION/scripts/phase2a_drop_indexes.sql`
- Create: `SOURCE/DB_OPTIMIZATION/scripts/phase2b_create_indexes.sql`
- Create: `SOURCE/DB_OPTIMIZATION/scripts/phase2b_drop_indexes.sql`

- [ ] **Step 1: Create perf_snapshots.sql**

```sql
-- ============================================
-- Performance Snapshot Scripts
-- Run on staging PostgreSQL to capture before/after metrics
-- ============================================

-- 1. Enable pg_stat_statements
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- 2. Create snapshot table
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

-- 3. Reset stats before baseline collection
-- SELECT pg_stat_statements_reset();

-- 4. Take snapshot (change snapshot_id per phase)
-- Valid IDs: 'baseline', 'after_phase2a', 'after_phase2b', 'after_phase3a', 'after_phase3b'
INSERT INTO dbo."_perf_query_snapshot"
SELECT
    'baseline' AS snapshot_id,
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
      OR query ILIKE '%"Order"%'
      OR query ILIKE '%RentalRequiredItem%'
  );

-- 5. Compare two snapshots
SELECT
    LEFT(b.query, 120) AS query_preview,
    b.calls AS before_calls,
    a.calls AS after_calls,
    ROUND(b.mean_exec_time::numeric, 2) AS before_mean_ms,
    ROUND(a.mean_exec_time::numeric, 2) AS after_mean_ms,
    ROUND(((b.mean_exec_time - a.mean_exec_time) / NULLIF(b.mean_exec_time, 0) * 100)::numeric, 1) AS improvement_pct
FROM dbo."_perf_query_snapshot" b
JOIN dbo."_perf_query_snapshot" a
  ON b.queryid = a.queryid
  OR (b.queryid != a.queryid AND LEFT(b.query, 100) = LEFT(a.query, 100))
WHERE b.snapshot_id = 'baseline'
  AND a.snapshot_id = 'after_phase2a'
ORDER BY b.mean_exec_time DESC;
```

- [ ] **Step 2: Create phase2a_create_indexes.sql**

```sql
-- ============================================
-- Phase 2a: Indexes directly impacting Detail flow
-- Run on staging PostgreSQL
-- ============================================

-- 1. ServiceItem_Image (BuildDictServiceItem_ImageAsync)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_serviceitem_image_rentalserviceitemid
ON dbo."ServiceItem_Image" ("RentalServiceItemId")
WHERE "RentalServiceItemId" IS NOT NULL;

-- 2. ServiceItem_Feature_Mapping (GetFeatures)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_feature_mapping_lookup
ON dbo."ServiceItem_Feature_Mapping" ("RentalServiceItemId", "IsDisable", "IsFeatured");

-- 3. ServiceItem_BookedRentalSchedule (GetBusySchedules - query 1)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_booked_schedule_lookup
ON dbo."ServiceItem_BookedRentalSchedule" ("RentalServiceItemId", "IsBooked", "ToDate");

-- 4. ServiceItem_DateBusyRentalSchedule (GetBusySchedules - query 3)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_busy_schedule_lookup
ON dbo."ServiceItem_DateBusyRentalSchedule" ("RentalServiceItemId", "IsBusy", "ToDate");

-- 5. ServiceItem_WeekdaysBusyRentalSchedule (GetBusySchedules - query 2)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_weekday_busy_schedule_lookup
ON dbo."ServiceItem_WeekdaysBusyRentalSchedule" ("RentalServiceItemId", "IsBusy");

-- 6. Order (GetReviewAsync)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_order_rentalserviceitemid
ON dbo."Order" ("RentalServiceItemId");

-- 7. Order (GetDefaultNote)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_order_renter_status
ON dbo."Order" ("RenterId", "StatusCode");

-- 8. Order_DiscountCode_Applied_OneTimeUse (GetVouchers)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_onetimeuse_discount_lookup
ON dbo."Order_DiscountCode_Applied_OneTimeUse" ("DiscountCodeId", "Order_UserId", "IsCanceled");
```

- [ ] **Step 3: Create phase2a_drop_indexes.sql (rollback)**

```sql
-- ============================================
-- Phase 2a ROLLBACK: Drop indexes
-- ============================================

DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_serviceitem_image_rentalserviceitemid;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_feature_mapping_lookup;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_booked_schedule_lookup;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_busy_schedule_lookup;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_weekday_busy_schedule_lookup;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_order_rentalserviceitemid;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_order_renter_status;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_onetimeuse_discount_lookup;
```

- [ ] **Step 4: Create phase2b_create_indexes.sql**

```sql
-- ============================================
-- Phase 2b: Indexes from DB_OPTIMIZATION report (indirect impact)
-- Run on staging PostgreSQL
-- ============================================

-- 1. RentalServiceItemViewHistory (CreateLogView in Detail flow)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_rental_view_history_main
ON dbo."RentalServiceItemViewHistory" (
    "RentalServiceItemId", "IsDeleted", "ViewIP", "ViewFrom", "ViewAt"
);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_rental_view_history_viewat
ON dbo."RentalServiceItemViewHistory" ("ViewAt", "IsDeleted", "RentalServiceItemId")
WHERE "IsDeleted" = false;

-- 2. Order_ChangeStatus
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_order_changeStatus_orderId
ON dbo."Order_ChangeStatus" ("OrderId", "CancelReasonId", "ChangedDate");

-- 3. NotificationMessage
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_notification_entity_type
ON dbo."NotificationMessage" ("EntityId", "Type");
```

- [ ] **Step 5: Create phase2b_drop_indexes.sql (rollback)**

```sql
-- ============================================
-- Phase 2b ROLLBACK: Drop indexes
-- ============================================

DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_rental_view_history_main;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_rental_view_history_viewat;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_order_changeStatus_orderId;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_notification_entity_type;
```

- [ ] **Step 6: Commit**

```bash
git add SOURCE/DB_OPTIMIZATION/scripts/
git commit -m "feat: add SQL scripts for performance snapshots and index creation (Phase 2)"
```

---

## Task 4: Collect baseline data (Phase 1 deliverable)

This is a manual task on staging environment.

- [ ] **Step 1: Deploy application with perf tracking to staging**

Deploy the build from Tasks 1-2 to the staging environment.

- [ ] **Step 2: Reset pg_stat_statements on staging**

Run on staging DB:
```sql
SELECT pg_stat_statements_reset();
```

- [ ] **Step 3: Run 35 API calls with varied vehicle IDs**

Use curl, Postman, or a script to call `POST /api/v1/SearchingRentalService/Detail` 35 times with different vehicle IDs. Discard first 5 as warmup.

- [ ] **Step 4: Take baseline DB snapshot**

Run the snapshot INSERT from `perf_snapshots.sql` with `snapshot_id = 'baseline'`.

- [ ] **Step 5: Export application logs**

Collect all `[DetailPerf]` log lines from the staging server. Calculate avg/p50/p95/max for total and each step.

- [ ] **Step 6: Document baseline results**

Create `SOURCE/DB_OPTIMIZATION/reports/phase1_baseline.md` with the collected metrics.

- [ ] **Step 7: Commit baseline report**

```bash
git add SOURCE/DB_OPTIMIZATION/reports/phase1_baseline.md
git commit -m "docs: Phase 1 baseline performance report"
```

---

## Task 5: Apply Phase 2a indexes and measure

- [ ] **Step 1: Take "before" snapshot on staging**

```sql
-- Change snapshot_id
INSERT INTO dbo."_perf_query_snapshot"
SELECT 'before_phase2a' AS snapshot_id, NOW(), ...
```

- [ ] **Step 2: Run Detail API 30 times and collect app logs (pre-index)**

- [ ] **Step 3: Apply Phase 2a indexes**

Run `phase2a_create_indexes.sql` on staging PostgreSQL.

- [ ] **Step 4: Run Detail API 30 times and collect app logs (post-index)**

Remember to discard first 5 as warmup.

- [ ] **Step 5: Take "after" snapshot**

Run snapshot with `snapshot_id = 'after_phase2a'`.

- [ ] **Step 6: Compare and document results**

Run the comparison query from `perf_snapshots.sql`. Create `SOURCE/DB_OPTIMIZATION/reports/phase2a_results.md`.

- [ ] **Step 7: Commit**

```bash
git add SOURCE/DB_OPTIMIZATION/reports/phase2a_results.md
git commit -m "docs: Phase 2a index optimization results"
```

---

## Task 6: Apply Phase 2b indexes and measure

Same process as Task 5, but using `phase2b_create_indexes.sql` and `snapshot_id = 'after_phase2b'`.

- [ ] **Step 1: Run Detail API 30 times (pre-index)**
- [ ] **Step 2: Apply Phase 2b indexes**
- [ ] **Step 3: Run Detail API 30 times (post-index)**
- [ ] **Step 4: Take snapshot and compare**
- [ ] **Step 5: Document results in `SOURCE/DB_OPTIMIZATION/reports/phase2b_results.md`**
- [ ] **Step 6: Commit**

```bash
git add SOURCE/DB_OPTIMIZATION/reports/phase2b_results.md
git commit -m "docs: Phase 2b index optimization results"
```

---

## Task 7: Add GetFeaturesAsync method (Phase 3b prerequisite)

**Files:**
- Modify: `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailService.cs`

- [ ] **Step 1: Add async version of GetFeatures**

Add this method directly after the existing `GetFeatures` method (after line 136):

```csharp
private async Task<BaseIconCategoryItem[]> GetFeaturesAsync(RentalService_SelfdriveCarRental rentalService)
{
    BaseIconCategoryItem[] result = null;
    using (var repo = DC_CreateRepository<ServiceItem_Feature_Mapping>())
    {
        var mappings = await repo.GetQueryable()
            .Where(c => c.RentalServiceItemId == rentalService.Id && !c.IsDisable && c.IsFeatured)
            .ToArrayAsync();
        if (mappings != null && mappings.Any())
        {
            result = mappings.Select(c =>
            {
                BaseIconCategoryItem t = null;
                var cf = CachedDataManagement.ConfigServiceItemFeature_Get_Instance_Id(c.ServiceItemFeatureId);
                if (cf != null && !cf.IsDisable) t = new()
                {
                    Text = cf.Name,
                    IconUrl = GetIconUrl(cf),
                    Icon = cf.Icon,
                    IconMobile = cf.IconMobile
                };
                return t;
            }).Where(c => c != null).ToArray();
        }
    }
    return result;
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build SOURCE/AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Service.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailService.cs
git commit -m "feat: add GetFeaturesAsync method for parallel execution support"
```

---

## Task 8: Add GetDefaultNoteAsync method (Phase 3a prerequisite)

**Files:**
- Modify: `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailServiceAsync.cs`

The "Get default note" block (lines 435-452) is currently inline code. Extract it into an async method so it can participate in `Task.WhenAll`.

- [ ] **Step 1: Add the async method**

Add this method to the same partial class file (at the end, before the closing braces):

```csharp
private async Task<string> GetDefaultNoteAsync(long? userLoginId)
{
    string messageToOwner = null;
    using (var repoO = DC_CreateRepository<Order>())
    {
        var now = DateTime_Now().Date;
        var order = await repoO.GetQueryable(t => t.RenterId == userLoginId && t.StatusCode == OrderStatus.OWNER2CONFIRM)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync();
        if (order != null)
        {
            messageToOwner = order.MessageToOwner;
        }
        else
        {
            order = await repoO.GetQueryable(t => t.RenterId == userLoginId
                && (t.StatusCode == OrderStatus.CUSCANCEL || t.StatusCode == OrderStatus.OWNERCANCEL || t.StatusCode == OrderStatus.SYSTEMCANCEL)
                && t.Log_CreatedDate.HasValue && t.Log_CreatedDate.Value.Date == now)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();
            if (order != null)
                messageToOwner = order.MessageToOwner;
        }
    }
    return messageToOwner;
}
```

- [ ] **Step 2: Add `using Microsoft.EntityFrameworkCore;` if not present**

Check line 1-12 of the file. If `using Microsoft.EntityFrameworkCore;` is missing, add it. `FirstOrDefaultAsync` requires this namespace.

- [ ] **Step 3: Verify it compiles**

Run: `dotnet build SOURCE/AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Service.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailServiceAsync.cs
git commit -m "feat: extract GetDefaultNoteAsync method for parallel execution support"
```

---

## Task 9: Parallelize top-level operations in GetDetailAsync (Phase 3a)

**Files:**
- Modify: `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailServiceAsync.cs`

This is the core optimization. Replace the sequential execution of independent operations with `Task.WhenAll`.

- [ ] **Step 1: Restructure GetDetailAsync to parallelize independent operations**

After `SetRentalServiceItemAsync` completes (line 37) and the `RentalServiceItem != null` check (line 38), restructure the body as follows:

```csharp
if (RentalServiceItem != null)
{
    DetailScreen = true;

    // Capture values before parallel group to avoid shared-state issues
    var rentalServiceItemId = RentalServiceItem.Id;
    var userLoginId = ID_ConvertStringToPKId(UserLoginId);

    perfTracker.Start("ParallelGroup");

    // ===== GROUP 1: Independent async operations (parallel) =====
    // Use temp objects for methods that write to result properties,
    // because `result` doesn't exist yet (created by BuildPublicData later).
    var tempDocResult = new RentalServicePublicModel();
    var tempReviewResult = new RentalServicePublicModel();

    var imageTask = SearchingVehicleHelper.BuildDictServiceItem_ImageAsync(
        new long[] { rentalServiceItemId });
    var docSecurityTask = GetDocumentAndSecurityAsync(tempDocResult, RentalServiceItem);
    var reviewTask = GetReviewAsync(tempReviewResult);
    var busyScheduleTask = GetBusySchedules(param);
    var featuresTask = GetFeaturesAsync(RentalServiceItem);
    var defaultNoteTask = GetDefaultNoteAsync(userLoginId);

    // ===== GROUP 2: Runs on current thread while Group 1 is in flight =====
    // GetTotalPriceModel is synchronous and CPU-bound — execute it on the
    // current thread while the IO-bound tasks from Group 1 are awaiting DB.
    // SAFETY NOTE: GetTotalPriceModel reads param properties but GetBusySchedules
    // (in Group 1) also reads param. Verify GetTotalPriceModel does not mutate
    // any param property that GetBusySchedules reads. If it does, move
    // GetTotalPriceModel BEFORE launching the parallel group.
    perfTracker.Start("GetTotalPrice");
    param.NeedGetCriteriaPoint = true;
    var totalPriceModel = GetTotalPriceModel(param);
    sMessage = totalPriceModel.Error;
    perfTracker.Stop("GetTotalPrice");

    DateTime? fromDate = DateTime_To_ClientDateTime(param.FromDate),
        toDate = DateTime_To_ClientDateTime(param.ToDate);
    var rentalDate = RentalServiceHelper.GetRentalDate(fromDate.Value, toDate.Value);

    perfTracker.Start("GetDeliveryInfo");
    var rentalPriceDetails = GetRentalPriceDetails(totalPriceModel, rentalDate);
    rentalPriceDetails = GetDeliveryInfo(param, out sMessage, rentalPriceDetails,
        out RentalServiceDeliveryAddress deliveryInfo);
    perfTracker.Stop("GetDeliveryInfo");

    // ===== Wait for all Group 1 tasks =====
    try
    {
        await Task.WhenAll(imageTask, docSecurityTask, reviewTask,
            busyScheduleTask, featuresTask, defaultNoteTask);
    }
    catch (Exception ex)
    {
        // Log individual task failures for debugging
        var failedTasks = new[] { imageTask, docSecurityTask, reviewTask, busyScheduleTask, featuresTask, defaultNoteTask }
            .Where(t => t.IsFaulted);
        foreach (var ft in failedTasks)
            SaveLogException(ft.Exception?.InnerException ?? ft.Exception, "GetDetailAsync_ParallelGroup", param);
        throw; // Re-throw to let the outer catch handle it
    }
    perfTracker.Stop("ParallelGroup");

    // ===== Assign results from parallel group =====
    var (dictAvaImage, dictImages) = imageTask.Result;
    var avaImage = dictAvaImage.GetValue_Dic(rentalServiceItemId);
    var images = dictImages.GetValue_Dic(rentalServiceItemId);

    result = BuildPublicData<RentalServicePublicModel>(totalPriceModel, RentalServiceItem, avaImage);
    result.DeliveryInfo = deliveryInfo;
    result.RentalDate = rentalDate;
    result.CanChangeDeliveryAddress =
        RentalServiceItem.Vehicle_RentalSetting.HaveDeliverySurcharge ?? false;
    result.ImageUrls = SearchingVehicleHelper.GetServiceItem_ImageFileUrl(images);
    result.FullDescription = RentalServiceItem.FullDescription;
    result.Features = featuresTask.Result;
    result.BusySchedules = busyScheduleTask.Result;
    result.MessageToOwner = defaultNoteTask.Result;

    // Copy results from temp objects into the real result
    result.Documents = tempDocResult.Documents;
    result.Securities = tempDocResult.Securities;
    result.Review = tempReviewResult.Review;

    // ===== SEQUENTIAL: remaining dependent operations =====
    // (keep all existing code for PopularPlace, Characteristics, Owner,
    //  CMS, Vouchers, ExtraSurcharges, BookingFeatureInfos, ThingsToKnow,
    //  LogView, ReportUserReasonList — these either depend on result.Securities
    //  or are cheap cached lookups)

    // ... PopularPlace (lines 78-104) — unchanged ...
    // ... Characteristics (line 110) — unchanged ...
    // ... Owner (lines 116-150) — unchanged ...
    // ... CMS (lines 168-265) — unchanged ...

    // GetVouchers stays sequential (mutates param.Id, uses out sMessage)
    perfTracker.Start("GetVouchers");
    param.Id = rentalServiceItemId.ToString();
    result.Vouchers = GetVouchers(param, out sMessage);
    perfTracker.Stop("GetVouchers");

    // ... ExtraSurcharges, BookingFeatureInfos, ThingsToKnow, LogView,
    //     ReportUserReasonList — all unchanged ...

    // REMOVE the old inline GetDefaultNote block (lines 434-452) — now handled by defaultNoteTask
    // REMOVE the old GetFeatures call (line 156) — now handled by featuresTask
    // REMOVE the old GetDocumentAndSecurityAsync call (line 162) — now handled by docSecurityTask
    // REMOVE the old GetReviewAsync call (line 271) — now handled by reviewTask
    // REMOVE the old GetBusySchedules call (line 431) — now handled by busyScheduleTask
    // REMOVE the old BuildDictServiceItem_ImageAsync block (lines 44-48) — now handled by imageTask
}
```

**CRITICAL: `GetDocumentAndSecurityAsync` and `GetReviewAsync` write directly to the `result` object's properties.** Since `result` is created by `BuildPublicData` (which runs AFTER the parallel group), the code above uses temporary `RentalServicePublicModel` objects (`tempDocResult`, `tempReviewResult`) and copies the relevant properties after `BuildPublicData`.

Ensure `result.Documents`, `result.Securities`, and `result.Review` are assigned from temp objects BEFORE `BookingFeatureInfos` accesses `result.Securities`.

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build SOURCE/AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Service.csproj`
Expected: Build succeeded

- [ ] **Step 3: Test locally by calling the Detail endpoint**

Verify:
1. API returns the same response format as before
2. No exceptions in logs
3. `[DetailPerf]` log shows `ParallelGroup` timing is less than sum of individual steps

- [ ] **Step 4: Commit**

```bash
git add SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailServiceAsync.cs
git commit -m "perf: parallelize independent operations in GetDetailAsync (Phase 3a)"
```

---

## Task 10: Measure Phase 3a results on staging

- [ ] **Step 1: Deploy to staging**
- [ ] **Step 2: Run Detail API 35 times (discard first 5)**
- [ ] **Step 3: Take snapshot `after_phase3a`**
- [ ] **Step 4: Compare with previous phase and document in `SOURCE/DB_OPTIMIZATION/reports/phase3a_results.md`**
- [ ] **Step 5: Commit**

```bash
git add SOURCE/DB_OPTIMIZATION/reports/phase3a_results.md
git commit -m "docs: Phase 3a parallelization results"
```

---

## Task 11: Parallelize GetBusySchedules internal queries (Phase 3b)

**Files:**
- Modify: `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalServiceItem/RentalServiceItemService_Detail.cs`

The method `GetBusySchedules` (line 282) runs 3 DB queries sequentially. Convert to parallel.

- [ ] **Step 1: Replace the 3 sequential `using` blocks with parallel async queries**

The method is already `async Task<double[]>`. Replace the 3 sequential `using` blocks with:

```csharp
// Create repositories outside using blocks so we can parallelize
var repo1 = DC_CreateRepository<ServiceItem_BookedRentalSchedule>();
var repo2 = DC_CreateRepository<ServiceItem_WeekdaysBusyRentalSchedule>();
var repo3 = DC_CreateRepository<ServiceItem_DateBusyRentalSchedule>();

try
{
    // Launch all 3 queries in parallel
    var bookedTask = repo1.GetQueryableReadOnly()
        .Where(c => c.RentalServiceItemId == RentalServiceItem.Id &&
                    c.IsBooked == true &&
                    c.ToDate >= todayUTC)
        .ToArrayAsync();

    var weekdaysTask = repo2.GetQueryableReadOnly()
        .Where(c => c.RentalServiceItemId == RentalServiceItem.Id && c.IsBusy == true)
        .ToArrayAsync();

    var dateBusyTask = repo3.GetQueryableReadOnly()
        .Where(c => c.RentalServiceItemId == RentalServiceItem.Id &&
                    c.IsBusy == true &&
                    c.ToDate >= todayUTC)
        .ToArrayAsync();

    await Task.WhenAll(bookedTask, weekdaysTask, dateBusyTask);

    var bookedItems = bookedTask.Result;
    var weekItems = weekdaysTask.Result;
    var dateBusyItems = dateBusyTask.Result;

    // Process bookedItems (same logic as before)
    // ... existing processing code for booked schedules ...

    // Process weekItems (same logic as before)
    // ... existing processing code for weekday schedules ...

    // Process dateBusyItems (same logic as before)
    // ... existing processing code for date busy schedules ...
}
finally
{
    repo1.Dispose();
    repo2.Dispose();
    repo3.Dispose();
}
```

**Important:**
- Add `using Microsoft.EntityFrameworkCore;` at the top of the file if not already present (needed for `ToArrayAsync`).
- Keep all the processing logic (the loops that populate `busyDates`) exactly as-is. Only the data fetching changes from sequential to parallel. The processing runs sequentially after all 3 queries complete.
- The processing code for booked schedules, weekday schedules, and date busy schedules should operate on `bookedTask.Result`, `weekdaysTask.Result`, and `dateBusyTask.Result` respectively (instead of the old local variables).

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build SOURCE/AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Service.csproj`
Expected: Build succeeded

- [ ] **Step 3: Verify the API still returns correct busy schedules**

Compare response with a known vehicle that has busy dates. The `BusySchedules` array should be identical to before.

- [ ] **Step 4: Commit**

```bash
git add SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalServiceItem/RentalServiceItemService_Detail.cs
git commit -m "perf: parallelize 3 queries inside GetBusySchedules (Phase 3b)"
```

---

## Task 12: Measure Phase 3b results and create final report

- [ ] **Step 1: Deploy to staging**
- [ ] **Step 2: Run Detail API 35 times (discard first 5)**
- [ ] **Step 3: Take snapshot `after_phase3b`**
- [ ] **Step 4: Create cumulative report**

Create `SOURCE/DB_OPTIMIZATION/reports/final_performance_report.md` with:

```markdown
# Vehicle Detail Performance Optimization - Final Report

## Executive Summary
- Baseline: Xms average response time
- After optimization: Yms average response time
- Total improvement: Z%

## Phase-by-Phase Results

| Phase | Description | Avg Before | Avg After | Improvement |
|-------|-------------|------------|-----------|-------------|
| Baseline | No optimization | - | Xms | - |
| Phase 2a | Direct indexes | Xms | X1ms | A% |
| Phase 2b | Indirect indexes | X1ms | X2ms | B% |
| Phase 3a | Top-level parallelization | X2ms | X3ms | C% |
| Phase 3b | Internal parallelization | X3ms | X4ms | D% |
| **Total** | | **Xms** | **X4ms** | **Z%** |

## Per-Step Breakdown
[table with avg/p50/p95/max for each step, before vs after]

## Database Query Statistics
[pg_stat_statements comparison across all snapshots]
```

- [ ] **Step 5: Commit**

```bash
git add SOURCE/DB_OPTIMIZATION/reports/
git commit -m "docs: final cumulative performance optimization report"
```

---

## Task 13: Phase 3c - Sync path cleanup (DEFERRED)

**Status:** Deferred / Lower Priority

**Reason:** The main public endpoint (`POST /api/v1/SearchingRentalService/Detail`) uses the async path (`GetDetailAsync`). The sync `GetDetail` method containing `.GetAwaiter().GetResult()` calls is used by management endpoints (`POST /api/v1/RentalService/Detail`).

**Files when implemented:**
- Modify: `SOURCE/AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService_SelfdriveCarRental/RentalService_SelfdriveCarRental_DetailService.cs` (sync `GetDetail` method, look for `.GetAwaiter().GetResult()` patterns)
- Modify: `SOURCE/AllianceMiddlemanWebAPI/Controllers/MainBusiness/Sigo/RentalServiceController.cs` (line 108-117, `GetMyVehicleDetail` — verify if it calls sync or async path)

**Scope:** Convert management endpoints to use the async path, or eliminate `.GetAwaiter().GetResult()` by making the sync method fully async.

---

## Task Summary

| Task | Phase | Type | Description |
|------|-------|------|-------------|
| 1 | 1 | Code | Create DetailPerformanceTracker utility |
| 2 | 1 | Code | Integrate tracking into GetDetailAsync |
| 3 | 1-2 | SQL | Create DB snapshot and index scripts |
| 4 | 1 | Manual | Collect baseline data on staging |
| 5 | 2a | Manual | Apply Phase 2a indexes and measure |
| 6 | 2b | Manual | Apply Phase 2b indexes and measure |
| 7 | 3b prep | Code | Add GetFeaturesAsync method |
| 8 | 3a prep | Code | Add GetDefaultNoteAsync method |
| 9 | 3a | Code | Parallelize top-level operations in GetDetailAsync |
| 10 | 3a | Manual | Measure Phase 3a results |
| 11 | 3b | Code | Parallelize GetBusySchedules internal queries |
| 12 | 3b | Manual | Final measurement and cumulative report |
| 13 | 3c | Deferred | Sync path cleanup (lower priority) |
