# Module: Hotline — Tổng đài hỗ trợ

> **Solution:** `Ezy.Module.Hotline` | **Trạng thái:** Active

## Mục lục
- [Tổng quan](#tổng-quan)
- [Cấu trúc](#cấu-trúc)
- [Entities & Models](#entities--models)
- [Services](#services)
- [API Endpoints](#api-endpoints)
- [OMI Call Integration](#omi-call-integration)
- [Webhook flow](#webhook-flow)

---

## Tổng quan

Module Hotline tích hợp **OMI Call** (Operator Management Interface) vào Sigo platform, cung cấp hệ thống tổng đài cho đội support.

**Tính năng:**
- Cấu hình kết nối OMI Call API (token, webhook endpoint)
- Quản lý số hotline của tổng đài
- Nhận webhook từ OMI Call khi có cuộc gọi đến/đi
- Ghi nhận và xử lý sự kiện cuộc gọi trong hệ thống Sigo

**Đối tượng sử dụng:**
- **Admin:** Cấu hình OMI Call API credentials và hotline numbers
- **Staff Support:** Xử lý cuộc gọi đến qua OMI Call interface
- **Hệ thống:** Tự động nhận webhook events từ OMI Call

**OMI Call là gì:**
> OMI Call là nền tảng tổng đài cloud (CPaaS) của Việt Nam. Khi khách hàng gọi vào số hotline, OMI Call route cuộc gọi tới agent và gửi webhook events về hệ thống Sigo để log và xử lý.

---

## Cấu trúc

```
Ezy.Module.Hotline/
├── Ezy.Module.Hotline.API/
│   └── Controllers/
│       ├── ConfigOMICallAPIController.cs        # Cấu hình OMI API credentials
│       ├── ConfigOMICallHotlineController.cs    # Cấu hình số hotline
│       ├── OMICallWebhookController.cs          # Nhận webhook từ OMI
│       └── ProjectBaseCategoryController.cs     # Base controller
│
├── Ezy.Module.Hotline.Core/
│   ├── Categories/
│   │   ├── CategoryDataContext.cs               # DbContext
│   │   ├── CategoryEntities.ConfigOMICallAPI.cs # Entity cấu hình API
│   │   └── CategoryEntities.ConfigOMICallHotline.cs  # Entity số hotline
│   ├── Cached/
│   │   ├── CachedDataManagement_ConfigOMICallAPI.cs
│   │   └── CachedDataManagement_ConfigOMICallHotline.cs
│   └── DataInfo/
│       ├── ConfigOMICallAPIInfo.cs              # Cached API info
│       └── ConfigOMICallHotlineInfo.cs          # Cached hotline info
│
└── Ezy.Module.Hotline.Shared/
    ├── Helpers/
    │   ├── OMICallAPIHelper.cs                  # API call wrapper
    │   ├── OMICallWebhookHelper.cs              # Webhook processor
    │   ├── HttpRequestHelper.cs                 # HTTP utilities
    │   ├── GGChatNotifyHelper.cs                # Google Chat alerts
    │   └── EzyMicroserviceFrameWorkManagement.cs
    ├── Models/
    │   ├── ConfigOMICallAPIModel.cs
    │   └── ConfigOMICallHotlineModel.cs
    └── Services/Categories/
        ├── ConfigOMICallAPI/
        │   ├── IConfigOMICallAPIService.cs
        │   └── ConfigOMICallAPIService.cs
        └── ConfigOMICallHotline/
            ├── IConfigOMICallHotlineService.cs
            └── ConfigOMICallHotlineService.cs
```

**Tổng số C# files:** ~41

---

## Entities & Models

### `ConfigOMICallAPI` — Cấu hình kết nối OMI

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | int | PK |
| `ID_GUID` | Guid | Business key |
| `ApiUrl` | string | Base URL của OMI Call API |
| `AccessToken` | string | Bearer token để gọi API |
| `TokenExpiry` | DateTime? | Thời điểm hết hạn token |
| `WebhookUrl` | string | URL Sigo nhận webhook từ OMI |
| `WebhookSecret` | string | Secret để verify webhook |
| `IsActive` | bool | Config đang sử dụng |
| `Note` | string | Ghi chú |
| `Log_CreatedDate` | DateTime | |
| `Log_UpdatedDate` | DateTime | |

### `ConfigOMICallHotline` — Cấu hình số hotline

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | int | PK |
| `ID_GUID` | Guid | Business key |
| `HotlineNumber` | string | Số điện thoại hotline |
| `DisplayName` | string | Tên hiển thị (VD: "Sigo Hỗ trợ") |
| `Department` | string | Phòng ban (Support, Sales...) |
| `OMICallExtension` | string | Extension trong hệ thống OMI |
| `IsActive` | bool | Hotline đang hoạt động |
| `MaxConcurrentCalls` | int | Số cuộc gọi đồng thời tối đa |
| `WorkingHoursStart` | TimeSpan | Giờ bắt đầu làm việc |
| `WorkingHoursEnd` | TimeSpan | Giờ kết thúc làm việc |
| `Log_CreatedDate` | DateTime | |

---

## Services

### `IConfigOMICallAPIService`

| Method | Mô tả |
|--------|-------|
| `GetActiveConfig()` | Lấy cấu hình API đang active |
| `UpdateAccessToken(id, token, expiry)` | Cập nhật access token |
| `UpdateWebhookConfig(id, url, secret)` | Cập nhật webhook config |
| `TestConnection(id)` | Test kết nối tới OMI API |

### `IConfigOMICallHotlineService`

| Method | Mô tả |
|--------|-------|
| `GetActiveHotlines()` | Lấy danh sách hotline đang active |
| `GetByExtension(extension)` | Tìm hotline theo OMI extension |
| `IsWithinWorkingHours(id)` | Kiểm tra trong giờ làm việc |

---

## API Endpoints

Base URL: `/api/v1/`

### Cấu hình OMI API

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/ConfigOMICallAPI/List` | Danh sách cấu hình API | Required |
| POST | `/ConfigOMICallAPI/Add` | Thêm cấu hình | Admin |
| POST | `/ConfigOMICallAPI/Update` | Cập nhật cấu hình | Admin |
| POST | `/ConfigOMICallAPI/UpdateAccessToken` | Cập nhật token | Admin |
| POST | `/ConfigOMICallAPI/WebhookAction` | Cấu hình webhook | Admin |

### Cấu hình Hotline

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/ConfigOMICallHotline/List` | Danh sách số hotline | Required |
| POST | `/ConfigOMICallHotline/Add` | Thêm số hotline | Admin |
| POST | `/ConfigOMICallHotline/Update` | Cập nhật số hotline | Admin |
| POST | `/ConfigOMICallHotline/Delete` | Xoá số hotline | Admin |

### Webhook Receiver

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/OMICallWebhook/Receiving` | Nhận webhook events từ OMI | **Public** (webhook) |

> **Lưu ý bảo mật:** Endpoint `/OMICallWebhook/Receiving` không có JWT auth (OMI Call không gửi JWT). Verify tính hợp lệ bằng `WebhookSecret` trong request headers.

---

## OMI Call Integration

### Luồng Token Management

```
Admin → POST /ConfigOMICallAPI/UpdateAccessToken
    │ { accessToken: "eyJ...", expiresAt: "2026-03-01" }
    ▼
ConfigOMICallAPI table updated
    │
    ▼
CachedDataManagement_ConfigOMICallAPI invalidated
    │
    ▼
OMICallAPIHelper.GetToken() → reads from cache
```

### Gọi OMI API

```csharp
// OMICallAPIHelper
public async Task<T> CallAsync<T>(string endpoint, object body)
{
    var config = await _cacheService.GetActiveConfig();

    // Auto-refresh token nếu gần hết hạn
    if (config.TokenExpiry < DateTime.UtcNow.AddMinutes(5))
        await RefreshTokenAsync();

    var response = await _httpClient.PostAsync(
        $"{config.ApiUrl}/{endpoint}",
        JsonContent.Create(body),
        headers: { Authorization = $"Bearer {config.AccessToken}" }
    );

    return await response.Content.ReadFromJsonAsync<T>();
}
```

---

## Webhook flow

Khi có cuộc gọi qua hotline, OMI Call gửi POST request tới `/OMICallWebhook/Receiving`:

```
[Caller] → gọi vào hotline
    │
    ▼
[OMI Call Platform]
    │ Xử lý routing, ghi âm...
    │
    ├─ POST /api/v1/OMICallWebhook/Receiving ──────────────────►│
    │   Content-Type: application/json                           │
    │   { "event": "call.started",                              │
    │     "callId": "abc123",                                   │
    │     "from": "0901234567",                                 │
    │     "to": "19001234",                                     │
    │     "hotlineExtension": "1001",                           │
    │     "timestamp": "..." }                                  │
    │                                                           │
    │               [OMICallWebhookController]                  │
    │               │ → OMICallWebhookHelper.ReceivingWebhook() │
    │               │ → Match hotline với ConfigOMICallHotline  │
    │               │ → Log call event                          │
    │               │ → Notify support staff (Google Chat)      │
    │               │ → Return 200 OK                           │
    │◄─ 200 OK ─────│                                           │
```

### Các loại webhook events từ OMI Call

| Event | Mô tả |
|-------|-------|
| `call.started` | Cuộc gọi mới bắt đầu |
| `call.answered` | Agent nghe máy |
| `call.ended` | Cuộc gọi kết thúc |
| `call.missed` | Cuộc gọi nhỡ (không ai nghe) |
| `recording.ready` | File ghi âm sẵn sàng |

### Notify qua Google Chat

Khi có cuộc gọi nhỡ hoặc lỗi, `GGChatNotifyHelper` gửi alert:

```csharp
// GGChatNotifyHelper
public async Task SendAlertAsync(string message)
{
    // POST tới Google Chat Webhook URL
    await _httpClient.PostAsync(_chatWebhookUrl, new
    {
        text = $"[Hotline Alert] {message}"
    });
}
```

---

*Xem thêm: [overview.md](../01_ARCHITECTURE/overview.md) | [config-keys.md](../06_OPERATIONS/config-keys.md)*
