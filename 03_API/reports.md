# API: Reports (Báo cáo)

> **Controllers:** `BangTinhCongNoController`, `TongHopThongTinKhaiThueSanTMDTController`, `ProjectReportSimpleController`
> **Base:** `/api/v1/BangTinhCongNo`, `/api/v1/TongHopThongTinKhaiThueSanTMDT`, `/api/v1/ProjectReportSimple*`
> **Phân quyền:** `[Authorize]` (Admin/Staff)

## Mục lục
- [Tổng quan](#tổng-quan)
- [Luồng gọi API](#luồng-gọi-api)
- [Bảng tính công nợ](#post-apiv1bangtinhcongnollist)
- [Tổng hợp khai thuế TMĐT](#post-apiv1tonghopthongtinkhaithuesantmdtlist)
- [Report động — ProjectReportSimple](#post-apiv1projectreportsimplelist)
- [Xuất Excel](#post-apiv1projectreportsimpleexportexcel2)

---

## Tổng quan

Module Reports cung cấp 3 loại báo cáo:

| Controller | Mô tả | Đối tượng dùng |
|-----------|-------|---------------|
| `BangTinhCongNo` | Công nợ giữa platform và chủ xe/người thuê | Admin, Kế toán |
| `TongHopThongTinKhaiThueSanTMDT` | Báo cáo khai thuế sàn TMĐT (nộp cho BCT) | Admin, Kế toán |
| `ProjectReportSimple` | Report động chạy từ stored procedure | Admin, Dev |

---

## Luồng gọi API

### Luồng 1: Xem và xuất báo cáo công nợ

```
Bước 1: POST /api/v1/BangTinhCongNo/List
        → Mục đích: Lấy danh sách giao dịch công nợ trong khoảng thời gian
        → Input: FromDate, ToDate, UserId (tuỳ chọn)
        → Output: Danh sách công nợ theo từng chủ xe/người thuê
```

### Luồng 2: Xuất báo cáo thuế TMĐT theo quý

```
Bước 1: POST /api/v1/TongHopThongTinKhaiThueSanTMDT/List
        → Mục đích: Lấy dữ liệu khai thuế trong kỳ
        → Input: FromDate (đầu quý), ToDate (cuối quý)
        → Output: Tổng hợp doanh thu, số giao dịch, thông tin người dùng
```

### Luồng 3: Xem và xuất report động

```
Bước 1: POST /api/v1/ProjectReportSimple/List
        → Mục đích: Chạy stored procedure, xem kết quả
        → Input: object (tham số động tuỳ SP)
        → Output: Dictionary<string, object>[] — dữ liệu động

Bước 2a (tuỳ chọn): POST /api/v1/ProjectReportSimple/ExportExcel2
        → Mục đích: Xuất kết quả ra file Excel
        → Output: FileUrl để download

Bước 2b (tuỳ chọn): POST /api/v1/ProjectReportSimple/ExportExcelByTemplate2
        → Mục đích: Xuất theo template Excel có sẵn (format đẹp hơn)
        → Output: FileUrl để download
```

---

## `POST /api/v1/BangTinhCongNo/List`

**Mô tả:** Lấy bảng tính công nợ giữa platform Sigo và các bên (chủ xe, người thuê). Dùng để đối soát thanh toán, kiểm tra tiền còn nợ.

**Phân quyền:** `[Authorize]` (Admin/Kế toán)

**Vai trò trong luồng:** Bước 1 trong [Luồng 1](#luồng-1-xem-và-xuất-báo-cáo-công-nợ)

### Request Body — `BangTinhCongNoParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 50,
  "FromDate": "2026-01-01",
  "ToDate": "2026-01-31",
  "UserId": null,
  "UserType": null
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| FromDate | date | Có | Từ ngày |
| ToDate | date | Có | Đến ngày |
| UserId | string | Không | Lọc theo user cụ thể |
| UserType | string | Không | Lọc theo loại: `OWNER` (chủ xe), `RENTER` (người thuê) |

### Response — `EzyResultObject<EzyDataSourceResult<BangTinhCongNoModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 120,
    "Data": [
      {
        "UserId": "user-guid",
        "UserName": "Nguyễn Văn A",
        "UserType": "OWNER",
        "OrderNumber": "ORD-20260101-001",
        "OrderDate": "2026-01-05",
        "TotalAmount": 2100000,
        "PaidAmount": 2100000,
        "DebtAmount": 0,
        "Status": "Đã thanh toán"
      }
    ]
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| TotalAmount | decimal | Tổng tiền phải thanh toán |
| PaidAmount | decimal | Số tiền đã thanh toán |
| DebtAmount | decimal | Còn nợ (`TotalAmount - PaidAmount`) |
| Status | string | Trạng thái: `Đã thanh toán`, `Còn nợ`, `Hoàn tiền` |

---

## `POST /api/v1/TongHopThongTinKhaiThueSanTMDT/List`

**Mô tả:** Tổng hợp thông tin khai thuế sàn thương mại điện tử — dữ liệu nộp cho Bộ Công Thương (BCT) và Thuế định kỳ.

**Phân quyền:** `[Authorize]` (Admin)

**Vai trò trong luồng:** Bước 1 trong [Luồng 2](#luồng-2-xuất-báo-cáo-thuế-tmđt-theo-quý)

### Request Body — `Order_ListViewParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 100,
  "FromDate": "2026-01-01",
  "ToDate": "2026-03-31"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<TongHopThongTinKhaiThueSanTMDTModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 500,
    "Data": [
      {
        "OrderNumber": "ORD-20260101-001",
        "OrderDate": "2026-01-05",
        "RenterName": "Nguyễn Văn B",
        "RenterTaxId": "0123456789",
        "OwnerName": "Nguyễn Văn A",
        "OwnerTaxId": "9876543210",
        "TotalRevenue": 2100000,
        "PlatformFee": 210000,
        "OwnerRevenue": 1890000,
        "VATAmount": 19000
      }
    ]
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| PlatformFee | decimal | Phí sàn Sigo thu |
| OwnerRevenue | decimal | Doanh thu thực của chủ xe |
| VATAmount | decimal | Thuế VAT |

> **Ghi chú:** Dữ liệu này dùng để nộp báo cáo BCT theo Nghị định 85/2021/NĐ-CP về thương mại điện tử.

---

## `POST /api/v1/ProjectReportSimple/List`

**Mô tả:** Chạy stored procedure bất kỳ và trả về dữ liệu động — dùng cho báo cáo ad-hoc. Tên SP được xác định qua `ScreenCode` trong cấu hình.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 1 trong [Luồng 3](#luồng-3-xem-và-xuất-report-động)

### Request Body — `object` (dynamic — tuỳ stored procedure)
```json
{
  "FromDate": "2026-01-01",
  "ToDate": "2026-01-31",
  "PageIndex": 1,
  "PageSize": 100
}
```

> **Lưu ý:** Tham số phụ thuộc vào stored procedure được cấu hình cho màn hình. Dev cần kiểm tra SP tương ứng.

### Response — `EzyResultObject<EzyDataSourceResult<Dictionary<string, object>>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 50,
    "Data": [
      {
        "Column1": "Value1",
        "Column2": 12345,
        "Column3": "2026-01-05"
      }
    ]
  }
}
```

> Response trả về dữ liệu động — các cột phụ thuộc vào SP. Schema không cố định.

---

## `POST /api/v1/ProjectReportSimple/ExportExcel2`

**Mô tả:** Xuất dữ liệu report ra file Excel.

**Vai trò trong luồng:** Bước 2a trong [Luồng 3](#luồng-3-xem-và-xuất-report-động)

### Request Body — `object` (tương tự `/List`)

### Response — `EzyResultObject<ExportExcelResultModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "FileUrl": "https://{host}/exports/report_2026_01.xlsx",
    "FileName": "report_2026_01.xlsx"
  }
}
```

---

## `POST /api/v1/ProjectReportSimple/ExportExcelByTemplate2`

**Mô tả:** Xuất report theo template Excel định sẵn (format đẹp, có header/footer chuẩn).

**Vai trò trong luồng:** Bước 2b trong [Luồng 3](#luồng-3-xem-và-xuất-report-động)

Tương tự `ExportExcel2` nhưng dùng template `.xlsx` có sẵn.

---

## Các variant của ProjectReportSimple

| Route | Controller | Đặc điểm |
|-------|-----------|---------|
| `/api/v1/ProjectReportSimple` | Default | Tự động set default date range |
| `/api/v1/ProjectReportSimple_V2` | V2 | Dùng SP name làm screen code |
| `/api/v1/ProjectReportSimpleDetail` | Detail | Không tự set default date |
| `/api/v1/ProjectReportSimple_Default` | Default | Set date mặc định |
| `/api/v1/ProjectReportSimple_NonDefault` | NonDefault | Không set date mặc định |
| `/api/v1/ProjectReportSimple_DefaultUTC` | DefaultUTC | Dùng UTC cho date mặc định |

---

## Ghi chú kỹ thuật

- **`TongHopThongTinKhaiThueSanTMDT`** dùng chung `Order_ListViewParamModel` — tận dụng filter của order list.
- **`ProjectReportSimple`** chạy bất kỳ SP nào — cần kiểm tra quyền và validate SP trước khi deploy.
- **Export file:** File được lưu tạm trên server, URL có thời hạn. Client cần download ngay sau khi nhận URL.
- **Hiệu năng:** Report phức tạp có thể mất 10-30s — client nên hiển thị loading indicator.

---

## Error Responses

| Trường hợp | StatusCode | Message |
|-------------|-----------|---------|
| SP không tồn tại | 0 | Stored procedure không tìm thấy |
| Thiếu parameter bắt buộc | 0 | Thiếu tham số: {paramName} |
| Date range quá lớn (>365 ngày) | 0 | Khoảng thời gian vượt quá giới hạn |
| Export timeout | 0 | Xuất báo cáo quá thời gian, vui lòng thu hẹp bộ lọc |
| Không có dữ liệu | 1 | (trả Data rỗng, Total = 0 — không phải lỗi) |
| Template file không tìm thấy | 0 | Template Excel không tồn tại |

### Dynamic SP Execution Pattern

```
Client gọi /List với ScreenCode (= SP name)
  ├─ Controller lookup SP từ ScreenCode
  ├─ Inject default date range (nếu variant = Default/DefaultUTC)
  ├─ Execute: SELECT dbo."sp_name"('{"FromDate":"...", "ToDate":"..."}')
  ├─ Parse JSON result → DataTable
  └─ Return EzyDataSourceResult<dynamic>
```

**Lưu ý bảo mật:** SP name được whitelist — không phải client truyền tên SP tự do. ScreenCode map qua bảng config.
