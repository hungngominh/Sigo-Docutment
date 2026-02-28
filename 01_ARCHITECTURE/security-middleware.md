# Security & Middleware

> **Source:** `Startup.cs`, `JwtSecurityKey`, `JwtBearerEventHelper`, `NotificationHub`, `ESCBaseServiceController`

---

## Middleware Pipeline

Thứ tự middleware trong `Startup.Configure()`:

```
Request
  │
  ├── 1. Exception Handler (dev: DeveloperExceptionPage)
  ├── 2. HTTPS Redirection
  ├── 3. Response Compression (Gzip + Brotli)
  ├── 4. Routing
  ├── 5. CORS
  ├── 6. Authentication (JWT Bearer)
  ├── 7. Authorization
  ├── 8. Forwarded Headers (XForwardedFor, XForwardedProto)
  ├── 9. Static Files
  │
  ├── Endpoints:
  │   ├── MapControllers()
  │   ├── MapHub<NotificationHub>("/NotificationHub")
  │   ├── /health/ready
  │   ├── /health/live
  │   └── /deployment
  │
  ▼
Response
```

---

## JWT Authentication

### Cấu hình

```csharp
// Startup.ConfigureServices()
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtSecurityKey.GetTokenValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context => { /* 401 nếu token invalid */ },
            OnTokenValidated = context => JwtBearerEventHelper.OnTokenValidated(context),
            OnMessageReceived = context => { /* Extract token từ query cho SignalR */ }
        };
    });
```

### JWT Claims

| Claim | Mô tả | Dùng ở |
|-------|-------|--------|
| `user_uniqueid` | GUID duy nhất của user (= `UserLogin.ID_GUID`) | Xác định user trong hầu hết service |
| `ClaimTypes.NameIdentifier` | User ID (numeric) | Fallback identification |

### Token Extraction

```
1. Authorization header: "Bearer {token}"          → Mặc định
2. Query parameter: ?access_token={token}           → Cho SignalR connections
3. Query parameter: ?key={System_Security_Key_SignalR}&appid={appId}  → Cho internal services
```

### User Identification Flow

```csharp
// Từ JWT claim → tìm user trong cache
var userGUId = Context.User?.FindFirst("user_uniqueid")?.Value;
var user = CachedDataManagement.UserLogins
    .FirstOrDefault(t => t.ID_GUID.HasValue
        && t.ID_GUID.Value.ToString() == userGUId);
```

### Token Object khi login

```csharp
EzyLoginUserInfo {
    user_name       // Username
    user_id         // Numeric ID
    name            // Display name
    avatar_url      // Avatar path
    user_mobile     // Phone number
    user_type       // Loại user
    user_uniqueid   // GUID → claim chính trong JWT
    fa2_needverify  // Cần xác thực 2FA?
    fa2_needenable  // Bắt buộc bật 2FA?
}
```

---

## Anonymous Endpoints

Các endpoint không cần JWT token:

```csharp
var dontAllow = new string[] {
    "/api/v1/SearchingRentalService/GetSettingApp",
    "/api/v1/User/HomePage_Website",
    "/api/v2/User/HomePage_App",
    "/api/v1/User/HomePage_App",
    "/api/v1/GlobalAppSetting/FisrtSetting",
    "/api/v1/Account/SocialLogin"
};
```

Ngoài ra, các endpoint không có `[Authorize]` attribute cũng cho phép anonymous access.

---

## Two-Factor Authentication (2FA)

### Flow

```
Login request
  │
  ├── Validate username/password → OK
  │
  ├── Check Auth2FAHelper.GetAauth2FASetting(userId)
  │   ├── Using2FA = false → trả token bình thường
  │   └── Using2FA = true
  │       ├── IsWhiteListPassed(appName, username) = true → bypass 2FA
  │       └── IsWhiteListPassed = false
  │           ├── 2FA chưa bật → token_type = "FA2_Enable" (yêu cầu setup)
  │           └── 2FA đã bật → token_type = "FA2_Verify" (yêu cầu nhập mã)
  │
  ▼
Response: token + token_type
```

**Library:** Google Authenticator v2.1.1 — tạo TOTP codes.

**IsWhiteListPassed():** Kiểm tra kết hợp nhiều tiêu chí — device (DeviceId) + IP address + app version. Nếu cả 3 match whitelist → bypass 2FA. Admin cấu hình whitelist qua portal.

### Request Logging & PII Redaction

Request body được log vào `RequestParam` nhưng **có cơ chế redaction**: các fields sensitive (password, token, secret) được filter trước khi lưu vào DB log. Đảm bảo không lưu plain-text credentials.

---

## Authorization — Role-Based Access

### Pattern

