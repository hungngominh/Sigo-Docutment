# API: Mioto Integration

> **Controllers:** `Mioto_VehicleOwnerController`, `Mioto_VehiclePricingController`, `Mioto_VehiclePricing_MissingVehicleInfoController`, `Mioto_VehicleQueryController`, `Mioto_VehicleTripCountDetailController`
> **Base:** `/api/v1/Mioto`
> **Phân quyền:** `[Authorize]` (tất cả endpoint)

## Mục lục
- [Tổng quan](#tổng-quan)
- [Chủ xe Mioto](#post-apiv1miotovehicleownerlist)
- [Giá xe Mioto — Danh sách](#post-apiv1miotovehiclepricinglist)
- [Giá xe Mioto — Đồng bộ](#post-apiv1miotovehiclepricingfetchdata)
- [Giá xe Mioto — Cập nhật](#post-apiv1miotovehiclepricingupdatemioto_vehicleinfo)
- [Giá xe Mioto — Mapping](#post-apiv1miotovehiclepricingmappingwithsigo_usingplatenumber)
- [Truy vấn xe Mioto](#post-apiv1miotovehiclequerylist)
- [Lịch sử chuyến](#post-apiv1miotovehicletripcountdetaillist)

---

## Tổng quan

Tích hợp với nền tảng **Mioto** để đồng bộ dữ liệu xe, giá và chuyến đi giữa hệ thống Alliance và Mioto.

**Data flow:** Mioto → Alliance (một chiều — Mioto là nguồn dữ liệu gốc, Alliance đồng bộ về)

Chức năng chính:
1. Tra cứu thông tin chủ xe đã đăng ký trên Mioto
2. Đồng bộ bảng giá xe từ Mioto
3. Mapping xe Mioto với xe trên hệ thống Alliance (theo biển số)
4. Theo dõi số chuyến xe (trip count)

---

## `POST /api/v1/Mioto/VehicleOwner/List`

**Mô tả:** Lấy danh sách chủ xe đã đăng ký trên Mioto.

**Phân quyền:** `[Authorize]`

### Request Body — `Mioto_VehicleOwnerParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "Keyword": ""
}
```

### Response — `EzyResultObject<EzyDataSourceResult<Mioto_VehicleOwnerModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 50,
    "Data": [
      {
        "MiotoOwnerId": "mioto-owner-guid",
        "FullName": "Nguyễn Văn A",
        "MobilePhone": "0901234567"
      }
    ]
  }
}
```

---

## `POST /api/v1/Mioto/VehiclePricing/List`

**Mô tả:** Lấy danh sách bảng giá xe đồng bộ từ Mioto.

**Phân quyền:** `[Authorize]`

### Request Body — `Mioto_VehiclePricingParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "PlateNumber": ""
}
```

### Response — `EzyResultObject<EzyDataSourceResult<Mioto_VehiclePricingModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 100,
    "Data": [
      {
        "PlateNumber": "51A-12345",
        "DailyPrice": 500000,
        "MiotoVehicleId": "mioto-vehicle-guid"
      }
    ]
  }
}
```

---

## `POST /api/v1/Mioto/VehiclePricing/FetchData`

**Mô tả:** Gọi API Mioto để lấy dữ liệu giá xe mới nhất về hệ thống.

**Phân quyền:** `[Authorize]`

### Request Body — `Mioto_FetchDataModel`
```json
{
  "FromDate": "2026-01-01",
  "ToDate": "2026-01-31"
}
```

### Response — `EzyResultObject<IEnumerable<Mioto_VehiclePricingModel>>`
```json
{
  "StatusCode": 1,
  "Msg": "Đã đồng bộ 25 xe",
  "Data": [ { ... } ]
}
```

---

## `POST /api/v1/Mioto/VehiclePricing/UpdateMioto_VehicleInfo`

**Mô tả:** Cập nhật thông tin xe Mioto vào hệ thống Alliance (tự động từ dữ liệu Mioto đã đồng bộ).

**Phân quyền:** `[Authorize]`

### Request Body — `Mioto_VehiclePricingParamModel`
```json
{
  "MiotoVehicleId": "mioto-vehicle-guid"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Cập nhật thành công"
}
```

---

## `POST /api/v1/Mioto/VehiclePricing/UpdateMioto_VehicleInfo_Manual`

**Mô tả:** Cập nhật thủ công thông tin xe Mioto (admin override).

**Phân quyền:** `[Authorize]`

Tương tự `UpdateMioto_VehicleInfo` nhưng cho phép nhập tay thay vì lấy từ dữ liệu đồng bộ.

---

## `POST /api/v1/Mioto/VehiclePricing/MappingWithSigo_UsingPlateNumber`

**Mô tả:** Tự động mapping xe Mioto với xe trên hệ thống Sigo/Alliance dựa theo biển số xe.

**Phân quyền:** `[Authorize]`

### Request Body — `Mioto_VehiclePricingParamModel`
```json
{
  "PlateNumber": "51A-12345"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Mapping thành công: 51A-12345 → vehicle-guid"
}
```

---

## `POST /api/v1/Mioto/VehiclePricing/GetList_OptionQuery`

**Mô tả:** Lấy danh sách xe Mioto theo option (dùng cho dropdown/autocomplete).

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<EzyDataSourceResult<object>>`

---

## `POST /api/v1/Mioto/VehiclePricing_MissingVehicleInfo/List`

**Mô tả:** Lấy danh sách xe Mioto chưa đủ thông tin (missing vehicle info) — dùng cho màn hình xử lý tồn đọng.

**Phân quyền:** `[Authorize]`

Tương tự `/VehiclePricing/List` nhưng chỉ hiển thị xe thiếu thông tin.

---

## `POST /api/v1/Mioto/VehicleQuery/List`

**Mô tả:** Lấy danh sách truy vấn xe Mioto (lịch sử fetch data).

**Phân quyền:** `[Authorize]`

### Request Body — `Mioto_VehicleQueryParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20
}
```

---

## `POST /api/v1/Mioto/VehicleQuery/FetchPricingData`

**Mô tả:** Khởi động job đồng bộ dữ liệu giá từ Mioto.

**Phân quyền:** `[Authorize]`

### Request Body — `Mioto_FetchDataModel`
```json
{
  "FromDate": "2026-01-01",
  "ToDate": "2026-01-31"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Job đã được khởi động"
}
```

---

## `POST /api/v1/Mioto/VehicleQuery/AddFromConfigAddress`

**Mô tả:** Thêm địa chỉ mới vào danh sách truy vấn từ ConfigAddress.

**Phân quyền:** `[Authorize]`

### Request Body
Không có body.

---

## `POST /api/v1/Mioto/VehicleTripCountDetail/List`

**Mô tả:** Lấy chi tiết số chuyến xe (trip count) đã đồng bộ từ Mioto.

**Phân quyền:** `[Authorize]`

### Request Body — `Mioto_VehicleTripCountDetailParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "PlateNumber": ""
}
```

### Response — `EzyResultObject<EzyDataSourceResult<Mioto_VehicleTripCountDetailModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 200,
    "Data": [
      {
        "PlateNumber": "51A-12345",
        "TripCount": 45,
        "LastSyncAt": "2026-01-01T08:00:00Z"
      }
    ]
  }
}
```

---

## Ghi chú

- Đồng bộ dữ liệu chạy tự động theo schedule (Hosted Service).
- Mapping xe dùng `PlateNumber` làm khóa chính để khớp giữa 2 hệ thống.
- Màn hình `VehiclePricing_MissingVehicleInfo` dùng để xử lý xe chưa đủ thông tin sau khi đồng bộ.
