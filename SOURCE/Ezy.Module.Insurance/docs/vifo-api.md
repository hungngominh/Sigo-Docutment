# VIFO API – Tài liệu nội bộ

> Nguồn gốc: tổng hợp từ `https://docs.vifo.vn` + code thực tế trong `VIFOService.cs`
> Cập nhật: 2026-04-22

---

## Môi trường

| Môi trường | Base URL |
|------------|----------|
| Sandbox    | `https://sapi.vifo.vn` |
| Production | `https://api.vifo.vn`  |

---

## 1. Authentication

### POST `/v1/clients/web/admin/login`

Đăng nhập bằng username/password để lấy JWT token.

**Request body:**
```json
{
  "username": "VIFO_SIGO_demotest",
  "password": "sigo@123"
}
```

**Response 200:**
```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "expires_in": 31536000
}
```

**Lưu ý:** Token có thời hạn ~1 năm. Module tự cache 50 phút và tự refresh khi hết hạn cache.

---

## 2. Tính giá đơn bảo hiểm

### POST `/v2/insurance/total-price`

- Trả về phí bảo hiểm cho payload đã truyền.
- `final_amount` phải = **0** khi chỉ tính giá.
- Có thể bỏ qua: `address`, `city`, `ward`, `attachment_url_files`, `email`.
- Payload thay đổi theo `family_code` — xem Section 4.

**Response 200:**
```json
{
  "success": true,
  "message": "...",
  "data": {
    "final_amount": 480700,
    "product_code": "HDITNCAR20120102",
    "total_amount": 500000,
    "discount_amount": 19300
  }
}
```

**Response lỗi (HTTP 200, success=false):**
```json
{
  "success": false,
  "message": "Lỗi xác thực hoặc dữ liệu không hợp lệ",
  "status_code": 422,
  "errors": { }
}
```

---

## 3. Tạo đơn bảo hiểm

### POST `/v2/insurance`

- Payload giống tính giá, **nhưng `final_amount` phải là giá thật** (lấy từ bước tính giá).
- HTTP **201** khi thành công.

**Response 201:**
```json
{
  "success": true,
  "message": "...",
  "data": {
    "id": "abc123",
    "order_number": "VIFO-2025-001234",
    "provider_order_number": "VNI-2025-XYZ",
    "status": "pending",
    "final_amount": 480700,
    "product_code": "HDITNCAR20120102",
    "contract_files": [
      {
        "filename": "contract.pdf",
        "display_name": "Giấy chứng nhận BH",
        "type": "certificate",
        "url": "https://..."
      }
    ],
    "created_at": {
      "date": "2025-01-15T10:00:00.000000Z",
      "timezone_type": 3,
      "timezone": "UTC"
    }
  }
}
```

---

## 4. Order Payload theo Family Code

### 4.1 CARSHORT – BH vật chất xe ô tô cho thuê theo chuyến

> **Lưu ý:** CARSHORT là **sản phẩm riêng** của VIFO dành cho SIGO (không có trong docs public).
> Sau khi tìm kiếm toàn bộ sitemap + 66 JS chunk files của docs.vifo.vn, không tìm thấy bất kỳ
> tài liệu nào về CARSHORT. Payload dưới đây xác định từ code `VIFOService.cs` đã chạy đúng trên production.
> CARSHORT dùng `family_code` + `provider_code` thay vì `product_code`.

**Payload tính giá / tạo đơn:**
```json
{
  "phone":         "0901234567",
  "fullname":      "Nguyen Van A",
  "email":         "SIGO_sale@email.com",
  "family_code":   "CARSHORT",
  "provider_code": "VNI_SGD2",
  "start_date":    "2025-01-15",
  "end_date":      "2025-01-20",
  "plate_no":      "51A-12345",
  "year":          2020,
  "brand":         "Toyota",
  "model":         "Vios",
  "seat":          5
}
```

| Field | Kiểu | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| `phone` | string | ✅ | SĐT chủ xe |
| `fullname` | string | ✅ | Họ tên chủ xe |
| `email` | string | | Email — dùng email cấu hình SIGO |
| `family_code` | string | ✅ | Luôn = `"CARSHORT"` |
| `provider_code` | string | ✅ | Mã nhà cung cấp, ví dụ `"VNI_SGD2"` |
| `start_date` | string | ✅ | Ngày bắt đầu BH (yyyy-MM-dd, **không được là ngày quá khứ**) |
| `end_date` | string | ✅ | Ngày kết thúc BH (yyyy-MM-dd) |
| `plate_no` | string | ✅ | Biển số xe |
| `year` | int | ✅ | Năm sản xuất xe |
| `brand` | string | ✅ | Tên hãng xe (ví dụ: `"Toyota"`) |
| `model` | string | ✅ | Tên dòng xe (ví dụ: `"Vios"`) |
| `seat` | int | ✅ | Số chỗ ngồi |

