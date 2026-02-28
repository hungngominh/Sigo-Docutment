# API: VietQR Integration

> **Controllers:** `VietQR_BankController`, `VietQR_BankAccountLookupController`, `VietQR_CitizenLookupController`
> **Base:** `/api/v1/VietQR`
> **Phân quyền:** `[Authorize]` (tất cả endpoint)

## Mục lục
- [Tổng quan](#tổng-quan)
- [Danh sách ngân hàng](#post-apiv1vietqrbanklistlist)
- [Đồng bộ ngân hàng](#post-apiv1vietqrbanksync)
- [Tra cứu tài khoản ngân hàng](#post-apiv1vietqrbankaccount_lookuplookup)
- [Tra cứu thông tin công dân](#post-apiv1vietqrcitizen_lookuplookup)

---

## Tổng quan

Tích hợp VietQR API để:
1. Đồng bộ danh sách ngân hàng từ VietQR
2. Tra cứu thông tin tài khoản ngân hàng (xác minh chủ tài khoản)
3. Tra cứu thông tin công dân theo CCCD/CMND (KYC)

---

## `POST /api/v1/VietQR/Bank/List`

**Mô tả:** Lấy danh sách ngân hàng hỗ trợ VietQR.

**Phân quyền:** `[Authorize]`

### Request Body — `VietQR_BankParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 50,
  "Keyword": ""
}
```

### Response — `EzyResultObject<EzyDataSourceResult<VietQR_BankModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 50,
    "Data": [
      {
        "BankCode": "MB",
        "BankName": "Ngân hàng Quân đội MB Bank",
        "LogoUrl": "https://...",
        "IsSupported": true
      }
    ]
  }
}
```

---

## `POST /api/v1/VietQR/Bank/Sync`

**Mô tả:** Đồng bộ danh sách ngân hàng mới nhất từ VietQR API.

**Phân quyền:** `[Authorize]`

### Request Body
Không có body (empty request).

### Response — `EzyResultObject<VietQR_BankModel[]>`
```json
{
  "StatusCode": 1,
  "Msg": "Đồng bộ thành công 50 ngân hàng",
  "Data": [ { ... } ]
}
```

---

## `POST /api/v1/VietQR/BankAccount_Lookup/List`

**Mô tả:** Lấy lịch sử tra cứu tài khoản ngân hàng.

**Phân quyền:** `[Authorize]`

### Request Body — `VietQR_BankAccountLookupParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "AccountNumber": "0123456789"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<VietQR_BankAccountLookupModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 5,
    "Data": [
      {
        "AccountNumber": "0123456789",
        "AccountName": "NGUYEN VAN A",
        "BankCode": "MB",
        "LookupAt": "2026-01-01T10:00:00Z"
      }
    ]
  }
}
```

---

## `POST /api/v1/VietQR/BankAccount_Lookup/Lookup`

**Mô tả:** Tra cứu thông tin chủ tài khoản ngân hàng qua VietQR API (dùng để xác minh người thụ hưởng trước khi chuyển tiền).

**Phân quyền:** `[Authorize]`

### Request Body — `VietQR_BankAccountLookupParamModel`
```json
{
  "AccountNumber": "0123456789",
  "BankCode": "MB"
}
```

### Response — `EzyResultObject<VietQR_BankAccountLookupModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "AccountNumber": "0123456789",
    "AccountName": "NGUYEN VAN A",
    "BankCode": "MB",
    "IsVerified": true
  }
}
```

---

## `POST /api/v1/VietQR/Citizen_Lookup/List`

**Mô tả:** Lấy lịch sử tra cứu thông tin công dân.

**Phân quyền:** `[Authorize]`

### Request Body — `VietQR_CitizenLookupParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "CitizenId": "012345678901"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<VietQR_CitizenLookupModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 2,
    "Data": [ { ... } ]
  }
}
```

---

## `POST /api/v1/VietQR/Citizen_Lookup/Lookup`

**Mô tả:** Tra cứu thông tin công dân theo số CCCD/CMND qua VietQR API (KYC — Know Your Customer).

**Phân quyền:** `[Authorize]`

### Request Body — `VietQR_CitizenLookupParamModel`
```json
{
  "CitizenId": "012345678901"
}
```

### Response — `EzyResultObject<VietQR_CitizenLookupModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "CitizenId": "012345678901",
    "FullName": "NGUYEN VAN A",
    "DateOfBirth": "1990-01-01",
    "Gender": "Nam",
    "Address": "Hà Nội",
    "IsVerified": true
  }
}
```

---

## Ghi chú

- Tất cả endpoint tra cứu (`Lookup`) đều lưu lịch sử vào DB để kiểm tra sau.
- `BankAccount_Lookup` được dùng trong luồng xác minh thông tin thụ hưởng khi chủ xe đăng ký nhận tiền.
- `Citizen_Lookup` dùng cho quy trình KYC khi người dùng đăng ký dịch vụ.
