# Luồng nghiệp vụ: Chủ xe đăng ký dịch vụ

## Tổng quan

Luồng từ khi chủ xe đăng ký tài khoản đến khi xe được duyệt và hiển thị trên nền tảng.

---

## Sơ đồ luồng

```
[Chủ xe]                        [Hệ thống]                    [Admin]
     │                               │                              │
     │── Đăng ký tài khoản ─────────►│ Account/Register             │
     │── Xác thực OTP ──────────────►│ Account/VerifyOTP            │
     │── Cập nhật profile ──────────►│ User/MyProfile               │
     │── Upload CCCD/GPLX ──────────►│ UserUpload                   │
     │                               │──── Gửi yêu cầu duyệt ──────►│
     │                               │◄─── Duyệt profile ───────────│
     │                               │                              │
     │── Thêm thông tin xe ─────────►│ Vehicle/Create               │
     │── Upload ảnh xe ─────────────►│ ServiceItem_Image            │
     │── Upload giấy tờ xe ─────────►│ ServiceItem_Document         │
     │── Cấu hình giá ─────────────►│ ServiceItem_DateRentalPrice   │
     │── Cấu hình lịch ─────────────►│ ServiceItem_DateBusySchedule │
     │                               │──── Gửi yêu cầu duyệt xe ───►│
     │                               │◄─── Duyệt xe ────────────────│
     │                               │                              │
     │◄─ Xe được hiển thị trên app ──│                              │
```

---

## Chi tiết từng bước

### Bước 1: Đăng ký & xác thực
1. `POST /api/v1/Account/GetOTP` — Gửi OTP
2. `POST /api/v1/Account/VerifyOTP` — Xác minh OTP
3. `POST /api/v1/Account/Register` — Tạo tài khoản

### Bước 2: Hoàn thiện profile
- Upload ảnh CCCD, GPLX qua `UserUpload`
- Cấu hình thông tin ngân hàng nhận tiền: `UserLogin_BeneficiaryBank_Mapping`

### Bước 3: Đăng ký xe
- `POST /api/v1/Vehicle/Create` — Tạo xe
- Upload ảnh: `ServiceItem_Image`
- Upload giấy tờ: `ServiceItem_Document`

### Bước 4: Cấu hình giá & lịch
- Giá cố định: `ServiceItem_DateRentalPrice`
- Giá theo thứ: `ServiceItem_WeekdayRentalPrice`
- Giảm giá nhiều ngày: `VehicleMultidayRentalDiscount_Detail`
- Lịch bận: `ServiceItem_DateBusyRentalSchedule`

### Bước 5: Cấu hình tính năng xe
- `ServiceItem_Feature_Mapping` — GPS, camera hành trình...
- `ServiceItem_RentalRequiredItem_Mapping` — Yêu cầu GPLX, CCCD...

---

## Điều kiện duyệt xe

### Trạng thái duyệt (RentalServiceItem)

| Trạng thái | Điều kiện | Mô tả |
|------------|-----------|-------|
| `IsAddNew` | `IsAddNew==true && IsApproved==null` | Đang thêm mới, chưa submit |
| `Waiting2Approve` | `IsApproved==null && !IsAddNew && IsWaiting2Approve==true` | Đang chờ admin duyệt |
| `NotApproved` | `IsApproved==false` | Bị từ chối |
| `Approved` | `IsApproved==true` | Đang hoạt động |
| `Suspended` | `IsSuspended==true` | Tạm ngưng |

### Thông tin xe bắt buộc (CheckConditionApprove)

Admin không thể duyệt nếu thiếu các fields sau:

| Field | Mô tả |
|-------|-------|
| `VehicleMakeId` | Hãng xe (Toyota, Honda...) |
| `VehicleModelId` | Dòng xe (Camry, Civic...) |
| `PlateNumber` | Biển số xe (unique) |
| `VehicleNoOfSeatId` | Số chỗ ngồi |
| `YearModel` | Năm sản xuất |

**Error nếu thiếu:** *"Không thể duyệt. Vui lòng nhập đủ các thông tin [missing fields]"*

**Source:** `RentalService_SelfdriveCarRentalBaseService.CheckConditionApprove()`

### Bảo hiểm xe (Insurance Validation)

Xe được đánh dấu `HaveInsurance = true` khi thỏa tất cả:

| Điều kiện | Logic |
|-----------|-------|
| Có nhà bảo hiểm | `InsuranceProviderId != null` |
| Trong thời hạn | `CoverageStartDate ≤ now ≤ CoverageEndDate` |
| Đã xác minh | `IsVerified == true` (admin verify) |

System tự động cập nhật `Vehicle_RentalSetting.HaveInsurance` flag.

**Source:** `Vehicle_InsuranceInformationService.cs:75,171`

### Ảnh xe (Required Images)

Ảnh được cấu hình qua `ServiceCategory_ImageType_Mapping` theo từng loại dịch vụ:
- **AVATAR** — Ảnh đại diện xe (bắt buộc, hiển thị kết quả tìm kiếm)
- Các loại khác tùy config category (FRONT, BACK, LEFT, RIGHT, INTERIOR...)

Lưu trữ: `ServiceItem_Image` → `ServiceItem_Image_File` (child records)