**Quy tắc xử lý ngày (từ code VIFOService):**
- Convert từ UTC → VN timezone, lấy `.Date` (bỏ giờ phút)
- Nếu `start_date < hôm nay` → clamp về hôm nay
- Nếu `end_date < hôm nay` → trả lỗi "Không thể lấy BH cho ngày quá khứ"

---

### 4.2 TNCAR – BH TNDS xe ô tô

**Payload:**
```json
{
  "product_code":             "HDITNCAR20120102",
  "fullname":                 "Nguyen Van A",
  "phone":                    "0972640911",
  "email":                    "",
  "distributor_order_number": "",
  "no_plate":                 "59A-123.23",
  "chassis_no":               "123123123",
  "engine_no":                "123123123",
  "brand": {
    "id":   "kxeml73oyx4d9qbr",
    "code": "nissan",
    "name": "NISSAN"
  },
  "car_type":    "8",
  "seat":        "3",
  "load":        "3",
  "uses":        "PICKUP",
  "start_date":  "2025-04-18",
  "end_date":    "2026-04-18",
  "year":        "1",
  "options":     [],
  "beneficiary_list": [
    { "city": "Hà Nội" }
  ],
  "final_amount":  480700,
  "address_mode":  "2",
  "address":       "1196 Đường 3 Tháng 2",
  "city":          "79",
  "district":      "",
  "ward":          "27226",
  "attachment_url_files": [],
  "attachment":    []
}
```

| Field | Kiểu | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| `product_code` | string | ✅ | Mã sản phẩm từ `/v2/products` |
| `fullname` | string | ✅ | Họ tên chủ xe |
| `phone` | string | ✅ | SĐT chủ xe |
| `chassis_no` | string | ✅ | Số khung (tối thiểu 5 ký tự) |
| `engine_no` | string | ✅ | Số máy (tối thiểu 5 ký tự) |
| `brand` | object | ✅ | Lấy từ API `/v1/car-brand` |
| `start_date` | string | ✅ | Ngày bắt đầu (yyyy-MM-dd, t+1) |
| `end_date` | string | ✅ | Ngày kết thúc = start + số năm BH |
| `year` | string | | Số năm mua BH (1–3) |
| `options` | string[] | ✅ | SKU option bổ sung, `[]` nếu không có |
| `final_amount` | int | ✅ | 0 khi tính giá; giá thật khi tạo đơn |
| `no_plate` | string | | Biển số (bắt buộc với Bảo Minh) |
| `seat` | string | | Số chỗ ngồi |
| `car_type` | string | | Loại xe — xem API caruselist |
| `uses` | string | | Mục đích sử dụng — xem API caruselist |

---

### 4.3 TNDS – BH TNDS xe máy

**Payload tối giản:**
```json
{
  "product_code":  "...",
  "fullname":      "Nguyen Van A",
  "phone":         "0901234567",
  "email":         "",
  "start_date":    "2025-01-01",
  "license_plate": "59A1-12345",
  "chassis_number": "...",
  "engine_number":  "...",
  "options":       [],
  "final_amount":  0
}
```

---

### 4.4 BHYT / BHYTHGD – BH Y tế

**Payload tối giản:**
```json
{
  "product_code": "BHYT0305220003",
  "fullname":     "Nguyen Van A",
  "phone":        "0901234567",
  "email":        "",
  "options":      [],
  "final_amount": 0,
  "beneficiary_list": [
    {
      "fullname":   "Nguyen Van A",
      "birthday":   "1990-05-15",
      "nic":        "012345678901",
      "hospital":   "82-238",
      "gender":     "1",
      "renewal":    false,
      "start_date": "2025-01-01"
    }
  ]
}
```

---

## 5. Kiểm tra đơn bảo hiểm

### GET `/v2/insurance/{order_number}`

