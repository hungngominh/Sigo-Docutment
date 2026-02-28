# API: Dynamic Report (Báo cáo động)

> **Module:** `Ezy.Module.DynamicReport`
> **Controllers:** `Report2ClientController`, `Report2Client_PartController`, `Report2Client_TextArrayController`, `Report2Client_SignatureController`, `Report2Client_RecommendationNRemarkController`, `Report2Client_TreatmentScheduleController`, `Staff_SignatureController`, `DynamicReportUIControlSettingController`, `ConfigReportItemTypeController`
> **Base:** `/api/v1/Report2Client*`, `/api/v1/Staff_Signature`, `/api/v1/DynamicReportUIControlSetting`, `/api/v1/ConfigReportItemType`
> **Phân quyền:** `[Authorize]` (tất cả endpoint)

## Mục lục
- [Tổng quan](#tổng-quan)
- [Báo cáo — CRUD](#post-apiv1report2clientlist)
- [Báo cáo — Tạo từ Form](#post-apiv1report2clientaddfromform)
- [Báo cáo — Tạo từ Template](#post-apiv1report2clientaddfromtemplate)
- [Báo cáo — Xác nhận & Xuất](#post-apiv1report2clientconfirm)
- [Báo cáo — Sinh Blog](#post-apiv1report2clientgenerateblogpost)
- [Part — Phần báo cáo](#post-apiv1report2client_partlist)
- [Text Array — Nội dung văn bản](#post-apiv1report2client_textarraylist)
- [Signature — Chữ ký báo cáo](#post-apiv1report2client_signaturelist)
- [Recommendation — Khuyến nghị](#post-apiv1report2client_recommendationnremarklist)
- [Treatment Schedule — Lịch xử lý](#post-apiv1report2client_treatmentschedulelist)
- [Staff Signature — Chữ ký nhân viên](#post-apiv1staff_signaturelist)
- [UI Control Settings](#post-apiv1dynamicreportuicontrolsettingoptions)
- [Cấu hình loại mục báo cáo](#post-apiv1configreportitemtypelist)

---

## Tổng quan

Module DynamicReport quản lý hệ thống báo cáo động (report builder) cho khách hàng:
- Tạo báo cáo từ form hoặc từ template
- Quản lý các phần của báo cáo (Parts), văn bản, chữ ký
- Lịch trình xử lý và khuyến nghị
- Xuất báo cáo sang HTML, config files
- Sinh bài viết blog từ báo cáo

---

## `POST /api/v1/Report2Client/List`

**Mô tả:** Lấy danh sách báo cáo khách hàng.

### Request Body — `Report2ClientParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "Keyword": "",
  "Status": "Confirmed"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<Report2ClientModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 50,
    "Data": [
      {
        "ReportId": "report-guid",
        "Title": "Báo cáo kiểm tra xe 51A-12345",
        "Status": "Draft",
        "CreatedAt": "2026-01-01T10:00:00Z",
        "ClientName": "Nguyễn Văn A"
      }
    ]
  }
}
```

---

## `POST /api/v1/Report2Client/AddFromForm`

**Mô tả:** Tạo báo cáo mới từ form nhập liệu.

### Request Body — `Report2Client_AddNewFormModel`
```json
{
  "Title": "Báo cáo kiểm tra xe",
  "ClientId": "client-guid",
  "VehicleId": "vehicle-guid",
  "ReportTypeId": 1,
  "InspectionDate": "2026-01-01"
}
```

### Response — `EzyResultObject<Report2ClientModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "ReportId": "report-guid",
    "Title": "Báo cáo kiểm tra xe",
    "Status": "Draft"
  }
}
```

---

## `POST /api/v1/Report2Client/AddFromTemplate`

**Mô tả:** Tạo báo cáo từ template có sẵn (copy cấu trúc template).

### Request Body — `Report2Client_AddFromTemplateFormModel`
```json
{
  "TemplateId": "template-guid",
  "ClientId": "client-guid",
  "Title": "Báo cáo kiểm tra tháng 1/2026"
}
```

---

## `POST /api/v1/Report2Client/Confirm`

**Mô tả:** Xác nhận hoàn tất báo cáo (chuyển từ Draft → Confirmed).

### Request Body — `Report2ClientParamModel`
```json
{
  "ReportId": "report-guid"
}
```

### Response — `EzyResultObject<Report2ClientModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "ReportId": "report-guid",
    "Status": "Confirmed",
    "ConfirmedAt": "2026-01-01T15:00:00Z"
  }
}
```

---

## `POST /api/v1/Report2Client/ExportHtml`

**Mô tả:** Xuất báo cáo sang định dạng HTML.

### Request Body — `Report2ClientParamModel`
```json
{
  "ReportId": "report-guid"
}
```

### Response — `EzyResultObject<Report2ClientModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "HtmlContent": "<html>...</html>",
    "FileUrl": "https://..."
  }
}
```

---

## `POST /api/v1/Report2Client/ExportConfigs`

**Mô tả:** Xuất cấu hình báo cáo (dùng để backup hoặc chuyển sang hệ thống khác).

---

## `POST /api/v1/Report2Client/ImportConfigs`

**Mô tả:** Import cấu hình báo cáo từ file đã export.

---

## `POST /api/v1/Report2Client/GenerateChildReports`

**Mô tả:** Sinh ra các báo cáo con từ báo cáo gốc (1 → nhiều).

### Request Body — `Report2ClientParamModel`
```json
{
  "ReportId": "parent-report-guid"
}
```

### Response — `EzyResultObject<Report2ClientModel[]>`
```json
{
  "StatusCode": 1,
  "Data": [
    { "ReportId": "child-report-guid-1", "Title": "Báo cáo con 1" },
    { "ReportId": "child-report-guid-2", "Title": "Báo cáo con 2" }
  ]
}
```

---

## `POST /api/v1/Report2Client/GenerateBlogPost`

**Mô tả:** Tự động sinh bài viết blog từ nội dung báo cáo (tích hợp với CMS module).

### Request Body — `Report2ClientParamModel`
```json
{
  "ReportId": "report-guid"
}
```

### Response — `EzyResultObject<EzyWeb_BlogPostModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "PostId": "post-guid",
    "Title": "Kiểm tra xe 51A-12345 tháng 1/2026",
    "Slug": "kiem-tra-xe-51a-12345-thang-1-2026"
  }
}
```

---

## `POST /api/v1/Report2Client_Part/List`

**Mô tả:** Lấy danh sách các phần (sections) của báo cáo.

### Request Body — `Report2Client_PartParamModel`
```json
{
  "ReportId": "report-guid",
  "PageIndex": 1,
  "PageSize": 50
}
```

---

## `POST /api/v1/Report2Client_Part/UpdateCanShow`

**Mô tả:** Cập nhật trạng thái hiển thị của một phần trong báo cáo.

---

## `POST /api/v1/Report2Client_Part/ExportConfigs` / `ImportConfigs`

**Mô tả:** Xuất/Import cấu hình từng phần báo cáo.

---

## `POST /api/v1/Report2Client_TextArray/List`

**Mô tả:** Lấy nội dung văn bản dạng mảng trong báo cáo.

---

## `POST /api/v1/Report2Client_Signature/List`

**Mô tả:** Lấy danh sách chữ ký trong báo cáo.

---

## `POST /api/v1/Report2Client_Signature/Sign`

**Mô tả:** Ký xác nhận vào báo cáo.

### Request Body — `Report2Client_SignatureParamModel`
```json
{
  "SignatureId": "sig-guid",
  "SignatureData": "base64-image-data",
  "SignerName": "Nguyễn Văn A"
}
```

### Response — `EzyResultObject<object>`
```json
{
  "StatusCode": 1,
  "Msg": "Ký thành công"
}
```

---

## `POST /api/v1/Report2Client_RecommendationNRemark/List`

**Mô tả:** Lấy danh sách khuyến nghị và ghi chú trong báo cáo.

---

## `POST /api/v1/Report2Client_TreatmentSchedule/List`

**Mô tả:** Lấy danh sách lịch trình xử lý trong báo cáo.

---

## `POST /api/v1/Report2Client_TreatmentSchedule/AddTreatmentArea`

**Mô tả:** Thêm vùng xử lý vào lịch trình.

### Request Body — `Report2Client_TreatmentSchedule_TreatmentAreaAddMultiModel`
```json
{
  "ScheduleId": "schedule-guid",
  "Areas": [
    { "AreaName": "Khu A", "AreaCode": "A1" }
  ]
}
```

---

## `POST /api/v1/Report2Client_TreatmentSchedule/RemoveTreatmentArea`

**Mô tả:** Xoá vùng xử lý khỏi lịch trình.

---

## `POST /api/v1/Report2Client_TreatmentSchedule/AddUsedChemical`

**Mô tả:** Thêm hoá chất đã sử dụng vào lịch trình.

### Request Body — `Report2Client_TreatmentSchedule_UsedChemicalAddMultiModel`
```json
{
  "ScheduleId": "schedule-guid",
  "Chemicals": [
    { "ChemicalName": "Permethrin", "Dosage": "50ml", "Unit": "ml" }
  ]
}
```

---

## `POST /api/v1/Report2Client_TreatmentSchedule/RemoveUsedChemical`

**Mô tả:** Xoá hoá chất khỏi lịch trình.

---

## `POST /api/v1/Staff_Signature/List`

**Mô tả:** Lấy danh sách chữ ký nhân viên (dùng để điền vào báo cáo).

### Request Body — `Staff_SignatureParamModel`
```json
{
  "StaffId": "staff-guid"
}
```

---

## `POST /api/v1/DynamicReportUIControlSetting/Options`

**Mô tả:** Lấy cấu hình UI controls (dropdowns, field types) cho trang báo cáo.

### Request Body — `DynamicReportGetPageConfigParamModel`
```json
{
  "PageCode": "Report2Client_Detail"
}
```

### Response — `EzyResultObject<AppScreenUIControlSettingOptionModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Fields": [
      {
        "FieldName": "Status",
        "ControlType": "Dropdown",
        "Options": [
          { "Value": "Draft", "Label": "Nháp" },
          { "Value": "Confirmed", "Label": "Đã xác nhận" }
        ]
      }
    ]
  }
}
```

---

## `POST /api/v1/ConfigReportItemType/List`

**Mô tả:** Lấy danh sách loại mục báo cáo (cấu hình).

---

## Ghi chú

- `GenerateBlogPost` tích hợp với CMS module — cần cả 2 module cùng hoạt động.
- `TreatmentSchedule` sử dụng cho báo cáo có lịch trình xử lý (phun thuốc, vệ sinh, v.v.).
- `ExportConfigs` / `ImportConfigs` dùng để clone template báo cáo giữa các môi trường (staging → production).
