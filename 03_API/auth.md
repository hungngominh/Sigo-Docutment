# API: Authentication

> **Base:** `/api/v1/Account`
> **Controllers:** `AccountController`, `UserAccessRegisterTokenController`

## Mục lục
- [Login](#post-apiv1accountlogin)
- [Register](#post-apiv1accountregister)
- [GetOTP](#post-apiv1accountgetotp)
- [VerifyOTP](#post-apiv1accountverifyotp)
- [Social Login](#post-apiv1accountsocialuserinfo)
- [Logout](#post-apiv1accountlogout)
- [ForgotPassword](#post-apiv1accountforgotpassword)
- [ResetPassword](#post-apiv1accountresetpassword)
- [ChangePassword](#post-apiv1accountchangepassword)
- [ValidateToken](#post-apiv1accountvalidatetoken)
- [UserInfo](#get-apiv1accountuserinfo)
- [UpdateFields](#post-apiv1accountupdatefields)
- [DeleteAccount](#post-apiv1accountdeleteaccount)

---

## `POST /api/v1/Account/Login`

**Mô tả:** Đăng nhập bằng số điện thoại + mật khẩu, trả về JWT token.

**Phân quyền:** `[AllowAnonymous]`

### Request Body
```json
{
  "MobilePhone": "0901234567",
  "Password": "string"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| MobilePhone | string | Có | Số điện thoại VN (10 số) |
| Password | string | Có | Mật khẩu |

### Response — `EzyResultObject<EzyLoginToken>`
```json
{
  "StatusCode": 1,
  "Msg": null,
  "Data": {
    "access_token": "eyJ...",
    "token_type": "bearer",
    "expires_in": 3600,
    "refresh_token": "..."
  }
}
```

### Lỗi thường gặp
| StatusCode | Msg | Nguyên nhân |
|-----------|-----|------------|
| 0 | Vui lòng nhập đúng mật khẩu | Sai mật khẩu |
| 0 | Vui lòng nhập đúng số điện thoại | Sai định dạng SĐT |

---

## `POST /api/v1/Account/Register`

**Mô tả:** Đăng ký tài khoản mới. Trả về token ngay sau khi đăng ký thành công (auto-login).

**Phân quyền:** `[AllowAnonymous]`

**Hiệu năng:** avg ~944ms

### Request Body — `UserRegisterModel`
```json
{
  "MobilePhone": "0901234567",
  "Password": "string",
  "FullName": "Nguyễn Văn A",
  "OTP": "123456"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| MobilePhone | string | Có | SĐT đăng ký (phải verify OTP trước) |
| Password | string | Có | Mật khẩu mới |
| FullName | string | Có | Họ tên đầy đủ |
| OTP | string | Có | Mã OTP đã verify |

### Response — `EzyResultObject<OAuthTokenResponse>`
```json
{
  "StatusCode": 1,
  "Msg": "Đăng ký thành công",
  "Data": {
    "access_token": "eyJ...",
    "token_type": "bearer",
    "expires_in": 3600,
    "refresh_token": "..."
  }
}
```

> **Lưu ý:** Gọi `GetOTP` → `VerifyOTP` trước khi gọi `Register`.

---

## `POST /api/v1/Account/GetOTP`

**Mô tả:** Gửi mã OTP về số điện thoại qua SMS.

**Phân quyền:** `[AllowAnonymous]`

**Thống kê:** 460,229 lượt/30 ngày | avg **1.75ms**

### Request Body — `AccountParamModel`
```json
{
  "MobilePhone": "0901234567"
}
```

### Response — `EzyResultObject<AccountResultModel>`
```json
{
  "StatusCode": 1,
  "Msg": "OTP đã gửi",
  "Data": {
    "OTPSent": true,
    "ExpireInSeconds": 120
  }
}
```

> **Rate limit:** SĐT bị giới hạn N requests/giờ. Nếu vượt → thêm vào `GetOTPBlacklist`.

---

## `POST /api/v1/Account/VerifyOTP`

**Mô tả:** Xác minh mã OTP đã nhận.

**Phân quyền:** `[AllowAnonymous]`

**Thống kê:** 2,272 lượt/30 ngày | avg 9.17ms

### Request Body — `AccountParamModel`
```json
{
  "MobilePhone": "0901234567",
  "OTP": "123456"
}
```

### Response — `EzyResultObject<AccountResultModel>`
```json
{
  "StatusCode": 1,
  "Msg": "Xác minh thành công",
  "Data": {
    "IsVerified": true
  }
}
```

---

## `POST /api/v1/Account/SocialUserInfo`

**Mô tả:** Đăng nhập / đăng ký bằng tài khoản mạng xã hội (Google, Facebook, Apple). Nếu user chưa tồn tại → tự động tạo tài khoản mới.

**Phân quyền:** `[AllowAnonymous]`

### Request Body — `UserLoginSocialModel`
```json
{
  "Provider": "Google",
  "AccessToken": "ya29.a0...",
  "IdToken": "eyJ..."
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| Provider | string | Có | `"Google"` / `"Facebook"` / `"Apple"` |
| AccessToken | string | Có | Access token từ OAuth provider |
| IdToken | string | Không | ID token (Google) |

### Response — `EzyResultObject<SocialUserInfo>`
```json
{
  "StatusCode": 1,
  "Data": {
    "access_token": "eyJ...",
    "token_type": "bearer",
    "expires_in": 3600,
    "IsNewUser": false
  }
}
```

---

## `POST /api/v1/Account/Logout`

**Mô tả:** Đăng xuất, huỷ refresh token và xoá device registration.

**Phân quyền:** `[Authorize]`

### Request Body — `LogOutModel`
```json
{
  "DeviceNumber": "device-uuid",
  "FCMTokenKey": "fcm-token"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| DeviceNumber | string | Không | Device ID để xoá push notification registration |
| FCMTokenKey | string | Không | FCM token để unregister |

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Đăng xuất thành công"
}
```

---

## `POST /api/v1/Account/ForgotPassword`

**Mô tả:** Yêu cầu reset mật khẩu. Gửi OTP về SĐT để xác thực.

**Phân quyền:** `[AllowAnonymous]`

### Request Body — `UserForgotPassModel`
```json
{
  "MobilePhone": "0901234567"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "OTP đã gửi về số điện thoại"
}
```

---

## `POST /api/v1/Account/ResetPassword`

**Mô tả:** Đặt mật khẩu mới sau khi verify OTP (forgot password flow). Trả về token để auto-login.

**Phân quyền:** `[AllowAnonymous]`

### Request Body — `AccountParamModel`
```json
{
  "MobilePhone": "0901234567",
  "OTP": "123456",
  "NewPassword": "newpassword123"
}
```

### Response — `EzyResultObject<OAuthTokenResponse>`
```json
{
  "StatusCode": 1,
  "Msg": "Đặt lại mật khẩu thành công",
  "Data": {
    "access_token": "eyJ...",
    "token_type": "bearer"
  }
}
```

---

## `POST /api/v1/Account/ChangePassword`

**Mô tả:** Đổi mật khẩu (user đã đăng nhập).

**Phân quyền:** `[Authorize]`

### Request Body — `UserChangePassModel`
```json
{
  "OldPassword": "currentpassword",
  "NewPassword": "newpassword123"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Đổi mật khẩu thành công"
}
```

---

## `POST /api/v1/Account/ValidateToken`

**Mô tả:** Kiểm tra password recovery token còn hợp lệ không.

**Phân quyền:** `[AllowAnonymous]`

### Request Body — `PasswordRecoveryModel`
```json
{
  "Token": "recovery-token-string"
}
```

### Response — `EzyResultObject<ValidationToken>`
```json
{
  "StatusCode": 1,
  "Data": {
    "IsValid": true,
    "ExpiresAt": "2026-03-01T00:00:00Z"
  }
}
```

---

## `GET /api/v1/Account/UserInfo`

**Mô tả:** Lấy thông tin tài khoản hiện tại.

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<UserProfileInfo>`
```json
{
  "StatusCode": 1,
  "Data": {
    "UserLoginId": "user-guid",
    "FullName": "Nguyễn Văn A",
    "MobilePhone": "0901234567",
    "Email": "a@email.com",
    "AvatarUrl": "https://...",
    "IsVerified": true,
    "Roles": ["User", "Owner"]
  }
}
```

---

## `POST /api/v1/Account/UpdateFields`

**Mô tả:** Cập nhật một hoặc nhiều field trong profile (partial update).

**Phân quyền:** `[Authorize]`

### Request Body — `EzyObjectFieldValues`
```json
{
  "FieldValues": [
    { "FieldName": "FullName", "Value": "Nguyễn Văn B" },
    { "FieldName": "Email", "Value": "b@email.com" }
  ]
}
```

### Response — `EzyResultObject<UserProfileInfo>`
Trả về profile đã cập nhật.

---

## `POST /api/v1/Account/DeleteAccount`

**Mô tả:** Yêu cầu xoá tài khoản. Cần verify OTP trước.

**Phân quyền:** `[AllowAnonymous]`

### Request Body — `AccountParamModel`
```json
{
  "MobilePhone": "0901234567",
  "OTP": "123456"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Tài khoản đã được xoá"
}
```

> **Lưu ý:** Soft delete — dữ liệu vẫn lưu trong DB nhưng user không thể đăng nhập.

---

## Luồng xác thực

### Đăng ký mới
```
1. POST /Account/GetOTP       → { MobilePhone }
2. POST /Account/VerifyOTP    → { MobilePhone, OTP }
3. POST /Account/Register     → { MobilePhone, Password, FullName, OTP }
   ← access_token (auto-login)
```

### Quên mật khẩu
```
1. POST /Account/ForgotPassword  → { MobilePhone }
2. POST /Account/GetOTP          → { MobilePhone }
3. POST /Account/VerifyOTP       → { MobilePhone, OTP }
4. POST /Account/ResetPassword   → { MobilePhone, OTP, NewPassword }
   ← access_token (auto-login)
```

### Social Login
```
1. OAuth flow (Google/Facebook/Apple) → nhận AccessToken
2. POST /Account/SocialUserInfo       → { Provider, AccessToken }
   ← access_token + IsNewUser flag
```

---

## Ghi chú kỹ thuật

- **Token format:** JWT Bearer — truyền trong header `Authorization: Bearer {token}`
- **Token lifetime:** `expires_in` = 3600 giây (1 giờ)
- **Refresh token:** Dùng để lấy access token mới khi hết hạn → gọi OAuth2 server `localhost:12391`
- **OTP lifetime:** 120 giây
- **OTP rate limit:** Quản lý bởi `GetOTPBlacklist` — giới hạn spam
- **Response wrapper:** Tất cả trả về `EzyResultObject<T>` — `StatusCode = 1` thành công, `StatusCode = 0` thất bại

---

*Xem thêm: [identity.md](../02_MODULES/identity.md) | [ADR-0004](../07_ADR/0004-jwt-authentication.md)*
