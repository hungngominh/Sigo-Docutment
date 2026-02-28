# API: Hotline (OMI Call)

> **Module:** `Ezy.Module.Hotline`
> **Controllers:** `ConfigOMICallAPIController`, `ConfigOMICallAPIDetailController`, `ConfigOMICallHotlineController`, `OMICallWebhookController`
> **Base:** `/api/v1/ConfigOMICallAPI`, `/api/v1/ConfigOMICallHotline`, `/api/v1/OMICallWebhook`

## Mục lục
- [Tổng quan](#tổng-quan)
- [Cấu hình OMI API](#post-apiv1configomicallapilist)
- [Cập nhật Access Token](#post-apiv1configomicallapiupdateaccesstoken)
- [Webhook Action](#post-apiv1configomicallapiwebhookaction)
- [Cấu hình Hotline](#post-apiv1configomicallhotlinelist)
- [Webhook nhận cuộc gọi](#post-apiv1omicallwebhookreceiving)

---

## Tổng quan

Module Hotline tích hợp với **OMI Call** (nền tảng gọi điện IP) để:
1. Quản lý cấu hình kết nối OMI Call API
2. Cấu hình hotline cho từng dự án/chi nhánh
3. Nhận và xử lý webhook sự kiện cuộc gọi từ OMI Call

---

## `POST /api/v1/ConfigOMICallAPI/List`

**Mô tả:** Lấy danh sách cấu hình OMI Call API.

**Phân quyền:** `[Authorize]`

### Request Body — `ConfigOMICallAPIParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ConfigOMICallAPIModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 1,
    "Data": [
      {
        "ConfigId": "config-guid",
        "AppId": "omi-app-id",
        "ApiKey": "***masked***",
        "IsActive": true
      }
    ]
  }
}
```

---

## `POST /api/v1/ConfigOMICallAPI/UpdateAccessToken`

**Mô tả:** Cập nhật Access Token kết nối OMI Call API (refresh khi token hết hạn).

**Phân quyền:** `[Authorize]`

### Request Body — `ConfigOMICallAPIParamModel`
```json
{
  "ConfigId": "config-guid"
}
```

### Response — `EzyResultObject<ConfigOMICallAPIModel>`
```json
{
  "StatusCode": 1,
  "Msg": "Access token đã được cập nhật",
  "Data": {
    "ConfigId": "config-guid",
    "AccessToken": "new-access-token",
    "TokenExpiredAt": "2026-03-01T00:00:00Z"
  }
}
```

---

## `POST /api/v1/ConfigOMICallAPI/WebhookAction`

**Mô tả:** Cấu hình webhook action cho OMI Call (thiết lập URL nhận sự kiện).

**Phân quyền:** `[Authorize]`

### Request Body — `ConfigOMICallAPIParamModel`
```json
{
  "ConfigId": "config-guid",
  "WebhookUrl": "https://api.sigo.vn/api/v1/OMICallWebhook/Receiving"
}
```

---

## `POST /api/v1/ConfigOMICallAPIDetail/List`

**Mô tả:** Lấy danh sách cấu hình OMI Call API chi tiết.

Tương tự `ConfigOMICallAPI/List`.

---

## `POST /api/v1/ConfigOMICallHotline/List`

**Mô tả:** Lấy danh sách cấu hình hotline (số điện thoại hotline + extension).

**Phân quyền:** `[Authorize]`

### Request Body — `ConfigOMICallHotlineParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "IsActive": true
}
```

### Response — `EzyResultObject<EzyDataSourceResult<ConfigOMICallHotlineModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 3,
    "Data": [
      {
        "HotlineId": "hotline-guid",
        "HotlineNumber": "1900xxxx",
        "Extension": "101",
        "AgentName": "CSKH Team",
        "IsActive": true
      }
    ]
  }
}
```

---

## `POST /api/v1/OMICallWebhook/Receiving`

**Mô tả:** Nhận sự kiện webhook từ OMI Call (cuộc gọi đến/đi, trạng thái cuộc gọi).

**Phân quyền:** Không yêu cầu (Public webhook — OMI Call gọi vào)

### Request Body — `Dictionary<string, object>`

OMI Call gửi payload dạng dynamic JSON, cấu trúc tuỳ theo loại event:

```json
{
  "event": "call_started",
  "call_id": "call-uuid",
  "caller": "0901234567",
  "callee": "1900xxxx",
  "timestamp": "2026-01-01T10:00:00Z"
}
```

Các event thường gặp:
| Event | Mô tả |
|-------|-------|
| `call_started` | Cuộc gọi bắt đầu |
| `call_ended` | Cuộc gọi kết thúc |
| `call_answered` | Cuộc gọi được trả lời |
| `call_missed` | Cuộc gọi nhỡ |

### Response — `object`
```json
{
  "success": true
}
```

> **Bảo mật:** Xác thực webhook bằng secret key trong header hoặc payload. Endpoint này công khai nhưng nên validate chữ ký.

---

## Ghi chú

- `OMICallWebhook/Receiving` là endpoint duy nhất không cần auth — OMI Call gọi vào từ bên ngoài.
- Khi nhận webhook, hệ thống xử lý và cập nhật trạng thái cuộc gọi vào DB.
- `UpdateAccessToken` cần được gọi khi token OMI hết hạn (kiểm tra qua `ConfigOMICallAPI/List`).
