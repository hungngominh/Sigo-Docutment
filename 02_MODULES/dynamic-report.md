# Module: DynamicReport — Báo cáo động

> **Solution:** `Ezy.Module.DynamicReport` | **Trạng thái:** Active

## Mục lục
- [Tổng quan](#tổng-quan)
- [Cấu trúc](#cấu-trúc)
- [Entities & Models](#entities--models)
- [Services](#services)
- [API Endpoints](#api-endpoints)
- [Cách tạo báo cáo mới](#cách-tạo-báo-cáo-mới)
- [Export formats](#export-formats)

---

## Tổng quan

Module DynamicReport cung cấp hệ thống **báo cáo động** (configurable reports) cho Sigo platform. Thay vì hardcode từng loại báo cáo, module này cho phép:

- Tạo cấu trúc báo cáo từ template
- Điền nội dung theo từng khách hàng/context
- Export ra nhiều định dạng (HTML, config)
- Tự động tạo blog post từ báo cáo

**Đối tượng sử dụng:**
- **Staff/Admin:** Tạo và quản lý báo cáo cho từng khách hàng
- **Manager:** Review và confirm báo cáo
- **Hệ thống:** Tự động generate báo cáo theo lịch

**Use cases thực tế:**
- Báo cáo tình trạng xe (vehicle inspection report) gửi cho chủ xe
- Báo cáo doanh thu theo kỳ
- Báo cáo sự cố/vi phạm
- Tài liệu lịch điều trị/lịch trình (nếu dùng cho domain khác)

**Tích hợp:**
- CMS module — tự động tạo blog post từ báo cáo (GenerateBlogPost)
- EPPlus — export Excel
- EvoPdfToImage — export PDF

---

## Cấu trúc

```
Ezy.Module.DynamicReport/
├── Ezy.Module.DynamicReport.API/
│   └── Controllers/
│       └── Categories/
│           ├── Report2ClientController.cs                   # Core report CRUD
│           ├── Report2Client_PartController.cs              # Report sections
│           ├── Report2Client_TextArrayController.cs         # Text fields
│           ├── Report2Client_SignatureController.cs         # Chữ ký
│           ├── Report2Client_TreatmentScheduleController.cs # Lịch trình
│           ├── Report2Client_RecommendationNRemarkController.cs # Ghi chú
│           ├── ConfigReportItemTypeController.cs            # Loại report item
│           ├── DynamicReportUIControlSettingController.cs   # UI settings
│           └── Staff_SignatureController.cs                 # Chữ ký staff
│
├── Ezy.Module.DynamicReport.Core/
│   └── Data/                                               # DbContext + Entities
│
├── Ezy.Module.DynamicReport.Service/
│   └── Services/                                           # Business logic
│
├── Ezy.Module.DynamicReport.DataShared/                    # DTOs
├── Ezy.Module.DynamicReport.Shared/                        # Shared helpers
└── Script/                                                 # DB migrations
```

**Tổng số C# files:** ~130

---

## Entities & Models

### `Report2Client` — Báo cáo gửi khách

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `ID_GUID` | Guid | Business key |
| `ClientId` | Guid | FK → User/Customer |
| `TemplateId` | int | FK → template gốc |
| `Title` | string | Tiêu đề báo cáo |
| `Status` | string | `Draft` / `Confirmed` / `Published` |
| `Version` | int | Phiên bản báo cáo |
| `IsDeleted` | bool | Soft delete |
| `Log_CreatedDate` | DateTime | |
| `Log_CreatedBy` | string | Staff tạo báo cáo |
| `Log_UpdatedDate` | DateTime | |
| `ConfirmedBy` | string | Staff xác nhận |
| `ConfirmedAt` | DateTime | |

### `Report2Client_Part` — Phần/section của báo cáo

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `ReportGuid` | Guid | FK → Report2Client |
| `PartType` | string | Loại phần (header, body, section...) |
| `Title` | string | Tiêu đề section |
| `DisplayOrder` | int | Thứ tự hiển thị |
| `Content` | string | Nội dung HTML |

### `Report2Client_TextArray` — Text fields động

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `ReportGuid` | Guid | FK → Report2Client |
| `FieldKey` | string | Key của field (ví dụ: `vehicle_plate`, `rental_date`) |
| `FieldValue` | string | Giá trị điền vào |
| `FieldType` | string | Loại: `text`, `number`, `date`, `boolean` |

### `Report2Client_Signature` — Chữ ký

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `ReportGuid` | Guid | FK → Report2Client |
| `SignerName` | string | Tên người ký |
| `SignerRole` | string | Vai trò (Owner, Renter, Staff...) |
| `SignatureImagePath` | string | Ảnh chữ ký (base64 hoặc SFTP path) |
| `SignedAt` | DateTime | Thời điểm ký |

### `Report2Client_TreatmentSchedule` — Lịch trình

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `ReportGuid` | Guid | FK → Report2Client |
| `ScheduleDate` | DateTime | Ngày trong lịch |
| `Description` | string | Mô tả hoạt động |
| `Status` | string | `Pending` / `Completed` |

### `Report2Client_RecommendationNRemark` — Ghi chú/Khuyến nghị

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `ReportGuid` | Guid | FK → Report2Client |
| `Type` | string | `Recommendation` hoặc `Remark` |
| `Content` | string | Nội dung ghi chú |
| `CreatedBy` | string | Staff ghi |

### `ConfigReportItemType` — Loại report item

Cấu hình các loại field/component trong báo cáo (lookup table).

### `DynamicReportUIControlSetting` — UI settings

Cấu hình hiển thị UI cho từng loại control trong báo cáo.

---

## Services

| Service | Mô tả |
|---------|-------|
| `IReport2ClientService` | Core CRUD + workflow báo cáo |
| `IReport2Client_PartService` | Quản lý sections của báo cáo |
| `IReport2Client_TextArrayService` | Quản lý text fields |
| `IReport2Client_SignatureService` | Xử lý chữ ký |
| `IReport2Client_TreatmentScheduleService` | Quản lý lịch trình |
| `IReport2Client_RecommendationNRemarkService` | Quản lý ghi chú |
| `IConfigReportItemTypeService` | Quản lý config loại item |
| `IDynamicReportUIControlSettingService` | Quản lý UI settings |

---

## API Endpoints

Base URL: `/api/v1/`

### Báo cáo chính (Report2Client)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Report2Client/List` | Danh sách báo cáo với filter | Required |
| POST | `/Report2Client/AddFromForm` | Tạo báo cáo từ form input | Required |
| POST | `/Report2Client/AddFromTemplate` | Tạo báo cáo từ template | Required |
| POST | `/Report2Client/Confirm` | Confirm/finalize báo cáo | Required |
| POST | `/Report2Client/ExportHtml` | Export báo cáo ra HTML | Required |
| POST | `/Report2Client/ExportConfigs` | Export cấu hình báo cáo (JSON) | Required |
| POST | `/Report2Client/ImportConfigs` | Import cấu hình báo cáo | Required |
| POST | `/Report2Client/GenerateChildReports` | Tạo báo cáo con tự động | Required |
| POST | `/Report2Client/GenerateBlogPost` | Tạo blog post từ báo cáo | Required |

### Thành phần báo cáo

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Report2Client_Part/List` | Danh sách sections | Required |
| POST | `/Report2Client_Part/Add` | Thêm section | Required |
| POST | `/Report2Client_Part/Update` | Cập nhật section | Required |
| POST | `/Report2Client_TextArray/List` | Danh sách text fields | Required |
| POST | `/Report2Client_TextArray/Add` | Thêm text field | Required |
| POST | `/Report2Client_Signature/List` | Danh sách chữ ký | Required |
| POST | `/Report2Client_Signature/Add` | Thêm chữ ký | Required |
| POST | `/Report2Client_TreatmentSchedule/List` | Lịch trình | Required |
| POST | `/Report2Client_RecommendationNRemark/List` | Ghi chú | Required |

### Cấu hình

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/ConfigReportItemType/List` | Loại report items | Required |
| POST | `/DynamicReportUIControlSetting/List` | UI control settings | Required |
| POST | `/Staff_Signature/List` | Chữ ký của staff | Required |

---

## Cách tạo báo cáo mới

### Bước 1: Chọn phương thức tạo

```
Cách 1: Từ form                    Cách 2: Từ template
POST /Report2Client/AddFromForm    POST /Report2Client/AddFromTemplate
    │ Điền manual                      │ Load preset fields từ template
    ▼                                  ▼
Report2Client {Draft}              Report2Client {Draft} + pre-filled fields
```

### Bước 2: Điền nội dung

```
1. Thêm text fields:
   POST /Report2Client_TextArray/Add
   { ReportGuid: "...", FieldKey: "vehicle_plate", FieldValue: "51A-12345" }

2. Thêm lịch trình (nếu cần):
   POST /Report2Client_TreatmentSchedule/Add

3. Thêm ghi chú:
   POST /Report2Client_RecommendationNRemark/Add
   { Type: "Recommendation", Content: "Kiểm tra lốp xe trước chuyến" }

4. Thu thập chữ ký:
   POST /Report2Client_Signature/Add
   { SignerName: "Nguyễn Văn A", SignerRole: "Owner", SignatureImagePath: "..." }
```

### Bước 3: Confirm và Export

```
1. Confirm báo cáo:
   POST /Report2Client/Confirm
   → Status: Draft → Confirmed

2. Export HTML:
   POST /Report2Client/ExportHtml
   → Trả về HTML string

3. (Tùy chọn) Tạo blog post:
   POST /Report2Client/GenerateBlogPost
   → Tạo EzyWeb_BlogPost trong CMS module
```

### Bước 4: Config Import/Export (để tái sử dụng template)

```
Export cấu hình báo cáo mẫu:
POST /Report2Client/ExportConfigs → JSON config

Import để tạo báo cáo tương tự:
POST /Report2Client/ImportConfigs + JSON → Report2Client mới
```

---

## Export formats

| Format | Endpoint | Mô tả |
|--------|---------|-------|
| **HTML** | `ExportHtml` | HTML string cho preview hoặc print |
| **JSON Config** | `ExportConfigs` | Cấu trúc báo cáo để import lại |
| **Blog Post** | `GenerateBlogPost` | Publish lên CMS module |

> **Lưu ý:** Export PDF và Excel hiện tại thực hiện qua `FileWillBeDownload` mechanism — tạo file trong background, user download sau.

---

*Xem thêm: [cms.md](./cms.md) | [background-engines.md](../01_ARCHITECTURE/background-engines.md)*
