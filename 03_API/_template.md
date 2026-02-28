# API Template — Hướng dẫn viết tài liệu endpoint

> Copy file này khi viết tài liệu cho endpoint mới.
> Xoá các dòng comment `<!-- TODO -->` sau khi điền xong.

---

## `[METHOD] /api/v[version]/[Controller]/[Action]`

**Mô tả:** <!-- Một câu mô tả endpoint làm gì -->

**Phân quyền:** `[AllowAnonymous]` / `[Authorize]`

---

### Request

#### Headers
| Header | Bắt buộc | Giá trị |
|--------|----------|---------|
| `Authorization` | Có (nếu Authorize) | `Bearer {token}` |
| `Content-Type` | Có | `application/json` |

#### Body / Query Parameters
```json
{
  "field1": "string",
  "field2": 0
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| field1 | string | Có | <!-- TODO --> |
| field2 | integer | Không | <!-- TODO --> |

---

### Response

**Thành công (200):**
```json
{
  "Status": 1,
  "Message": "Thành công",
  "Data": {
  }
}
```

**Thất bại:**
```json
{
  "Status": 0,
  "Message": "Mô tả lỗi",
  "Data": null
}
```

---

### Ví dụ

```bash
curl -X POST "https://api.sigo.vn/api/v1/[Controller]/[Action]" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{}'
```

---

### Ghi chú
<!-- TODO: Lưu ý đặc biệt, side effects, rate limit, ... -->