**Response 200:**
```json
{
  "success": true,
  "data": {
    "order_number":    "VIFO-2025-001234",
    "status":          "active",
    "product_code":    "...",
    "family_code":     "CARSHORT",
    "final_amount":    480700,
    "start_date":      "2025-01-15",
    "end_date":        "2025-01-20",
    "certificate_url": "https://...",
    "created_at":      "2025-01-14T10:00:00Z"
  }
}
```

**Trạng thái đơn:** `pending` | `active` | `expired` | `cancelled`

---

## 6. Hủy đơn bảo hiểm

### POST `/v2/order/{order_number}/terminate`

> Chỉ áp dụng cho một số sản phẩm nhất định. Liên hệ VIFO trước khi dùng.

**Response 200:**
```json
{
  "success": true,
  "message": "Đã hủy đơn thành công"
}
```

---

## 7. Kiểm tra trạng thái BHXH từ PVI

### GET `/v2/order/{order_number}/pvi`

Kiểm tra tờ khai BHXH từ phía công ty bảo hiểm PVI.

---

## 8. API danh mục

### GET `/v2/families` — Danh sách loại bảo hiểm

| Family Code | Tên |
|-------------|-----|
| `BHYT` | BH Y tế cá nhân |
| `BHYTHGD` | BH Y tế hộ gia đình |
| `BHXHTN` | BH Xã hội tự nguyện |
| `TNDS` | BH TNDS xe máy |
| `TNCAR` | BH TNDS xe ô tô |
| `CARSHORT` | BH vật chất xe ô tô (cho thuê theo chuyến) |
| `FLCA` | BH hủy chuyến bay |
| `IN-10` | BH trễ/đổi chuyến bay |
| `PA-3` | BH tai nạn cá nhân |
| `FA` | BH tai nạn hộ gia đình |
| `CANCER` | BH ung thư & bệnh hiểm nghèo |
| `HS` | BH trợ cấp viện phí |
| `INBO` | BH người nước ngoài du lịch tại VN |
| `VNT` | BH du lịch trong nước |
| `05` | BH du lịch quốc tế |
| `08` | BH sức khỏe toàn diện |
| `08-G` | BH sức khỏe toàn diện nhóm |
| `BANCA` | BH bảo an tín dụng |
| `03` | BH nhà tư nhân |

---

### GET `/v2/products?family_code=&page=&per_page=` — Danh sách sản phẩm

Query params: `family_code`, `page` (default 1), `per_page` (default 50).

**Response:**
```json
{
  "data": [
    {
      "family_code":    "TNCAR",
      "provider_code":  "HDI",
      "product_code":   "HDITNCAR20120102",
      "name":           "TNDS xe ô tô HDI",
      "name_vi":        "BH TNDS xe ô tô HDI",
      "price":          480700,
      "payment_term":   365,
      "options": [
        {
          "title": "Bảo hiểm tai nạn người trên xe",
          "type":  "checkbox",
          "values": [
            { "key": "VF1", "title": "Gói 10 triệu", "sku": "VF1", "price": 50000 }
          ]
        }
      ]
    }
  ],
  "meta": {
    "pagination": {
      "total": 10, "count": 10,
      "per_page": 50, "current_page": 1, "total_pages": 1
    }
  }
}
```

---

### GET `/v1/car-brand` — Danh sách hãng xe ô tô

Query: `?site_id=2`

### GET `/v1/car-brand/{brand_id}/models` — Danh sách dòng xe

Query: `?site_id=2`

---

## 9. Lỗi thường gặp

| HTTP Status | Ý nghĩa |
|-------------|---------|
| 200 + `success=false` | Dữ liệu không hợp lệ hoặc lỗi nghiệp vụ |
| 201 | Tạo đơn thành công |
| 401 | Token hết hạn hoặc sai |
| 422 | Dữ liệu request không đúng format |
| 500 | Lỗi server VIFO |

---

## 10. Config key trong System Config

| Key | Kiểu | Mô tả |
|-----|------|-------|
| `SYSTEM_VIFO_SETTINGS` | JSON | Cấu hình kết nối VIFO |

**Giá trị mẫu:**
```json
{
  "BaseUrl":                     "https://sapi.vifo.vn",
  "UserName":                    "VIFO_SIGO_demotest",
  "Password":                    "sigo@123",
  "Email":                       "SIGO_sale@email.com",
  "ProductFamilyCode":           "CARSHORT",
  "ProductProviderCode":         "VNI_SGD2",
  "IsGetVNIInsuranceInBackground": true,
  "VifoCompanyName":             "VNI",
  "VifoCompanyTaxCode":          "0102737963-034"
}
```
