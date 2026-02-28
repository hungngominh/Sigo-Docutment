# [Tên Module] - Module Documentation

> **Module:** `Ezy.Module.{TênModule}`
> **Phiên bản:** x.x.x
> **Trạng thái:** Active / Deprecated

---

## Mục lục

- [Tổng quan](#tổng-quan)
- [Cấu trúc module](#cấu-trúc-module)
- [Entities chính](#entities-chính)
- [Services](#services)
- [API Endpoints](#api-endpoints)
- [Cấu hình & tích hợp](#cấu-hình--tích-hợp)
- [Ghi chú & hạn chế](#ghi-chú--hạn-chế)

---

## Tổng quan

[Mô tả 3–5 câu: module này làm gì, phục vụ nghiệp vụ nào, ai sử dụng]

**Các tính năng chính:**
- [Tính năng 1]
- [Tính năng 2]
- [Tính năng 3]

---

## Cấu trúc module

```
Ezy.Module.{TênModule}/
├── Ezy.Module.{TênModule}/              # API Layer — Controllers
│   └── Controllers/
│
├── Ezy.Module.{TênModule}.Core/         # Data Layer — EF Core entities
│   └── Data/
│
├── Ezy.Module.{TênModule}.DataShared/   # Shared DTOs
│
└── Ezy.Module.{TênModule}.Service/      # Business Logic
    └── Services/
```

---

## Entities chính

### `{EntityName}`

| Column      | Type     | Mô tả                  |
|-------------|----------|------------------------|
| Id          | int      | Primary key            |
| Name        | string   | Tên                    |
| CreatedDate | DateTime | Ngày tạo               |
| IsDeleted   | bool     | Đánh dấu xóa mềm       |
| ...         | ...      | ...                    |

### `{EntityName2}`

| Column      | Type     | Mô tả                  |
|-------------|----------|------------------------|
| ...         | ...      | ...                    |

---

## Services

### `{ServiceName}` — `I{ServiceName}`

| Method              | Mô tả                            |
|---------------------|----------------------------------|
| `GetById(id)`       | Lấy bản ghi theo ID              |
| `GetList(filter)`   | Lấy danh sách có lọc + phân trang |
| `Create(model)`     | Tạo mới                          |
| `Update(model)`     | Cập nhật                         |
| `Delete(id)`        | Xóa mềm                          |

---

## API Endpoints

### Base route: `/api/v1/{TênModule}`

| Method | Endpoint              | Mô tả                     | Auth       |
|--------|-----------------------|---------------------------|------------|
| POST   | `/GetList`            | Lấy danh sách phân trang  | Required   |
| GET    | `/GetById/{id}`       | Lấy chi tiết theo ID      | Required   |
| POST   | `/Create`             | Tạo mới                   | Required   |
| POST   | `/Update`             | Cập nhật                  | Required   |
| POST   | `/Delete/{id}`        | Xóa                       | Required   |
| ...    | ...                   | ...                       | ...        |

> Chi tiết từng endpoint xem tại: [TEMPLATE_API_REFERENCE.md](./TEMPLATE_API_REFERENCE.md)

---

## Cấu hình & tích hợp

### Đăng ký service (Startup.cs / DI)

```csharp
services.AddScoped<I{ServiceName}, {ServiceName}>();
```

### Cấu hình appsettings.json

```json
{
  "{ModuleName}": {
    "SettingKey1": "value",
    "SettingKey2": true
  }
}
```

### Phụ thuộc module khác

| Module phụ thuộc         | Lý do                         |
|--------------------------|-------------------------------|
| `Ezy.Module.Identity`    | Xác thực người dùng           |
| `AllianceMiddlemanWebAPI.Core` | Truy cập dữ liệu chung  |

---

## Ghi chú & hạn chế

- [Lưu ý quan trọng 1]
- [Lưu ý quan trọng 2]
- **Deprecated:** [Tính năng nào đã bị loại bỏ hoặc không dùng nữa]

---

*Cập nhật lần cuối: {ngày}*
*Tác giả: {tên}*
