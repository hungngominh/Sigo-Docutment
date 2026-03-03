# Stored Procedures — Chi tiết đầy đủ

> **Tổng cộng:** 17 SPs (14 main + 3 CMS)
> **Định nghĩa enum:** `AllianceMiddlemanWebAPI.Core/Data/Stores/EEzyStoredProcedureNames.cs`
> **Repository:** `StoreRepository_Json.cs` (sync) + `StoreRepository_JsonAsync.cs` (async)

---

## Mục lục
- [Execution Pattern](#execution-pattern)
- [1. sp_GetNotifyMessage4Push_Json](#1-sp_getnotifymessage4push_json)
- [2. sp_Get_OrderOverTimeButDontEnd_Json](#2-sp_get_orderovertimebutdontend_json)
- [3. sp_GetHostAddressForSearch_Json](#3-sp_gethostaddressforsearch_json)
- [4. sp_GetRentalServiceItemViewReport_Json](#4-sp_getrentalserviceitemviewreport_json)
- [5. sp_GetOwnerForDiscountNotification_Json](#5-sp_getownerfordiscountnotification_json)
- [6. sp_GetOrdersNeedUnhidePhone_Json](#6-sp_getordersneedunhidephone_json)
- [7. sp_GetUnreadLatestNotify_Json](#7-sp_getunreadlatestnotify_json)
- [8. sp_GetDataReportWebsite4BCT_Json](#8-sp_getdatareportwebsite4bct_json)
- [9. sp_AutoInsertStaff_Json](#9-sp_autoinsertstaff_json)
- [10. sp_report_VehicleMasterLocationNotSameDetail_Json](#10-sp_report_vehiclemasterlocationnotsamedetail_json)
- [11. sp_GetMessageQueueReport_Json](#11-sp_getmessagequeuereport_json)
- [12. sp_GetSMSQueueReport_Json](#12-sp_getsmsqueuereport_json)
- [13. sp_GetReduceWebSupport_Url_Json](#13-sp_getreducewebsupport_url_json)
- [14. sp_GetIPAddressFromLog_PropertyChanged_Today_Json](#14-sp_getipaddressfromlog_propertychanged_today_json)
- [15. sp_GetConfigLandingPage_Json (CMS)](#15-sp_getconfiglandingpage_json-cms)
- [16. sp_GetBlogLinkInContent_Json (CMS)](#16-sp_getbloglinkincontent_json-cms)
- [17. sp_GetTopicLinkInContent_Json (CMS)](#17-sp_gettopiclinkincontent_json-cms)

---

## SP Quick Reference

| # | SP Name | Caller | Trigger | Return Type |
|---|---------|--------|---------|-------------|
| 1 | `sp_GetNotifyMessage4Push_Json` | PushNotificationHelper | Engine timer | Pending FCM notifications |
| 2 | `sp_Get_OrderOverTimeButDontEnd_Json` | AutoCompleteOrderEngine | Engine timer | Overtime orders (warning + auto-complete) |
| 3 | `sp_GetHostAddressForSearch_Json` | SearchingVehicleHelper_Ext | API call | HostAddress[] by geo/admin |
| 4 | `sp_GetRentalServiceItemViewReport_Json` | NotificationRentalCarHighViewsJob | Batch job | Vehicle view stats for notification |
| 5 | `sp_GetOwnerForDiscountNotification_Json` | NotificationRentalCarDiscount_InWeekJob | Weekly batch | Owners needing discount reminder |
| 6 | `sp_GetOrdersNeedUnhidePhone_Json` | NotificationOrderUnhidePhoneNumberEngine | Engine timer | Orders needing phone unhide |
| 7 | `sp_GetUnreadLatestNotify_Json` | PushNotificationHelper | Re-push timer | Unread proactive notifications |
| 8 | `sp_GetDataReportWebsite4BCT_Json` | BCTController | API call | BCT regulatory report |
| 9 | `sp_AutoInsertStaff_Json` | SolidStaffService | Before list | Void (sync UserLogin → Staff) |
| 10 | `sp_report_VehicleMasterLocationNotSameDetail_Json` | HostAddressBaseService | API call | Address mismatch audit |
| 11 | `sp_GetMessageQueueReport_Json` | Queue monitoring | API call | Email queue status |
| 12 | `sp_GetSMSQueueReport_Json` | Queue monitoring | API call | SMS queue status |
| 13 | `sp_GetReduceWebSupport_Url_Json` | Web support | API call | Shortened URLs |
| 14 | `sp_GetIPAddressFromLog_PropertyChanged_Today_Json` | Security audit | API call | Today's IP addresses |
| 15 | `sp_GetConfigLandingPage_Json` | SolidEzyWeb_UrlRecordService | CMS API | Landing page configs |
| 16 | `sp_GetBlogLinkInContent_Json` | SolidEzyWeb_BlogPostService | CMS API | Blog broken links |
| 17 | `sp_GetTopicLinkInContent_Json` | SolidEzyWeb_TopicService | CMS API | Topic broken links |

---

## Execution Pattern

Tất cả SP được gọi thông qua wrapper repository, trả về JSON string được deserialize trong C#:

```csharp
// Async pattern (phổ biến):
TResult[] result = await StoreRepository_JsonAsync
    .Exec_JsonStoredProceduceAsync<TResult, TParam>(spName, param);

// Sync pattern:
TResult[] result = StoreRepository_Json
    .Exec_JsonStoredProceduce_GetArray<TResult, TParam>(spName, param);

// SQL thực thi:
// SELECT dbo."sp_name"(@jsonParam)  → trả về JSON string
```

**Base parameter class:** `sp_BaseParam`
```csharp
public class sp_BaseParam
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
```

---

## 1. sp_GetNotifyMessage4Push_Json

**Mục đích:** Lấy danh sách notification pending cần push qua FCM.

**Caller:** `PushNotificationHelper.PushNotify_SQLSP()` (line 196)
**File:** `AllianceMiddlemanWebAPI.Service/Helper/PushNotificationHelper.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | Mốc thời gian bắt đầu |
| ToDate | DateTime? | Mốc thời gian kết thúc |

**Return:** `sp_GetNotifyMessage4Push_Json_Result[]`

| Field | Type | Mô tả |
|-------|------|-------|
| NotifyMgsId | long | PK notification message |
| Action | string | Loại action (ORDER_CREATED, ORDER_CONFIRMED...) |
| EntityId | long? | ID entity liên quan |
| EntityName | string | Tên entity |
| EntityUrl | string | URL entity |
| EntityCode | string | Code entity |
| Message | string | Nội dung text |
| MessageWeb | string | Nội dung hiển thị web |
| MessageApp | string | Nội dung hiển thị app |
| Subject | string | Tiêu đề |
| IsPushOnApp | bool | Có push trên app không |
| NotifyById | long? | User gửi notification |
| NotifyToId | long? | User nhận notification |
| DeviceId | long? | Device ID nhận |
| DeviceNumber | string | Device number |
| FCMTokenKey | string | Firebase Cloud Messaging token |
| RequestData | string | JSON data gửi kèm |
| IsSendGGChat | bool | Có gửi Google Chat không |
| ConfigPushNotify | string | JSON config (TTL, Priority) |
| NotifyAt | DateTime? | Thời điểm tạo notification |
| RecipientType | string | "Renter" hoặc "Owner" |

**SQL WHERE clause (actual):**
```sql
WHERE m."PushNotifyAt" IS NULL           -- Chưa được push (chưa có lần push nào)
  AND m."NotifyAt" <= CURRENT_TIMESTAMP  -- Notification đã tới giờ gửi
  AND m."IsDeleted" = false
```

**JOINs:**
```sql
NotificationMessage m
  → UserLogin ul       ON m."NotifyToId" = ul."Id"        -- User nhận
  → UserUseDevice uud  ON ul."Id" = uud."UserLoginId"     -- Device mapping
  → UserDevice ud      ON uud."UserDeviceId" = ud."Id"    -- Device info (FCM token)
WHERE ud."IsDeleted" = false AND ud."IsActive" = true
```

**Quan trọng:** `PushNotifyAt IS NULL` là điều kiện chính — khi push thành công, field này được set = timestamp → notification không bị lấy lại.

**Sample Input/Output:**
```json
// Input (sp_BaseParam):
{ "FromDate": null, "ToDate": null }  // Thường null — SP dùng CURRENT_TIMESTAMP

// Output (1 row sample):
{
  "NotifyMgsId": 45231,
  "Action": "ORDER_CREATED",
  "EntityId": 1892,
  "EntityCode": "ORD-20240301-001",
  "Message": "Bạn có đơn hàng mới từ Nguyễn Văn A",
  "Subject": "Đơn hàng mới",
  "IsPushOnApp": true,
  "NotifyToId": 5672,
  "FCMTokenKey": "dK8x...FCM_token...4Qm",
  "ConfigPushNotify": "{\"Expiration\":900,\"Priority\":true}",
  "NotifyAt": "2024-03-01T10:30:00Z",
  "RecipientType": "Owner"
}
```

**Business logic:**
1. SP lấy notifications chưa được push (`PushNotifyAt IS NULL`) và đã tới giờ (`NotifyAt <= CURRENT_TIMESTAMP`)
2. `PushNotify_SQLSP()` group theo `NotifyMgsId`, dùng `ConcurrentDictionary<long, int> DictNotifyMgsId` để tránh duplicate
3. Mỗi notification → `GetPushNotifyItem()` → check TTL (900s default) → `SendPushNotification_REST_HTTP_V1Async()` → ghi `PushNotificationHistory`

---

## 2. sp_Get_OrderOverTimeButDontEnd_Json

**Mục đích:** Lấy đơn hàng quá hạn chưa được kết thúc, để hệ thống auto-complete.

**Caller:** `AutoCompleteOrderEngine.DoJob()` (line 34)
**File:** `AllianceMiddlemanWebAPI.Service/Engines/AutoCompleteOrderEngine.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| AutoCompleteOrder_AfterMinutes | int | Số phút sau ToDate mới auto-complete |
| FromDate | DateTime? | Mốc bắt đầu |
| ToDate | DateTime? | Mốc kết thúc |

**Return:** `sp_Get_OrderOverTimeButDontEnd_JsonResult` — chứa 2 arrays:

**Array 1: `OrderOverTimeButDontEndInfo[]`** (cảnh báo)
**Array 2: `OrderOver1HourButDontEndInfo[]`** (auto-complete)

| Field | Type | Mô tả |
|-------|------|-------|
| ID_GUID | Guid | UUID đơn hàng |
| OrderNumber | string | Mã đơn |
| RenterId | long? | FK → User (người thuê) |
| OwnerId | long? | FK → User (chủ xe) |
| FromDate | DateTime? | Ngày nhận xe |
| ToDate | DateTime? | Ngày trả xe |
| DepositAmount | decimal? | Tiền cọc |
| OwnerMoneyReceive | decimal? | Tiền owner nhận |
| OwnerCancelRefund | decimal? | Hoàn tiền owner nếu cancel |
| RenterCancelRefund | decimal? | Hoàn tiền renter nếu cancel |
| RenterMoneyReceive | decimal? | Tiền renter nhận |
| OwnerName | string | Tên chủ xe |
| UserName | string | Tên người đặt |
| RentalServiceInfo | string | JSON thông tin dịch vụ |

**Business logic:**

SP trả về 2 JSON arrays trong 1 kết quả:

**Array 1: `OrderOverTimeButDontEndInfo[]`** (cảnh báo — đơn vừa quá hạn):
```sql
-- Điều kiện:
WHERE o."StatusCode" = 'INTHETRIP'
  AND o."ToDate" < NOW()                                          -- Đã quá giờ trả
  AND o."ToDate" >= NOW() - INTERVAL '1 minute' * AutoCompleteOrder_AfterMinutes  -- Chưa quá lâu
  AND NOT EXISTS (                                                -- Chưa gửi cảnh báo
      SELECT 1 FROM dbo."NotificationMessage" n
      WHERE n."EntityId" = o."Id" AND n."Action" = 'ORDER_OVERTIME_WARNING'
  )
```

**Array 2: `OrderOver1HourButDontEndInfo[]`** (auto-complete — đơn quá hạn lâu):
```sql
-- 2 điều kiện OR:
-- Condition A: Quá hạn > AutoCompleteOrder_AfterMinutes
WHERE o."StatusCode" = 'INTHETRIP'
  AND o."ToDate" < NOW() - INTERVAL '1 minute' * AutoCompleteOrder_AfterMinutes

-- Condition B: Quá hạn VÀ qua nửa đêm VN (≥23:50 VN = 1430 phút)
WHERE o."StatusCode" = 'INTHETRIP'
  AND o."ToDate" < NOW()
  AND EXTRACT(HOUR FROM NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh') * 60
      + EXTRACT(MINUTE FROM NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh') >= 1430
      -- 1430 phút = 23:50 VN → auto-complete cuối ngày
```

**JOINs:**
```sql
"Order" o
  → RentalService_SelfdriveCarRental rs  ON o."RentalServiceItemId" = rs."Id"
  → Vehicle v                             ON rs."VehicleId" = v."Id"
  → ConfigVehicleModel cvm                ON v."VehicleModelId" = cvm."Id"
  → Order_Finance of                      ON o."Id" = of."OrderId"
```

**Trigger flow:**
1. Engine gọi `OrderService.SystemAutoDoneOrderAsync()` cho mỗi đơn trong array 2
2. Set `Order.IsSystemAutoDone = true`
3. Gửi notification cho cả owner và renter

---

## 3. sp_GetHostAddressForSearch_Json

**Mục đích:** Tìm xe khả dụng theo toạ độ (bounding box) hoặc địa chỉ hành chính.

**Caller:** `SearchingVehicleHelper_Ext.GetDictHostAddressAsync()` (line 300)
**File:** `AllianceMiddlemanWebAPI.Service/Helper/SearchingVehicleHelper_Ext.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| SW_Lat | decimal? | Bounding box: Southwest latitude |
| NE_Lat | decimal? | Bounding box: Northeast latitude |
| SW_Lng | decimal? | Bounding box: Southwest longitude |
| NE_Lng | decimal? | Bounding box: Northeast longitude |
| Center_Lat | decimal? | Tâm tìm kiếm latitude |
| Center_Lng | decimal? | Tâm tìm kiếm longitude |
| MaxDistance | decimal? | Khoảng cách tối đa (mét) |
| MaxCarCount | int? | Số xe tối đa trả về |
| CaseRunning | int | 1 = Theo tỉnh/quận/phường, 2 = Theo bounding box |
| Province | string | Tên tỉnh (CaseRunning=1) |
| District | string | Tên quận (CaseRunning=1) |
| Ward | string | Tên phường (CaseRunning=1) |

**Return:** `HostAddress[]`

| Field | Type | Mô tả |
|-------|------|-------|
| Id | long | PK HostAddress |
| Latitude | decimal? | Vĩ độ |
| Longitude | decimal? | Kinh độ |
| Detail | string | Địa chỉ chi tiết |
| Province | string | Tỉnh/thành |
| District | string | Quận/huyện |
| Ward | string | Phường/xã |
| AirDistance | decimal? | Khoảng cách đường chim bay (mét) — chỉ CaseRunning=2 |

**SQL logic (actual):**

**CaseRunning=1 (Tìm theo địa chỉ hành chính):**
```sql
-- ⚠️ Dùng exact match (=), KHÔNG phải LIKE
WHERE ha."Province" = p_Province
  AND (p_District IS NULL OR ha."District" = p_District)
  AND (p_Ward IS NULL OR ha."Ward" = p_Ward)
  AND ha."IsDeleted" = false
```

**CaseRunning=2 (Bounding box + Haversine):**
```sql
-- Step 1: Bounding box filter (index-friendly)
WHERE ha."Latitude"  >= p_SW_Lat AND ha."Latitude"  <= p_NE_Lat
  AND ha."Longitude" >= p_SW_Lng AND ha."Longitude" <= p_NE_Lng
  AND ha."IsDeleted" = false

-- Step 2: Haversine distance (mét)
-- AirDistance = 6371 * 1000 * acos(
--     cos(radians(p_Center_Lat)) * cos(radians(ha."Latitude"))
--     * cos(radians(ha."Longitude") - radians(p_Center_Lng))
--     + sin(radians(p_Center_Lat)) * sin(radians(ha."Latitude"))
-- )

-- Step 3: Filter + sort
WHERE AirDistance <= p_MaxDistance
ORDER BY AirDistance ASC
LIMIT p_MaxCarCount
```

**Service filter (cả 2 case):**
```sql
-- Chỉ lấy HostAddress có RentalService active
JOIN RentalService_SelfdriveCarRental rs ON ha."Id" = rs."HostAddressId"
WHERE rs."IsApproved" = true
  AND rs."IsAddNew" = false
  AND rs."IsSuspended" = false
  AND rs."IsDeactive" = false
```

**Business logic:**
1. **CaseRunning=1:** Tìm theo tên tỉnh/quận/phường — exact match (`=`), không phải LIKE
2. **CaseRunning=2:** Tìm trong bounding box (inclusive `>=`/`<=`) → tính Haversine → sort by distance
3. Kết quả được build thành `Dictionary<long?, HostAddress>` để filter RentalServiceItem theo HostAddressId
4. Default: MaxDistance = 30km (30000m), MaxCarCount = 20

**Sample Input/Output:**
```json
// Input (CaseRunning=2, tìm quanh Quận 1, HCM):
{
  "CaseRunning": 2,
  "Center_Lat": 10.7769,
  "Center_Lng": 106.7009,
  "SW_Lat": 10.6769,
  "NE_Lat": 10.8769,
  "SW_Lng": 106.6009,
  "NE_Lng": 106.8009,
  "MaxDistance": 30000,
  "MaxCarCount": 20
}

// Output (2 rows):
[
  { "Id": 142, "Latitude": 10.7821, "Longitude": 106.6953,
    "Province": "Hồ Chí Minh", "District": "Quận 1", "Ward": "Bến Nghé",
    "Detail": "123 Lê Lợi", "AirDistance": 850.5 },
  { "Id": 287, "Latitude": 10.7712, "Longitude": 106.7102,
    "Province": "Hồ Chí Minh", "District": "Quận 3", "Ward": "Phường 6",
    "Detail": "45 Nguyễn Thị Minh Khai", "AirDistance": 1205.3 }
]
```

---

## 4. sp_GetRentalServiceItemViewReport_Json

**Mục đích:** Thống kê lượt xem xe để gửi batch notification (high views, strong views, low views).

**Caller:** `NotificationRentalCarHighViewsJob.DoJob()` (line 61)
**File:** `AllianceMiddlemanWebAPI.Service/Engines/BatchJobs/NotificationRentalCarHighViewsJob.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| HighViewDays | int | Số ngày tính "lượt xem cao" |
| HighViewMin | int | Ngưỡng tối thiểu view cao |
| StrongViewDays | int | Số ngày tính "lượt xem mạnh" |
| StrongViewMin | int | Ngưỡng tối thiểu view mạnh |
| LowViewDays | int | Số ngày tính "lượt xem thấp" |
| LowViewMax | int | Ngưỡng tối đa view thấp |
| IgnoreRecentBookingHours | int | Bỏ qua xe có booking gần đây (giờ) |
| IgnoreIfTotalBookingsOver | int | Bỏ qua xe có quá nhiều booking |
| FromDate | DateTime? | Mốc bắt đầu |
| ToDate | DateTime? | Mốc kết thúc |

**Return:** `sp_GetRentalServiceItemViewReport_Json_Result[]`

| Field | Type | Mô tả |
|-------|------|-------|
| Id | long | PK RentalServiceItem |
| ID_GUID | Guid | UUID |
| OwnerId | long? | FK → owner |
| HighViewStatsTotalView | int | Tổng view (high period) |
| HighViewStatsTotalBooking | int | Tổng booking (high period) |
| HighViewStatsTotalDeposit | int | Tổng deposit (high period) |
| StrongViewStatsTotalView | int | Tổng view (strong period) |
| StrongViewStatsTotalBooking | int | Tổng booking (strong period) |
| StrongViewStatsTotalDeposit | int | Tổng deposit (strong period) |
| LowViewStatsTotalView | int | Tổng view (low period) |
| LowViewStatsTotalBooking | int | Tổng booking (low period) |
| LowViewStatsTotalDeposit | int | Tổng deposit (low period) |
| LastBookingAt | DateTime? | Lần cuối có booking |
| RentalServiceInfo | string | JSON thông tin dịch vụ |
| PlateNumber | string | Biển số xe |

**Business logic:**

**Classification logic (actual SQL):**
```sql
-- Mỗi xe được phân loại dựa trên view count:
CASE
  WHEN StrongViewCount >= p_StrongViewMin THEN 'VeryHighView'
  WHEN HighViewCount   >= p_HighViewMin   THEN 'HighView'
  WHEN LowViewCount    <  p_LowViewMax    THEN 'LowView'
  ELSE 'None'
END AS NotificationType

-- Chỉ trả về xe có NotificationType != 'None'
```

**View counting (source tables):**
```sql
-- Views: RentalServiceItemViewHistory WHERE ViewFrom = 'Detail'
-- Bookings: ServiceItem_BookedRentalSchedule (count, 7 ngày gần nhất)
-- Active xe: IsApproved=true, IsSuspended=false, IsDeactive=false, IsAddNew=false

-- Full query (actual DB):
SELECT car."Id", car."Vehicle", car."OwnerId", car."RentalServiceCategoryId",
       car."LicensePlate", car."CreatedAt",
       COALESCE(vh."ViewCount", 0) AS "TotalView",
       COALESCE(bk."BookingCount", 0) AS "TotalBooking",
       bk."LastBookingAt",
       CASE
         WHEN bk."LastBookingAt" IS NOT NULL
              AND EXTRACT(EPOCH FROM (NOW()-bk."LastBookingAt"))/3600 < IgnoreRecentBookingHours
              THEN 'Skip_RecentBooking'
         WHEN COALESCE(bk."BookingCount",0) > IgnoreIfTotalBookingsOver THEN 'Skip_TooManyBookings'
         WHEN COALESCE(vh."ViewCount",0) = 0 THEN 'NoView'
         WHEN COALESCE(vh."ViewCount",0) < LowViewMax THEN 'LowView'
         WHEN COALESCE(vh."ViewCount",0) >= StrongViewMin
              AND COALESCE(bk."BookingCount",0) = 0 THEN 'VeryHighView'
         WHEN COALESCE(vh."ViewCount",0) >= HighViewMin
              AND COALESCE(bk."BookingCount",0) = 0 THEN 'HighView'
         ELSE 'None'
       END AS "NotificationType"
FROM dbo."RentalService_SelfdriveCarRental" car
LEFT JOIN (/*ViewHistory grouped by RentalServiceItemId*/) vh ...
LEFT JOIN (/*BookedSchedule grouped by RentalServiceItemId*/) bk ...
WHERE car."IsApproved"=TRUE AND car."IsSuspended"=FALSE
  AND car."IsDeactive"=FALSE AND car."IsAddNew"=FALSE
ORDER BY car."CreatedAt" DESC LIMIT 1000
-- Post-filter: chỉ giữ NotificationType NOT IN ('Skip_*','None')
```

**Default parameters (từ caller job):**
| Param | Default | Mô tả |
|-------|---------|-------|
| HighViewDays | 7 | Tính views trong 7 ngày gần nhất |
| HighViewMin | 10 | ≥10 views = HighView |
| StrongViewDays | 14 | Tính views trong 14 ngày |
| StrongViewMin | 30 | ≥30 views = VeryHighView |
| LowViewDays | 7 | Tính views trong 7 ngày |
| LowViewMax | 5 | <5 views = LowView |

**Giới hạn:** `LIMIT 1000` — tối đa 1000 xe mỗi lần chạy job

**Job flow:**
- SP tính thống kê 3 giai đoạn (high/strong/low) cho từng xe
- Job dựa vào `NotificationType` để gửi notification phù hợp:
  - `VeryHighView` → "Xe bạn đang rất hot!"
  - `HighView` → "Xe bạn đang được quan tâm"
  - `LowView` → "Hãy cập nhật giá/ảnh để tăng lượt xem"

---

## 5. sp_GetOwnerForDiscountNotification_Json

**Mục đích:** Lấy danh sách owner cần nhận thông báo giảm giá hàng tuần.

**Caller:** `NotificationRentalCarDiscount_InWeekJob.DoJob()` (line 32)
**File:** `AllianceMiddlemanWebAPI.Service/Engines/BatchJobs/NotificationRentalCarDiscount_InWeekJob.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | Mốc bắt đầu tuần |
| ToDate | DateTime? | Mốc kết thúc tuần |

**Return:** `sp_GetOwnerForDiscountNotification_Json_Result[]`

| Field | Type | Mô tả |
|-------|------|-------|
| OwnerId | long | PK owner cần nhận thông báo |

**Business logic:**

**⚠️ Correction:** SP KHÔNG tìm owners "chưa có booking". SP tìm owners có xe mà **chưa có giá giảm** (discount pricing) trong tuần.

**"Trong tuần" definition (actual SQL):**
```sql
-- Calendar week: Monday → Sunday
date_trunc('week', NOW()) AS week_start
date_trunc('week', NOW()) + INTERVAL '6 days' AS week_end
```

**"Active" vehicle definition:**
```sql
WHERE rs."IsApproved" = true
  AND rs."IsDeactive" = false
  AND rs."IsAddNew" = false
  AND rs."IsDeleted" = false
  AND rs."IsDisable" = false
```

**Core logic — tìm xe CHƯA CÓ GIÁ GIẢM trong tuần:**
```sql
-- Xe có giá giảm = ServiceItem_DateRentalPrice.RentalPrice < ServiceItem_WeekdayRentalPrice.RentalPrice
-- cho bất kỳ ngày nào trong tuần đó
-- SP tìm owners có ÍT NHẤT 1 xe active mà KHÔNG có ngày nào có giá giảm trong tuần
```

**Job flow:**
- SP trả về danh sách OwnerId
- Job gửi notification nhắc owner đặt giá giảm cuối tuần để tăng booking rate

---

## 6. sp_GetOrdersNeedUnhidePhone_Json

**Mục đích:** Lấy đơn hàng cần hiện số điện thoại (unhide) cho renter/owner liên hệ.

**Caller:** `NotificationOrderUnhidePhoneNumberEngine.DoJob()` (line 28)
**File:** `AllianceMiddlemanWebAPI.Service/Engines/NotificationOrderUnhidePhoneNumberEngine.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | Mốc bắt đầu |
| ToDate | DateTime? | Mốc kết thúc |

**Return:** `sp_GetOrdersNeedUnhidePhone_JsonResult[]`

| Field | Type | Mô tả |
|-------|------|-------|
| ID_GUID | Guid | UUID đơn hàng |
| OrderNumber | string | Mã đơn |
| OwnerId | long? | FK → owner |
| RenterId | long? | FK → renter |
| OwnerName | string | Tên chủ xe |
| RenterName | string | Tên người thuê |
| RentalServiceInfo | string | JSON thông tin dịch vụ |

**Business logic:**
- SĐT bị ẩn mặc định khi đơn ở trạng thái OWNER2CONFIRM/CUS2DEPOSIT
- Khi đơn chuyển sang WAITING2DEPARTURE, SP tìm đơn cần unhide
- Engine gửi notification nhắc cả 2 bên

**SQL Logic (có 2 overloads trong DB):**
```sql
-- Overload 1: Chỉ check deposit + 1h
SELECT o."ID_GUID", o."OrderNumber", o."OwnerId", o."RenterId",
       o."OwnerName", o."RenterName",
       (CM."Name" || ' ' || V."YearModel" || ' (' || V."PlateNumber" || ')') AS "RentalServiceInfo"
FROM dbo."Order" o
  LEFT JOIN dbo."RentalService_SelfdriveCarRental" RS ON RS."Id" = o."RentalServiceItemId"
  LEFT JOIN dbo."Vehicle" V ON V."Id" = RS."CarId"
  LEFT JOIN dbo."ConfigVehicleModel" CM ON CM."Id" = V."VehicleModelId"
WHERE o."DepositDoneAt" IS NOT NULL
  AND (NOW() - o."DepositDoneAt") >= INTERVAL '1 hour'
  AND o."EntDate" >= '2025-11-29T00:00:00Z'
  AND o."StatusCode" NOT LIKE '%CANCEL%'
  AND NOT EXISTS (
    SELECT 1 FROM dbo."NotificationMessage" nm
    WHERE nm."EntityId" = o."ID_GUID"::text
      AND nm."Type" LIKE '%order_unhide_phonenumber%'
  )
ORDER BY o."DepositDoneAt" ASC

-- Overload 2: Thêm điều kiện (FromDate - DepositDoneAt) > 1h
-- (loại bỏ đơn mà thời gian nhận xe quá gần deposit)
AND (o."FromDate" - o."DepositDoneAt") > INTERVAL '1 hour'
```

---

## 7. sp_GetUnreadLatestNotify_Json

**Mục đích:** Lấy notification chưa đọc mới nhất để re-push (retry failed).

**Caller:** `PushNotificationHelper.RePushProactiveNotify_SQLSP()` (line 233)
**File:** `AllianceMiddlemanWebAPI.Service/Helper/PushNotificationHelper.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | Mốc bắt đầu |
| ToDate | DateTime? | Mốc kết thúc |

**Return:** `sp_GetUnreadLatestNotify_Json_Result[]`

| Field | Type | Mô tả |
|-------|------|-------|
| NotifyMgsId | long | PK notification |
| Action | string | Loại action |
| EntityId | long? | ID entity |
| EntityName | string | Tên entity |
| EntityUrl | string | URL |
| EntityCode | string | Code |
| Message | string | Nội dung |
| Subject | string | Tiêu đề |
| Type | string | Loại notification |
| IsPushOnApp | bool | Push trên app |
| IsNeedPush | bool | Cần push lại |
| IsShowUI | bool | Hiển thị UI |
| NotifyById | long? | User gửi |
| NotifyToId | long? | User nhận |
| DeviceId | long? | Device |
| DeviceNumber | string | Device number |
| FCMTokenKey | string | FCM token |
| RequestData | string | JSON data |
| IsSendGGChat | bool | Gửi Google Chat |
| ConfigPushNotify | string | JSON config |
| NotifyAt | DateTime? | Thời điểm notify |
| RecipientType | string | "Renter" / "Owner" |

**Business logic:**
- Khác với SP #1 (lấy pending push), SP này lấy notification đã gửi nhưng chưa đọc
- Dùng cho re-push proactive (nhắc lại quan trọng)
- TTL vẫn áp dụng: nếu `NotifyAt + TTL <= UtcNow` → bỏ qua

**SQL Logic:**
```sql
SELECT DISTINCT ON (m."EntityId")
    m."Id" AS "NotifyMgsId", m."Type" AS "Action",
    m."EntityId", m."EntityName", m."EntityUrl", m."EntityCode",
    m."Message", m."Subject", m."IsPushOnApp",
    m."NotifyById", m."NotifyToId", m."Type",
    m."IsNeedPush", m."IsShowUI",
    ud."DeviceId", ud."DeviceNumber", d."FCMTokenKey",
    m."RequestData", m."IsSendGGChat", ud."AppName",
    m."ConfigPushNotify", m."NotifyAt", m."RecipientType"
FROM dbo."NotificationMessage" m
  JOIN dbo."UserLogin" u ON m."NotifyToId" = u."Id"
  JOIN dbo."UserUseDevice" ud ON ud."UserLoginId" = u."Id" AND ud."IsActive" = true
  JOIN dbo."UserDevice" d ON d."Id" = ud."DeviceId"
WHERE m."IsDeleted" = false
  AND u."IsDeleted" = false
  AND ud."IsDeleted" = false
  AND m."IsRead" = false
  AND m."EntityName" = 'Notification'   -- 🔑 chỉ lấy loại proactive
  AND NOT EXISTS (
    SELECT 1 FROM dbo."NotificationMessage" m2
    WHERE m2."EntityId" = m."EntityId" AND m2."IsRead" = true
  )
ORDER BY m."EntityId", m."NotifyAt" DESC
```

**Key differences vs SP #1:**
| | SP #1 (Push pending) | SP #7 (Re-push unread) |
|---|---|---|
| Filter | `PushNotifyAt IS NULL` | `IsRead = false` |
| EntityName | Bất kỳ | `= 'Notification'` (proactive only) |
| Dedup | Không | `DISTINCT ON (EntityId)` — chỉ lấy notify mới nhất per entity |
| Read check | Không | `NOT EXISTS (IsRead = true)` — skip nếu đã đọc bất kỳ version nào |

---

## 8. sp_GetDataReportWebsite4BCT_Json

**Mục đích:** Báo cáo thống kê website cho **Bộ Công Thương (BCT)** — khai báo bắt buộc theo quy định quản lý sàn TMĐT tại Việt Nam.

**Caller:** `BCTController.GetListReportWebsite4BCT()` (line 49)
**File:** `AllianceMiddlemanWebAPI/Controllers/AppSystem/BCTController.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | Bắt đầu kỳ báo cáo |
| ToDate | DateTime? | Kết thúc kỳ báo cáo |

**Return:** `sp_GetDataReportWebsite4BCT_Json_Result[]` → maps to `ReportWebsite4BCTModel`

| Field | Type | Mô tả |
|-------|------|-------|
| soLuongTruyCap | long | Số lượt truy cập |
| soNguoiBan | long | Số người bán (owner) |
| soNguoiBanMoi | long | Số owner mới trong kỳ |
| tongSoSanPham | long | Tổng số sản phẩm (xe cho thuê) |
| soSanPhamMoi | long | Số xe mới trong kỳ |
| soLuongGiaoDich | long | Số lượng giao dịch |
| tongSoDonHangThanhCong | long | Tổng đơn thành công |
| tongSoDonHangKhongThanhCong | long | Tổng đơn không thành công |
| tongGiaTriGiaoDich | decimal | Tổng giá trị giao dịch (VNĐ) |

**Business logic:**
- Báo cáo bắt buộc theo quy định TMĐT Bộ Công Thương (Nghị định 52/2013/NĐ-CP và các văn bản liên quan)
- Sigo phải khai báo định kỳ: số lượt truy cập, số người bán, số xe, số đơn, doanh thu
- **SP trả về hardcoded 0 là đúng theo thiết kế hiện tại** — đây là placeholder (thể bào), dữ liệu thực chưa cần tổng hợp tự động, có thể nhập tay qua admin khi nộp báo cáo

**SQL Logic:**
```sql
-- ⚠️ STUB: SP hiện tại chưa implement — trả về hardcoded 0 cho tất cả fields.
-- Lý do: Báo cáo BCT yêu cầu format cố định, nhưng data aggregation chưa hoàn thiện.
SELECT 0 AS "soLuongTruyCap", 0 AS "soNguoiBan", 0 AS "soNguoiBanMoi",
       0 AS "tongSoSanPham", 0 AS "soSanPhamMoi", 0 AS "soLuongGiaoDich",
       0 AS "tongSoDonHangThanhCong", 0 AS "tongSoDonHangKhongThanhCong",
       0 AS "tongGiaTriGiaoDich"

-- Cần implement:
-- soLuongTruyCap      ← COUNT(*) FROM "RentalServiceItemViewHistory" WHERE ViewAt BETWEEN @From AND @To
-- soNguoiBan          ← COUNT(DISTINCT OwnerId) FROM "RentalServiceItem" WHERE IsApproved=true
-- soNguoiBanMoi       ← COUNT(DISTINCT OwnerId) FROM "RentalServiceItem" WHERE ApprovedAt BETWEEN @From AND @To
-- tongSoSanPham       ← COUNT(*) FROM "RentalServiceItem" WHERE IsApproved=true AND IsDeleted=false
-- soSanPhamMoi        ← COUNT(*) FROM "RentalServiceItem" WHERE Log_CreatedDate BETWEEN @From AND @To
-- soLuongGiaoDich     ← COUNT(*) FROM "Order" WHERE Log_CreatedDate BETWEEN @From AND @To
-- tongSoDonHangThanhCong      ← COUNT(*) FROM "Order" WHERE StatusCode='DONE' AND ...
-- tongSoDonHangKhongThanhCong ← COUNT(*) FROM "Order" WHERE StatusCode IN ('CUSCANCEL','OWNERCANCEL','SYSTEMCANCEL') AND ...
-- tongGiaTriGiaoDich  ← SUM(TotalAmountPayment) FROM "Order" WHERE StatusCode='DONE' AND ...
```

---

## 9. sp_AutoInsertStaff_Json

**Mục đích:** Tự động đồng bộ/tạo staff records từ UserLogin.

**Caller:** `SolidStaffService.ActionRunBeforeListAsync()` (line 90)
**File:** `AllianceMiddlemanWebAPI.Service/Services/AppSystem/Staff/StaffService.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | (inherited from sp_BaseParam) |
| ToDate | DateTime? | (inherited from sp_BaseParam) |

**Return:** `SP_EmptyResult` (void — chỉ thực thi INSERT, không trả dữ liệu)

**Business logic:**
- Chạy trước khi list staff (`ActionRunBeforeListAsync`)
- Tìm UserLogin có role Staff nhưng chưa có record trong bảng Staff
- Auto-insert staff record với thông tin từ UserLogin

**SQL Logic:**
```sql
-- Insert UserLogin chưa có Staff record
INSERT INTO dbo."Staff" ("Id", "IsDeleted")
SELECT "Id", "IsDeleted"
FROM dbo."UserLogin"
WHERE "Id" NOT IN (SELECT "Id" FROM dbo."Staff");
```

---

## 10. sp_report_VehicleMasterLocationNotSameDetail_Json

**Mục đích:** Audit xe có địa chỉ master (HostAddress) khác với chi tiết tỉnh/quận/phường.

**Caller:** `HostAddressBaseService.UpdateProvinceDistrictAndWardFromAddress()` (line 132)
**File:** `AllianceMiddlemanWebAPI.Service/Services/MainBusiness/HostAddress/HostAddressBaseService.cs`

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| Status | int? | Filter theo trạng thái (1-6, null = tất cả) |
| RemoveAccents | boolean? | Filter theo kết quả so sánh accent-insensitive |

**Return:** `sp_report_VehicleMasterLocationNotSameDetail_JsonResultModel[]`

| Field | Type | Mô tả |
|-------|------|-------|
| HostAddressId | long | PK HostAddress |
| PlateNumber | string | Biển số xe |
| MobilePhone | string | SĐT chủ xe |
| DisplayName | string | Tên chủ xe |
| Province | string | Tỉnh/Thành phố |
| District | string | Quận/Huyện |
| Ward | string | Phường/Xã |
| Detail | string | Địa chỉ chi tiết |
| RentalServiceId | long | PK RentalServiceItem |
| Status | string | Trạng thái xe (text) |
| RemoveAccents | boolean | true = mismatch khi so accent-insensitive |

**Business logic:**
- So sánh HostAddress.Detail (text) với Province/District/Ward
- Dùng `remove_accents()` để so sánh accent-insensitive
- Filter theo Status (1-6) và RemoveAccents flag
- Dùng cho data quality: đảm bảo xe hiển thị đúng khu vực khi search

**SQL Logic:**
```sql
-- Params: Status (int 1-6), RemoveAccents (boolean)
-- Status mapping: 1=Đang thêm mới, 2=Chờ duyệt, 3=Bị từ chối,
--                 4=Đang hoạt động, 5=Tạm ngưng, 6=Vô hiệu hóa

SELECT R."HostAddressId", V."PlateNumber", U."MobilePhone", U."DisplayName",
       H."Province", H."District", H."Ward", H."Detail",
       R."Id" AS "RentalServiceId",
       CASE
         WHEN R."IsAddNew" = true THEN 'Đang thêm mới'
         WHEN R."IsApproved" IS NULL AND R."IsAddNew" = false THEN 'Đang chờ duyệt'
         WHEN R."IsApproved" = false THEN 'Bị từ chối duyệt'
         WHEN R."IsApproved" = true AND R."IsSuspended" = false AND R."IsDeactive" = false THEN 'Đang hoạt động'
         WHEN R."IsSuspended" = true THEN 'Đang tạm ngưng'
         WHEN R."IsDeactive" = true THEN 'Đang bị vô hiệu hóa'
       END AS "Status",
       -- True nếu Detail KHÔNG chứa Province/District/Ward (dấu hoặc không dấu)
       CASE WHEN remove_accents("Detail") ILIKE '%' || remove_accents("Province") || '%'
                 OR remove_accents("Detail") ILIKE '%' || remove_accents("District") || '%'
                 OR remove_accents("Detail") ILIKE '%' || remove_accents("Ward") || '%'
            THEN false ELSE true END AS "RemoveAccents"
FROM dbo."RentalService_SelfdriveCarRental" RS
  JOIN dbo."HostAddress" H ON R."HostAddressId" = H."Id"
  JOIN dbo."Vehicle" V ON V."Id" = RS."CarId"
  JOIN dbo."RentalServiceItem" R ON R."Id" = RS."Id"
  JOIN dbo."UserLogin" U ON U."Id" = R."OwnerId"
WHERE ("Detail" NOT ILIKE '%' || "Province" || '%')
   OR ("Detail" NOT ILIKE '%' || "District" || '%')
   OR ("Detail" NOT ILIKE '%' || "Ward" || '%')
   OR "Detail" IS NULL OR "Province" IS NULL OR "District" IS NULL OR "Ward" IS NULL
```

---

## 11. sp_GetMessageQueueReport_Json

**Mục đích:** Báo cáo trạng thái hàng chờ email/message.

**Caller:** Queue monitoring service
**File:** `AllianceMiddlemanWebAPI.Core/Data/Stores/SPEntities/sp_BaseParam.cs` (line 72)

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| Subject | string | Filter theo tiêu đề |
| Receive | string | Filter theo người nhận |
| FromDate | DateTime? | Mốc bắt đầu |
| ToDate | DateTime? | Mốc kết thúc |

**Return:** `sp_GetMessageQueueReport_JsonResult[]`

| Field | Type | Mô tả |
|-------|------|-------|
| Subject | string | Tiêu đề email |
| Body | string | Nội dung |
| Receiver | string | Email người nhận |
| SentDate | DateTime? | Ngày gửi |
| CreatedDate | DateTime? | Ngày tạo |
| SendStatus | string | Trạng thái gửi |
| Sender | string | Email người gửi |
| FromIP | string | IP gửi |
| SQLAppName | string | Tên application |
| SendStatusFromTwilio | string | Status từ Twilio |
| ErrorMessage | string | Lỗi (nếu có) |
| TrackingID | string | ID tracking |
| RecipientMain | string | Người nhận chính |
| SendStatusFromSendGrid | string | Status từ SendGrid |

> **Lưu ý:** SP này KHÔNG tồn tại trong DB PostgreSQL — chỉ có entity class definition trong C#. Có thể nằm trong SQL Server hoặc chưa được migrate sang PostgreSQL.

---

## 12. sp_GetSMSQueueReport_Json

**Mục đích:** Báo cáo trạng thái hàng chờ SMS.

**Caller:** Queue monitoring service
**File:** `AllianceMiddlemanWebAPI.Core/Data/Stores/SPEntities/sp_BaseParam.cs` (line 65)

**Parameters:** Kế thừa `sp_GetMessageQueueReport_JsonParam` (Subject, Receive, FromDate, ToDate)

**Return:** `sp_GetSMSQueueReport_JsonResult[]` — cùng cấu trúc với SP #11

**Business logic:** Tương tự SP #11 nhưng cho kênh SMS thay vì email.

**SQL Logic:**
```sql
SELECT "Recipient", "MessageBody", "Date_Sent", "SendStatus"
FROM dbo."SMSQueue"
WHERE (FromDate IS NULL OR "Date_Sent" >= FromDate)
  AND (ToDate IS NULL OR "Date_Sent" <= ToDate)
  AND "IsDeleted" = false
ORDER BY "Date_Sent" DESC
LIMIT 100
```

---

## 13. sp_GetReduceWebSupport_Url_Json

**Mục đích:** Rút gọn URL cho web support links.

**File:** `AllianceMiddlemanWebAPI.Core/Data/Stores/SPEntities/sp_BaseParam.cs` (line 68)

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| Id | long? | ID URL cần reduce |

**Return:** URL reduction data

**Business logic:** Quản lý và rút gọn URL hỗ trợ khách hàng.

> **Lưu ý:** SP này KHÔNG tồn tại trong DB PostgreSQL — chỉ có entity class definition trong C#.

---

## 14. sp_GetIPAddressFromLog_PropertyChanged_Today_Json

**Mục đích:** Lấy danh sách IP đã thay đổi property hôm nay — phục vụ security audit.

**File:** `AllianceMiddlemanWebAPI.Core/Data/Stores/SPEntities/sp_ResultClass.cs` (line 25)

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | (inherited from sp_BaseParam) |
| ToDate | DateTime? | (inherited from sp_BaseParam) |

**Return:** `sp_GetIPAddressFromLog_PropertyChanged_Today_JsonResult[]`

| Field | Type | Mô tả |
|-------|------|-------|
| IPAddress | string | Địa chỉ IP |

**Business logic:**
- Trích xuất unique IP từ bảng Log_PropertyChanged cho ngày hôm nay
- Dùng cho security monitoring: phát hiện IP bất thường

> **Lưu ý:** SP này KHÔNG tồn tại trong DB PostgreSQL. Tuy nhiên SP liên quan `sp_GetDataLog_PropertyChanged_Json` có tồn tại và query bảng `Log_PropertyChanged` với filter FromDate/ToDate, FieldName, ObjType, Log_CreatedBy (LIMIT 2000). IP address extraction có thể được thực hiện từ kết quả SP đó.

---

## 15. sp_GetConfigLandingPage_Json (CMS)

**Mục đích:** Lấy cấu hình landing page cho CMS, dùng để generate SEO slugs.

**Caller:** `SolidEzyWeb_UrlRecordService.UpdateSlugByTitle()` (line 482)
**File:** `Ezy.Module.CMS/Ezy.Module.CMS.Shared/Services/Categories/EzyWeb_UrlRecord/SolidEzyWeb_UrlRecordService.cs`
**Repository:** `Ezy.Module.CMS.Core/Repository/ESCStoreRepository_Json.cs` (line 51)

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | (inherited from sp_BaseParam) |
| ToDate | DateTime? | (inherited from sp_BaseParam) |

**Return:** `sp_ConfigLandingPageResultModel[]`

| Field | Type | Mô tả |
|-------|------|-------|
| Id | long | PK |
| ParentId | long? | FK → parent page |
| Title | string | Tiêu đề |
| Content | string | Nội dung HTML |
| IsTemplate | bool | Là template |
| MoreInfo | string | Thông tin thêm |
| CreatedFromId | long? | Tạo từ template nào |
| Templated | string | Template code |
| IsNoIndex | bool | noindex cho SEO |
| CanonicalUrl | string | Canonical URL |
| Name | string | Tên page |
| Description | string | Mô tả |
| ColorCode | string | Mã màu |
| IconUrl | string | URL icon |
| ID_GUID | Guid | UUID |
| MetaTitle | string | SEO meta title |
| MetaKeywords | string | SEO meta keywords |
| MetaDescription | string | SEO meta description |
| Slug | string | URL slug |
| Files | string | JSON files |
| IsDeleted | bool | Soft delete |
| Log_CreatedDate | DateTime? | Ngày tạo |
| Log_CreatedBy | string | Người tạo |
| Log_UpdatedDate | DateTime? | Ngày cập nhật |
| Log_UpdatedBy | string | Người cập nhật |
| Note | string | Ghi chú |
| OrderNo | decimal | Thứ tự |
| IsDisable | bool | Vô hiệu |
| Code | string | Mã page |

**SQL Logic:**
```sql
SELECT * FROM dbo."ConfigLandingPage"
WHERE "IsDeleted" = false
```

---

## 16. sp_GetBlogLinkInContent_Json (CMS)

**Mục đích:** Validate links blog trong nội dung — tìm broken links.

**Caller:** `SolidEzyWeb_BlogPostService`
**Repository:** `ESCStoreRepository_Json.cs` (line 59)

**Parameters:**
| Param | Type | Mô tả |
|-------|------|-------|
| FromDate | DateTime? | (inherited from sp_BaseParam) |
| ToDate | DateTime? | (inherited from sp_BaseParam) |

**Return:** `sp_GetBlogLinkInContent_JsonResultModel[]`

| Field | Type | Mô tả |
|-------|------|-------|
| Id | long | PK blog post |
| LinkPath | string | URL link trong content |
| ValidLink | bool | Link hợp lệ hay không |

**Business logic:**
- Parse HTML content trong blog posts
- Trích xuất tất cả internal links sigo.vn (regex)
- Validate mỗi link → check exists trong EzyWeb_UrlRecord

**SQL Logic:**
```sql
-- Step 1: Extract slug từ mỗi href="https://sigo.vn/{slug}" trong blog body
SELECT blog."Id", matches[1] AS "LinkPath"
FROM dbo."EzyWeb_BlogPost" AS blog
  JOIN dbo."EzyWeb_UrlRecord" AS slugRec
    ON blog."Id" = slugRec."EntityId" AND slugRec."EntityName" = 'BlogPost'
  CROSS JOIN LATERAL regexp_matches(
    blog."Body",
    '(?i)href="https?://sigo\.vn/([^"/#?]+)(?=[#"]|$)', 'g'
  ) AS matches
WHERE blog."IsDeleted" = false AND blog."IsDisable" = false
  AND slugRec."IsDeleted" = false AND slugRec."IsActive" = true

-- Step 2: Validate — LEFT JOIN EzyWeb_UrlRecord xem slug có tồn tại không
LEFT JOIN dbo."EzyWeb_UrlRecord" AS slug
  ON tb1."LinkPath" = slug."Slug" AND slug."IsActive" = true AND slug."IsDeleted" = false

-- Step 3: Filter out search/detail URLs
WHERE tb1."LinkPath" IS NOT NULL AND tb1."LinkPath" != ''
  AND LEFT(tb1."LinkPath", 8) != 'searchxe'
  AND LEFT(tb1."LinkPath", 10) != 'car_detail'
```

---

## 17. sp_GetTopicLinkInContent_Json (CMS)

**Mục đích:** Validate links topic trong nội dung — tìm broken links.

**Caller:** `SolidEzyWeb_TopicService.InitService()` (line 68)
**File:** `Ezy.Module.CMS/Ezy.Module.CMS.Shared/Services/Categories/EzyWeb_Topic/SolidEzyWeb_TopicService.cs`
**Repository:** `ESCStoreRepository_Json.cs` (line 66)

**Parameters:** Kế thừa `sp_BaseParam` (FromDate, ToDate)

**Return:** `sp_GetTopicLinkInContent_JsonResultModel[]`

| Field | Type | Mô tả |
|-------|------|-------|
| Id | long | PK topic |
| LinkPath | string | URL link trong content |
| ValidLink | bool | Link hợp lệ hay không |

**Business logic:** Tương tự SP #16 nhưng cho `EzyWeb_Topic` entities thay vì `EzyWeb_BlogPost`.

**SQL Logic:** Giống SP #16 nhưng thay `dbo."EzyWeb_BlogPost"` bằng `dbo."EzyWeb_Topic"`.
```sql
SELECT blog."Id", matches[1] AS "LinkPath"
FROM dbo."EzyWeb_Topic" AS blog ...  -- phần còn lại giống SP #16
```

---

*Xem thêm: [database-design.md](./database-design.md) | [background-engines.md](./background-engines.md) | [helper-algorithms.md](./helper-algorithms.md)*
