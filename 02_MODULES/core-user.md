# Core Module: User — Quản lý người dùng

> **Project:** `AllianceMiddlemanWebAPI` | **Domain:** Nghiệp vụ cốt lõi

## Mục lục
- [Tổng quan](#tổng-quan)
- [Controllers](#controllers)
- [Entities](#entities)
- [API Endpoints](#api-endpoints)
- [Xác minh danh tính](#xác-minh-danh-tính)
- [Thống kê người dùng](#thống-kê-người-dùng)

---

## Tổng quan

Module User quản lý toàn bộ thông tin người dùng trên Sigo platform:
- Hồ sơ cá nhân (tên, SĐT, email, avatar)
- Xác minh danh tính (CMND/CCCD, GPLX)
- Tài khoản ngân hàng thụ hưởng
- Thống kê hoạt động (tổng chuyến, rating, tỷ lệ huỷ)

**Phân biệt với Module Identity:**
- `Ezy.Module.Identity` → infrastructure (auth utilities, config keys)
- `Core User module` → business logic (profile, verification, stats)

---

## Controllers

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `UserController` | `api/v1/User` | Hồ sơ user, homepage data |
| `UserItem_ImageController` | `api/v1/UserItem_Image` | Ảnh hồ sơ (avatar, CMND...) |
| `UserItem_ImageVerifiedController` | `api/v1/UserItem_ImageVerified` | Ảnh đã xác minh |
| `UserLogin_BeneficiaryBank_MappingController` | `api/v1/UserLogin_BeneficiaryBank_Mapping` | Tài khoản ngân hàng |
| `UserUploadController` | `api/v1/UserUpload` | Upload ảnh/file |
| `AccountController` | `api/v1/Account` | Đăng nhập, đăng ký, OTP |

---

## Entities

### Hồ sơ & Xác minh

| Entity | Mô tả |
|--------|-------|
| `UserAccessRegisterToken` | Token đăng ký/truy cập user |
| `UserActionHistory` | Lịch sử hành động (login, update profile...) |
| `UserVerified` | Trạng thái xác minh danh tính |
| `UserItem_Image` | Metadata ảnh hồ sơ (loại: avatar, CMND mặt trước/sau, GPLX) |
| `UserItem_Image_File` | File ảnh thực tế |
| `UserItem_ImageVerified` | Ảnh đã được admin xác minh |
| `ReportUser` | Báo cáo vi phạm từ user khác |

### Tài chính

| Entity | Mô tả |
|--------|-------|
| `UserLogin_BeneficiaryBank_Mapping` | Tài khoản ngân hàng thụ hưởng (để rút tiền) |

### Thống kê

| Entity | Mô tả |
|--------|-------|
| `User_Calculating` | Thống kê tổng hợp: tổng chuyến, avg rating, doanh thu |
| `User_Calculating_OrderStatus` | Thống kê theo trạng thái đơn |
| `OwnerCancelOrderRatio` | Tỷ lệ huỷ đơn của owner |

---

## API Endpoints

### Account (Authentication)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Account/Login` | Đăng nhập phone + password | Public |
| POST | `/api/v1/Account/Register` | Đăng ký tài khoản mới | Public |
| POST | `/api/v1/Account/GetOTP` | Yêu cầu OTP qua SMS | Public |
| POST | `/api/v1/Account/VerifyOTP` | Xác thực OTP | Public |
| POST | `/api/v1/Account/SocialUserInfo` | Đăng nhập mạng xã hội | Public |
| POST | `/api/v1/Account/GetFaceBookAvatar` | Lấy avatar Facebook | Public |
| POST | `/api/v1/Account/ForgotPassword` | Quên mật khẩu | Public |
| POST | `/api/v1/Account/ResetPassword` | Reset mật khẩu | Public |
| POST | `/api/v1/Account/DeleteAccount` | Xoá tài khoản | Public |
| POST | `/api/v1/Account/ValidateToken` | Kiểm tra token hợp lệ | Public |
| POST | `/api/v1/Account/Logout` | Đăng xuất | Required |
| GET | `/api/v1/Account/UserInfo` | Thông tin tài khoản | Required |
| POST | `/api/v1/Account/UpdateFields` | Cập nhật thông tin | Required |
| POST | `/api/v1/Account/ChangePassword` | Đổi mật khẩu | Required |

### User Profile

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/User/HomePage_Website` | Homepage data (web) | Public |
| POST | `/api/v1/User/HomePage_App` | Homepage data (app v1) | Public |
| POST | `/api/v2/User/HomePage_App` | Homepage data (app v2) | Public |
| POST | `/api/v1/User/List` | Danh sách users | Required |
| POST | `/api/v1/User/Detail` | Chi tiết user | Required |
| POST | `/api/v1/User/Update` | Cập nhật hồ sơ | Required |

### Ảnh & Upload

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/UserItem_Image/List` | Danh sách ảnh hồ sơ | Required |
| POST | `/api/v1/UserItem_Image/Add` | Upload ảnh mới | Required |
| POST | `/api/v1/UserItem_ImageVerified/List` | Ảnh đã xác minh | Required |
| POST | `/api/v1/UserUpload/Upload` | Upload file | Required |

### Ngân hàng

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/UserLogin_BeneficiaryBank_Mapping/List` | Danh sách TK ngân hàng | Required |
| POST | `/api/v1/UserLogin_BeneficiaryBank_Mapping/Add` | Thêm TK ngân hàng | Required |
| POST | `/api/v1/UserLogin_BeneficiaryBank_Mapping/Update` | Cập nhật TK | Required |
| POST | `/api/v1/UserLogin_BeneficiaryBank_Mapping/Delete` | Xoá TK | Required |

---

## Xác minh danh tính

Sigo yêu cầu xác minh danh tính trước khi cho phép giao dịch tài chính (thuê xe, nhận tiền).

### Loại xác minh

| Loại | Yêu cầu | Quản lý bởi |
|------|---------|------------|
| **CMND/CCCD mặt trước** | Ảnh rõ, chưa hết hạn | Admin duyệt |
| **CMND/CCCD mặt sau** | Ảnh rõ | Admin duyệt |
| **GPLX** (Giấy phép lái xe) | Ảnh rõ, chưa hết hạn | Admin duyệt |
| **Ảnh chân dung** | Selfie match với CMND | Admin duyệt |

### VerifiedStepCode

User_Ext.VerifiedStepCode theo flow:

| Code | Ý nghĩa | Hành động tiếp theo |
|------|---------|-------------------|
| `STEP_0` | Chưa upload | Upload ảnh CMND mặt trước |
| `STEP_1` | Đã upload CMND trước | Upload CMND mặt sau |
| `STEP_2` | Đã upload CMND sau | Upload GPLX |
| `STEP_3` | Đã upload GPLX | Upload chân dung |
| `STEP_4` | Đã upload chân dung | Chờ admin duyệt |
| `VERIFIED` | Đã xác minh hoàn tất | Có thể giao dịch |

### Flow xác minh

```
User mở app → kiểm VerifiedStepCode
    │
    ├─ STEP_0-3: Hiển thị upload screen tương ứng
    │   POST /UserUpload/UploadImage (multipart/form-data)
    │   POST /User/ConfirmUpload → chuyển sang step tiếp
    │
    ├─ STEP_4: Hiển thị "Đang chờ duyệt"
    │   Admin review qua portal → Approve/Reject
    │   Reject → về STEP_0 (re-upload toàn bộ)
    │
    └─ VERIFIED: Cho phép thuê xe / cho thuê xe / rút tiền
```

### Điều kiện bắt buộc xác minh

| Hành động | Yêu cầu xác minh |
|-----------|------------------|
| Thuê xe (renter) | CMND + GPLX verified |
| Cho thuê xe (owner) | CMND verified |
| Rút tiền | CMND verified + TK ngân hàng |
| Đăng xe mới | CheckValidOwnerInfo_AddNewCar → kiểm tra đủ điều kiện |

---

## Thống kê người dùng

### `User_Calculating` — Tổng hợp

Được cập nhật bởi `User_Calculating_RentCarEngine` và `AutoUpdateUser_Calculating_OrderStatusEngine`:

| Chỉ số | Mô tả |
|--------|-------|
| Tổng chuyến cho thuê | Số chuyến xe owner đã cho thuê thành công |
| Tổng chuyến thuê | Số chuyến renter đã thuê thành công |
| Rating trung bình | Avg rating nhận được |
| Tỷ lệ phản hồi | % đơn được confirm trong thời gian quy định |
| Tỷ lệ huỷ | % đơn bị huỷ bởi user |
| Tổng doanh thu | Tổng tiền nhận được (owner) |

### `OwnerCancelOrderRatio` — Tỷ lệ huỷ owner

Được tính bởi `CalcCancelOrderRatioJob` (batch job daily):
- Owner có tỷ lệ huỷ cao → hạ priority trong search results
- Ảnh hưởng `RentalServiceItemPriorityList`

### OTP Blacklist

`GetOTPBlacklist` — Danh sách SĐT bị giới hạn yêu cầu OTP:
- Tránh spam OTP (tốn phí SMS)
- Giới hạn N requests / giờ
- Managed bởi `GetOTPBlacklistController`

---

*Xem thêm: [auth.md](../03_API/auth.md) | [identity.md](./identity.md) | [ewallet.md](./ewallet.md)*
