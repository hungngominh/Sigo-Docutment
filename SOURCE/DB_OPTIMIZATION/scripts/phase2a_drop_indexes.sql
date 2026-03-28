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
