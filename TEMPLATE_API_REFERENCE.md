# API Reference - [Tên Module/Controller]

> **Base URL:** `https://{host}/api/v1/`
> **Auth:** Bearer Token (JWT) — trừ các endpoint `[AllowAnonymous]`
> **Content-Type:** `application/json`

---

## Mục lục

- [Authentication](#authentication)
- [Luồng gọi API](#luồng-gọi-api)
- [Endpoint 1](#endpoint-1)
- [Endpoint 2](#endpoint-2)
- [Error Codes](#error-codes)

---

## Authentication

Hầu hết các endpoint yêu cầu JWT token:

```
Authorization: Bearer {access_token}
```

Token lấy từ endpoint `POST /api/v1/Account/Login`.

---

## Luồng gọi API

> Phần này mô tả **thứ tự gọi API đúng** để thực hiện một chức năng nghiệp vụ.
> Mỗi luồng = một hành trình người dùng hoặc tác vụ hoàn chỉnh.

### Luồng 1: [Tên chức năng]

```
Bước 1: POST /api/v1/{Controller}/{Action1}
        → Mục đích: [lý do gọi bước này]
        → Input: [field chính cần truyền]
        → Output dùng cho bước sau: [field lấy từ response]

Bước 2: POST /api/v1/{Controller}/{Action2}
        → Mục đích: [lý do gọi bước này]
        → Input: [dùng output từ bước 1 + field khác]
        → Output: [kết quả cuối]
```

**Điều kiện tiên quyết:** [Yêu cầu trước khi bắt đầu luồng, nếu có]

**Side effects:** [Những gì xảy ra sau khi luồng hoàn tất — notification, trạng thái thay đổi, v.v.]

**Xử lý lỗi trong luồng:**

| Bước | Lỗi có thể xảy ra | Xử lý |
|------|-------------------|-------|
| 1 | [Mô tả lỗi] | [Hành động khi gặp lỗi] |
| 2 | [Mô tả lỗi] | [Hành động khi gặp lỗi] |

---

### Luồng 2: [Tên chức năng khác]

```
Bước 1: ...
Bước 2: ...
```

---

## Endpoint 1

### `POST /api/v1/{Controller}/{Action}`

**Mô tả:** [Mô tả ngắn gọn endpoint này làm gì — bắt đầu bằng động từ: Tạo / Lấy / Cập nhật / Xoá / Kiểm tra...]

**Phân quyền:** `[AllowAnonymous]` / `[Authorize]`

**Vai trò trong luồng:** Bước N trong [Luồng X](#luồng-1-tên-chức-năng) — [mục đích cụ thể]

**Hiệu năng:** [X lượt/30 ngày | avg Xms] _(nếu có dữ liệu từ api_log)_

#### Request Body

```json
{
  "field1": "string",
  "field2": 0,
  "field3": true
}
```

| Field    | Type    | Bắt buộc | Mô tả              |
|----------|---------|----------|--------------------|
| field1   | string  | Có       | [Mô tả field1]     |
| field2   | integer | Không    | [Mô tả field2]     |
| field3   | boolean | Không    | [Mô tả field3]     |

#### Response

**Thành công (200):**

```json
{
  "StatusCode": 1,
  "Msg": "Thành công",
  "Data": {
    "id": 1,
    "name": "..."
  }
}
```

**Thất bại:**

```json
{
  "StatusCode": 0,
  "Msg": "Mô tả lỗi",
  "Data": null
}
```

#### Lỗi thường gặp

| StatusCode | Msg | Nguyên nhân | Xử lý |
|-----------|-----|------------|-------|
| 0 | [Nội dung lỗi] | [Nguyên nhân] | [Hướng xử lý] |

#### Ví dụ cURL

```bash
curl -X POST "https://{host}/api/v1/{Controller}/{Action}" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"field1": "value"}'
```

---

## Endpoint 2

### `GET /api/v1/{Controller}/{Action}`

**Mô tả:** [Mô tả ngắn gọn]

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước N trong [Luồng X](#luồng-1-tên-chức-năng) — [mục đích cụ thể]

#### Query Parameters

| Tham số   | Type    | Bắt buộc | Mô tả              |
|-----------|---------|----------|--------------------|
| pageIndex | integer | Không    | Trang hiện tại (mặc định: 1) |
| pageSize  | integer | Không    | Số bản ghi / trang (mặc định: 20) |
| keyword   | string  | Không    | Từ khóa tìm kiếm   |

#### Response

**Thành công (200) — Danh sách phân trang:**

```json
{
  "StatusCode": 1,
  "Msg": "Thành công",
  "Data": {
    "Total": 100,
    "Data": [ ]
  }
}
```

---

## Error Codes

| StatusCode | Ý nghĩa                        |
|------------|-------------------------------|
| 1          | Thành công                    |
| 0          | Thất bại (xem Msg)            |
| -1         | Lỗi hệ thống / Exception      |
| 401        | Chưa xác thực (token hết hạn) |
| 403        | Không có quyền truy cập       |
| 404        | Không tìm thấy dữ liệu        |

---

## Ghi chú kỹ thuật

- **Response wrapper:** Tất cả API trả về `EzyResultObject<T>` — `StatusCode = 1` thành công, `StatusCode = 0` thất bại
- **Ngày tháng:** Truyền dưới dạng Unix timestamp (double) hoặc ISO 8601 tuỳ endpoint — kiểm tra từng API
- **Phân trang:** `PageIndex` bắt đầu từ 1, `PageSize` mặc định 20
- [Ghi chú đặc thù của module này]

---

*Cập nhật lần cuối: {ngày}*
