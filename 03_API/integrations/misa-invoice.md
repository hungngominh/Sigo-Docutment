# API: MISA Invoice Integration

> **Controllers:** `MisaInvoiceController`, `MisaInvoice_ChildController`
> **Base:** `/api/v1/MisaInvoice`, `/api/v1/MisaInvoice_Child`
> **Helper:** `MisaInvoiceHelper.cs`
> **Service:** `MisaInvoiceService.cs`

## Mục lục
- [Tổng quan](#tổng-quan)
- [App Endpoints](#app-endpoints)
- [Xuất hoá đơn (PublishHSM)](#xuất-hoá-đơn-publishhsm)
- [Tra cứu hoá đơn](#tra-cứu-hoá-đơn)
- [Tải PDF](#tải-pdf)
- [Huỷ hoá đơn](#huỷ-hoá-đơn)
- [Preview hoá đơn](#preview-hoá-đơn)
- [Thông tin công ty](#thông-tin-công-ty)
- [Cấu hình](#cấu-hình)
- [MISA External API](#misa-external-api)
- [Auto Export Engine](#auto-export-engine)
- [Logging](#logging)

---

## Tổng quan

Tích hợp MISA meInvoice để xuất hoá đơn điện tử (VAT) cho giao dịch thuê xe. Flow chính:

```
Order hoàn thành → PublishHSM (tạo hoá đơn) → Download PDF → Lưu file
                                                    ↓
                              Auto-export nếu cấu hình CanAutoExport=true
```

---

## App Endpoints

### MisaInvoiceController (`/api/v1/MisaInvoice`)

| # | Method | Endpoint | Mô tả |
|---|--------|----------|-------|
| 1 | POST | `/PublishHSM` | Xuất hoá đơn HSM cho đơn hàng |
| 2 | POST | `/PublishHSM_Test` | Test xuất hoá đơn |
| 3 | POST | `/PublishHSM_IgnoreDuplication` | Xuất hoá đơn, bỏ qua trùng lặp |
| 4 | POST | `/PublishHSMAndGetInvoice` | Xuất + lấy hoá đơn |
| 5 | POST | `/PublishHSMAndGetInvoice4Company` | Xuất hoá đơn cho công ty (có mã số thuế) |
| 6 | POST | `/PublishHSMAndGetInvoice_IgnoreDuplication` | Xuất + lấy, bỏ qua trùng |
| 7 | POST | `/GetPublishedInvoice` | Lấy hoá đơn đã xuất |
| 8 | POST | `/CancelPublishedInvoice` | Huỷ hoá đơn theo log ID |
| 9 | POST | `/CancelPublishedInvoice_FromRefId` | Huỷ hoá đơn theo RefId (Order GUID) |
| 10 | POST | `/DownloadPublishedInvoice` | Tải PDF hoá đơn |
| 11 | POST | `/PreviewInvoice` | Xem trước hoá đơn |
| 12 | POST | `/PreviewInvoice4Company` | Xem trước hoá đơn công ty |
| 13 | POST | `/GetCompanyInfo` | Lấy form thông tin công ty |
| 14 | POST | `/GetCompanyInfoAndInvoicePreview` | Lấy thông tin công ty + preview |
| 15 | POST | `/List` | Danh sách log xuất hoá đơn |

> **MisaInvoice_ChildController** (`/api/v1/MisaInvoice_Child`) — Cùng methods, dùng screen code `_CHILD`.

---

## Xuất hoá đơn (PublishHSM)

### Request

```json
// POST /api/v1/MisaInvoice/PublishHSM
// POST /api/v1/MisaInvoice/PublishHSMAndGetInvoice
{
  "OrderId": "12345",           // Order ID
  "OrderGUID": "abc-def-...",   // Hoặc Order GUID
  "CompanyTaxCode": "0312345678"  // Optional: mã số thuế công ty (cho hoá đơn công ty)
}
```

### Business Logic (line item calculation)

Hệ thống tự build invoice data từ Order:

```
Total Rental = (NumberOfRentalDay × PriceByDay) + DeliveryFee
CompletionFeePercentage = 12% (default, cấu hình được)

Line 1: Phí dịch vụ nền tảng (Middleman share)
  = Total × 12% - DiscountMoney
  VAT: 8%

Line 2: Thu nhập chủ xe (Owner share)
  = Total × 88%
  VAT: 8%

Line 3: Phí bảo hiểm (nếu có)
  = OrderVehicle.InsuranceFee
  VAT: 10%
```

### Response

```json
// Success
{
  "Status": 1,
  "Data": {
    "TransactionId": "MISA-xxx",
    "InvSeries": "1C24TSG",
    "PublishedInvoice": "/Upload/MISAInvoice/{RefId}/{TransactionID}/Invoice_{timestamp}.pdf"
  }
}

// Error
{
  "Status": 0,
  "Message": "Error description"
}
```

### Tính năng đặc biệt

| Feature | Mô tả |
|---------|-------|
| `PublishHSM_IgnoreDuplication` | Bỏ qua check trùng RefId, cho phép xuất lại |
| `PublishHSMAndGetInvoice` | Xuất → đợi 5 giây → download PDF tự động |
| `PublishHSMAndGetInvoice4Company` | Dùng `CompanyTaxCode` từ request thay vì từ Order |

---

## Tra cứu hoá đơn

### `POST /api/v1/MisaInvoice/GetPublishedInvoice`

```json
// Request
{ "OrderId": "12345" }

// Response: link xem hoá đơn đã published trên MISA
```

Gọi MISA API `/invoice/publishview` với TransactionID từ log.

---

## Tải PDF

### `POST /api/v1/MisaInvoice/DownloadPublishedInvoice`

```json
// Request
{ "OrderId": "12345" }
```

Gọi MISA API `/invoice/download` với params:
- `invoiceWithCode=true`
- `invoiceCalcu=false`
- `downloadDataType=pdf`

Response: Base64-encoded PDF trong `MisaAPIDownloadInvoiceData.Data`

Lưu tại: `Upload/MISAInvoice/{RefId}/{TransactionID}/Invoice_{timestamp}.pdf`

---

## Huỷ hoá đơn

### `POST /api/v1/MisaInvoice/CancelPublishedInvoice`

```json
// Request
{ "OrderId": "12345" }
```

### `POST /api/v1/MisaInvoice/CancelPublishedInvoice_FromRefId`

```json
// Request
{ "OrderGUID": "abc-def-..." }
```

**Business Rules:**
- Chỉ cancel khi `InvoiceIsCancelled == false`
- Cập nhật `Log_MISAInvoice_PublishHSM.InvoiceIsCancelled = true`
- Ghi nhận `InvoiceCancelledById`, `InvoiceCancelledAt`
- Cancel reason mặc định: `"Others"`

---

## Preview hoá đơn

### `POST /api/v1/MisaInvoice/PreviewInvoice`

```json
{ "OrderId": "12345" }
```

### `POST /api/v1/MisaInvoice/PreviewInvoice4Company`

```json
{
  "OrderId": "12345",
  "CompanyTaxCode": "0312345678"
}
```

Gọi MISA API `/invoice/unpublishview` — xem trước hoá đơn chưa publish.

---

## Thông tin công ty

### `POST /api/v1/MisaInvoice/GetCompanyInfo`

```json
// Request
{ "Id": "12345" }  // Order ID

// Response: form với default values
{
  "Status": 1,
  "Data": {
    "DefaultValues": {
      "CompanyTaxCode": "0312345678",
      "CompanyName": "Công ty ABC",
      "CompanyAddress": "123 Nguyễn Huệ, Q1, HCM"
    },
    "DataForm": { ... }
  }
}
```

Validate mã số thuế qua `masothue.com` (nếu available).

### `POST /api/v1/MisaInvoice/GetCompanyInfoAndInvoicePreview`

Kết hợp lấy thông tin công ty + preview hoá đơn trong 1 call.

---

## Cấu hình

### ConfigMISAInvoice (Database — Category DB)

| Field | Type | Mô tả |
|-------|------|-------|
| `AppId` | string | MISA application ID |
| `TaxCode` | string | Mã số thuế công ty |
| `UserName` | string | MISA username |
| `Password` | string | MISA password |
| `BaseUrl` | string | MISA API base URL (ví dụ: `https://testapi.meinvoice.vn/api/integration/`) |
| `InvSeries` | string | Ký hiệu hoá đơn (ví dụ: `1C24TSG`) |

### SYSTEM_MISA_INVOICE_SETTING (SystemConfig — key-value)

```json
{
  "ConfigCode": "MISA_CODE",           // Reference tới ConfigMISAInvoice.Code
  "DefaultReceiverEmail": "abc@co.vn", // Email nhận hoá đơn mặc định
  "IsSendEmail": true,                 // Gửi email khi xuất hoá đơn
  "WaitTimeBeforeDownloadInvoice_Second": 5,  // Đợi N giây trước khi download
  "CanAutoExport": false,              // Tự động xuất hoá đơn cho order hoàn thành
  "OrderToExportInNDay": 7             // Auto-export orders trong N ngày gần
}
```

### Truy xuất config

```csharp
var config = MisaInvoiceHelper.GetConfig(configCode);    // ConfigMISAInvoice từ cache
var settings = MisaInvoiceHelper.GetSettings();           // SYSTEM_MISA_INVOICE_SETTING
var token = MisaInvoiceHelper.GetAccessToken(out msg, configCode);  // Access token
```

---

## MISA External API

Base URL: cấu hình trong `ConfigMISAInvoice.BaseUrl`

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/auth/token` | POST | Lấy access token |
| `/invoice` | POST | Xuất hoá đơn HSM |
| `/invoice/unpublishview` | POST | Preview hoá đơn chưa publish |
| `/invoice/publishview` | POST | Xem hoá đơn đã publish |
| `/invoice/download` | POST | Tải PDF hoá đơn |
| `/invoice/cancel` | POST | Huỷ hoá đơn |

### Headers

```
Authorization: Bearer {accessToken}
Content-Type: application/json
CompanyTaxCode: {companyTaxCode}    // Chỉ cho /invoice endpoint
```

### Authentication

```json
// POST /auth/token
{
  "appid": "MISA_APP_ID",
  "taxcode": "0312345678",
  "username": "user@company.vn",
  "password": "***"
}

// Response
{
  "Success": true,
  "Data": "eyJhbGciOiJ..."   // Access token
}
```

### Invoice Data Structure (gửi tới MISA)

```csharp
InvoiceData {
  // General
  RefID: string,            // Order GUID
  InvSeries: string,        // "1C24TSG"
  InvTemplateNo: "1",
  InvDate: "yyyy-MM-dd",
  InvoiceName: "HÓA ĐƠN GIÁ TRỊ GIA TĂNG",
  CurrencyCode: "VND",
  PaymentMethodName: "TM/CK",

  // Buyer
  BuyerFullName, BuyerLegalName, BuyerTaxCode, BuyerAddress,
  BuyerPhoneNumber, BuyerEmail, BuyerBankAccount, BuyerBankName,

  // Amounts
  TotalSaleAmountOC, TotalDiscountAmountOC,
  TotalAmountWithoutVATOC, TotalVATAmountOC,
  TotalAmountOC, TotalAmountInWords,   // "Một triệu hai trăm..."

  // Line Items
  OriginalInvoiceDetail[]: {
    ItemCode, ItemName, UnitName,
    Quantity, UnitPrice, Amount,
    DiscountRate, DiscountAmount,
    VATRateName,        // "8%", "10%"
    VATAmount
  },

  // Tax breakdown
  TaxRateInfo[]: { VATRateName, AmountWithoutVATOC, VATAmountOC }
}
```

---

## Auto Export Engine

**File:** `AutoExportBillEngine.cs`

- Background service tự động xuất hoá đơn cho orders hoàn thành
- Chạy khi `SYSTEM_MISA_INVOICE_SETTING.CanAutoExport = true`
- Xử lý orders trong khoảng `OrderToExportInNDay` ngày

---

## Logging

### Log_MISAInvoice_PublishHSM

Mọi tương tác với MISA API được ghi log:

| Field | Type | Mô tả |
|-------|------|-------|
| `RefId` | string | Order GUID |
| `TransactionId` | string | MISA transaction ID |
| `RequestBody` | string | JSON request gửi tới MISA |
| `ResponseBody` | string | JSON response từ MISA |
| `RequestIsSuccessful` | bool | HTTP success flag |
| `StatusCode` | int | HTTP status code |
| `ErrorMessage` | string | Error details |
| `PublishedInvoice` | string | PDF file path |
| `MISA_APIType` | string | `PublishHSM` / `GetInvoice` / `CancelInvoice` / `DownloadInvoice` / `PreviewInvoice` |
| `MISA_APIUrl` | string | API endpoint called |
| `InvSeries` | string | Ký hiệu hoá đơn |
| `InvoiceIsCancelled` | bool | Đã huỷ chưa |
| `IsPublish4Company` | bool | Xuất cho công ty |
| `InvoiceCancelledById` | string | Người huỷ |
| `InvoiceCancelledAt` | double? | Timestamp huỷ |

### Action Hooks

```csharp
MisaInvoiceHelper.Action_AfterCancelInvoice;       // Callback sau khi huỷ
MisaInvoiceHelper.Action_AfterGetPublishedInvoice;  // Callback sau khi lấy invoice
```

---

### DB Scripts

| Script | Mô tả |
|--------|-------|
| `20241230-0726-[ConfigMISAInvoice]-CreateTable.txt` | Tạo bảng config |
| `20241230-0727-[Log_MISAInvoice_PublishHSM]-CreateTable.txt` | Tạo bảng log |
| `20241231-1350-[Log_MISAInvoice_PublishHSM]-AddColumns.txt` | Thêm cột log |
| `20241231-1351-[Log_MISAInvoice_PublishHSM]-FillData_[InvSeries].txt` | Fill data |
| `20250116-0838-[Log_MISAInvoice_PublishHSM]-AddColumn-[IsPublish4Company].txt` | Thêm cột IsPublish4Company |

---

*Xem thêm: [categories.md](../categories.md) | [ewallet.md](../../02_MODULES/ewallet.md)*
