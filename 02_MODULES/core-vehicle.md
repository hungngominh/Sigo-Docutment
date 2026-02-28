# Core Module: Vehicle — Quản lý xe

> **Project:** `AllianceMiddlemanWebAPI` | **Domain:** Nghiệp vụ cốt lõi

## Mục lục
- [Tổng quan](#tổng-quan)
- [Entity Schemas](#entity-schemas)
- [Controllers & Endpoints](#controllers--endpoints)
- [Business Logic & Workflows](#business-logic--workflows)
- [Data Models (Request/Response)](#data-models-requestresponse)
- [Validation & Errors](#validation--errors)
- [Categories (Config xe)](#categories-config-xe)

---

## Tổng quan

Module Vehicle quản lý thông tin xe và cài đặt cho thuê. Mỗi xe có đầy đủ:
- Thông tin cơ bản (biển số, hãng, model, năm, số chỗ, hộp số, nhiên liệu)
- Bảo hiểm xe (công ty bảo hiểm, hợp đồng, ngày hiệu lực)
- Cài đặt cho thuê (deposit, giới hạn km, phí giao xe, phí dọn vệ sinh)
- Giảm giá thuê nhiều ngày (tier-based: 3+ ngày, 7+ ngày...)
- Gợi ý giảm giá cho xe ít booking (dựa trên P90/P95 giá thuê)

**Phân biệt Vehicle vs ServiceItem:**
- `Vehicle` = thông tin xe vật lý (biển số, model, bảo hiểm, cài đặt cho thuê)
- `ServiceItem` = xe trong context cho thuê (giá, lịch, ảnh, features) — xem [core-rental-service.md](./core-rental-service.md)
- Quan hệ: `Vehicle` ←(1:1)→ `RentalService_SelfdriveCarRental` ←(N:1)→ `RentalServiceItem`

---

## Entity Schemas

### Vehicle — 35 fields

**File:** `BusinessEntities.Vehicle.cs`

| Column | Type | Nullable | Mô tả |
|--------|------|----------|-------|
| Id | long | Không | PK, auto-increment |
| PlateNumber | string | Có | Biển số xe (unique per approved rental) |
| Name | string | Có | Tên tham chiếu |
| Series | string | Có | Series xe |
| YearModel | int | Có | Năm sản xuất |
| VehicleTypeId | long | Có | FK → ConfigVehicleType (Sedan, SUV...) |
| VehicleTypeSubId | long | Có | FK → ConfigVehicleTypeSub (4-chỗ, 7-chỗ...) |
| VehicleNoOfSeatId | long | Có | FK → ConfigVehicleNoOfSeat |
| VehicleMakeId | long | Có | FK → ConfigVehicleMake (Toyota, Honda...) |
| VehicleModelId | long | Có | FK → ConfigVehicleModel (Vios, City...) |
| FuelTypeId | long | Có | FK → ConfigVehicleFuelType (Xăng, Dầu, Điện, Hybrid) |
| VehicleColorId | long | Có | FK → ConfigVehicleColor |
| VehicleTransmissionTypeId | long | Có | FK → ConfigVehicleTransmissionType (Số sàn, Số tự động) |
| MakeCountryId | long | Có | FK → Country |
| EngineNumber | string | Có | Số động cơ |
| ChassicNumber | string | Có | Số khung (lưu ý: typo "Chassic" trong code) |
| PistonDisplacement | string | Có | Dung tích xi-lanh |
| NumOfCylinders | int | Có | Số xi-lanh |
| GrossWeight | decimal(18,3) | Có | Tổng trọng lượng (kg) |
| NetWeight | decimal(18,3) | Có | Trọng lượng ròng |
| ShippingWeight | decimal(18,3) | Có | Trọng lượng vận chuyển |
| NetCapacity | decimal | Có | Dung tích chứa |
| FuelEfficiency | string | Có | Mức tiêu hao nhiên liệu |
| VRC_OriginalRegistrationNumber | string | Có | Số đăng ký gốc |
| VRC_OriginalRegistrationDate | DateTime | Có | Ngày đăng ký gốc |
| VRC_ExpiryRegistrationDate | DateTime | Có | Ngày hết hạn đăng ký |
| ID_GUID | Guid | Có | UUID (auto-generated) |
| IsDeleted | bool | Không | Soft delete |
| OrderNo | decimal(20,6) | Không | Thứ tự hiển thị |
| IsDisable | bool | Không | Vô hiệu hóa |

**Navigation Properties:**

| Navigation | Type | Mô tả |
|------------|------|-------|
| RentalService_SelfdriveCarRental | `IList` | Xe cho thuê tự lái |
| Vehicle_InsuranceInformation | `IList` | Danh sách bảo hiểm |

---

### Vehicle_InsuranceInformation — 16 fields

**File:** `BusinessEntities.Vehicle_InsuranceInformation.cs`

| Column | Type | Nullable | Mô tả |
|--------|------|----------|-------|
| Id | long | Không | PK |
| VehicleId | long | Có | FK → Vehicle |
| InsuranceProviderId | long | Có | FK → ConfigVehicleInsuranceCompany |
| InsuranceProvider | string | Có | Tên công ty bảo hiểm |
| PolicyNumber | string | Có | Số hợp đồng bảo hiểm |
| CoverageStartDate | DateTime | Có | Ngày bắt đầu hiệu lực |
| CoverageEndDate | DateTime | Có | Ngày hết hiệu lực |
| IsActive | bool | Có | **Auto-calculated**: `InsuranceProviderId != null AND now >= StartDate AND now <= EndDate` |
| IsVerified | bool | Có | Admin đã xác minh bảo hiểm |
| Hotline | string | Có | Hotline công ty bảo hiểm |
| SalePersonPhoneNumber | string | Có | SĐT nhân viên bán hàng |
| ID_GUID | Guid | Có | UUID |

---

### Vehicle_RentalSetting — 25 fields

**File:** `BusinessEntities.Vehicle_RentalSetting.cs`

| Column | Type | Nullable | Mô tả |
|--------|------|----------|-------|
| Id | long | Không | PK |
| RentalServiceCategoryId | long | Có | FK → RentalServiceCategory |
| **Giảm giá nhiều ngày** |
| HaveMultidayRentalDiscount | bool | Có | Bật/tắt giảm giá nhiều ngày |
| **Giao xe** |
| HaveDeliverySurcharge | bool | Có | Tính phí giao xe |
| MaximumDeliveryMileage | decimal | Có | Giới hạn km giao xe miễn phí |
| DeliverySurcharge | decimal | Có | Phí giao xe / km vượt mức |
| FreeDeliveryMileage | decimal | Có | Km giao miễn phí |
| **Giới hạn km** |
| HaveExcessMileageSurcharge | bool | Có | Tính phí vượt km |
| MaximumMileage | decimal | Có | Giới hạn km / ngày |
| ExcessMileageSurcharge | decimal | Có | Phí vượt km / km |
| **Phụ phí** |
| HaveSecurity | bool | Có | Yêu cầu đặt cọc an ninh |
| HaveCleaningFee | bool | Có | Tính phí dọn vệ sinh |
| CleaningFee | decimal | Có | Phí dọn vệ sinh |
| EarlyHourDeliveryFee | decimal | Có | Phí giao xe sớm (trước giờ mở cửa) |
| LateHourReturnFee | decimal | Có | Phí trả xe muộn (sau giờ đóng cửa) |
| **Giờ hoạt động** |
| RentalHourStart | int | Có | Giờ bắt đầu cho thuê (0-23) |
| RentalHourEnd | int | Có | Giờ kết thúc cho thuê (0-23) |
| NumOfEarlyHourToBeOneDay | int | Có | Số giờ sớm tính 1 ngày |
| NumOfLateHourToBeOneDay | int | Có | Số giờ muộn tính 1 ngày |
| RentalHour_Using24Hours | bool | Không | Chế độ 24h |
| **Bảo hiểm** |
| HaveInsurance | bool | Không | **Auto-synced** từ Vehicle_InsuranceInformation.IsActive |
| **Xe điện** |
| ChargingFee | decimal | Có | Phí sạc (chỉ hiển thị nếu FuelType = "ELECTRIC") |
| DeodorizingFee | decimal | Có | Phí khử mùi |

---

### VehicleDiscountSuggestion — 10 fields

**File:** `BusinessEntities.VehicleDiscountSuggestion.cs`

| Column | Type | Nullable | Mô tả |
|--------|------|----------|-------|
| Id | long | Không | PK |
| CfId | long | Có | FK → ConfigVehicleDiscountSuggestion |
| StartAtUTC | DateTime | Có | Thời điểm gợi ý bắt đầu |
| IsUsed | bool | Không | Đã kích hoạt giảm giá → lock record |
| UsingRentPrice | decimal | Có | Giá thuê áp dụng |
| UsingRentPriceType | string | Có | Chiến lược giá: `Manual`, `AvgRentPrice`, `P90_RentPrice`, `P95_RentPrice` |
| NotificationMessage | string | Có | Nội dung thông báo |

**Giải thích UsingRentPriceType:**
- `Manual` — Chủ xe tự nhập giá
- `AvgRentPrice` — Trung bình giá thuê trong khu vực
- `P90_RentPrice` — Percentile 90 (10% xe giá cao hơn)
- `P95_RentPrice` — Percentile 95 (5% xe giá cao hơn)

---

### VehicleMultidayRentalDiscount_Detail — 8 fields

**File:** `BusinessEntities.VehicleMultidayRentalDiscount_Detail.cs`

| Column | Type | Nullable | Mô tả |
|--------|------|----------|-------|
| Id | long | Không | PK |
| VehicleRentalSettingId | long | Có | FK → Vehicle_RentalSetting |
| MultidayRentalDiscountId | long | Có | FK → ConfigVehicleMultidayRentalDiscount |
| DiscountPercent | decimal | Có | Phần trăm giảm (ví dụ: 5 = giảm 5%) |
| DiscountMoney | decimal | Có | Số tiền giảm cố định |

**Data Model (response) bổ sung:**
- `RentalDiscountName` — "3+ ngày: -5%"
- `NumOfMinDay` — Số ngày tối thiểu (read-only, từ Config)
- `PriceInfo` — Thông tin giá tính sau khi giảm

---

## Controllers & Endpoints

### VehicleController — `api/v1/Vehicle`

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/List` | Danh sách xe (filter by Id, RentalServiceItemId) | Required |
| POST | `/Add` | Thêm xe mới | Required |
| POST | `/Update` | Cập nhật thông tin xe | Required |
| POST | `/Delete` | Xoá mềm xe | Required |
| POST | `/UpdateFields` | Cập nhật field cụ thể | Required |

**Request `/List`:**
```json
{
  "Id": 0,
  "RentalServiceItemId": 0,
  "PageIndex": 0,
  "PageSize": 20
}
```

**Response:**
```json
{
  "StatusCode": 0,
  "Data": {
    "Data": [{
      "Id": 1,
      "PlateNumber": "51A-12345",
      "VehicleMakeId": 10,
      "VehicleModelId": 25,
      "YearModel": 2022,
      "VehicleTypeName": "Sedan",
      "VehicleSummary": "51A-12345 - Toyota Vios 2022 - Trắng",
      "Exp_Insurance": "Bảo Việt — HĐ: 2025-12-31"
    }],
    "Total": 1
  }
}
```

---

### Vehicle_ListViewController — `api/v1/Vehicle_ListView`

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/List` | Danh sách xe mở rộng (kèm category options) | Required |

**Response bổ sung dropdown data:**
- `VehicleTypeList` — Loại xe (Sedan, SUV...)
- `VehicleNoOfSeatList` — Số chỗ
- `FuelTypeList` — Loại nhiên liệu
- `VehicleColorList` — Màu xe
- `VehicleTransmissionTypeList` — Loại hộp số
- `VehicleMakeList` — Hãng xe
- `CountryList` — Quốc gia sản xuất

---

### Vehicle_InsuranceInformationController — `api/v1/Vehicle_InsuranceInformation`

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/List` | DS bảo hiểm (auto-create record nếu chưa có) | Required |
| POST | `/Add` | Thêm bảo hiểm | Required |
| POST | `/Update` | Cập nhật bảo hiểm | Required |
| POST | `/Delete` | Xoá bảo hiểm | Required |
| POST | `/UpdateFields` | Cập nhật field + auto-sync HaveInsurance | Required |

**Request `/List`:**
```json
{
  "VehicleId": 1,
  "PageIndex": 0,
  "PageSize": 20
}
```

**Response:**
```json
{
  "StatusCode": 0,
  "Data": {
    "Data": [{
      "Id": 5,
      "VehicleId": 1,
      "InsuranceProviderId": 2,
      "InsuranceProvider": "Bảo Việt",
      "PolicyNumber": "BV-2024-001234",
      "CoverageStartDate": 1704067200,
      "CoverageEndDate": 1735689599,
      "IsActive": true,
      "IsVerified": true,
      "Hotline": "1900558891"
    }]
  }
}
```

---

### Vehicle_RentalSettingController — `api/v1/Vehicle_RentalSetting`

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/List` | DS cài đặt cho thuê | Required |
| POST | `/Update` | Cập nhật cài đặt | Required |
| POST | `/Add` | Thêm cài đặt (inherited) | Required |

**Response Update cài đặt:**
```json
{
  "StatusCode": 0,
  "Data": {
    "Id": 3,
    "HaveMultidayRentalDiscount": true,
    "HaveDeliverySurcharge": true,
    "MaximumDeliveryMileage": 10.0,
    "DeliverySurcharge": 5000,
    "FreeDeliveryMileage": 5.0,
    "HaveExcessMileageSurcharge": true,
    "MaximumMileage": 300,
    "ExcessMileageSurcharge": 4000,
    "RentalHourStart": 8,
    "RentalHourEnd": 20,
    "HaveInsurance": true,
    "CanShowChargingFee": false
  }
}
```

---

### VehicleDiscountSuggestionController — `api/v1/VehicleDiscountSuggestion`

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/List` | Danh sách gợi ý giảm giá | Required |
| POST | `/UpdatePrice` | Cập nhật chiến lược giá giảm | Required |

**Request `/UpdatePrice`:**
```json
{
  "Id": 10,
  "UsingRentPrice": 650000,
  "UsingRentPriceType": "Manual"
}
```

---

### VehicleMultidayRentalDiscount_DetailController — `api/v1/VehicleMultidayRentalDiscount_Detail`

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/List` | DS chính sách giảm giá nhiều ngày | Required |
| POST | `/Add` | Thêm tier giảm giá | Required |
| POST | `/Update` | Cập nhật % hoặc số tiền | Required |
| POST | `/Delete` | Xoá tier | Required |

---

## Business Logic & Workflows

### 1. Vehicle Creation/Update

```
Thêm/sửa xe
  ├─ Validate biển số: case-insensitive, bỏ ký tự đặc biệt → check unique
  ├─ Khi VehicleModelId thay đổi:
  │   ├─ Auto-update VehicleTypeSubId từ model
  │   ├─ Auto-update VehicleMakeId từ model
  │   ├─ Auto-update VehicleNoOfSeatId từ type sub
  │   └─ Regenerate slug cho RentalServiceItem
  └─ Khi YearModel thay đổi:
      ├─ Regenerate slug
      └─ Trigger UpdateRentalServiceItem_Calculating_RentCarEngine
```

### 2. Insurance Management

```
Auto-create: Khi gọi /List mà chưa có record → tạo record rỗng
                                     ▼
Khi cập nhật (/UpdateFields):
  ├─ Validate: CoverageEndDate ≥ CoverageStartDate
  ├─ Auto-default: nếu chỉ có StartDate → EndDate = StartDate + 1 năm
  ├─ Tính IsActive:
  │   IsActive = (InsuranceProviderId ≠ null)
  │              AND (now ≥ CoverageStartDate)
  │              AND (now ≤ CoverageEndDate)
  └─ Sync → Vehicle_RentalSetting.HaveInsurance:
      ├─ IsActive = false → HaveInsurance = false (luôn luôn)
      └─ IsActive = true  → HaveInsurance = IsVerified
```

### 3. Discount Suggestion

```
System xác định xe ít booking (qua ConfigVehicleDiscountSuggestion)
  ├─ Tạo VehicleDiscountSuggestion với 4 chiến lược giá:
  │   ├─ Manual: chủ xe tự nhập
  │   ├─ AvgRentPrice: trung bình giá thuê khu vực
  │   ├─ P90_RentPrice: percentile 90
  │   └─ P95_RentPrice: percentile 95
  ├─ Chủ xe chọn chiến lược → /UpdatePrice
  │   Validate: UsingRentPrice > 0
  └─ Khi kích hoạt (IsUsed = true):
      └─ Record bị lock → không sửa được nữa
```

### 4. Multi-day Discount

```
Ví dụ config:
  ├─ 3+ ngày: giảm 5%
  ├─ 7+ ngày: giảm 10%
  └─ 14+ ngày: giảm 15%

Khi tính giá đơn hàng:
  NumberOfRentalDay = 10
  → Match tier "7+ ngày: 10%"
  → OriginalPriceByDay * 10 * (1 - 10%) = TotalPriceByDay
```

---

## Data Models (Request/Response)

### VehicleModel (response)

```json
{
  "Id": 1,
  "PlateNumber": "51A-12345",
  "Name": "Toyota Vios 2022",
  "YearModel": 2022,
  "VehicleTypeId": 1,
  "VehicleTypeSubId": 3,
  "VehicleNoOfSeatId": 2,
  "VehicleModelId": 25,
  "VehicleMakeId": 10,
  "FuelTypeId": 1,
  "VehicleColorId": 5,
  "VehicleTransmissionTypeId": 2,
  "EngineNumber": "2NR-FE-123456",
  "ChassicNumber": "JTDBT923081234567",
  "VRC_OriginalRegistrationNumber": "12345",
  "VRC_OriginalRegistrationDate": 1609459200,
  "VRC_ExpiryRegistrationDate": 1767225600,
  "FuelEfficiency": "6.5L/100km",
  "VehicleSummary": "51A-12345 - Toyota Vios 2022 - Trắng",
  "Options": {
    "VehicleTypeSubList": [{"Id": 3, "Name": "5 chỗ"}],
    "ModelList": [{"Id": 25, "Name": "Vios"}]
  }
}
```

### Vehicle_RentalSettingModel (response)

```json
{
  "Id": 3,
  "HaveMultidayRentalDiscount": true,
  "HaveDeliverySurcharge": true,
  "MaximumDeliveryMileage": 10.0,
  "DeliverySurcharge": 5000,
  "FreeDeliveryMileage": 5.0,
  "HaveExcessMileageSurcharge": true,
  "MaximumMileage": 300,
  "ExcessMileageSurcharge": 4000,
  "HaveSecurity": true,
  "HaveCleaningFee": true,
  "CleaningFee": 100000,
  "EarlyHourDeliveryFee": 100000,
  "LateHourReturnFee": 100000,
  "RentalHourStart": 8,
  "RentalHourEnd": 20,
  "NumOfEarlyHourToBeOneDay": 4,
  "NumOfLateHourToBeOneDay": 4,
  "RentalHour_Using24Hours": false,
  "HaveInsurance": true,
  "ChargingFee": null,
  "DeodorizingFee": null,
  "CanShowChargingFee": false
}
```

---

## Validation & Errors

| Rule | Error Message |
|------|---------------|
| Biển số trùng | "Biển số xe {plate} đã tồn tại" |
| CoverageEndDate < CoverageStartDate | "Ngày kết thúc hợp đồng nhỏ hơn ngày bắt đầu có hiệu lực" |
| Giá thuê ≤ 0 (discount suggestion) | "Giá thuê phải > 0" |
| Chưa chọn loại xe khi update | "Bạn vui lòng chọn ít nhất một loại" |

---

## Categories (Config xe)

Toàn bộ dữ liệu lookup cho xe, quản lý qua `Categories/` controllers:

| Controller Route | Mô tả | Ví dụ giá trị |
|-----------------|-------|---------------|
| `ConfigVehicleMake` | Hãng xe | Toyota, Honda, Hyundai, KIA, VinFast |
| `ConfigVehicleModel` | Model xe (theo hãng) | Vios, City, Accent, Morning |
| `ConfigVehicleType` | Loại xe | Sedan, SUV, Hatchback, Pickup |
| `ConfigVehicleTypeSub` | Loại xe chi tiết | 4-chỗ, 5-chỗ, 7-chỗ, 16-chỗ |
| `ConfigVehicleSegment` | Phân khúc | Hạng A, B, C, D, E |
| `ConfigVehicleNoOfSeat` | Số chỗ ngồi | 4, 5, 7, 9, 16 |
| `ConfigVehicleColor` | Màu xe | Trắng, Đen, Bạc, Đỏ |
| `ConfigVehicleFuelType` | Nhiên liệu | Xăng, Dầu, Điện, Hybrid |
| `ConfigVehicleTransmissionType` | Hộp số | Số sàn, Số tự động |
| `ConfigVehicleInsuranceCompany` | Công ty bảo hiểm | Bảo Việt, PVI, PTI |
| `ConfigVehicleMultidayRentalDiscount` | Quy tắc giảm giá nhiều ngày | 3+ ngày: 5%, 7+ ngày: 10% |
| `ConfigVehicleRentalCancelReason` | Lý do huỷ cho thuê | Xe hỏng, Lý do cá nhân |

---

## Relationship Diagram

```
Vehicle ──(1:N)──► Vehicle_InsuranceInformation
  │                    └─ FK: VehicleId
  │
  ├──(1:1)──► Vehicle_RentalSetting
  │               ├─ HaveInsurance (auto-sync từ Insurance)
  │               └──(1:N)──► VehicleMultidayRentalDiscount_Detail
  │
  └──(1:N)──► RentalService_SelfdriveCarRental (CarId FK)
                  └──(N:1)──► RentalServiceItem
                                  └──► Order (RentalServiceItemId FK)

VehicleDiscountSuggestion
  └─ CfId FK → ConfigVehicleDiscountSuggestion

Config* (12 lookup tables) ◄── Vehicle FK references
```

---

*Xem thêm: [rental-service.md](./core-rental-service.md) | [categories.md](../03_API/categories.md) | [database-design.md](../01_ARCHITECTURE/database-design.md)*
