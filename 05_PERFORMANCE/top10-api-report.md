# Top 10 API — Tài liệu chi tiết

> **Nguồn dữ liệu:** `Sigo_Live_Log` · `dbo."Log_APIRequest"` · 30 ngày (27/01 – 25/02/2026)
> **Tổng records phân tích:** ~680,000 lượt gọi

---

## Tổng quan

| # | API Path | Tổng lượt | Avg (ms) | P95 (ms) | Max (ms) | Đánh giá |
|---|----------|-----------|----------|----------|----------|---------|
| 1 | [/api/v1/Account/GetOTP](#1-accountgetotp) | **460,229** | 1.75 | 1.91 | 877 | ✅ Tốt |
| 2 | [/api/v1/DashboardForWebsite/GetListDataWithSlug](#2-dashboardforwebsitegetlistdatawithslug) | 56,269 | 70.54 | 260 | 6,586 | ✅ Chấp nhận |
| 3 | [/api/v1/GlobalAppSetting/KeepSessionAlive](#3-globalappsettingkeepsessionalive) | 43,183 | 0.10 | 0.12 | 11 | ✅ Tốt |
| 4 | [/api/v1/NotificationSender/SendMessage](#4-notificationsendersendmessage) | 36,307 | 0.48 | 0.80 | 6 | ✅ Tốt |
| 5 | [/api/v1/SearchingRentalService/List](#5-searchingrentalservicelist) | 26,785 | 1,781 | 3,164 | 335,733 | 🔴 CHẬM |
| 6 | [/api/v1/SearchingRentalService/Detail](#6-searchingrentalservicedetail) | 21,341 | 1,982 | 4,018 | 30,261 | 🔴 CHẬM |
| 7 | [/api/v1/User/GetSetting](#7-usergetsetting) | 11,522 | 55.92 | 64 | 504 | ✅ Chấp nhận |
| 8 | [/api/v2/User/HomePage_App](#8-userhomepage_app-v2) | 11,415 | 40.57 | 18 | 14,877 | ✅ Chấp nhận |
| 9 | [/api/v1/SearchingRentalService/GetSettingApp](#9-searchingrentalservicegetsettingapp) | 10,441 | 21.81 | 33 | 1,049 | ✅ Tốt |
| 10 | [/api/v1/SearchingRentalService/CheckBeforeUpdateBookingInfo](#10-searchingrentalservicecheckbeforeupdatebookinginfo) | 9,578 | 784 | 2,669 | 15,033 | 🟡 Cần theo dõi |

---

## 1. Account/GetOTP

**Controller:** `AccountController` · `AppSystem/`
**Method:** `POST`
**Auth:** `[AllowAnonymous]`

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **460,229** — API bận nhất hệ thống |
| Avg | **1.75 ms** |
| P50 (median) | ~1.7 ms |
| P95 | 1.91 ms |
| Max | 877 ms |
| Min | 0.73 ms |
| Lỗi | ~0% |

> **Nhận xét:** Hiệu năng rất tốt. Avg cực thấp do API chỉ validate SĐT và enqueue SMS — không query DB phức tạp. Max 877ms là outlier hiếm gặp (có thể do cold start hoặc SMS gateway chậm).

### Mô tả chức năng

Gửi mã OTP 6 chữ số về số điện thoại của user. Dùng cho:
- Đăng ký tài khoản mới
- Đặt lại mật khẩu
- Xác minh số điện thoại

### Request

```http
POST /api/v1/Account/GetOTP
Content-Type: application/json
```

```json
{
  "MobilePhone": "0901234567"
}
```

| Field | Type | Bắt buộc | Validation |
|-------|------|----------|-----------|
| `MobilePhone` | string | Có | SĐT Việt Nam 10 số, bắt đầu bằng 0 |

### Response

**Thành công:**
```json
{
  "Status": 1,
  "Message": "Đã gửi mã OTP",
  "Data": null
}
```

**Thất bại:**
```json
{
  "Status": 0,
  "Message": "Số điện thoại không hợp lệ",
  "Data": null
}
```

### Lưu ý

- OTP có hiệu lực trong **5 phút**
- Số điện thoại bị blacklist sẽ bị từ chối (`GetOTPBlacklist` table)
- Log mỗi request vào `Log_GetOTP`
- Có rate limiting ngầm (cần xác nhận thêm)

---

## 2. DashboardForWebsite/GetListDataWithSlug

**Controller:** `DashboardForWebsiteController` · `Ezy.Module.CMS`
**Method:** `POST`
**Auth:** `[AllowAnonymous]` (public — dùng cho website frontend)

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **56,269** |
| Avg | **70.54 ms** |
| P50 | ~50 ms |
| P95 | 260 ms |
| Max | **6,586 ms** |
| Min | 0.59 ms |

> **Nhận xét:** Avg tốt nhưng Max 6.5s và P95 260ms cho thấy một số slug phức tạp mất nhiều thời gian hơn. Nên thêm cache Redis cho các slug được gọi nhiều.

### Mô tả chức năng

Lấy nội dung trang web theo URL slug. Đây là API backbone cho toàn bộ **website frontend** (sigo.vn) — trang chủ, danh sách xe, blog, landing page đều gọi API này.

### Request

```http
POST /api/v1/DashboardForWebsite/GetListDataWithSlug
Content-Type: application/json
```

```json
{
  "Slug": "thue-xe-ha-noi",
  "PageIndex": 1,
  "PageSize": 20
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| `Slug` | string | Có | URL slug của trang |
| `PageIndex` | int | Không | Phân trang (default: 1) |
| `PageSize` | int | Không | Số item/trang (default: 20) |

### Response

```json
{
  "Status": 1,
  "Data": {
    "SlugType": "ServiceItem",
    "Title": "Thuê xe Hà Nội",
    "Items": [ ],
    "TotalRecord": 0
  }
}
```

### Luồng xử lý

```
Slug nhận vào
    │
    ▼
Lookup EzyWeb_UrlRecord (slug → EntityId + EntityType)
    │
    ├── ServiceItem → SearchingRentalService data
    ├── BlogPost    → CMS blog content
    ├── NewsPost    → CMS news content
    └── Topic       → Topic + related items
    │
    ▼
Trả dữ liệu tương ứng
```

### Tối ưu đề xuất
- [ ] Cache Redis cho slug phổ biến (TTL 5-10 phút)
- [ ] Add index trên `EzyWeb_UrlRecord.Slug`

---

## 3. GlobalAppSetting/KeepSessionAlive

**Controller:** `GlobalAppSettingController` · `AppSystem/`
**Method:** `GET`
**Auth:** `[Authorize]`

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **43,183** |
| Avg | **0.10 ms** |
| P95 | 0.12 ms |
| Max | 10.51 ms |
| Min | 0.06 ms |

> **Nhận xét:** API nhẹ nhất hệ thống, chỉ làm nhiệm vụ ping để giữ JWT session.

### Mô tả chức năng

Ping endpoint để giữ session người dùng không bị timeout. Mobile app gọi định kỳ (mỗi vài phút khi app đang mở) để đảm bảo JWT token được refresh.

### Request

```http
GET /api/v1/GlobalAppSetting/KeepSessionAlive
Authorization: Bearer {token}
```

### Response

```json
{
  "Status": 1,
  "Message": "OK",
  "Data": null
}
```

### Lưu ý

- Không có logic nghiệp vụ — chỉ xác nhận token hợp lệ
- Nếu token hết hạn → 401 → app redirect về login
- Được gọi từ mobile app theo interval ~3-5 phút

---

## 4. NotificationSender/SendMessage

**Controller:** `NotificationSenderController` · `AppSystem/`
**Method:** `POST`
**Auth:** `[Authorize]`

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **36,307** |
| Avg | **0.48 ms** |
| P95 | 0.80 ms |
| Max | 5.61 ms |
| Min | 0.19 ms |

> **Nhận xét:** Hiệu năng xuất sắc nhờ **async queue pattern** — API chỉ enqueue message, không gửi thực tế. `NotificationBatchJobEngine` xử lý delivery ở background.

### Mô tả chức năng

Enqueue một push notification để gửi đến user. Không gửi trực tiếp mà đẩy vào hàng chờ để `NotificationBatchJobEngine` xử lý async.

### Request

```http
POST /api/v1/NotificationSender/SendMessage
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "RecipientUserId": "guid-of-user",
  "Title": "Đơn hàng được xác nhận",
  "Body": "Chủ xe đã xác nhận đơn #12345 của bạn",
  "NotificationType": "ORDER_CONFIRMED",
  "RelatedEntityId": "order-guid",
  "Data": {}
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| `RecipientUserId` | Guid | Có | User nhận notification |
| `Title` | string | Có | Tiêu đề |
| `Body` | string | Có | Nội dung |
| `NotificationType` | string | Không | Loại notification (dùng để navigate trong app) |
| `RelatedEntityId` | Guid | Không | ID entity liên quan |

### Response

```json
{
  "Status": 1,
  "Message": "Đã enqueue",
  "Data": null
}
```

### Luồng xử lý

```
POST /SendMessage
    │ Enqueue → NotificationMessage table
    │ Return 200 ngay lập tức (avg 0.48ms)
    │
    ▼ (background)
NotificationBatchJobEngine
    ├── SignalR push → connected clients
    └── FCM/APNs → device push notification
```

---

## 5. SearchingRentalService/List

**Controller:** `SearchingRentalServiceController` · `MainBusiness/Sigo/`
**Method:** `POST`
**Auth:** `[AllowAnonymous]`

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **26,785** |
| Avg | **1,781 ms** 🔴 |
| P50 | ~900 ms |
| P95 | **3,164 ms** 🔴 |
| P99 | ~8,000 ms |
| Max | **335,733 ms** 🔴🔴 |
| Min | 1.32 ms |

> **Nhận xét:** API quan trọng nhất về mặt nghiệp vụ nhưng là **bottleneck lớn nhất** hệ thống. Max 335 giây là hoàn toàn không chấp nhận được. Cần tối ưu khẩn.

### Mô tả chức năng

Tìm kiếm danh sách xe cho thuê theo điều kiện — **trang tìm kiếm chính** của app và website.

### Request

```http
POST /api/v1/SearchingRentalService/List
Content-Type: application/json
```

```json
{
  "FromDate": "2026-03-01T08:00:00",
  "ToDate": "2026-03-03T20:00:00",
  "Address": "Hà Nội",
  "Latitude": 21.0278,
  "Longitude": 105.8342,
  "VehicleTypeId": null,
  "NumberOfSeat": null,
  "TransmissionTypeId": null,
  "FuelTypeId": null,
  "MinPrice": null,
  "MaxPrice": null,
  "SortBy": "Price",
  "SortOrder": "ASC",
  "PageIndex": 1,
  "PageSize": 20
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| `FromDate` | datetime | Có | Ngày giờ nhận xe |
| `ToDate` | datetime | Có | Ngày giờ trả xe |
| `Address` | string | Không | Địa chỉ tìm kiếm |
| `Latitude` | decimal | Không | Toạ độ — dùng để tính khoảng cách |
| `Longitude` | decimal | Không | Toạ độ |
| `VehicleTypeId` | int? | Không | Lọc loại xe |
| `NumberOfSeat` | int? | Không | Lọc số chỗ |
| `TransmissionTypeId` | int? | Không | Số tự động / số sàn |
| `FuelTypeId` | int? | Không | Loại nhiên liệu |
| `MinPrice` | decimal? | Không | Giá tối thiểu/ngày |
| `MaxPrice` | decimal? | Không | Giá tối đa/ngày |
| `SortBy` | string | Không | `Price` / `Distance` / `Rating` / `ViewCount` |
| `SortOrder` | string | Không | `ASC` / `DESC` |
| `PageIndex` | int | Không | Default: 1 |
| `PageSize` | int | Không | Default: 20, max: 50 |

### Response

```json
{
  "Status": 1,
  "Data": {
    "TotalRecord": 150,
    "PageIndex": 1,
    "PageSize": 20,
    "Items": [
      {
        "ServiceItemId": "guid",
        "VehicleName": "Toyota Camry 2022",
        "PricePerDay": 850000,
        "Rating": 4.8,
        "TotalTrip": 45,
        "Address": "Cầu Giấy, Hà Nội",
        "Distance": 2.3,
        "Images": [],
        "Features": []
      }
    ]
  }
}
```

### Vấn đề hiệu năng & Hướng tối ưu

**Nguyên nhân chậm (cần điều tra):**
- Query tính khoảng cách địa lý cho toàn bộ xe đủ điều kiện
- JOIN nhiều bảng: `RentalServiceItem`, `ServiceItem_BookedRentalSchedule`, `ServiceItem_DateBusyRentalSchedule`, `Vehicle`, `ServiceItem_Image`...
- Tính giá động theo `ServiceItem_DateRentalPrice` và `ServiceItem_WeekdayRentalPrice`
- Không có index spatial cho toạ độ

**Tối ưu đề xuất:**
```
[ ] 1. Thêm spatial index (geography) cho cột toạ độ
[ ] 2. Materialize kết quả tính giá vào bảng cache
[ ] 3. Pre-compute "xe còn trống" theo khoảng ngày (scheduled job)
[ ] 4. Redis cache kết quả tìm kiếm phổ biến (TTL 2 phút)
[ ] 5. Pagination sớm hơn — giới hạn tập xe trước khi JOIN bảng chi tiết
[ ] 6. Điều tra query execution plan trên production
```

---

## 6. SearchingRentalService/Detail

**Controller:** `SearchingRentalServiceController` · `MainBusiness/Sigo/`
**Method:** `POST`
**Auth:** `[AllowAnonymous]`

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **21,341** |
| Avg | **1,982 ms** 🔴 |
| P95 | **4,018 ms** 🔴 |
| Max | **30,261 ms** 🔴 |
| Min | 0.74 ms |

### Mô tả chức năng

Lấy toàn bộ thông tin chi tiết của một xe cho thuê: thông tin xe, giá, lịch bận, ảnh, tính năng, đánh giá.

### Request

```http
POST /api/v1/SearchingRentalService/Detail
Content-Type: application/json
```

```json
{
  "ServiceItemId": "guid-of-service-item",
  "FromDate": "2026-03-01T08:00:00",
  "ToDate": "2026-03-03T20:00:00"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| `ServiceItemId` | Guid | Có | ID xe cần xem |
| `FromDate` | datetime | Không | Nếu có → tính giá cho khoảng ngày này |
| `ToDate` | datetime | Không | |

### Response

```json
{
  "Status": 1,
  "Data": {
    "ServiceItemId": "guid",
    "VehicleName": "Toyota Camry 2022",
    "Description": "...",
    "PricePerDay": 850000,
    "PriceForSelectedDates": 1700000,
    "Rating": 4.8,
    "TotalTrip": 45,
    "BusyDates": ["2026-03-05", "2026-03-06"],
    "Images": [],
    "Features": [],
    "Documents": [],
    "Owner": {},
    "Reviews": []
  }
}
```

### Vấn đề hiệu năng

Tương tự API `/List` — cần JOIN nhiều bảng để build response đầy đủ. Min 0.74ms cho thấy có cache hit nhưng avg 1.9s là quá cao cho trang detail.

**Tối ưu đề xuất:**
```
[ ] 1. Cache detail theo ServiceItemId (TTL 5 phút, invalidate khi xe update)
[ ] 2. Lazy load Reviews (không cần thiết ở lần render đầu)
[ ] 3. Tách endpoint riêng cho BusyDates (chỉ fetch khi user chọn ngày)
```

---

## 7. User/GetSetting

**Controller:** `UserController` · `MainBusiness/`
**Method:** `GET`
**Auth:** `[Authorize]`

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **11,522** |
| Avg | **55.92 ms** |
| P95 | 64 ms |
| Max | 504 ms |
| Min | **32.52 ms** |

> **Nhận xét:** Min 32ms khá cao cho một setting API — có thể do query không dùng cache. Cần xem xét cache user settings.

### Mô tả chức năng

Lấy cài đặt cá nhân của user đang đăng nhập: ngôn ngữ, thông báo, quyền riêng tư, preferences.

### Request

```http
GET /api/v1/User/GetSetting
Authorization: Bearer {token}
```

### Response

```json
{
  "Status": 1,
  "Data": {
    "Language": "vi",
    "PushNotification": true,
    "EmailNotification": false,
    "ShowPhoneNumber": true,
    "Currency": "VND"
  }
}
```

### Tối ưu đề xuất
- [ ] Cache setting theo UserId (TTL 10 phút, invalidate khi user update setting)

---

## 8. User/HomePage_App (v2)

**Controller:** `UserController` · `MainBusiness/`
**Method:** `POST`
**Auth:** `[AllowAnonymous]` *(bypass JWT error — xem [overview.md](../01_ARCHITECTURE/overview.md))*

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **11,415** |
| Avg | **40.57 ms** |
| P95 | 18 ms |
| Max | **14,877 ms** |
| Min | 7.90 ms |

> **Nhận xét:** P95 18ms < Avg 40ms — nghĩa là phần lớn request nhanh, nhưng có một số ít outlier rất chậm kéo avg lên. Max 14.8s cần điều tra.

### Mô tả chức năng

Lấy dữ liệu trang chủ app: banner slides, xe đề xuất, cấu hình app, thông báo hệ thống. Được gọi mỗi lần user mở app.

### Request

```http
POST /api/v2/User/HomePage_App
Content-Type: application/json
Authorization: Bearer {token}  (tùy chọn)
```

```json
{
  "Latitude": 21.0278,
  "Longitude": 105.8342,
  "AppVersion": "2.5.0",
  "Platform": "iOS"
}
```

### Response

```json
{
  "Status": 1,
  "Data": {
    "Banners": [],
    "FeaturedCars": [],
    "AppConfig": {
      "ForceUpdate": false,
      "MinVersion": "2.0.0",
      "MaintenanceMode": false
    },
    "SystemMessages": []
  }
}
```

---

## 9. SearchingRentalService/GetSettingApp

**Controller:** `SearchingRentalServiceController` · `MainBusiness/Sigo/`
**Method:** `GET`
**Auth:** `[AllowAnonymous]`

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **10,441** |
| Avg | **21.81 ms** |
| P95 | 33 ms |
| Max | 1,049 ms |
| Min | 0.94 ms |

> **Nhận xét:** Hiệu năng tốt. Cân nhắc cache mạnh hơn vì data ít thay đổi.

### Mô tả chức năng

Lấy cấu hình cho tính năng tìm kiếm xe: filter options, sort options, giá trị mặc định, cấu hình bản đồ. Được gọi khi user vào màn hình tìm kiếm.

### Request

```http
GET /api/v1/SearchingRentalService/GetSettingApp
```

### Response

```json
{
  "Status": 1,
  "Data": {
    "VehicleTypes": [],
    "TransmissionTypes": [],
    "FuelTypes": [],
    "SeatOptions": [4, 5, 7, 9, 16],
    "DefaultSearchRadius": 10,
    "MaxSearchRadius": 50,
    "SortOptions": ["Price", "Distance", "Rating"]
  }
}
```

### Tối ưu đề xuất
- [ ] Cache với TTL 30 phút (data thay đổi rất ít)

---

## 10. SearchingRentalService/CheckBeforeUpdateBookingInfo

**Controller:** `SearchingRentalServiceController` · `MainBusiness/Sigo/`
**Method:** `POST`
**Auth:** `[Authorize]`

### Thống kê hiệu năng (30 ngày)

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi | **9,578** |
| Avg | **784 ms** 🟡 |
| P95 | **2,669 ms** 🔴 |
| Max | **15,033 ms** 🔴 |
| Min | 9.20 ms |

> **Nhận xét:** API này được gọi ngay trước bước booking — P95 2.7s tạo ra trải nghiệm không tốt ngay lúc user sắp đặt xe. Cần tối ưu để giảm xuống < 500ms.

### Mô tả chức năng

Kiểm tra toàn bộ điều kiện trước khi cho phép user cập nhật thông tin booking:
- Xe còn trống trong khoảng ngày đã chọn không?
- Giá có thay đổi không?
- User có đủ điều kiện thuê không (CCCD, GPLX)?
- Promotion code còn hiệu lực không?

### Request

```http
POST /api/v1/SearchingRentalService/CheckBeforeUpdateBookingInfo
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "ServiceItemId": "guid",
  "FromDate": "2026-03-01T08:00:00",
  "ToDate": "2026-03-03T20:00:00",
  "DiscountCode": "SUMMER2026",
  "DeliveryAddress": "123 Cầu Giấy, Hà Nội"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| `ServiceItemId` | Guid | Có | Xe cần đặt |
| `FromDate` | datetime | Có | Ngày nhận xe |
| `ToDate` | datetime | Có | Ngày trả xe |
| `DiscountCode` | string | Không | Mã giảm giá |
| `DeliveryAddress` | string | Không | Địa chỉ giao xe (nếu có) |

### Response

```json
{
  "Status": 1,
  "Data": {
    "IsAvailable": true,
    "PriceDetail": {
      "BasePrice": 1700000,
      "DiscountAmount": 170000,
      "DeliveryFee": 0,
      "TotalPrice": 1530000
    },
    "Warnings": [],
    "RequiredDocuments": ["CCCD", "GPLX"]
  }
}
```

### Vấn đề hiệu năng

API phải thực hiện nhiều check song song:
1. Lock-free check lịch bận
2. Tính giá động (có thể gọi nhiều bảng giá)
3. Validate discount code
4. Check user verification status

**Tối ưu đề xuất:**
```
[ ] 1. Chạy các check song song (Task.WhenAll) thay vì tuần tự
[ ] 2. Cache trạng thái verification của user (ít thay đổi)
[ ] 3. Cache discount code validation (TTL 1 phút)
[ ] 4. Tách endpoint tính giá riêng nếu cần call nhiều lần
```

---

## Phân tích tổng hợp

### Phân loại theo hiệu năng

```
✅ Tốt (avg < 100ms)          🟡 Cần cải thiện (100ms–1s)      🔴 Cần tối ưu ngay (> 1s)
─────────────────────          ────────────────────────────      ──────────────────────────
GetOTP           1.75ms        CheckBeforeUpdateBooking 784ms    SearchingRentalService/List    1,781ms
KeepSessionAlive 0.10ms        User/GetSetting          55ms     SearchingRentalService/Detail  1,982ms
SendMessage      0.48ms        HomePage_App             40ms
GetSettingApp    21.81ms       GetListDataWithSlug      70ms
```

### Tải trọng theo giờ trong ngày (7 ngày gần nhất)

| Khung giờ | Mức tải | Avg response |
|-----------|---------|-------------|
| 00h–01h | Thấp | 178ms |
| 01h–02h | **Cao nhất** (12,098 req) | 78ms |
| 08h–09h | Cao | 182ms |
| 11h–16h | Trung bình | ~300ms — chậm nhất ban ngày |
| 17h–20h | Thấp dần | 40–120ms |

> **Điểm bất thường:** 1h sáng có lượt gọi cao nhất (~12K/7 ngày) — nhiều khả năng do **batch job / automated process** chứ không phải user thực. Cần xác nhận.

### Khuyến nghị ưu tiên

| Ưu tiên | Hành động | API ảnh hưởng | Impact |
|---------|-----------|--------------|--------|
| 🔴 Cao | Điều tra & tối ưu query tìm kiếm xe | `/List`, `/Detail` | 48,000+ lượt/30 ngày |
| 🔴 Cao | Thêm cache Redis cho search results | `/List`, `/Detail`, `/GetListDataWithSlug` | Giảm load DB ~50% |
| 🟡 Trung bình | Chạy check song song (Task.WhenAll) | `/CheckBeforeUpdateBookingInfo` | P95: 2.7s → <500ms |
| 🟡 Trung bình | Cache user settings & app config | `/GetSetting`, `/GetSettingApp` | Giảm DB read |
| 🟢 Thấp | Điều tra outlier 1h sáng | Tất cả | Hiểu traffic pattern |

---

*Xem thêm: [API_Performance_Report.html](./API_Performance_Report.html) | [db-optimization-notes.md](./db-optimization-notes.md)*
*Controllers liên quan: [rental-service.md](../03_API/rental-service.md) | [auth.md](../03_API/auth.md) | [user.md](../03_API/user.md)*
