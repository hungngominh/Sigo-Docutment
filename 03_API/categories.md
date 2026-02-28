# API: Categories (Config)

> **Controllers:** 59 `Config*Controller` files (50+ distinct controllers)
> **Base:** `/api/v1/Config*`
> **Authorization:** `[Authorize]` (Admin/Staff roles)

---

## Mục lục
- [Pattern chung](#pattern-chung)
- [Vehicle Domain (13 controllers)](#vehicle-domain-13-controllers)
- [Payment & Finance Domain (7 controllers)](#payment--finance-domain-7-controllers)
- [User & Auth Domain (5 controllers)](#user--auth-domain-5-controllers)
- [Service Domain (8 controllers)](#service-domain-8-controllers)
- [UI / Content Domain (6 controllers)](#ui--content-domain-6-controllers)
- [System Domain (7 controllers)](#system-domain-7-controllers)
- [Infrastructure Domain (4 controllers)](#infrastructure-domain-4-controllers)
- [CMS Module (3 controllers)](#cms-module-3-controllers)
- [Other Modules (6 controllers)](#other-modules-6-controllers)
- [Controllers với Custom Endpoints](#controllers-với-custom-endpoints)

---

## Pattern chung

Tất cả Config controllers kế thừa `ProjectBaseCategoryController<TService, TModel>`.

### Base CRUD Endpoints (inherited)

```
POST   /api/v1/{ConfigEntity}/GetList          → Danh sách (paginated, filter)
GET    /api/v1/{ConfigEntity}/GetById/{id}      → Chi tiết
POST   /api/v1/{ConfigEntity}/Create            → Tạo mới
POST   /api/v1/{ConfigEntity}/Update            → Cập nhật
POST   /api/v1/{ConfigEntity}/Delete/{id}       → Xoá (soft delete)
```

### Standard Request/Response

```csharp
// Request (GetList)
POST { PageSize: int, PageIndex: int, Filter: string, OrderBy: string }

// Response
EzyResultObject<EzyDataSourceResult<TModel>> {
    Status: 1,  // 1=OK, 0=Error
    Data: { Data: TModel[], TotalCount: int }
}
```

### Concrete Examples

**GetList — ConfigVehicleMake:**
```json
// POST /api/v1/ConfigVehicleMake/GetList
// Request:
{ "PageSize": 10, "PageIndex": 1, "Filter": "", "OrderBy": "Name ASC" }

// Response:
{
  "Status": 1,
  "Message": "",
  "Data": {
    "Data": [
      { "Id": 1, "Name": "Toyota", "Code": "TOYOTA", "MakeCountryId": 5,
        "IsDeleted": false, "OrderNo": 1 },
      { "Id": 2, "Name": "Honda", "Code": "HONDA", "MakeCountryId": 5,
        "IsDeleted": false, "OrderNo": 2 }
    ],
    "TotalCount": 25
  }
}
```

**Create — ConfigVehicleColor:**
```json
// POST /api/v1/ConfigVehicleColor/Create
// Request:
{ "Name": "Đỏ", "Code": "RED", "ColorHex": "#FF0000", "OrderNo": 10 }

// Response:
{ "Status": 1, "Message": "", "Data": { "Id": 15, "Name": "Đỏ", "Code": "RED" } }

// Error (duplicate Code):
{ "Status": 0, "Message": "Code đã tồn tại", "Data": null }
```

**GetById:**
```json
// GET /api/v1/ConfigVehicleColor/GetById/15
// Response:
{
  "Status": 1,
  "Data": {
    "Id": 15, "Name": "Đỏ", "Code": "RED", "ColorHex": "#FF0000",
    "OrderNo": 10, "IsDeleted": false, "IsDisable": false,
    "Log_CreatedDate": "2024-01-15T10:30:00Z",
    "Log_CreatedBy": "admin@sigo.vn"
  }
}
```

**Delete (soft):**
```json
// POST /api/v1/ConfigVehicleColor/Delete/15
// Response:
{ "Status": 1, "Message": "", "Data": null }
// Effect: IsDeleted = true (không xoá khỏi DB)
```

### Common Entity Base Fields

Tất cả Config entities đều kế thừa base fields:

| Field | Type | Mô tả |
|-------|------|-------|
| Id | long | PK auto-increment |
| Name | string | Tên hiển thị |
| Code | string? | Mã unique (optional, tuỳ entity) |
| OrderNo | decimal | Thứ tự sắp xếp |
| IsDeleted | boolean | Soft delete |
| IsDisable | boolean | Vô hiệu hoá |
| Log_CreatedDate | timestamptz | Ngày tạo |
| Log_CreatedBy | string | Người tạo |
| Log_UpdatedDate | timestamptz | Ngày cập nhật |
| Log_UpdatedBy | string | Người cập nhật |
| ID_GUID | uuid | UUID (auto-generated) |

### Field Validation Rules (chung)

| Rule | Áp dụng | Mô tả |
|------|---------|-------|
| Name required | Tất cả entities | Không được null/empty |
| Code unique | Entities có Code field | Unique per entity type, case-insensitive |
| FK exists | Entities có FK | FK phải tồn tại và IsDeleted=false |
| Soft delete cascade | Delete endpoint | Set IsDeleted=true, không xoá FK references |

### Pagination Defaults (từ base service)

```
Default PageSize: int.MaxValue (nếu client không truyền → lấy hết)
Hard Maximum:     2001 records per query (server-side cap)
Default Page:     1
Min Page:         1 (nếu < 1 → reset về 1)

Refine Logic (PagingParamModel.Refine()):
  Nếu PageSize == 0 VÀ Take == 0 → PageSize = Take = int.MaxValue
  Nếu PageSize < 1 VÀ Take > 0 → PageSize = Take
  Nếu Take < 1 VÀ Take > 0 → Take = PageSize
  Skip = (Page - 1) × PageSize (nếu Skip == 0 và Page > 1)

⚠️ Hard limit 2001: Dù client gửi PageSize=10000, server chỉ trả tối đa 2001 rows.
   Source: SQLDataContextHelper.cs line 1067 → _iTake = 2001
```

---

## Vehicle Domain (13 controllers)

| # | Controller | Endpoints | Custom Endpoints | Entity quan trọng |
|---|------------|-----------|-----------------|-------------------|
| 1 | `ConfigVehicleType` | 3 | AddFuelType_Mapping, RemoveFuelType_Mapping | Name, Code, OrderNo |
| 2 | `ConfigVehicleTypeSub` | 1 | — | Name, VehicleTypeId (FK) |
| 3 | `ConfigVehicleMake` | 1 | — | Name, Code, MakeCountryId |
| 4 | `ConfigVehicleModel` | 1 | — | Name, VehicleMakeId, VehicleSegmentId |
| 5 | `ConfigVehicleColor` | 1 | — | Name, Code, ColorHex |
| 6 | `ConfigVehicleNoOfSeat` | 1 | — | Name, Value (int) |
| 7 | `ConfigVehicleTransmissionType` | 1 | — | Name, Code (AT/MT) |
| 8 | `ConfigVehicleFuelType` | 1 | — | Name, Code (GASOLINE/DIESEL/ELECTRIC) |
| 9 | `ConfigVehicleSegment` | 1 | — | Name, Code |
| 10 | `ConfigVehicleInsuranceCompany` | 1 | — | Name, Code |
| 11 | `ConfigVehicleMultidayRentalDiscount` | 1 | — | NumOfMinDay, DiscountPercent |

### ConfigVehicleMultidayRentalDiscount — Actual Data (từ DB)

6 tiers giảm giá theo số ngày thuê (system-wide config, owner override qua `VehicleMultidayRentalDiscount_Detail`):

| Code | NumOfMinDay | Ý nghĩa |
|------|-------------|---------|
| `giam_gia_3` | 3 | Thuê ≥3 ngày |
| `giam_gia_5` | 5 | Thuê ≥5 ngày |
| `giam_gia_7` | 7 | Thuê ≥7 ngày |
| `giam_gia_10` | 10 | Thuê ≥10 ngày |
| `giam_gia_14` | 14 | Thuê ≥14 ngày |
| `giam_gia_20` | 20 | Thuê ≥20 ngày |

Mỗi owner có thể set `DiscountPercent` riêng cho từng tier qua `VehicleMultidayRentalDiscount_Detail` (0-10% tuỳ xe).
| 12 | `ConfigVehicleDiscountSuggestion` | 2 | UpdatePrice | DiscountPercent, PriceRange |
| 13 | `ConfigVehicleRentalCancelReason` | 3 | AddStatusCode, RemoveStatusCode | Name, Code, ApplicableStatusCodes |

### ConfigVehicleType — Custom Endpoints

```
POST /api/v1/ConfigVehicleType/AddFuelType_Mapping
  Body: { VehicleTypeId: long, FuelTypeId: long }
  → Thêm mapping FuelType cho VehicleType

POST /api/v1/ConfigVehicleType/RemoveFuelType_Mapping
  Body: { VehicleTypeId: long, FuelTypeId: long }
  → Xoá mapping FuelType
```

### ConfigVehicleRentalCancelReason — Custom Endpoints

```
POST /api/v1/ConfigVehicleRentalCancelReason/AddStatusCode
  Body: { CancelReasonId: long, StatusCode: string }
  → Thêm status code cho phép cancel với lý do này

POST /api/v1/ConfigVehicleRentalCancelReason/RemoveStatusCode
  Body: { CancelReasonId: long, StatusCode: string }
  → Xoá mapping
```

---

## Payment & Finance Domain (7 controllers)

| # | Controller | Endpoints | Custom | Entity quan trọng |
|---|------------|-----------|--------|-------------------|
| 1 | `ConfigPaymentType` | 1 | — | Name, Code (EWALLET/BANK/CASH) |
| 2 | `ConfigDiscountMethod` | 1 | — | Name, Code (PERCENT/MONEY) |
| 3 | `ConfigDiscountCodeGroup` | 1 | — | Name, Prefix |
| 4 | `ConfigCompletionFee` | 1 | — | FeePercent, ApplyLevel |
| 5 | `ConfigDeliveryFee` | 1 | — | Code, Operator, ChargeType, FirstValue, SecondValue, Value, OrderNo |
| 6 | `ConfigMBBank` | 1 | — | APIUrl, ClientId, IsActive |
| 7 | `ConfigMISAInvoice` | 1 | — | APIUrl, AppId, TaxCode |

### ConfigDeliveryFee — Entity Fields (quan trọng cho pricing)

| Field | Type | Mô tả |
|-------|------|-------|
| Code | string | `EARLY_CAR_DELIVERY_FEE` / `LATE_CAR_DELIVERY_FEE` |
| ParentId | long? | FK self-reference (parent-child hierarchy) |
| Operator | string | Rule operator (10 types — xem helper-algorithms.md) |
| ChargeType | string | `ByHour` / `ByDay` |
| FirstValue | decimal? | Giá trị 1 cho operator |
| SecondValue | decimal? | Giá trị 2 cho InRange operators |
| Value | decimal? | Hệ số (1.0 = full day, 0.5 = half day) |
| OrderNo | decimal | Thứ tự ưu tiên match (first match wins) |
| ArrJson | string | JSON array: `ConfigDeliveryFeeArrJsonModel[]` |

**Cấu trúc parent-child (từ DB):**
```
EARLY_CAR_DELIVERY_FEE (Id=1, parent)
  ├── OrderNo=1: LessThanOrEqual 9h → ByDay 1.0
  ├── OrderNo=2: InRangeInclusiveStart 3-9h → ByDay 0.5
  └── OrderNo=3: LessThan 3h → ByHour

LATE_CAR_DELIVERY_FEE (Id=2, parent)
  ├── OrderNo=1: LessThanOrEqual 15h → ByDay 1.0
  ├── OrderNo=2: InRangeInclusiveStart 3-15h → ByDay 0.5
  └── OrderNo=3: LessThan 3h → ByHour
```

**ArrJson structure:**
```json
[{
  "OrderNo": 1,
  "Operator": "InRangeInclusiveStart",
  "FirstValue": 0,
  "SecondValue": 6,
  "ChargeType": "ByHour",
  "Value": null
}, {
  "OrderNo": 2,
  "Operator": "GreaterThanOrEqual",
  "FirstValue": 6,
  "SecondValue": null,
  "ChargeType": "ByDay",
  "Value": 0.5
}]
```

---

## User & Auth Domain (5 controllers)

| # | Controller | Endpoints | Custom | Entity quan trọng |
|---|------------|-----------|--------|-------------------|
| 1 | `ConfigUserImageType` | 1 | — | Name, Code (AVATAR/CCCD_FRONT/CCCD_BACK/GPLX) |
| 2 | `ConfigUserImageTypeDetail` | 1 | — | Name, ImageTypeId, Description |
| 3 | `ConfigUserVerified` | 1 | — | Name, Code, StepOrder |
| 4 | `ConfigHMACKey` | 1 | — | KeyValue, IsActive (AddKey commented out) |
| 5 | `ConfigRSAKey` | 2 | AddKey | PublicKey, PrivateKey, IsActive |

### ConfigRSAKey — Custom Endpoint

```
POST /api/v1/ConfigRSAKey/AddKey
  Body: { KeySize: int }
  → Tự generate RSA key pair và lưu vào DB
```

---

## Service Domain (8 controllers)

| # | Controller | Endpoints | Custom | Entity quan trọng |
|---|------------|-----------|--------|-------------------|
| 1 | `ConfigRentalServiceCategory` | **19** | 15 custom | Name, Code, SettingJson |
| 2 | `ConfigRentalServiceCategorySetting` | 1 | — | RentalServiceCategoryId, SettingJson |
| 3 | `ConfigRentalRequiredItem` | 1 | — | Name, Code (CMND/GPLX/PASSPORT) |
| 4 | `ConfigServiceDocumentType` | 1 | — | Name, Code |
| 5 | `ConfigServiceImageType` | 1 | — | Name, Code (FRONT/BACK/LEFT/RIGHT/INTERIOR) |
| 6 | `ConfigServiceItemFeature` | 1 | — | Name, Code, IconUrl |
| 7 | `ConfigRentalServiceRatingComment` | 1 | — | Text, RatingPoint, ForRole |
| 8 | `ConfigRentalServiceRatingPoint` | 1 | — | Point, Label |

### ConfigRentalServiceCategory — 15 Custom Endpoints (phức tạp nhất)

Quản lý mapping giữa Category và các config entities khác:

```
// Image Type Mapping
POST /AddImage_Mapping      { CategoryId, ImageTypeId }
POST /RemoveImage_Mapping   { CategoryId, ImageTypeId }

// Image In Use Mapping
POST /AddImageInUse_Mapping    { CategoryId, ImageTypeId }
POST /RemoveImageInUse_Mapping { CategoryId, ImageTypeId }

// Document Type Mapping
POST /AddDocument_Mapping    { CategoryId, DocumentTypeId }
POST /RemoveDocument_Mapping { CategoryId, DocumentTypeId }

// Required Item Mapping
POST /AddRequiredItem_Mapping    { CategoryId, RequiredItemId }
POST /RemoveRequiredItem_Mapping { CategoryId, RequiredItemId }

// Feature Mapping
POST /AddFeature_Mapping    { CategoryId, FeatureId }
POST /RemoveFeature_Mapping { CategoryId, FeatureId }

// Order Status Mapping
POST /AddOrderStatus_Mapping    { CategoryId, OrderStatusId }
POST /RemoveOrderStatus_Mapping { CategoryId, OrderStatusId }

// Rating Comment Mapping
POST /AddComment_Mapping    { CategoryId, CommentId }
POST /RemoveComment_Mapping { CategoryId, CommentId }

// Settings
GET  /GetJsonSetting/{categoryId}  → RentalServiceCategorySettingJsonModel
```

**Behavioral Notes:**

| Scenario | Hành vi |
|----------|---------|
| Add mapping đã tồn tại | Idempotent — không duplicate, return Status=1 |
| Remove mapping không tồn tại | Silent success — return Status=1 |
| Add mapping với FK không tồn tại | Error Status=0 — FK phải exist và IsDeleted=false |
| Remove mapping đang được dùng bởi RentalService | Mapping bị xoá — KHÔNG cascade tới RentalService đã dùng |

**GetJsonSetting Response:**
```json
// GET /api/v1/ConfigRentalServiceCategory/GetJsonSetting/1
{
  "Status": 1,
  "Data": {
    "MaxDistance": 30000,
    "MaxVehicleCount": 20,
    "ShowDiscountInSearch": true,
    "IsIgnoreInsuranceValidCheck": false,
    "DefaultSortBy": "Distance",
    "BusinessHourStart": 7,
    "BusinessHourEnd": 21
  }
}
```

---

## UI / Content Domain (6 controllers)

| # | Controller | Endpoints | Custom | Entity quan trọng |
|---|------------|-----------|--------|-------------------|
| 1 | `ConfigLandingPage` | **13** | 11 custom | Title, Slug, MetaTitle, Content, IsTemplate |
| 2 | `ConfigLandingPageSetting` | 2 | UpdateFields | Key, Value, IsActive |
| 3 | `ConfigSlide` | 1 | — | Name, ScreenCode, OrderNo |
| 4 | `ConfigSlideScreenMapping` | 1 | — | SlideId, ScreenCode |
| 5 | `ConfigSlide_File` | 1 | — | SlideId, FileUrl, OrderNo |
| 6 | `ConfigMenu` (Main) | 1 | CategoryScreenList | Menu items list |

### ConfigLandingPage — 11 Custom Endpoints

```
// Website & API Settings
POST /UpdateWebsiteSetting   { LandingPageId, SettingJson }
POST /UpdateAPISetting       { LandingPageId, APISettingJson }

// Template Management
POST /CreateLandingPageFromTemplate  { TemplateId, Title }
POST /ChangeTemplateToEdit           { LandingPageId }

// Data Operations
GET  /GetLandingPageData/{id}   → Full page data with children
POST /UpdateSlugStatus          { LandingPageId, IsNoIndex }
POST /ExportConfig              { LandingPageId } → JSON export
POST /ImportConfig              { ConfigJson }    → JSON import

// Row Operations
POST /CopyFromDetailRow         { LandingPageId, SourceRowId }
POST /CopyFromDetailRow_NoData  { LandingPageId, SourceRowId }

// Form
GET  /GetRequestInfoFormDefault → Default form config
```

**Related controllers:**
- `ConfigLandingPageDetailController` — Detail rows
- `ConfigLandingPageDetail_DeletedController` — Deleted detail rows
- `ConfigLandingPage_ReleatedController` — Related pages
- `ConfigLandingPage_DeletedController` — Deleted pages + `Restore` endpoint

---

## System Domain (7 controllers)

| # | Controller | Endpoints | Custom | Entity quan trọng |
|---|------------|-----------|--------|-------------------|
| 1 | `ConfigOrderStatus` | 1 | — | Name, Code, ColorCode, OrderNo |
| 2 | `ConfigMyOrderGroup` | 1 | — | Name, StatusCodes (JSON array) |
| 3 | `ConfigReportOrderReason` | 1 | — | Name, Code |
| 4 | `ConfigReportUserReason` | 1 | — | Name, Code |
| 5 | `ConfigAppVersion` | 1 | — | Platform, Version, ForceUpdate |
| 6 | `ConfigCountry` | 1 | — | Name, Code, PhoneCode |
| 7 | `ConfigAddress` | 2 | UpdateLatAndLng | Name, Type (Tinh/Huyen/Xa), ParentId, Lat, Lng |

### ConfigOrderStatus — Actual Data (từ DB)

| Code | Name | ColorCode | OrderNo |
|------|------|-----------|---------|
| `OWNER2CONFIRM` | Chờ xác nhận | #FFA500 | 1 |
| `CUS2DEPOSIT` | Chờ thanh toán | #FFD700 | 2 |
| `WAITING2CONFIRMDEPOSIT` | Chờ xác nhận cọc | #DAA520 | 3 |
| `WAITING2DEPARTURE` | Chờ xuất phát | #4169E1 | 4 |
| `INTHETRIP` | Đang trong chuyến | #32CD32 | 5 |
| `DONE` | Hoàn thành | #228B22 | 6 |
| `CUSCANCEL` | Khách huỷ | #DC143C | 7 |
| `OWNERCANCEL` | Chủ xe huỷ | #B22222 | 8 |
| `SYSTEMCANCEL` | Hệ thống huỷ | #808080 | 9 |

### ConfigAddress — Custom Endpoint

```
POST /api/v1/ConfigAddress/UpdateLatAndLng
  Body: { AddressId: long, Latitude: decimal, Longitude: decimal }
  → Cập nhật toạ độ GPS cho địa chỉ hành chính
```

---

## Infrastructure Domain (4 controllers)

| # | Controller | Endpoints | Custom | Entity quan trọng |
|---|------------|-----------|--------|-------------------|
| 1 | `ConfigSFTP` | 1 | — | Host, Port, Username, RootPath |
| 2 | `ConfigBank` | 1 | — | Name, Code, BIN, SwiftCode |
| 3 | `ConfigBeneficiaryBank` | 1 | — | BankId, AccountNumber, AccountName |
| 4 | `ConfigCriteriaPriority` | 1+detail | — | Name, Weight, OrderNo |

---

## CMS Module (3 controllers)

**Path prefix:** `Ezy.Module.CMS.API/Controllers/Categories/`

| # | Controller | Endpoints | Custom | Entity quan trọng |
|---|------------|-----------|--------|-------------------|
| 1 | `ConfigArticleType` | 3 | AddTopicMapping, RemoveTopicMapping | Name, Code |
| 2 | `ConfigTopic` | 1 | — | Name, Slug, MetaTitle |
| 3 | `ConfigSystemDomain` | 1 | — | DomainName, IsActive |

### ConfigArticleType — Custom Endpoints

```
POST /AddTopicMapping    { ArticleTypeId, TopicId }
POST /RemoveTopicMapping { ArticleTypeId, TopicId }
```

---

## Other Modules (6 controllers)

### Module Menu Controllers (4)

Mỗi module có 1 `ConfigMenuController` với endpoint `CategoryScreenList`:

| Module | File Path |
|--------|-----------|
| Main | `AllianceMiddlemanWebAPI/Controllers/ConfigMenuController.cs` |
| CMS | `Ezy.Module.CMS/Controllers/ConfigMenuController.cs` |
| EWallet | `Ezy.Module.EWallet/Controllers/ConfigMenuController.cs` |
| TrafficTicket | `Ezy.Module.TrafficTicket/Controllers/ConfigMenuController.cs` |
| DynamicReport | `Ezy.Module.DynamicReport/Controllers/ConfigMenuController.cs` |

```
GET /api/v1/ConfigMenu/CategoryScreenList → BaseCategoryItem[]
  → Trả về danh sách category screens cho module admin UI
```

### Hotline Module (2)

| Controller | Endpoints | Custom |
|------------|-----------|--------|
| `ConfigOMICallAPI` | 3 | UpdateAccessToken, WebhookAction |
| `ConfigOMICallHotline` | 1 | — |

```
POST /api/v1/ConfigOMICallAPI/UpdateAccessToken  → Refresh OMI Call access token
POST /api/v1/ConfigOMICallAPI/WebhookAction       → OMI Call webhook receiver
```

### DynamicReport Module (1)

| Controller | Endpoints | Custom |
|------------|-----------|--------|
| `ConfigReportItemType` | 1 | — |

---

## Controllers với Custom Endpoints

Tổng hợp tất cả controllers có endpoints ngoài CRUD chuẩn:

| Controller | Domain | Custom Endpoints | Tổng |
|------------|--------|-----------------|------|
| `ConfigRentalServiceCategory` | Service | 15 mapping + settings | 19 |
| `ConfigLandingPage` | UI/Content | 11 template/export/import | 13 |
| `ConfigVehicleType` | Vehicle | 2 FuelType mapping | 3 |
| `ConfigVehicleRentalCancelReason` | Vehicle | 2 StatusCode mapping | 3 |
| `ConfigArticleType` | CMS | 2 Topic mapping | 3 |
| `ConfigOMICallAPI` | Hotline | 2 token/webhook | 3 |
| `ConfigVehicleDiscountSuggestion` | Vehicle | 1 UpdatePrice | 2 |
| `ConfigRSAKey` | Auth | 1 AddKey | 2 |
| `ConfigAddress` | System | 1 UpdateLatAndLng | 2 |
| `ConfigLandingPageSetting` | UI | 1 UpdateFields | 2 |
| `ConfigMenu` (all modules) | System | CategoryScreenList | 1 |

---

*Xem thêm: [base-service-pattern.md](../01_ARCHITECTURE/base-service-pattern.md) | [database-design.md](../01_ARCHITECTURE/database-design.md)*
