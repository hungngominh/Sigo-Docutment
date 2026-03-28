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
