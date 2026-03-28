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
