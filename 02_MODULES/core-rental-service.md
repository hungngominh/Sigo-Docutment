# Core Module: Rental Service — Dịch vụ cho thuê xe

> **Project:** `AllianceMiddlemanWebAPI` | **Domain:** Nghiệp vụ cốt lõi

## Mục lục
- [Tổng quan](#tổng-quan)
- [Controllers](#controllers)
- [Services](#services)
- [Entities](#entities)
- [API Endpoints](#api-endpoints)
- [Luồng nghiệp vụ chính](#luồng-nghiệp-vụ-chính)
- [Hiệu năng](#hiệu-năng)

---

## Tổng quan

Rental Service là **nghiệp vụ cốt lõi** của Sigo platform. Bao gồm toàn bộ lifecycle:

```
Tìm kiếm xe → Xem chi tiết → Đặt xe (Booking) → Xác nhận → Thanh toán
→ Nhận xe (Begin) → Trả xe (End) → Đánh giá (Rating)
```

**Hai vai trò chính:**
- **Renter (người thuê):** Tìm kiếm, đặt xe, thanh toán, nhận/trả xe
- **Owner (chủ xe):** Đăng xe, quản lý lịch, xác nhận đơn, nhận thanh toán

---

## Controllers

| Controller | Route | File | Mô tả |
|-----------|-------|------|-------|
| `SearchingRentalServiceController` | `api/v1/SearchingRentalService` | `Sigo/` | Tìm kiếm & xem chi tiết xe |
| `RentalServiceController` | `api/v1/RentalService` | `Sigo/` | Booking, confirm, pay, begin/end |
| `RentalServiceController_v2` | `api/v2/RentalService` | `Sigo/` | V2 endpoints |
| `RentalService_WebsiteController` | `api/v1/SearchingRentalService` | `Sigo/` | Endpoints cho website |
| `RentalServiceUploadFileController` | `api/v1/RentalServiceUpload` | `Sigo/` | Upload ảnh xe |
| `RentalServiceSMSController` | `api/v1/RentalServiceSMS` | `Sigo/` | SMS operations |
| `RentalService_SelfdriveCarRentalController` | `api/v1/RentalService_SelfdriveCarRental` | `MainBusiness/` | Cài đặt self-drive |
| `MyRentalServiceScheduleController` | `api/v1/MyRentalServiceSchedule` | `MainBusiness/` | Lịch cho thuê của owner |
| `RentalServiceItemPriorityListController` | `api/v1/RentalServiceItemPriorityList` | `MainBusiness/` | Thứ tự ưu tiên hiển thị |
| `RentalServiceTempDataController` | `api/v1/RentalServiceTempData` | `MainBusiness/` | Dữ liệu tạm booking |

---

## Services

| Service | File | Mô tả |
|---------|------|-------|
| `RentalService/` | Interfaces/ | Service chính: booking, confirm, cancel, begin, end |
| `RentalServiceItem/` | | Quản lý service item (mỗi xe = 1 item) |
| `RentalService_SelfdriveCarRental/` | | Self-drive car cài đặt |
| `RentalServiceItemPriorityList/` | | Quản lý priority list |
| `RentalServiceTempData/` | | Temp data cho booking in-progress |

---

## Entities

### Service Item — Xe cho thuê

| Entity | Mô tả |
|--------|-------|
| `RentalServiceItem` | Xe cho thuê (1 xe = 1 service item) |
| `RentalServiceItem_Calculating` | Thống kê: tổng chuyến, rating, views |
| `RentalServiceItem_ChangeStatus` | Lịch sử thay đổi trạng thái (active/paused/removed) |
| `RentalServiceItemPriorityList` | Thứ tự hiển thị trong search results |
| `RentalServiceItemViewHistory` | Log lượt xem chi tiết xe |
| `RentalServiceTempData` | Dữ liệu tạm trong quá trình booking |

### Lịch & Giá

| Entity | Mô tả |
|--------|-------|
| `ServiceItem_DateRentalPrice` | Giá theo ngày cụ thể (override) |
| `ServiceItem_WeekdayRentalPrice` | Giá theo thứ trong tuần (default) |
| `ServiceItem_DateBusyRentalSchedule` | Ngày xe bận (owner đánh dấu) |
| `ServiceItem_WeekdaysBusyRentalSchedule` | Thứ bận hàng tuần |
| `ServiceItem_BookedRentalSchedule` | Ngày đã có booking confirmed |
| `ServiceItem_MinimumRentalDayRequired` | Số ngày thuê tối thiểu |

### Tài liệu & Hình ảnh

| Entity | Mô tả |
|--------|-------|
| `ServiceItem_Document` | Metadata tài liệu xe |
| `ServiceItem_Document_File` | File tài liệu (đăng kiểm, bảo hiểm...) |
| `ServiceItem_Image` | Metadata ảnh xe |
| `ServiceItem_Image_File` | File ảnh xe |
| `ServiceItem_Feature_Mapping` | Tính năng xe (GPS, camera, USB, Bluetooth...) |
| `ServiceItem_RentalRequiredItem_Mapping` | Vật dụng yêu cầu (CMND, giấy phép...) |

### Cấu hình

| Entity | Mô tả |
|--------|-------|
| `RentalService_SelfdriveCarRental` | Cài đặt self-drive (deposit, giới hạn km...) |
| `ServiceRentalSetting` | Cài đặt chung của dịch vụ |

---

## API Endpoints

### Tìm kiếm xe

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/SearchingRentalService/List` | Tìm kiếm xe theo điều kiện | Mixed |
| POST | `/api/v1/SearchingRentalService/Detail` | Chi tiết xe | Mixed |
| POST | `/api/v1/SearchingRentalService/GetSettingApp` | Cài đặt app | Public |
| POST | `/api/v1/SearchingRentalService/CheckBeforeUpdateBookingInfo` | Validate trước booking | Required |
| POST | `/api/v1/SearchingRentalService/UpdateBookingInfo` | Lưu booking info | Required |

### Đặt xe & Xử lý đơn

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/RentalService/Booking` | Tạo đơn đặt xe | Required |
| POST | `/api/v1/RentalService/OrderConfirm` | Owner xác nhận đơn | Required |
| POST | `/api/v1/RentalService/OrderConfirmHasPay` | Xác nhận + đã thanh toán | Required |
| POST | `/api/v1/RentalService/OrderPay` | Thanh toán đơn | Required |
| POST | `/api/v1/RentalService/OrderBegin` | Bắt đầu thuê (nhận xe) | Required |
| POST | `/api/v1/RentalService/OrderEnd` | Kết thúc thuê (trả xe) | Required |
| POST | `/api/v1/RentalService/OrderCancel` | Huỷ đơn | Required |
| POST | `/api/v1/RentalService/OrderReview` | Đánh giá sau chuyến | Required |

### Quản lý xe (Owner)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/RentalService/InsertNewRentalService` | Đăng xe mới | Required |
| POST | `/api/v1/RentalService/UpdateRentalServiceStatus` | Cập nhật trạng thái xe | Required |
| POST | `/api/v1/RentalService/GetVehicleCreationData` | Data tạo xe mới | Required |
| POST | `/api/v1/RentalService/GetVehicleInfo` | Thông tin xe | Required |
| POST | `/api/v1/RentalService/GetVehicleManagementSummary` | Tổng hợp quản lý xe | Required |
| POST | `/api/v1/RentalService/GetVehicleRentalPrice` | Giá thuê xe | Required |

### Lịch & Giá

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/ServiceItem_DateRentalPrice/List` | Danh sách giá theo ngày | Required |
| POST | `/api/v1/ServiceItem_WeekdayRentalPrice/List` | Giá theo thứ | Required |
| POST | `/api/v1/ServiceItem_DateBusyRentalSchedule/List` | Ngày bận | Required |
| POST | `/api/v1/ServiceItem_BookedRentalSchedule/List` | Ngày đã book | Required |

---

## Luồng nghiệp vụ chính

### Booking Flow

```
[Renter]                             [Sigo API]                        [Owner]
   │                                      │                               │
   ├─ SearchingRentalService/List ────────►│ Tìm xe theo:                 │
   │   { location, dateFrom, dateTo,      │ - Vị trí (lat/lng)           │
   │     vehicleType, priceRange }        │ - Thời gian trống            │
   │◄─ Danh sách xe ─────────────────────│ - Loại xe, giá               │
   │                                      │                               │
   ├─ SearchingRentalService/Detail ──────►│ Chi tiết xe:                 │
   │   { serviceItemId }                  │ - Ảnh, tính năng             │
   │◄─ Chi tiết + giá + lịch ────────────│ - Rating, reviews            │
   │                                      │                               │
   ├─ CheckBeforeUpdateBookingInfo ───────►│ Validate:                    │
   │   { serviceItemId, dates }           │ - Xe còn trống?              │
   │◄─ OK / Conflict ────────────────────│ - Đủ ngày tối thiểu?         │
   │                                      │                               │
   ├─ UpdateBookingInfo ──────────────────►│ Lưu draft booking            │
   │   { contactInfo, pickupAddress }     │ → RentalServiceTempData      │
   │◄─ TempData ID ──────────────────────│                               │
   │                                      │                               │
   ├─ RentalService/Booking ──────────────►│ Tạo Order:                   │
   │   { serviceItemId, tempDataId }      │ - Status: WaitingConfirm     │
   │◄─ OrderId ──────────────────────────│ - Block lịch xe              │
   │                                      │ ─── Push notification ──────►│
   │                                      │                               │
   │                                      │◄─ RentalService/OrderConfirm ─│
   │                                      │   Owner xác nhận đơn         │
   │◄─ Notification: Đã confirm ─────────│ - Status: Confirmed           │
   │                                      │                               │
   ├─ RentalService/OrderPay ─────────────►│ Thanh toán                   │
   │   { orderId, paymentMethod }         │ - Status: Paid               │
   │◄─ Payment success ──────────────────│                               │
```

### Begin/End Flow

```
[Renter + Owner]                 [Sigo API]
      │                               │
      ├─ OrderBegin ──────────────────►│ Nhận xe:
      │   { orderId,                   │ - Upload ảnh nhận xe
      │     receivingImages[] }        │ - Xác nhận tình trạng
      │◄─ OK ────────────────────────│ - Status: InProgress
      │                               │
      │   ... sử dụng xe ...          │
      │                               │
      ├─ OrderEnd ────────────────────►│ Trả xe:
      │   { orderId,                   │ - Upload ảnh trả xe
      │     returnImages[] }           │ - So sánh tình trạng
      │◄─ OK ────────────────────────│ - Status: Completed
      │                               │ - Trigger: TransferMoney → Owner
      │                               │ - Trigger: Rating reminder
```

---

## Service Interfaces

> File nguồn: `AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalService/Interfaces/`

### IRentalService (Service chính)

```csharp
public partial interface IRentalService : IRentalService_Order, IRentalService_Website, IRentalService_Detail, IRentalService_v2
{
    // Properties
    bool NeedGetOrderDetailAfterBooking { get; set; }

    // Tìm kiếm
    Task<(RentalServiceSearchingModel data, string error)> SearchRentalServiceAsync(RentalServiceParamModel param);
    RentalServicePublicModel GetPublicRentalServiceDetail(RentalServiceParamModel param, out string sMessage);

    // Booking
    CheckBeforeUpdateBookingInfoResultModel CheckBeforeUpdateBookingInfo(RentalServiceParamModel param, out string sMessage);
    RentalServiceBookingInfoModel UpdateBookingInfo(RentalServiceParamModel param, out string sMessage);
    object Booking(RentalServiceParamModel param, out string sMessage);

    // Quản lý xe
    RentalServiceModel InsertNewRentalService(RentalServiceParamModel param, out string sMessage);
    MyRentalServiceModel[] GetMyRentalService(RentalServiceParamModel param, out string sMessage);
    RentalServiceModel UpdateRentalServiceStatus(RentalServiceParamModel param, out string sMessage);

    // Voucher & Settings
    RentalServiceVoucherPublicModel[] SearchVouchers(RentalServiceParamModel param, out string sMessage);
    Task<EzyResultAsyncModel<Dictionary<string, object>>> GetSettingApp(RentalService_GetSettingModel param);

    // Cancel
    object GetCancelOrderInfo(RentalServiceParamModel param, out string sMessage);
}
```

### IRentalService_Order (Quản lý đơn hàng)

```csharp
public interface IRentalService_Order
{
    // Danh sách đơn
    AppScreenUIControlSettingOptionModel GetMyOrder(RentalServiceParamModel param, out string sMessage);
    AppScreenUIControlSettingOptionModel GetMyOrder_RentCar_RoleOwner(RentalServiceParamModel param, out string sMessage);
    AppScreenUIControlSettingOptionModel GetMyOrder_RentCar_RoleRenter(RentalServiceParamModel param, out string sMessage);

    // Lifecycle đơn hàng
    object OrderConfirm(RentalServiceParamModel param, out string sMessage);
    object OrderConfirmHasPay(RentalServiceParamModel param, out string sMessage);
    object OrderCancel(RentalServiceParamModel param, out string sMessage);
    object OrderPay(RentalServiceParamModel param, out string sMessage);
    object OrderBegin(RentalServiceParamModel param, out string sMessage);
    object OrderEnd(RentalServiceParamModel param, out string sMessage);
    object OrderReview(RentalServiceParamModel param, out string sMessage);

    // Chi tiết
    RentalServiceOrderInfoModel GetOrderDetail(RentalServiceParamModel param, out string sMessage);
    object GetInsuranceContact(RentalServiceParamModel param, out string sMessage);
}
```

### IOrderService (Order Service cấp thấp)

```csharp
public interface IOrderBaseService<TModel, TParam, TOption>
{
    // Confirm
    TModel OwnerConfirm(TParam param, out string sMessage);
    TModel RenterConfirmHasPay(TParam param, out string sMessage);

    // Cancel
    TModel OwnerCancel(TParam param, out string sMessage);
    TModel RenterCancel(TParam param, out string sMessage);
    string CancelTimeOverOrder(TParam param);  // Auto-cancel timeout
    object GetCancelOrderInfo(TParam param, out string sMessage);

    // Payment
    TModel RenterPay(TParam param, out string sMessage);

    // Trip
    TModel Begin(TParam param, out string sMessage);
    TModel End(TParam param, out string sMessage);

    // Review
    TModel Review(TParam param, out string sMessage);
}
```

### IRentalService_Detail (Quản lý chi tiết xe)

```csharp
// Các method lấy/cập nhật từng phần thông tin xe (dùng cho form đăng ký xe)
RentalServiceModel Submit2Approve(RentalServiceParamModel param, out string sMessage);
RentalServiceModel GetImage(RentalServiceParamModel param, out string sMessage);
RentalServiceModel GetAddress(RentalServiceParamModel param, out string sMessage);
RentalServiceModel GetFeature(RentalServiceParamModel param, out string sMessage);
RentalServiceModel GetDocument(RentalServiceParamModel param, out string sMessage);
RentalServiceModel GetDailyRentalPrice(RentalServiceParamModel param, out string sMessage);
RentalServiceModel GetSetting(RentalServiceParamModel param, out string sMessage);
RentalServiceModel GetCurrentWorkingAndBusyDate(RentalServiceParamModel param, out string sMessage);
RentalServiceModel GetMiniumRentalDayRequired(RentalServiceParamModel param, out string sMessage);
```

### IRentalService_v2 (Version 2 — Async)

```csharp
Task<MyRentalServiceGroupResultModel> GetMyRentalServiceGroups(RentalServiceParamModel param);
Task<VehicleCreationDataModel> GetVehicleCreationData(RentalServiceParamModel param);
Task<RentalServiceModel> GetVehicleTypeSub(RentalServiceParamModel param);
Task<RentalServiceModel> GetVehicleInfo(RentalServiceParamModel param);
Task<RentalServiceModel> Update_v2(RentalServiceModel model);
```

> **Pattern chung:**
> - Tất cả method đều nhận `RentalServiceParamModel` làm input
> - Output lỗi qua `out string sMessage` (sync) hoặc tuple `(data, error)` (async)
> - `UIAppName` property trên service để phân biệt "sigoweb" / "sigoapp_new"

---

## Hiệu năng

| Endpoint | Avg (ms) | Max (ms) | Calls/30d | Trạng thái |
|---------|----------|----------|-----------|-----------|
| `SearchingRentalService/List` | **1,781** | 335,733 | 26,785 | CRITICAL |
| `SearchingRentalService/UpdateBookingInfo` | **2,806** | 45,044 | 8,762 | CRITICAL |
| `RentalService/OrderConfirm` | **8,829** | — | — | CRITICAL |

### Nguyên nhân & Giải pháp

**`SearchingRentalService/List` — 1,781ms avg:**
- N+1 queries khi load related data (images, features, schedule)
- Thiếu index trên location-based queries
- Giải pháp: Eager loading, spatial index, Redis cache cho hot searches

**`RentalService/OrderConfirm` — 8,829ms avg:**
- Synchronous processing: validate + update status + calculate fees + send notification
- Giải pháp: Async notification, pre-calculate fees

Xem chi tiết: [../05_PERFORMANCE/db-optimization-notes.md](../05_PERFORMANCE/db-optimization-notes.md)

---

*Xem thêm: [order.md](./core-order.md) | [booking-flow.md](../04_BUSINESS_FLOWS/booking-flow.md) | [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md)*