### Giấy tờ xe (Required Documents)

Giấy tờ được cấu hình qua `ServiceCategory_DocumentType_Mapping`:
- **VEHICLE_REGISTRATION** — Đăng ký xe
- **INSURANCE_CERTIFICATE** — Bảo hiểm
- **INSPECTION_CERTIFICATE** — Đăng kiểm

Lưu trữ: `ServiceItem_Document` → `ServiceDocumentTypeId` (FK config)

### Luồng duyệt xe (Approval Workflow)

```
Owner tạo xe (IsAddNew=true)
     │
     ▼
Owner submit duyệt ──► IsWaiting2Approve=true
     │                    Gửi notify: Insert_Notify_RentCar_Waiting_Approve_Add_New_Admin
     │
     ▼
Admin review ──► CheckConditionApprove(Vehicle)
     │
     ├── Approved (IsApproved=true)
     │     ├── Set ApprovedAt, ApprovedById, ApprovedByJson
     │     ├── Trigger UpdateRentalServiceItem_Calculating_RentCarEngine
     │     └── Notify: Insert_Notify_RentCar_ApproveFirstCar (xe đầu)
     │            hoặc Insert_Notify_RentCar_ApproveCar (xe tiếp theo)
     │
     ├── Rejected (IsApproved=false)
     │     ├── Set Comment, CommentAt
     │     └── Owner sửa → re-submit
     │
     └── Re-approval (IsApproved=null, IsReApproved=true)
           └── Reset trạng thái, cho phép submit lại
```

### Tracking thay đổi (RentalServiceTempData)

Khi owner chỉnh sửa xe đã approved, hệ thống tạo `RentalServiceTempData`:

| Field | Mô tả |
|-------|-------|
| `RentalServiceItemId` | Xe liên quan |
| `Step` | Bước thay đổi (address, image, document, price, schedule, feature) |
| `Data` | JSON payload thay đổi |
| `IsApproved` | null = pending, true = approved, false = rejected |

Admin chỉ duyệt khi không có pending temp data.

### API Endpoints

```
# Owner submit duyệt
POST /api/v1/RentalService/UpdateRentalServiceStatus
{ "Id": "{guid}", "RentalServiceStatus": "Waiting2Approve", "RentalServiceCategoryCode": "RENT_CAR" }

# Admin duyệt/từ chối
POST /api/v1/RentalService_SelfdriveCarRental/SetApprove
{ "Id": "{guid}", "IsApproved": true/false, "Comment": "feedback" }

# Xem danh sách chờ duyệt
POST /api/v1/RentalService_SelfdriveCarRental/List
{ "RentalServiceStatus": "Waiting2Approve" }
```

---

*API liên quan: [vehicle.md](../03_API/vehicle.md) | [service-item.md](../03_API/service-item.md)*

---

## QC Test Checkpoints

| # | Checkpoint | DB Assertions | API Response | Side Effects |
|---|-----------|---------------|-------------|-------------|
| 1 | Đăng ký tài khoản | UserLogin created, OTP sent | Status=1, token returned | SMS sent via OTP service |
| 2 | Upload CCCD/GPLX | UserUpload records created with file paths | Status=1, file URLs returned | Files stored on SFTP/Google Drive |
| 3 | Tạo xe | Vehicle created, RentalServiceItem with IsAddNew=true | Status=1, Vehicle ID returned | — |
| 4 | Submit duyệt | IsWaiting2Approve=true, IsAddNew=false | Status=1 | Notification to Admin: Insert_Notify_RentCar_Waiting_Approve_Add_New_Admin |
| 5 | Admin duyệt | IsApproved=true, ApprovedAt set, ApprovedById set | Status=1 | UpdateRentalServiceItem_Calculating_RentCarEngine triggered; Notification to Owner |

## Approval Condition Checklist

Admin verification checklist trước khi duyệt xe:

| # | Điều kiện | Field | Bắt buộc | Cách verify |
|---|-----------|-------|----------|-------------|
| 1 | Hãng xe | `VehicleMakeId` | ✅ | Not null |
| 2 | Dòng xe | `VehicleModelId` | ✅ | Not null |
| 3 | Biển số xe | `PlateNumber` | ✅ | Not null, unique |
| 4 | Số chỗ ngồi | `VehicleNoOfSeatId` | ✅ | Not null |
| 5 | Năm sản xuất | `YearModel` | ✅ | Not null |
| 6 | Ảnh xe | `ServiceItem_Image` (AVATAR type) | ✅ | Ít nhất 1 ảnh AVATAR |
| 7 | Giấy tờ xe | `ServiceItem_Document` | Tuỳ config | Theo ServiceCategory_DocumentType_Mapping |
| 8 | Bảo hiểm (nếu có) | `Vehicle_InsuranceInformation` | Nếu HaveInsurance=true | IsVerified=true, CoverageEndDate > now |
| 9 | Giá thuê | `ServiceItem_WeekdayRentalPrice` | ✅ | Ít nhất 1 ngày có giá > 0 |
| 10 | Không có pending changes | `RentalServiceTempData` | ✅ | Không có record IsApproved=null |

**Error khi thiếu fields bắt buộc:**
> *"Không thể duyệt. Vui lòng nhập đủ các thông tin [missing fields]"*
