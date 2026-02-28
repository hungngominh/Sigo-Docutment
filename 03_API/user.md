# API: User

> **Controllers:** `UserController` (v1 + v2), `UserUploadController`, `UserItem_ImageController`, `UserUseDeviceController`
> **Base:** `/api/v1/User`, `/api/v2/User`

## Mục lục
- [Home / App config](#home--app-config)
- [Profile](#profile)
- [Wallet](#wallet)
- [Settings](#settings)
- [Upload ảnh](#upload-ảnh)
- [Xác minh tài khoản](#xác-minh-tài-khoản)
- [Các endpoint khác](#các-endpoint-khác)

---

## Home / App config

### `POST /api/v2/User/HomePage_App`

**Mô tả:** Lấy dữ liệu trang chủ app (banner, xe gợi ý, thống kê, config). Gọi khi mở app.

**Phân quyền:** `[AllowAnonymous]` — nếu có token sẽ trả thêm dữ liệu cá nhân hoá

**Thống kê:** 11,415 lượt/30 ngày | avg 40.57ms

**Request Body:** `UserParamModel`
```json
{
  "AppVersion": "2.5.0",
  "Latitude": 21.0278,
  "Longitude": 105.8342
}
```

### Response — `EzyResultObject<HomePageModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Slides": [
      { "ImageUrl": "https://...", "LinkUrl": "https://...", "Title": "Ưu đãi tháng 3" }
    ],
    "PopularPlaces": [
      { "Name": "Hà Nội", "ImageUrl": "https://...", "Latitude": 21.0278, "Longitude": 105.8342 }
    ],
    "RecommendedCars": [],
    "AppConfig": {
      "MinAppVersion": "2.0.0",
      "ForceUpdate": false,
      "Hotline": "1900xxxx",
      "SupportChatEnabled": true
    }
  }
}
```

### `POST /api/v1/User/HomePage_Website`

**Mô tả:** Homepage data cho website (banner, landing page content). Response tương tự nhưng thêm SEO data.

### `POST /api/v1/User/HomePage_BecomeOwner`

**Mô tả:** Homepage cho trang "Trở thành chủ xe" — landing page marketing.

---

## Profile

### `POST /api/v2/User/MyProfile`

**Mô tả:** Lấy thông tin profile người dùng hiện tại.

**Phân quyền:** `[Authorize]`

**Thống kê:** 3,489 lượt/30 ngày | avg 353ms

### Response — `EzyResultObject<ProfileModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "UserLoginId": "user-guid",
    "FullName": "Nguyễn Văn A",
    "MobilePhone": "0901234567",
    "Email": "a@email.com",
    "AvatarUrl": "https://...",
    "DateOfBirth": "1990-01-15",
    "Gender": "Male",
    "Address": "Hà Nội",
    "IsVerified": true,
    "IsDriverLicenseVerified": true,
    "JoinedDate": "2024-01-01",
    "Rating": 4.8,
    "TotalTrips": 25,
    "ServedCount": 120,
    "CoinBalance": 500,
    "WalletBalance": "1.500.000đ",
    "BankAccount": {
      "BankName": "MB Bank",
      "AccountNumber": "****1234",
      "AccountName": "NGUYEN VAN A"
    },
    "VerificationStatus": {
      "IdentityCard": "Verified",
      "DriverLicense": "Verified",
      "Portrait": "Verified"
    }
  }
}
```

### `POST /api/v1/User/MyProfileSummary`

**Mô tả:** Profile rút gọn (tên, avatar, rating) — dùng cho header/navigation.

### `POST /api/v2/User/Profile`

**Mô tả:** Xem profile user khác (public info only).

### `POST /api/v1/User/UpdateProfile`

**Mô tả:** Cập nhật profile (full update).

**Phân quyền:** `[Authorize]`

**Request Body:** `ProfileModel` — gửi toàn bộ fields cần cập nhật

### `POST /api/v1/User/GetTabInfo`

**Mô tả:** Lấy thông tin tab trong trang profile (badges, counts).

---

## Wallet

### `POST /api/v1/User/MyWallet`

**Mô tả:** Lấy thông tin ví Sigo của user.

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<MyProfileWalletModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Balance": 1500000,
    "Balance_Text": "1.500.000đ",
    "WalletGuid": "wallet-guid",
    "BankAccount": {
      "BankName": "MB Bank",
      "AccountNumber": "0123456789",
      "AccountName": "NGUYEN VAN A"
    }
  }
}
```

### `POST /api/v1/User/TopUpPaymentWallet`

**Mô tả:** Nạp tiền vào ví Sigo.

**Phân quyền:** `[Authorize]`

### `POST /api/v1/User/WithdrawPaymentWallet`

**Mô tả:** Rút tiền từ ví Sigo về tài khoản ngân hàng.

**Phân quyền:** `[Authorize]`

### `POST /api/v1/User/UpdateBankAccount`

**Mô tả:** Cập nhật tài khoản ngân hàng thụ hưởng.

**Request Body:** `UserBankAccountModel`
```json
{
  "BankCode": "MB",
  "AccountNumber": "0123456789",
  "AccountName": "NGUYEN VAN A"
}
```

### `POST /api/v1/User/GetBankAccountName`

**Mô tả:** Tra cứu tên chủ tài khoản ngân hàng (qua VietQR lookup).

---

## Settings

### `POST /api/v1/User/GetSetting`

**Mô tả:** Lấy cấu hình/setting của user (notification preferences, language...).

**Phân quyền:** `[Authorize]`

**Thống kê:** 11,522 lượt/30 ngày | avg 55.92ms

### Response — `EzyResultObject<UserSettingModel>`

### `POST /api/v1/User/Options`

**Mô tả:** Lấy các tùy chọn hiển thị (dropdown data cho UI).

**Thống kê:** 7,589 lượt/30 ngày | avg 1.40ms

---

## Upload ảnh

### `POST /api/v1/UserUpload/UploadImage`

**Mô tả:** Upload ảnh hồ sơ (avatar, CMND/CCCD, GPLX). Gửi qua `multipart/form-data`.

**Phân quyền:** `[Authorize]`

**Content-Type:** `multipart/form-data`

**Request:** Form data với file attachment
```
POST /api/v1/UserUpload/UploadImage
Content-Type: multipart/form-data; boundary=---

---
Content-Disposition: form-data; name="file"; filename="cmnd_front.jpg"
Content-Type: image/jpeg

<binary data>
---
Content-Disposition: form-data; name="ImageType"

IDENTITY_CARD_FRONT
---
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| file | binary | Có | File ảnh (jpg/png) |
| ImageType | string | Có | Loại ảnh: `AVATAR`, `IDENTITY_CARD_FRONT`, `IDENTITY_CARD_BACK`, `DRIVER_LICENSE`, `PORTRAIT` |

### `POST /api/v1/UserUpload/ReUploadImage`

**Mô tả:** Upload lại ảnh đã bị reject (sau khi admin từ chối lần đầu).

### `POST /api/v1/User/ConfirmUpload`

**Mô tả:** Xác nhận hoàn tất upload ảnh → gửi cho admin duyệt.

### `POST /api/v1/User/ConfirmReUpload`

**Mô tả:** Xác nhận hoàn tất re-upload → gửi duyệt lại.

---

## Xác minh tài khoản

### `POST /api/v1/User/RequestVerifyCustomerAccount`

**Mô tả:** Yêu cầu xác minh danh tính (CMND/CCCD). Gọi sau khi đã upload đầy đủ ảnh.

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<RequestVerifyCustomerAccountModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Status": "PENDING",
    "Message": "Yêu cầu xác minh đã được gửi, vui lòng chờ duyệt"
  }
}
```

### `POST /api/v1/User/RequestVerifyDriverLicenseCustomerAccount`

**Mô tả:** Yêu cầu xác minh giấy phép lái xe.

### `POST /api/v1/User/CheckValidOwnerInfo_AddNewCar`

**Mô tả:** Kiểm tra owner đủ điều kiện để đăng xe mới không (đã verify, đủ thông tin).

### Response — `EzyResultObject<RentalService_AddNewCar_StatusInfo>`

---

## Các endpoint khác

### `POST /api/v1/User/List`

**Mô tả:** Danh sách users (admin).

**Phân quyền:** `[Authorize]`

### `POST /api/v1/User/ReportUser`

**Mô tả:** Báo cáo vi phạm từ user khác.

### `POST /api/v1/User/GetCoinReward`

**Mô tả:** Lấy thông tin coin/điểm thưởng.

### `POST /api/v1/User/UpdateUser_Calculating_OrderStatus`

**Mô tả:** Tính lại thống kê user (admin trigger).

### `POST /api/v1/UserItem_Image/List`

**Mô tả:** Danh sách ảnh hồ sơ của user.

### `POST /api/v1/UserItem_Image_ListView/List`

**Mô tả:** Danh sách ảnh dạng list view (admin).

---

## Homepage Variants (v2)

### `POST /api/v2/User/HomePage_App_New`

**Mô tả:** Homepage mới (UI redesign). Response tương tự `HomePage_App` nhưng layout khác.

**Phân quyền:** `[AllowAnonymous]`

### `POST /api/v2/User/HomePage_Website`

**Mô tả:** Homepage cho website. Thêm SEO metadata (title, description, canonical URL).

### `POST /api/v2/User/HomePage_BecomeOwner_Website`

**Mô tả:** Landing page "Trở thành chủ xe" cho website.

### `POST /api/v2/User/ConfirmUpload`

**Mô tả:** V2 variant của ConfirmUpload — tương tự v1 nhưng trả thêm upload progress status.

---

## User Image Verification (Admin)

### `POST /api/v1/UserItem_Image_Verified/List`

**Mô tả:** Danh sách ảnh user đã được admin xác minh (admin view).

**Phân quyền:** `[Authorize]`

---

## Error Responses

### Validation Errors

| Trường hợp | StatusCode | Message |
|-------------|-----------|---------|
| Token expired | -2 | Phiên đăng nhập hết hạn |
| Profile thiếu thông tin bắt buộc | 0 | Vui lòng điền đầy đủ thông tin |
| SĐT đã đăng ký | 0 | Số điện thoại đã được sử dụng |
| Upload file quá lớn | 0 | File vượt quá kích thước cho phép |
| ImageType không hợp lệ | 0 | Loại ảnh không được hỗ trợ |
| Chưa upload đủ ảnh để verify | 0 | Vui lòng upload đầy đủ ảnh trước khi gửi yêu cầu xác minh |
| BankCode không hợp lệ | 0 | Ngân hàng không được hỗ trợ |
| AccountNumber format sai | 0 | Số tài khoản không hợp lệ |
| Withdraw khi balance = 0 | 0 | Số dư ví không đủ để rút |
| Owner chưa đủ điều kiện đăng xe | 0 | Bạn cần hoàn tất xác minh danh tính trước khi đăng xe |

---

## Ghi chú kỹ thuật

- **Upload ảnh:** Dùng `multipart/form-data`, không phải JSON. Max file size phụ thuộc server config
- **ImageType values:** `AVATAR`, `IDENTITY_CARD_FRONT`, `IDENTITY_CARD_BACK`, `DRIVER_LICENSE`, `PORTRAIT`
- **Verify flow:** Upload ảnh → ConfirmUpload → Admin review → Approve/Reject → RequestVerify nếu cần re-upload
- **Wallet endpoints** ở UserController — không phải EWallet module. EWallet module quản lý backend, User controller expose cho mobile app
- **Date format:** Profile dates trả về ISO 8601 string, wallet amounts trả về decimal number

---

*Xem thêm: [auth.md](./auth.md) | [core-user.md](../02_MODULES/core-user.md) | [ewallet.md](../02_MODULES/ewallet.md)*
