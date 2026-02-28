# API: Service Item

> **Controllers:** `ServiceItem_DateRentalPriceController`, `ServiceItem_DateBusyRentalScheduleController`, `ServiceItem_WeekdayRentalPriceController`, `ServiceItem_WeekdaysBusyRentalScheduleController`, `ServiceItem_ImageController`, `ServiceItem_DocumentController`, `ServiceItem_Feature_MappingController`, `ServiceItem_MinimumRentalDayRequiredController`
> **Base:** `/api/v1/ServiceItem_*`
> **Phân quyền:** `[Authorize]` (tất cả endpoint)

## Mục lục
- [Tổng quan](#tổng-quan)
- [Luồng gọi API](#luồng-gọi-api)
- [Giá theo ngày đặc biệt](#giá-theo-ngày-đặc-biệt)
- [Lịch bận theo ngày](#lịch-bận-theo-ngày)
- [Giá theo thứ trong tuần](#giá-theo-thứ-trong-tuần)
- [Lịch bận theo thứ](#lịch-bận-theo-thứ)
- [Ảnh dịch vụ](#ảnh-dịch-vụ)
- [Tài liệu xe](#tài-liệu-xe)
- [Tính năng xe](#tính-năng-xe)
- [Yêu cầu số ngày tối thiểu](#yêu-cầu-số-ngày-tối-thiểu)

---

## Tổng quan

**ServiceItem** là đơn vị cho thuê trong hệ thống — mỗi xe đăng ký cho thuê có một ServiceItem tương ứng. Các controller ở đây quản lý **cấu hình chi tiết** của dịch vụ cho thuê:

| Controller | Chức năng | Ảnh hưởng tới |
|-----------|-----------|--------------|
| `DateRentalPrice` | Giá đặc biệt theo ngày cụ thể (lễ, tết) | Giá hiển thị khi search |
| `DateBusyRentalSchedule` | Lịch bận theo ngày | Khung ngày không thể đặt |
| `WeekdayRentalPrice` | Giá theo thứ (T2-CN) | Giá cơ bản mỗi ngày |
| `WeekdaysBusyRentalSchedule` | Lịch bận theo thứ (nghỉ cố định) | Ngày không nhận đặt hàng tuần |
| `Image` | Ảnh xe | Hiển thị trên trang chi tiết |
| `Document` | Tài liệu pháp lý xe | Yêu cầu giấy tờ khi đặt |
| `Feature_Mapping` | Tính năng xe (GPS, camera...) | Filter tìm kiếm |
| `MinimumRentalDayRequired` | Số ngày thuê tối thiểu | Validation khi đặt |

---

## Luồng gọi API

### Luồng 1: Chủ xe cấu hình xe mới (Owner setup)

```
Bước 1: POST /api/v1/ServiceItem_WeekdayRentalPrice_ListView/UpdateWeekdayRentalPrice
        → Mục đích: Thiết lập giá cơ bản theo từng thứ trong tuần
        → Input: ServiceItemId + bảng giá 7 ngày (T2-CN)

Bước 2: POST /api/v1/ServiceItem_DateBusyRentalSchedule_ListView/CreateMultiDay
        → Mục đích: Đánh dấu các ngày xe không thể đặt (đã có lịch riêng)
        → Input: ServiceItemId + danh sách ngày bận

Bước 3: POST /api/v1/ServiceItem_Image_ListView/CreateThumbnail  [nếu cần]
        → Mục đích: Tạo thumbnail từ ảnh đã upload
        → Input: ServiceItemId + ImageId

Bước 4: POST /api/v1/ServiceItem_Feature_Mapping_ListView/List
        → Mục đích: Xem tính năng hiện tại của xe, cập nhật mapping
```

### Luồng 2: Cấu hình giá ngày lễ/tết

```
Bước 1: POST /api/v1/ServiceItem_WeekdayRentalPrice/List
        → Mục đích: Xem giá hiện tại theo thứ để làm baseline

Bước 2: POST /api/v1/ServiceItem_DateRentalPrice_ListView/CreateMultiDay
        → Mục đích: Tạo giá đặc biệt cho nhiều ngày liên tiếp (VD: 30/04-02/05)
        → Input: ServiceItemId + DateFrom + DateTo + SpecialPrice

Bước 3: POST /api/v1/ServiceItem_DateRentalPrice_ListView/List
        → Mục đích: Kiểm tra lại giá đã cấu hình
```

### Luồng 3: Admin kiểm duyệt thông tin xe

```
Bước 1: POST /api/v1/ServiceItem_Image_ListView/List
        → Mục đích: Xem ảnh xe đã upload

Bước 2: POST /api/v1/ServiceItem_Document_ListView/List
        → Mục đích: Xem tài liệu pháp lý (đăng ký xe, bảo hiểm)

Bước 3: POST /api/v1/ServiceItem_Feature_Mapping_ListView/List
        → Mục đích: Xem tính năng xe đã khai báo
```

---

## Giá theo ngày đặc biệt

### `POST /api/v1/ServiceItem_DateRentalPrice/List`

**Mô tả:** Lấy danh sách giá đặc biệt theo ngày cụ thể (lễ, tết, sự kiện).

### Request Body — `ServiceItem_DateRentalPriceParamModel`
```json
{
  "ServiceItemId": "service-item-guid",
  "FromDate": "2026-04-30",
  "ToDate": "2026-05-02",
  "PageIndex": 1,
  "PageSize": 50
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_DateRentalPriceModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 3,
    "Data": [
      {
        "DateRentalPriceId": "price-guid",
        "ServiceItemId": "service-item-guid",
        "Date": "2026-04-30",
        "Price": 1200000,
        "Price_Text": "1.200.000đ/ngày",
        "Note": "Giá lễ 30/4"
      }
    ]
  }
}
```

---

### `POST /api/v1/ServiceItem_DateRentalPrice_ListView/List`

**Mô tả:** Lấy danh sách giá theo ngày dạng list view (có thêm thông tin tổng hợp).

---

### `POST /api/v1/ServiceItem_DateRentalPrice_ListView/CreateMultiDay`

**Mô tả:** Tạo giá đặc biệt cho nhiều ngày liên tiếp cùng lúc.

**Vai trò trong luồng:** Bước 2 trong [Luồng 2](#luồng-2-cấu-hình-giá-ngày-lễtết)

### Request Body — `ServiceItem_DateRentalPrice_ListViewParamModel`
```json
{
  "ServiceItemId": "service-item-guid",
  "FromDate": "2026-04-30",
  "ToDate": "2026-05-02",
  "Price": 1200000
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Tạo thành công 3 ngày giá đặc biệt"
}
```

---

## Lịch bận theo ngày

### `POST /api/v1/ServiceItem_DateBusyRentalSchedule/List`

**Mô tả:** Lấy danh sách các ngày xe đã được đánh dấu bận (không nhận đặt).

**Hiệu năng:** 56 lượt/30 ngày | avg **2,634ms** 🔴

### Request Body — `ServiceItem_DateBusyRentalScheduleParamModel`
```json
{
  "ServiceItemId": "service-item-guid",
  "FromDate": "2026-03-01",
  "ToDate": "2026-03-31"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_DateBusyRentalScheduleModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 5,
    "Data": [
      {
        "BusyScheduleId": "busy-guid",
        "ServiceItemId": "service-item-guid",
        "BusyDate": "2026-03-15",
        "Reason": "Bảo dưỡng định kỳ"
      }
    ]
  }
}
```

---

### `POST /api/v1/ServiceItem_DateBusyRentalSchedule_ListView/CreateMultiDay`

**Mô tả:** Đánh dấu nhiều ngày bận liên tiếp cùng lúc.

**Vai trò trong luồng:** Bước 2 trong [Luồng 1](#luồng-1-chủ-xe-cấu-hình-xe-mới-owner-setup)

### Request Body — `ServiceItem_DateBusyRentalSchedule_ListViewParamModel`
```json
{
  "ServiceItemId": "service-item-guid",
  "FromDate": "2026-03-15",
  "ToDate": "2026-03-17",
  "Reason": "Bảo dưỡng"
}
```

---

## Giá theo thứ trong tuần

### `POST /api/v1/ServiceItem_WeekdayRentalPrice/List`

**Mô tả:** Lấy bảng giá theo thứ trong tuần (T2-CN) của xe.

**Vai trò trong luồng:** Bước 1 trong [Luồng 2](#luồng-2-cấu-hình-giá-ngày-lễtết)

### Request Body — `ServiceItem_WeekdayRentalPriceParamModel`
```json
{
  "ServiceItemId": "service-item-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_WeekdayRentalPriceModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 7,
    "Data": [
      { "Weekday": "Monday",    "WeekdayName": "Thứ 2", "Price": 700000, "Price_Text": "700.000đ" },
      { "Weekday": "Tuesday",   "WeekdayName": "Thứ 3", "Price": 700000, "Price_Text": "700.000đ" },
      { "Weekday": "Wednesday", "WeekdayName": "Thứ 4", "Price": 700000, "Price_Text": "700.000đ" },
      { "Weekday": "Thursday",  "WeekdayName": "Thứ 5", "Price": 700000, "Price_Text": "700.000đ" },
      { "Weekday": "Friday",    "WeekdayName": "Thứ 6", "Price": 800000, "Price_Text": "800.000đ" },
      { "Weekday": "Saturday",  "WeekdayName": "Thứ 7", "Price": 900000, "Price_Text": "900.000đ" },
      { "Weekday": "Sunday",    "WeekdayName": "Chủ nhật", "Price": 900000, "Price_Text": "900.000đ" }
    ]
  }
}
```

---

### `POST /api/v1/ServiceItem_WeekdayRentalPrice/GetNewRentalPriceInfo`

**Mô tả:** Tính giá thuê mới dựa trên cấu hình theo thứ (preview trước khi lưu).

### Response — `EzyResultObject<ServiceItem_WeekdayRentalPriceDetailModel>`

---

### `POST /api/v1/ServiceItem_WeekdayRentalPrice_ListView/UpdateWeekdayRentalPrice`

**Mô tả:** Cập nhật bảng giá theo thứ — cập nhật tất cả 7 ngày cùng lúc.

**Vai trò trong luồng:** Bước 1 trong [Luồng 1](#luồng-1-chủ-xe-cấu-hình-xe-mới-owner-setup)

### Request Body — `ServiceItem_WeekdayRentalPrice_ListViewParamModel`
```json
{
  "ServiceItemId": "service-item-guid",
  "Prices": [
    { "Weekday": "Monday",    "Price": 700000 },
    { "Weekday": "Tuesday",   "Price": 700000 },
    { "Weekday": "Wednesday", "Price": 700000 },
    { "Weekday": "Thursday",  "Price": 700000 },
    { "Weekday": "Friday",    "Price": 800000 },
    { "Weekday": "Saturday",  "Price": 900000 },
    { "Weekday": "Sunday",    "Price": 900000 }
  ]
}
```

### Response — `EzyResultObject<string>`

---

## Lịch bận theo thứ

### `POST /api/v1/ServiceItem_WeekdaysBusyRentalSchedule/List`

**Mô tả:** Lấy lịch bận cố định theo thứ (VD: xe chỉ nhận từ T2-T6, không nhận T7-CN).

### Request Body — `ServiceItem_WeekdaysBusyRentalScheduleParamModel`
```json
{
  "ServiceItemId": "service-item-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_WeekdaysBusyRentalScheduleModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      { "Weekday": "Saturday", "IsBusy": true },
      { "Weekday": "Sunday",   "IsBusy": true }
    ]
  }
}
```

---

## Ảnh dịch vụ

### `POST /api/v1/ServiceItem_Image/List`

**Mô tả:** Lấy danh sách ảnh của xe.

### Request Body — `ServiceItem_ImageParamModel`
```json
{
  "ServiceItemId": "service-item-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_ImageModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 8,
    "Data": [
      {
        "ImageId": "img-guid",
        "ServiceItemId": "service-item-guid",
        "ImageUrl": "https://...",
        "ThumbnailUrl": "https://...",
        "IsAvatar": true,
        "SortOrder": 1
      }
    ]
  }
}
```

---

### `POST /api/v1/ServiceItem_Image_ListView/CreateThumbnail`

**Mô tả:** Tạo thumbnail từ ảnh gốc đã upload (async).

**Vai trò trong luồng:** Bước 3 trong [Luồng 1](#luồng-1-chủ-xe-cấu-hình-xe-mới-owner-setup)

### Request Body — `ServiceItem_Image_ListViewParamModel`
```json
{
  "ServiceItemId": "service-item-guid",
  "ImageId": "img-guid"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Thumbnail đang được tạo"
}
```

---

## Tài liệu xe

### `POST /api/v1/ServiceItem_Document/List`

**Mô tả:** Lấy danh sách tài liệu pháp lý của xe (đăng ký xe, bảo hiểm, đăng kiểm...).

**Vai trò trong luồng:** Bước 2 trong [Luồng 3](#luồng-3-admin-kiểm-duyệt-thông-tin-xe)

### Request Body — `ServiceItem_DocumentParamModel`
```json
{
  "ServiceItemId": "service-item-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_DocumentModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 3,
    "Data": [
      {
        "DocumentId": "doc-guid",
        "ServiceItemId": "service-item-guid",
        "DocumentType": "VEHICLE_REGISTRATION",
        "DocumentName": "Đăng ký xe",
        "FileUrl": "https://...",
        "ExpiryDate": "2027-01-15",
        "IsVerified": true
      }
    ]
  }
}
```

---

## Tính năng xe

### `POST /api/v1/ServiceItem_Feature_Mapping/List`

**Mô tả:** Lấy danh sách tính năng đã mapping cho xe (GPS, camera hành trình, ghế trẻ em...).

**Vai trò trong luồng:** Bước 4 trong [Luồng 1](#luồng-1-chủ-xe-cấu-hình-xe-mới-owner-setup), Bước 3 trong [Luồng 3](#luồng-3-admin-kiểm-duyệt-thông-tin-xe)

### Request Body — `ServiceItem_Feature_MappingParamModel`
```json
{
  "ServiceItemId": "service-item-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_Feature_MappingModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 4,
    "Data": [
      {
        "FeatureMappingId": "feat-guid",
        "ServiceItemId": "service-item-guid",
        "FeatureId": "GPS",
        "FeatureName": "GPS",
        "IconUrl": "https://..."
      },
      {
        "FeatureId": "CAMERA",
        "FeatureName": "Camera hành trình"
      }
    ]
  }
}
```

> **Ghi chú:** Danh sách tính năng cấu hình tại `ConfigVehicleFeature` (xem [categories.md](./categories.md)).
> Tính năng ảnh hưởng tới filter `FeatureIds` trong `SearchingRentalService/List`.

---

## Yêu cầu số ngày tối thiểu

### `POST /api/v1/ServiceItem_MinimumRentalDayRequired/List`

**Mô tả:** Lấy cấu hình số ngày thuê tối thiểu của xe.

### Request Body — `ServiceItem_MinimumRentalDayRequiredParamModel`
```json
{
  "ServiceItemId": "service-item-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_MinimumRentalDayRequiredModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      {
        "ServiceItemId": "service-item-guid",
        "MinimumDays": 2,
        "Note": "Tối thiểu 2 ngày"
      }
    ]
  }
}
```

> **Ghi chú:** Validation này được áp dụng khi user đặt xe — nếu `ToDate - FromDate < MinimumDays` → báo lỗi.

---

## Lịch đã đặt (Booked Schedule)

### `POST /api/v1/ServiceItem_BookedRentalSchedule/List`

**Mô tả:** Lấy lịch đã được đặt của xe (các khoảng thời gian bị block bởi order đang active).

**Phân quyền:** `[Authorize]`

### Request Body — `ServiceItem_BookedRentalScheduleParamModel`
```json
{
  "RentalServiceItemId": 100
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_BookedRentalScheduleModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      {
        "RentalServiceItemId": 100,
        "FromDate": 1740787200,
        "ToDate": 1741046400,
        "IsBooked": true
      }
    ]
  }
}
```

> **Ghi chú:** Dữ liệu này kết hợp với `DateBusyRentalSchedule` và `WeekdaysBusyRentalSchedule` để xác định toàn bộ lịch bận.

---

## Yêu cầu giấy tờ cho thuê

### `POST /api/v1/ServiceItem_RentalRequiredItem_Mapping/List`

**Mô tả:** Danh sách giấy tờ yêu cầu khi thuê xe (CMND, GPLX, Hộ khẩu...).

**Phân quyền:** `[Authorize]`

### Request Body
```json
{
  "RentalServiceItemId": 100
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ServiceItem_RentalRequiredItem_MappingModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      {
        "Id": 1,
        "RentalServiceItemId": 100,
        "RequiredItemId": 5,
        "RequiredItemName": "CMND/CCCD",
        "IsRequired": true
      },
      {
        "Id": 2,
        "RentalServiceItemId": 100,
        "RequiredItemId": 6,
        "RequiredItemName": "Giấy phép lái xe",
        "IsRequired": true
      }
    ]
  }
}
```

---

## Error Responses

| Trường hợp | StatusCode | Message |
|-------------|-----------|---------|
| ServiceItem không tồn tại | 0 | Xe không tồn tại |
| Ngày trùng lịch bận | 0 | Ngày đã có lịch bận, vui lòng kiểm tra lại |
| Giá thuê ≤ 0 | 0 | Giá thuê phải lớn hơn 0 |
| FromDate > ToDate | 0 | Ngày bắt đầu phải trước ngày kết thúc |
| WeekdayRentalPrice: thiếu giá cho weekday | 0 | Vui lòng nhập giá cho tất cả các ngày trong tuần |
| MinimumRentalDay ≤ 0 | 0 | Số ngày thuê tối thiểu phải ≥ 1 |

---

## Ghi chú kỹ thuật

- **`_ListView` controllers:** Mỗi controller chính có một `_ListView` variant với phân trang nâng cao và thêm action `CreateMultiDay` / `UpdateWeekdayRentalPrice`.
- **Ưu tiên giá:** `DateRentalPrice` (ngày cụ thể) > `WeekdayRentalPrice` (theo thứ) — giá ngày đặc biệt ghi đè giá theo thứ.
- **Lịch bận:** Cộng dồn `DateBusy` + `WeekdaysBusy` — cả hai đều ảnh hưởng tới `BusySchedules` trong `SearchingRentalService/Detail`.
- **Upload ảnh:** Thực hiện qua `RentalServiceUploadFile` controller, sau đó gọi `CreateThumbnail`.