```csharp
// Trong service layer:
sError = CheckAccessPermissionAndLog_Role(projectId, "SCREEN_CODE");

// Không cần projectId:
sError = CheckAccessPermissionAndLog_Role_NoProject("STAFF_LIST");
```

### Screen Codes phổ biến

| Code | Mô tả |
|------|-------|
| `LIST` | Xem danh sách |
| `EDIT` | Chỉnh sửa |
| `DELETE` | Xoá |
| `STAFF_LIST` | Quản lý nhân viên |

### Flow kiểm tra quyền

```
1. Lấy user từ JWT claim
2. Tra bảng quyền: User → Role → Permission → ScreenCode
3. Nếu không có quyền → trả EzyResultObject { Status = 0, Msg = "Không có quyền" }
4. Log action vào audit trail
```

---

## CORS Configuration

```csharp
app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true)  // Cho phép mọi origin
    .AllowCredentials());
```

> **Lưu ý:** CORS đang mở hoàn toàn (`SetIsOriginAllowed(origin => true)`). Phù hợp cho giai đoạn phát triển và khi API chỉ serve cho mobile apps.

---

## Response Compression

```csharp
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();     // Gzip
    options.Providers.Add<BrotliCompressionProvider>();   // Brotli
    options.MimeTypes = new[] { "application/json", "text/plain", "text/html" };
});

// Gzip level: Fastest (ưu tiên tốc độ)
```

---

## Error Handling

### Model Validation

```csharp
// Startup — Custom invalid model response
options.InvalidModelStateResponseFactory = context =>
{
    var errors = context.ModelState
        .Where(e => e.Value.Errors.Count > 0)
        .Select(e => new { Field = e.Key, Message = e.Value.Errors.First().ErrorMessage })
        .ToList();
    return new OkObjectResult(new {
        Success = false,
        Msg = sMessage,
        Errors = errors
    });
};
```

### Exception Logging (Client-side)

```
POST /api/v1/Exception/Save
→ Lưu exception từ mobile app vào DB
→ Params: exception data + source location + IP address
```

### Service-level Error Pattern

```csharp
// Mọi API trả về EzyResultObject<T>
{
    "StatusCode": 1,  // 1 = OK, 0 = Error
    "Msg": "...",     // Error message nếu StatusCode = 0
    "Data": { }       // Data nếu StatusCode = 1
}
```

---

## Request Logging

### Pattern trong Base Controllers

```csharp
// BaseAPIFileController / BaseEzyServiceController
LogRequestBaseItemParam logRequestParam = LogRequest_Start(oParam, sRequestPath, sRequestId);

// ... xử lý request ...

LogRequest_End(logRequestParam, oParam, totalMili, result);
```

### Dữ liệu được log

| Field | Mô tả |
|-------|-------|
| RequestId | GUID unique cho mỗi request |
| RequestPath | HTTP endpoint path |
| RequestParam | Request body (serialized) |
| ResponseTime | Tổng milliseconds xử lý |
| Result | Success/failure + status code |
| IpAddress | IP client (`GetRemoteIpAddress()`) |

### User Action Tracking

```csharp
// Trong BaseCategoryController.DoJob()
UserActionHistoryHelper.UpdateUserInteractWithApp(appName, logUserId);
// → Cập nhật thời gian tương tác cuối của user
```

---

## Health Checks

```
GET /health/ready    → Kiểm tra ứng dụng sẵn sàng nhận request
GET /health/live     → Kiểm tra ứng dụng đang chạy (return 200)
GET /deployment      → Thông tin version hiện tại
```

---

## Forwarded Headers

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                     | ForwardedHeaders.XForwardedProto
});
```

Cần thiết khi chạy sau reverse proxy (Nginx, Load Balancer) để lấy đúng IP client và scheme (HTTP/HTTPS).

---

## Base Controller Hierarchy

```
Controller
  └── ESCBaseServiceController<IService>        ← Hầu hết API controllers
        ├── LogUserId                            ← User ID từ JWT
        ├── LogUser                              ← User info đầy đủ
        ├── GetRemoteIpAddress()                 ← IP client
        └── BaseEzyServiceController<IService>
              ├── LogRequest_Start/End()          ← Request logging
              └── BaseAPIFileController<IService>
                    └── File upload/download + logging

  └── EzyBaseCategoryController                 ← Config/Category CRUD
        ├── CheckAccessPermissionAndLog_Role()   ← Role-based access
        └── DoJob<T>()                           ← Wrapper: validate + execute + log
```

---

*Xem thêm: [overview.md](./overview.md) | [project-structure.md](./project-structure.md) | [ADR-0004: JWT Authentication](../07_ADR/0004-jwt-authentication.md)*
