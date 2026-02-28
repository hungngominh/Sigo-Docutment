# Core Module: Integrations — Tích hợp bên ngoài

> **Project:** `AllianceMiddlemanWebAPI` | **Domain:** Third-party integrations

## Mục lục
- [Tổng quan](#tổng-quan)
- [MB Bank](#mb-bank)
- [MISA Invoice](#misa-invoice)
- [Mioto](#mioto)
- [VietQR](#vietqr)
- [Tổng hợp endpoints](#tổng-hợp-endpoints)

---

## Tổng quan

Sigo tích hợp với 4 hệ thống bên ngoài qua REST API:

| Hệ thống | Mục đích | Controller | Nhóm |
|----------|---------|-----------|------|
| **MB Bank** | Chuyển khoản tự động (rút tiền owner) | `MBBank/` | Tài chính |
| **MISA Invoice** | Xuất hoá đơn VAT điện tử | `MisaInvoice/` | Tài chính |
| **Mioto** | Listing xe từ platform Mioto | `Mioto/` | Đối tác |
| **VietQR** | Tra cứu tài khoản ngân hàng, CCCD | `VietQR/` | Thanh toán |

---

## MB Bank

### Mô tả
Tích hợp API của **MB Bank** (Ngân hàng Quân đội) để xử lý rút tiền tự động. Khi chủ xe muốn rút tiền từ ví Sigo → MB Bank API gọi lệnh chuyển khoản về tài khoản ngân hàng cá nhân.

### Controller
**File:** `Controllers/MainBusiness/MBBank/MBBank_TransferFundController.cs`

### API Endpoints

| Method | Route | Mô tả | Auth |
|--------|-------|-------|------|
| POST | `/api/v1/MBBank/List` | Danh sách giao dịch MB Bank | Required |
| POST | `/api/v1/MBBank/AutoWithdraw_MBBank` | Trigger rút tiền tự động | Required |
| POST | `/api/v1/MBBank/AccountInfo` | Tra cứu thông tin TK ngân hàng | Required |
| POST | `/api/v1/MBBank/TransactionStatus` | Kiểm tra trạng thái giao dịch | Required |
| POST | `/api/v1/BankSignalFromApp` | Nhận tín hiệu từ app (bank signal) | Required |

### Entities liên quan

| Entity | Mô tả |
|--------|-------|
| `AutoWithdrawQueue` | Hàng chờ rút tiền tự động |
| `WithdrawalHistory` | Lịch sử rút tiền |
| `MBBank_APICall_Log` | Log API calls tới MB Bank |
| `Log_EWallet_MBBank` | Log giao dịch EWallet ↔ MB Bank |
| `SMS_CompanyBankAccountActivity` | SMS ngân hàng nhận được |
| `SMS_CompanyBankAccountActivity_Import_Log` | Log import SMS |

### Luồng rút tiền

```
Owner request rút tiền
    │
    ▼
[Sigo API] → Insert vào AutoWithdrawQueue
    │
    ▼
[AutoWithdrawEngine] (background)
    │ Poll queue
    ▼
[MB Bank API] → POST /transfer
    │ Avg: 2,399ms
    ▼
Success → Cập nhật WithdrawalHistory
    │
    ▼
[AutoUpdateWithdrawStatus_MBBankEngine]
    │ Poll trạng thái giao dịch
    ▼
Final status → Push notification cho owner
```

### Cấu hình

```json
{
  "ConfigMBBank": {
    "ApiUrl": "https://...",
    "ClientId": "...",
    "ClientSecret": "...",
    "AccountNo": "...",      // TK công ty Sigo
    "PartnerCode": "..."
  }
}
```

### Background Engines

| Engine | Mô tả |
|--------|-------|
| `AutoWithdrawEngine` | Xử lý queue rút tiền → gọi MB Bank API |
| `AutoUpdateWithdrawStatus_MBBankEngine` | Poll trạng thái giao dịch |
| `DoTransferWithdrawFeeEngine` | Xử lý phí rút tiền |
| `AutoMappingSMS_CompanyBankAccountActivityEngine` | Parse SMS bank → map với đơn |

### Batch Job

| Job | Mô tả |
|-----|-------|
| `MBBank_CompareReportDataJob` | Đối soát giao dịch daily (reconciliation) |

---

## MISA Invoice

### Mô tả
Tích hợp **MISA** để xuất hoá đơn VAT điện tử (HSM — Hoá đơn số máy) tự động cho các giao dịch hoàn tất.

### Controller
**File:** `Controllers/MainBusiness/MisaInvoice/MisaInvoiceController.cs`

### API Endpoints

| Method | Route | Mô tả | Auth |
|--------|-------|-------|------|
| POST | `/api/v1/MisaInvoice/List` | Danh sách hoá đơn | Required |
| POST | `/api/v1/MisaInvoice/PublishHSM` | Xuất hoá đơn điện tử | Required |
| POST | `/api/v1/MisaInvoice/PublishHSM_Test` | Test xuất hoá đơn | Required |
| POST | `/api/v1/MisaInvoice/PublishHSM_IgnoreDuplication` | Xuất bỏ qua trùng | Required |
| POST | `/api/v1/MisaInvoice/PublishHSMAndGetInvoice` | Xuất + lấy nội dung | Required |
| POST | `/api/v1/MisaInvoice/CancelPublishedInvoice` | Huỷ hoá đơn đã xuất | Required |
| POST | `/api/v1/MisaInvoice/DownloadPublishedInvoice` | Tải hoá đơn (PDF) | Required |
| POST | `/api/v1/MisaInvoice/GetPublishedInvoice` | Xem hoá đơn đã xuất | Required |
| POST | `/api/v1/MisaInvoice/PreviewInvoice` | Xem trước hoá đơn | Required |

### Entity liên quan

| Entity | Mô tả |
|--------|-------|
| `Log_MISAInvoice_PublishHSM` | Log xuất hoá đơn (request/response, status) |

### Background Engine

| Engine | Mô tả |
|--------|-------|
| `AutoExportBillEngine` | Tự động xuất hoá đơn cho đơn đã hoàn tất |

### Cấu hình

```json
{
  "ConfigMISAInvoice": {
    "ApiUrl": "https://...",
    "AppId": "...",
    "TaxCode": "...",         // MST công ty Sigo
    "InvoicePattern": "...",   // Ký hiệu hoá đơn
    "InvoiceSeries": "..."     // Số sê-ri
  }
}
```

---

## Mioto

### Mô tả
Tích hợp **Mioto** (nền tảng cho thuê xe tự lái khác) để import listing xe từ Mioto vào Sigo. Cho phép owner listing xe trên cả hai platform.

### Controllers
**Folder:** `Controllers/MainBusiness/Mioto/`

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `Mioto_VehicleOwnerController` | `api/v1/Mioto/VehicleOwner` | Quản lý chủ xe Mioto |
| `Mioto_VehicleQueryController` | `api/v1/Mioto/VehicleQuery` | Query xe từ Mioto |
| `Mioto_VehiclePricingController` | `api/v1/Mioto/VehiclePricing` | Giá xe Mioto |
| `Mioto_VehicleTripCountDetailController` | `api/v1/Mioto/VehicleTripCountDetail` | Thống kê chuyến |

### API Endpoints

| Method | Route | Mô tả | Auth |
|--------|-------|-------|------|
| POST | `/api/v1/Mioto/VehicleOwner/List` | DS chủ xe Mioto | Required |
| POST | `/api/v1/Mioto/VehicleOwner/GetList_OptionUser` | DS owner có mapping | Required |
| POST | `/api/v1/Mioto/VehicleQuery/List` | DS xe từ Mioto | Required |
| POST | `/api/v1/Mioto/VehicleQuery/FetchPricingData` | Lấy dữ liệu giá | Required |
| POST | `/api/v1/Mioto/VehicleQuery/AddFromConfigAddress` | Thêm theo khu vực | Required |
| POST | `/api/v1/Mioto/VehiclePricing/List` | DS giá xe | Required |
| POST | `/api/v1/Mioto/VehicleTripCountDetail/List` | Chi tiết chuyến | Required |

### Entities liên quan

| Entity | Mô tả |
|--------|-------|
| `Mioto_VehicleInfo` | Thông tin xe từ Mioto |
| `Mioto_VehicleOwner` | Chủ xe trên Mioto |
| `Mioto_VehiclePricing` | Giá xe từ Mioto |
| `Mioto_VehicleQuery` | Query log tìm kiếm |
| `Mioto_VehicleTripCountDetail` | Số chuyến xe |
| `Mioto_Sigo_Car_Mapping` | Mapping giữa xe Mioto ↔ Sigo |

### Flow import xe

```
Admin trigger fetch
    │ POST /Mioto/VehicleQuery/FetchPricingData
    ▼
[Sigo API] → Gọi Mioto API
    │ Lấy danh sách xe, giá, thông tin owner
    ▼
Lưu vào Mioto_Vehicle* entities
    │
    ▼
Admin mapping thủ công
    │ Mioto_Sigo_Car_Mapping
    ▼
Xe Mioto hiển thị trên Sigo platform
```

---

## VietQR

### Mô tả
Tích hợp **VietQR / Napas** để tra cứu thông tin tài khoản ngân hàng và CCCD, phục vụ xác thực tài khoản trước khi chuyển tiền.

### Controllers
**Folder:** `Controllers/MainBusiness/VietQR/`

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `VietQR_BankController` | `api/v1/VietQR/Bank` | Danh sách ngân hàng |
| `VietQR_BankAccountLookupController` | `api/v1/VietQR/BankAccount_Lookup` | Tra cứu TK ngân hàng |
| `VietQR_CitizenLookupController` | `api/v1/VietQR/Citizen_Lookup` | Tra cứu CCCD |

### API Endpoints

| Method | Route | Mô tả | Auth |
|--------|-------|-------|------|
| POST | `/api/v1/VietQR/Bank/List` | DS ngân hàng hỗ trợ | Required |
| POST | `/api/v1/VietQR/Bank/Sync` | Sync danh sách bank | Admin |
| POST | `/api/v1/VietQR/BankAccount_Lookup/List` | Tra cứu tên chủ TK | Required |
| POST | `/api/v1/VietQR/Citizen_Lookup/List` | Tra cứu thông tin CCCD | Required |

### Entities liên quan

| Entity | Mô tả |
|--------|-------|
| `VietQR_BankAccountLookupLog` | Log tra cứu TK (input, result, timestamp) |
| `VietQR_CitizenLookupLog` | Log tra cứu CCCD |

### Use cases

| Use case | Flow |
|---------|------|
| Owner thêm TK ngân hàng | → VietQR lookup TK → Xác nhận tên chủ TK → Lưu `UserLogin_BeneficiaryBank_Mapping` |
| Admin xác minh user | → VietQR citizen lookup → So khớp với ảnh CCCD upload |

---

## Tổng hợp endpoints

| Hệ thống | Endpoints | Entities | Engines |
|----------|-----------|----------|---------|
| **MB Bank** | 5 | 6 | 4 + 1 batch job |
| **MISA Invoice** | 9 | 1 | 1 |
| **Mioto** | 7 | 6 | 0 |
| **VietQR** | 4 | 2 | 0 |
| **Tổng** | **25** | **15** | **6** |

---

## Error Handling tổng hợp

### MB Bank Errors

| Error | Mô tả | Xử lý |
|-------|-------|-------|
| Token expired | OAuth2 token hết hạn | Auto-refresh, retry 1 lần |
| Insufficient balance | TK công ty không đủ tiền | Alert admin, queue lại |
| Invalid account | TK thụ hưởng không hợp lệ | Báo user kiểm tra lại TK |
| Timeout (>30s) | MB Bank API không phản hồi | Queue lại, poll status sau |
| Duplicate transaction | TransactionId trùng | Skip, log warning |

### MISA Invoice Errors

| Error | Mô tả | Xử lý |
|-------|-------|-------|
| Duplicate invoice | Hoá đơn đã xuất cho đơn này | Dùng `PublishHSM_IgnoreDuplication` |
| Invalid tax code | MST không hợp lệ | Báo lỗi, yêu cầu user cập nhật |
| Template not found | Mẫu hoá đơn không tồn tại | Kiểm tra config InvoicePattern |
| API unavailable | MISA server down | Retry bởi AutoExportBillEngine |

### VietQR Errors

| Error | Mô tả | Xử lý |
|-------|-------|-------|
| Account not found | Không tìm thấy TK | Hiển thị "Không tìm thấy tài khoản" |
| CCCD invalid | Số CCCD không đúng format | Validate 12 số trước khi gọi API |
| Rate limit | Vượt giới hạn call/phút | Delay và retry |

### Mioto Errors

| Error | Mô tả | Xử lý |
|-------|-------|-------|
| Vehicle not found | Xe đã bị xoá trên Mioto | Deactivate mapping |
| Pricing mismatch | Giá Mioto khác với Sigo | Log alert, admin review |
| API connection failed | Mioto API timeout | Skip batch, retry next cycle |

---

*Xem thêm: [ewallet.md](./ewallet.md) | [ewallet-withdraw.md](../04_BUSINESS_FLOWS/ewallet-withdraw.md) | [config-keys.md](../06_OPERATIONS/config-keys.md)*
