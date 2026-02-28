# ADR-0004: Xác thực bằng JWT Bearer Token + OAuth2 nội bộ

## Status

Accepted

## Context

Sigo API phục vụ cả ứng dụng mobile (iOS/Android) và web client. Yêu cầu xác thực:
- Stateless authentication phù hợp với REST API
- Hỗ trợ multiple clients (mobile app, web admin, third-party)
- Tích hợp Google OAuth2 cho social login
- Real-time connection qua SignalR cần pass token
- OTP authentication cho đăng nhập bằng số điện thoại (thị trường Việt Nam)
- Token có thể revoke khi logout

Team cần quyết định cơ chế authentication và authorization cho toàn hệ thống.

## Decision Drivers

* **Stateless** — API server không lưu session state
* **Mobile-friendly** — JWT phù hợp với mobile app
* **Social login** — hỗ trợ Google OAuth2
* **SignalR support** — truyền token qua query string cho WebSocket
* **Internal control** — tự kiểm soát auth flow, không phụ thuộc external IdP
* **Vietnamese market** — OTP qua SMS cho đăng ký/đăng nhập

## Considered Options

### Option 1: JWT Bearer + OAuth2 Server nội bộ
- **Pros:**
  - Stateless, scalable
  - Kiểm soát hoàn toàn logic auth
  - Hỗ trợ refresh token
  - Dễ tích hợp social login
  - Pass token qua query string cho SignalR
- **Cons:**
  - Cần maintain OAuth2 server nội bộ
  - Token revocation phức tạp hơn session

### Option 2: ASP.NET Identity + Session
- **Pros:**
  - Built-in, ít code hơn
  - Server-side revocation dễ
- **Cons:**
  - Stateful, khó scale horizontally
  - Không phù hợp cho mobile API
  - Phụ thuộc sticky session trên load balancer

### Option 3: Azure Active Directory / External IdP
- **Pros:**
  - Managed service, không cần maintain
  - Enterprise features (MFA, Conditional Access)
- **Cons:**
  - Chi phí cao
  - Vendor lock-in
  - Phức tạp cho custom auth flow (OTP, phone login)
  - Latency cho mỗi token validation

### Option 4: Firebase Authentication
- **Pros:**
  - Free tier, Google managed
  - Social login dễ
- **Cons:**
  - Phụ thuộc Google/Firebase
  - Tùy chỉnh hạn chế cho OTP flow Việt Nam
  - Khó kiểm soát user data

## Decision

Chúng ta sẽ dùng **JWT Bearer Token** làm cơ chế xác thực chính, với **OAuth2 server nội bộ** (`Ezy.Module.Identity`) để issue và validate token. Hỗ trợ thêm **Google OAuth2** cho social login.

## Rationale

1. **Stateless scaling:** JWT không cần server lưu session state → dễ horizontal scaling
2. **Mobile optimized:** Mobile app lưu JWT ở local storage/keychain, refresh token tự động
3. **Internal control:** OAuth2 nội bộ cho phép custom auth flow (OTP bằng SĐT Việt Nam, quản lý device)
4. **SignalR support:** JWT có thể pass qua query string (`?access_token=...`) — session không làm được điều này
5. **Unified auth:** Cùng một token cho REST API và WebSocket (SignalR)

## Cấu hình thực tế

```json
// appsettings.json
{
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "SigoAPI",
    "Audience": "SigoClient",
    "ExpiresInMinutes": 60
  },
  "USER_AUTO_LOGIN_URL": "http://localhost:12391/oauth2/token"
}
```

**Authentication flows:**
```
1. Login (Phone + Password):
   POST /api/v1/Account/Login
   → Gọi OAuth2 server nội bộ
   → Trả về { access_token, expires_in, refresh_token }

2. Google Login:
   POST /api/v1/Account/LoginGoogle
   → Validate Google token
   → Issue internal JWT

3. OTP Login:
   POST /api/v1/Account/GetOTP → SMS OTP
   POST /api/v1/Account/VerifyOTP → Validate OTP → JWT

4. SignalR:
   ws://host/NotificationHub?access_token=<jwt>
```

## Consequences

### Tích cực
- API server stateless, dễ scale và deploy
- Single token cho cả REST và WebSocket
- Custom OTP flow cho thị trường Việt Nam
- `[AllowAnonymous]` / `[Authorize]` attribute rõ ràng, dễ kiểm soát per-endpoint
- Device management: mỗi token gắn với device ID

### Tiêu cực
- Token không thể revoke ngay lập tức (trừ khi implement blacklist)
- Cần maintain OAuth2 server nội bộ (`Ezy.Module.Identity`)
- Secret key management phức tạp hơn session secret

### Rủi ro
- JWT secret key bị lộ → toàn bộ tokens compromise
- **Giảm thiểu:** Secret key lưu trong environment variable, rotate định kỳ
- Token expiry quá dài (60 phút) tăng rủi ro nếu token bị đánh cắp
- **Giảm thiểu:** Kết hợp refresh token và short-lived access token

## Related Decisions

- ADR-0001: ASP.NET Core 5.0 (JWT middleware tích hợp sẵn)
- ADR-0008: SignalR (cần JWT qua query string)
- ADR-0010: Identity Module (OAuth2 server)

---

*Ngày tạo: 2021-01-01*
