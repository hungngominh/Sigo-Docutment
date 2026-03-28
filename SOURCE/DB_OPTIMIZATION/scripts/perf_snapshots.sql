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
