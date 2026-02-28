# API: Vehicle

> **Controllers:** `VehicleController`, `Vehicle_InsuranceInformationController`, `Vehicle_RentalSettingController`, `VehicleDiscountSuggestionController`, `VehicleMultidayRentalDiscount_DetailController`
> **Base:** `/api/v1/Vehicle`, `/api/v1/Vehicle_*`

## Mục lục
- [Danh sách xe](#danh-sách-xe)
- [Bảo hiểm xe](#bảo-hiểm-xe)
- [Cài đặt cho thuê](#cài-đặt-cho-thuê)
- [Giảm giá nhiều ngày](#giảm-giá-nhiều-ngày)
- [Gợi ý giảm giá](#gợi-ý-giảm-giá)

---

## Danh sách xe

### `POST /api/v1/Vehicle/List`

**Mô tả:** Danh sách xe (admin/staff management).

**Phân quyền:** `[Authorize]`

**Request Body:** `VehicleParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "SearchText": "",
  "VehicleMakeId": null,
  "VehicleModelId": null,
  "Status": null
}
```

### Response — `EzyResultObject<EzyDataSourceResult<VehicleModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      {
        "VehicleId": "vehicle-guid",
        "PlateNumber": "29A-12345",
        "VehicleMake": "Toyota",
        "VehicleModel": "Vios",
        "YearModel": 2022,
        "NumberOfSeat": 5,
        "TransmissionType": "Số tự động",
        "FuelType": "Xăng",
        "Color": "Trắng",
        "Status": "ACTIVE",
        "OwnerName": "Nguyễn Văn A",
        "HasInsurance": true,
        "InsuranceExpiry": "2027-01-15"
      }
    ],
    "Total": 50
  }
}
```

### `POST /api/v1/Vehicle_ListView/List`

**Mô tả:** Danh sách xe dạng list view với thêm thông tin tổng hợp.

---

## Bảo hiểm xe

### `POST /api/v1/Vehicle_InsuranceInformation/List`

**Mô tả:** Danh sách thông tin bảo hiểm của xe.

**Phân quyền:** `[Authorize]`

**Request Body:** `Vehicle_InsuranceInformationParamModel`
```json
{
  "VehicleId": "vehicle-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<Vehicle_InsuranceInformationModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      {
        "InsuranceId": "ins-guid",
        "VehicleId": "vehicle-guid",
        "InsuranceCompany": "Bảo Việt",
        "PolicyNumber": "BV-2025-001234",
        "InsuranceType": "Bảo hiểm xe ô tô",
        "StartDate": "2025-01-15",
        "EndDate": "2026-01-15",
        "Coverage": "Toàn diện",
        "IsActive": true
      }
    ],
    "Total": 1
  }
}
```

> **Ghi chú:** Thông tin bảo hiểm ảnh hưởng tới search filter `IsHaveInsurance` trong `SearchingRentalService/List`.

---

## Cài đặt cho thuê

### `POST /api/v1/Vehicle_RentalSetting/List`

**Mô tả:** Danh sách cài đặt cho thuê của xe.

**Phân quyền:** `[Authorize]`

**Request Body:** `Vehicle_RentalSettingParamModel`
```json
{
  "VehicleId": "vehicle-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<Vehicle_RentalSettingModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      {
        "VehicleId": "vehicle-guid",
        "DepositAmount": 5000000,
        "DepositAmount_Text": "5.000.000đ",
        "AllowDelivery": true,
        "DeliveryMaxDistance": 15,
        "DeliveryFeePerKm": 10000,
        "DailyKmLimit": 300,
        "OverKmFee": 5000,
        "MinRentalDays": 1,
        "RequiredDocuments": ["IDENTITY_CARD", "DRIVER_LICENSE"],
        "RequiredSecurities": ["MOTORBIKE_DEPOSIT"]
      }
    ],
    "Total": 1
  }
}
```

---

## Giảm giá nhiều ngày

### `POST /api/v1/VehicleMultidayRentalDiscount_Detail/List`

**Mô tả:** Cấu hình giảm giá theo số ngày thuê. Ví dụ: thuê 3+ ngày giảm 5%, 7+ ngày giảm 10%.

**Phân quyền:** `[Authorize]`

**Request Body:** `VehicleMultidayRentalDiscount_DetailParamModel`
```json
{
  "VehicleId": "vehicle-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<VehicleMultidayRentalDiscount_DetailModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      { "MinDays": 3, "MaxDays": 6, "DiscountPercent": 5, "DiscountPercent_Text": "Giảm 5%" },
      { "MinDays": 7, "MaxDays": 13, "DiscountPercent": 10, "DiscountPercent_Text": "Giảm 10%" },
      { "MinDays": 14, "MaxDays": null, "DiscountPercent": 15, "DiscountPercent_Text": "Giảm 15%" }
    ],
    "Total": 3
  }
}
```

---

## Gợi ý giảm giá

### `POST /api/v1/VehicleDiscountSuggestion/List`

**Mô tả:** Hệ thống gợi ý giảm giá cho xe ít booking. Admin/owner xem danh sách gợi ý.

**Phân quyền:** `[Authorize]`

---

## Ghi chú kỹ thuật

- **Vehicle vs ServiceItem:** Vehicle quản lý thông tin vật lý (biển số, model, bảo hiểm). ServiceItem quản lý thông tin cho thuê (giá, lịch, ảnh) → xem [service-item.md](./service-item.md)
- **CRUD pattern:** Tất cả Vehicle controllers follow pattern chuẩn với `List`, `Add`, `Update`, `Delete` từ `BaseCategoryController`
- **Bảo hiểm hết hạn:** Xe có bảo hiểm hết hạn sẽ không hiển thị flag `IsHaveInsurance` trong search

---

*Xem thêm: [service-item.md](./service-item.md) | [core-vehicle.md](../02_MODULES/core-vehicle.md) | [categories.md](./categories.md)*
