-- ============================================
-- Phase 2b ROLLBACK: Drop indexes
-- ============================================

DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_rental_view_history_main;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_rental_view_history_viewat;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_order_changeStatus_orderId;
DROP INDEX CONCURRENTLY IF EXISTS dbo.idx_notification_entity_type;
