# Config Keys — Cấu hình hệ thống đầy đủ

## Mục lục
- [appsettings.json Structure](#appsettingsjson-structure)
- [Database-driven Config (SystemConfigKeys)](#database-driven-config-systemconfigkeys)
- [Config Caching (CachedDataManagement)](#config-caching-cacheddatamanagement)
- [Config theo môi trường](#config-theo-môi-trường)

---

## appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "EPPlus": {
    "ExcelPackage": {
      "LicenseContext": "NonCommercial"
    }
  },
  "AllowedHosts": "*"
}
```

> **Lưu ý:** appsettings.json chứa rất ít config. Phần lớn config được lưu trong **database** (bảng SystemConfig) và truy xuất qua `SystemConfigHelper.GetValueFromConfig()`.

### Connection Strings

| Key | Mô tả | Ví dụ |
|-----|-------|-------|
| `DefaultConnection` | SQL Server chính | `Server=.;Database=Sigo;...` |
| `Redis` | Redis cache | `localhost:6379` |
| `PostgreSQL_AppSystem` | PostgreSQL — AppSystem DB | `Host=deb-postgresql.allianceitsc.com;Database=Sigo_Dev` |
| `PostgreSQL_Business` | PostgreSQL — Business DB | `Host=192.168.0.21;Database=Sigo_Dev` |
| `PostgreSQL_Category` | PostgreSQL — Category DB | Cùng host với AppSystem |
| `PostgreSQL_AppData` | PostgreSQL — AppData DB | Cùng host với AppSystem |

### JWT Settings

| Key | Mô tả | Ghi chú |
|-----|-------|---------|
| `JwtSettings:SecretKey` | Secret key ký JWT | Tối thiểu 32 ký tự |
| `JwtSettings:Issuer` | Issuer của token | Ví dụ: `sigo-api` |
| `JwtSettings:Audience` | Audience của token | Ví dụ: `sigo-app` |
| `JwtSettings:ExpiresInMinutes` | Thời gian hết hạn | Default: 60 |

### OAuth2 / Identity

| Key | Mô tả |
|-----|-------|
| `USER_AUTO_LOGIN_URL` | URL OAuth2 server (`http://localhost:12391`) |
| `Google:ClientId` | Google OAuth2 Client ID |
| `Google:ClientSecret` | Google OAuth2 Client Secret |

---

## Database-driven Config (SystemConfigKeys)

**File:** `AllianceMiddlemanWebAPI.DataShared/Common/SystemConfigKeys.cs`

Tất cả config lưu trong bảng `SystemConfig` (key-value pairs). Truy xuất qua:

```csharp
var value = SystemConfigHelper.GetValueFromConfig(SystemConfigKeys.KEY_NAME);
```

---

### APP_SETTING

**Model:** `GetSettingAppSettingModel` — `RentalServiceHelper.cs`
**Mục đích:** Cấu hình giao diện đặt xe — giờ mặc định, ngày mặc định, bảo mật xCheck.

```json
{
  "FromTimeAddHour": 1,
  "FromTimeMinHour": 0,
  "ToTimeAddHour": 2,
  "ToTimeMinHour": 20,
  "FromToDateSetDefault": false,
  "FromDateAddDay": 0,
  "ToDateAddDay": 3,
  "IsNeedXCheck_Website": false,
  "TimeXCheck_ExpireInMillisecond": 30000,
  "IsNeedXCheck_App": false,
  "MinRentalDurationByHour": null,
  "IsForceUpdateOSVersion": false
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `FromTimeAddHour` | int | Giờ bắt đầu mặc định = giờ hiện tại + N giờ |
| `FromTimeMinHour` | int | Phút mặc định cho giờ bắt đầu |
| `ToTimeAddHour` | int | Giờ trả xe mặc định = giờ hiện tại + N giờ |
| `ToTimeMinHour` | int | Phút mặc định cho giờ trả xe (20 = :20 phút) |
| `FromToDateSetDefault` | bool | Tự động điền ngày mặc định khi mở form đặt xe |
| `FromDateAddDay` | int | Ngày bắt đầu mặc định = hôm nay + N ngày |
| `ToDateAddDay` | int | Ngày trả xe mặc định = hôm nay + N ngày |
| `IsNeedXCheck_Website` | bool | Bật xác thực xCheck cho web (anti-bot) |
| `TimeXCheck_ExpireInMillisecond` | int | Thời gian hết hạn xCheck token (ms) |
| `IsNeedXCheck_App` | bool | Bật xCheck cho mobile app |
| `MinRentalDurationByHour` | double? | Số giờ thuê tối thiểu (null = không giới hạn) |
| `IsForceUpdateOSVersion` | bool | Bắt buộc cập nhật app khi có phiên bản mới |

---

### RENTAL_SERVICE_SETTING

**Model:** `RentalServiceSettingModel` — `RentalServiceHelper.cs`
**Mục đích:** Cấu hình tự động hoàn thành đơn hàng sau khi hết hạn chuyến đi.

```json
{
  "AutoCompleteOrder": {
    "CanAutoCompleteOrder": true,
    "AutoCompleteOrder_AfterMinutes": 60
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `CanAutoCompleteOrder` | bool | Bật/tắt tính năng auto-complete đơn |
| `AutoCompleteOrder_AfterMinutes` | int? | Số phút sau ToDate → tự chuyển INTHETRIP sang DONE (default: 60) |

> **Lưu ý:** Config này điều khiển `AutoCompleteOrderEngine`. Tắt `CanAutoCompleteOrder` → engine không xử lý, đơn phải hoàn thành thủ công.

---

### SEARCHING_VEHICLE_SETTING

**Model:** `SearchingVehicleSettingModel` — `SearchingVehicleHelper.cs`
**Mục đích:** Điều khiển thuật toán tìm xe — bán kính, số lượng kết quả, cache.

```json
{
  "Distance": 5,
  "MaxDistance": 30,
  "MaxVehicleCount": 20,
  "NeedReloadOption": false,
  "IgnoreReloadSearchingField": false,
  "RelatedVehicleMaximum": 10,
  "ShowOwnerInfomation": true
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `Distance` | int? | Bán kính tìm kiếm mặc định (km) khi user không chọn |
| `MaxDistance` | int? | Bán kính tối đa cho phép (km) — filter xe quá xa |
| `MaxVehicleCount` | int? | Số xe tối đa trả về trong 1 request search |
| `NeedReloadOption` | bool | Reload option filters (hãng xe, loại xe) mỗi request |
| `IgnoreReloadSearchingField` | bool | Bỏ qua reload filter fields — tăng performance |
| `RelatedVehicleMaximum` | int? | Số xe tương tự tối đa trong trang detail xe |
| `ShowOwnerInfomation` | bool | Hiển thị thông tin chủ xe trong kết quả tìm kiếm |

---

### USER_WALLET_SETTING

**Model:** `UserWalletSettingModel` — `WalletHelper.cs`
**Mục đích:** Quy tắc nạp/rút tiền ví điện tử.

```json
{
  "TopUpBankCode": "MBBANK",
  "TopUpTransferContent": "NAP TIEN - #Phone#",
  "AutoWithdrawBankCode": "MBBANK_WITHDRAW",
  "AutoWithdrawFee": 0,
  "WithdrawFee": 0,
  "MinMoneyCanWithdraw": 2000,
  "AutoWithdraw": false
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `TopUpBankCode` | string | Mã ngân hàng nhận tiền nạp (ConfigMBBank.Code) |
| `TopUpTransferContent` | string | Nội dung chuyển khoản nạp tiền — `#Phone#` được thay bằng SĐT user |
| `AutoWithdrawBankCode` | string | Mã ngân hàng dùng cho rút tiền tự động |
| `AutoWithdrawFee` | decimal? | Phí rút tiền tự động (VND) |
| `WithdrawFee` | decimal? | Phí rút tiền thủ công (VND) |
| `MinMoneyCanWithdraw` | decimal? | Số tiền tối thiểu mỗi lần rút (VND) — MB Bank yêu cầu ≥ 2,000đ |
| `AutoWithdraw` | bool | Bật rút tiền tự động khi chuyến kết thúc |

---

### Get_OTP_Setting

**Model:** `GetOTPSettingModel` — `AccountModel.cs`
**Mục đích:** Kiểm soát tần suất gửi OTP, chống spam theo IP và SĐT.

```json
{
  "IPSetting": {
    "CheckToAddInBlacklist": true,
    "MinutesToCheck": 30,
    "ContinuorsNumber": 5
  },
  "MobiPhoneSetting": {
    "CheckToAddInBlacklist": true,
    "MinutesToCheck": 30,
    "ContinuorsNumber": 5
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `CheckToAddInBlacklist` | bool | Tự động block IP/SĐT khi vượt giới hạn |
| `MinutesToCheck` | int | Khoảng thời gian theo dõi (phút) |
| `ContinuorsNumber` | int | Số lần OTP liên tiếp trước khi block |

> **Ví dụ:** `ContinuorsNumber=5, MinutesToCheck=30` → nếu 1 IP yêu cầu OTP 5 lần trong 30 phút → bị block.

---

### SYSTEM_QUICK_ORDER_REQUEST_SETTING

**Model:** `System_QuickOrderRequest_Setting` — `QuickOrderRequestModel.cs`
**Mục đích:** Điều khiển tính năng tìm xe thay thế tự động khi owner huỷ.

```json
{
  "SearchCar_NumOfCarNeedGet": 10,
  "Max_NumOfCarByOwner": 1,
  "AutoCreateFromOrderSetting": {
    "IsAutoTurnOn": true,
    "MaxQuickOrderByRenterPerDay": 100,
    "MaxOrderCanCreate": 3
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `SearchCar_NumOfCarNeedGet` | int | Số xe gợi ý tối đa trong mỗi quick order |
| `Max_NumOfCarByOwner` | int | Mỗi chủ xe chỉ xuất hiện tối đa N xe trong danh sách gợi ý |
| `IsAutoTurnOn` | bool | Tự động tạo QuickOrder khi owner huỷ đơn |
| `MaxQuickOrderByRenterPerDay` | long | Số QuickOrder tối đa 1 renter có thể nhận/ngày |
| `MaxOrderCanCreate` | long | Số QuickOrder tối đa được tạo cho 1 đơn bị huỷ |

---

### RENTAL_SERVICE_ADD_NEW_CAR_SETTING

**Model:** `RentalService_AddNewCar_SettingModel` — `RentalServiceModel.cs`
**Mục đích:** Kiểm soát yêu cầu thông tin khi chủ xe đăng ký xe mới.

```json
{
  "NeedCheckOwnerInfo_PermanentAddressAndTaxCode": true,
  "MsgError_OwnerInfo_PermanentAddressAndTaxCode_Empty": "Chủ xe cần bổ sung mã số thuế và địa chỉ thường trú trước khi đăng tải xe lên ứng dụng",
  "MsgError_UserInfo_PermanentAddressAndTaxCode_Empty": "Bạn cần bổ sung mã số thuế và địa chỉ thường trú trước khi đăng tải xe lên ứng dụng"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `NeedCheckOwnerInfo_PermanentAddressAndTaxCode` | bool | Bắt buộc chủ xe có địa chỉ thường trú + MST trước khi đăng xe |
| `MsgError_OwnerInfo_*` | string | Thông báo lỗi khi chủ xe thiếu thông tin (dùng khi owner khác tạo xe) |
| `MsgError_UserInfo_*` | string | Thông báo lỗi khi chính user đang đăng xe thiếu thông tin |

---

### GOOGLE_DISTANCE_SETTING

**Model:** `GGDistanceSettingModel` — `GGDistanceHelper.cs`
**Mục đích:** Google Maps API keys cho tính khoảng cách giao xe và place search.

```json
{
  "GGDistanceKey": "AIza...",
  "GGPlaceAndroidKey": "AIza...",
  "GGPlaceIOSKey": "AIza...",
  "GGPlaceAdminKey": "AIza...",
  "MaximumItemPerSearch": 25
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `GGDistanceKey` | string | API key cho Google Distance Matrix (tính km giữa 2 điểm) |
| `GGPlaceAndroidKey` | string | API key cho Google Places — Android app |
| `GGPlaceIOSKey` | string | API key cho Google Places — iOS app |
| `GGPlaceAdminKey` | string | API key cho Google Places — admin panel |
| `MaximumItemPerSearch` | int | Số điểm tối đa trong 1 Distance Matrix request (Google limit: 25) |

---

### DISTANCE_MATRIX_SETTING

**Model:** `DistanceSettingModel` — `DistanceMatrixHelper.cs`
**Mục đích:** Cấu hình service tính khoảng cách (dùng Openroute thay vì Google để tiết kiệm chi phí).

```json
{
  "UsingItem": "Openroute",
  "Openroute": {
    "APIUrl": "https://microservice.allianceitsc.com/api/v1/Distance/DrivingCar",
    "APIKey": ""
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `UsingItem` | string | Service đang dùng: `"Openroute"` hoặc `"Google"` |
| `Openroute.APIUrl` | string | URL microservice nội bộ wrap Openroute API |
| `Openroute.APIKey` | string | API key cho Openroute (nếu gọi trực tiếp) |

> **Lưu ý:** Hệ thống dùng microservice nội bộ `microservice.allianceitsc.com` làm proxy — không gọi thẳng Openroute.

---

### THIRD_PARTY_SETTING

**Model:** `ThirdPartySettingModel` — `ThirdPartyHelper.cs`
**Mục đích:** URL các microservice nội bộ (IP lookup, tra cứu biển số).

```json
{
  "IPLocationAPIUrl": "https://microservice.allianceitsc.com/api/v1/IPLocation/Get",
  "PlateInfoAPIUrl": "https://microservice.allianceitsc.com/api/v1/PlateInfo/Get"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `IPLocationAPIUrl` | string | API tra cứu vị trí từ IP (dùng cho search mặc định theo vị trí) |
| `PlateInfoAPIUrl` | string | API tra cứu thông tin xe từ biển số (xác minh khi đăng ký xe) |

---

### USER_ACCOUNT_SETTING

**Model:** `UserAccountSettingModel` — `UserAccountHelper.cs`
**Mục đích:** Cấu hình xác thực thông tin ngân hàng khi user đăng ký.

```json
{
  "NeedGetBankInfo": true,
  "UsingBankServiceCode": "MB"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `NeedGetBankInfo` | bool | Tự động tra cứu thông tin tài khoản ngân hàng khi user nhập STK |
| `UsingBankServiceCode` | string | Ngân hàng dùng để verify STK: `"MB"` = MB Bank VietQR lookup |

---

### VIET_QR_SETTING

**Model:** `VietQRSettingModel` — `QRCodeHelper.cs`
**Mục đích:** Thông tin tài khoản ngân hàng Sigo để generate QR nạp tiền.

```json
{
  "APIUrl": "https://api.vietqr.io/v2/generate",
  "AccountName": "CÔNG TY TNHH SIGO VIỆT NAM",
  "Template": "n2Poslv",
  "AccountNo": "399799799",
  "AcqId": "970422",
  "AddInfo": "",
  "VietQRHardCodeUrl": "https://img.vietqr.io/image/mbbank-399799799-szTwtIo.jpg?amount=#Money#&addInfo=#AddInfo#&accountName=CONG%20TY%20TNHH%20SIGO%20VIET%20NAM"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `APIUrl` | string | VietQR API endpoint để generate QR động |
| `AccountName` | string | Tên tài khoản nhận tiền |
| `Template` | string | Template ID của VietQR |
| `AccountNo` | string | Số tài khoản MB Bank của Sigo |
| `AcqId` | string | Mã BIN ngân hàng (`970422` = MB Bank) |
| `AddInfo` | string | Nội dung chuyển khoản mặc định |
| `VietQRHardCodeUrl` | string | URL ảnh QR cố định — `#Money#` và `#AddInfo#` được thay khi dùng |

---

### SYSTEM_QRCODE_TRANSFER_SETTING

**Model:** `QRCodeTransferSettingModel` — `QRCodeHelper.cs`
**Mục đích:** Template QR code theo chuẩn EMVCo (dùng khi generate QR tĩnh).

```json
{
  "BeneficiaryBank": "00020101021238570010A00000072701270006970436011307210005259090208QRIBFTTA",
  "CountryCode": "5802VN",
  "CRC": "6304",
  "Default1": "62",
  "Default2": "08"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `BeneficiaryBank` | string | Data EMVCo của ngân hàng thụ hưởng |
| `CountryCode` | string | Mã quốc gia theo EMVCo (`5802VN` = Việt Nam) |
| `CRC` | string | Tag CRC theo chuẩn EMVCo |
| `Default1`, `Default2` | string | Các tag EMVCo mặc định |

---

### BANK_SMS_DATA_SETTING

**Model:** `BankSMSDataSettingModel` — `GetBankSMSDataHelper.cs`
**Mục đích:** Parse SMS banking để tự động ghi nhận nạp tiền qua chuyển khoản.

```json
{
  "AllowSenders": ["MBBANK"],
  "Money_Diff_Transfer_With_Order": 10001,
  "Money_Diff_Transfer_With_Order_Suggest": 20001,
  "Check_Duplicate_Cus_Mapping": false,
  "Len_Of_Key_Has_Valid": 10,
  "CustomerOfficialSuggest": {
    "Keywords_Exclude": ["HOAN COC"]
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `AllowSenders` | string[] | Danh sách sender SMS được xử lý (chỉ nhận từ MB Bank) |
| `Money_Diff_Transfer_With_Order` | decimal? | Chênh lệch tối đa (VND) để tự động khớp giao dịch với đơn hàng |
| `Money_Diff_Transfer_With_Order_Suggest` | decimal? | Chênh lệch tối đa để gợi ý khớp (không tự động) |
| `Check_Duplicate_Cus_Mapping` | bool | Kiểm tra giao dịch trùng lặp khi map với customer |
| `Len_Of_Key_Has_Valid` | int | Độ dài tối thiểu của key giao dịch để coi là hợp lệ |
| `Keywords_Exclude` | string[] | Từ khoá trong nội dung CK bị loại khỏi auto-map (ví dụ: "HOAN COC" = hoàn cọc, không phải nạp tiền) |

---

### PUSH_NOTIFICATION_FCM_SETTING

**Model:** `PushNotificationSettingModel` — `PushNotificationHelper.cs`
**Mục đích:** Cấu hình Firebase Cloud Messaging để gửi push notification.

```json
{
  "ServerAPIKey": "AAAA94Cn4FE:APA91bH...",
  "ServerSenderId": "1063015407697",
  "AppTitle": "SIGO",
  "PathFirebaseJson": "/app/firebase-service-account.json",
  "PushNotificationMessageUrl": "https://fcm.googleapis.com/v1/projects/{project}/messages:send",
  "TTL": 4500,
  "Priority": "high",
  "SettingExts": []
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `ServerAPIKey` | string | FCM Legacy API key (cho FCM v1 legacy mode) |
| `ServerSenderId` | string | Firebase Project Number |
| `AppTitle` | string | Tên app hiển thị trong push notification |
| `PathFirebaseJson` | string | Đường dẫn file service account JSON (FCM v1 HTTP API) |
| `PushNotificationMessageUrl` | string | FCM API endpoint |
| `TTL` | int | Time-to-live của notification (giây) — sau TTL thiết bị offline sẽ không nhận |
| `Priority` | string | `"high"` = wake device ngay; `"normal"` = gửi khi tiện |
| `SettingExts` | array | Cấu hình FCM cho các app khác (multi-app) |

---

### IMAGE_COMPRESSION_SETTING

**Model:** `ImageCompressionSettingModel` — `ImageCompressHelper.cs`
**Mục đích:** Cấu hình service nén ảnh nội bộ khi upload xe/người dùng.

```json
{
  "APIUrl": "https://compressimage-api.allianceitsc.com",
  "ResizeAndCompress": "api/v1/ResizeAndCompress",
  "Compress": "api/v1/Compress",
  "GetImageGuids": "api/v1/GetImageGuids",
  "AuthKey": "jfpV2nsJ0UqW7Ksc...",
  "MaxReSize": 1200,
  "IsRunImageCompression": true,
  "MaxRetryGetCompress": 20,
  "MaxRetryWhenFailed": 3
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| `APIUrl` | string | Base URL của image compression microservice nội bộ |
| `ResizeAndCompress` | string | Endpoint resize + nén ảnh |
| `Compress` | string | Endpoint chỉ nén (không resize) |
| `GetImageGuids` | string | Endpoint lấy danh sách GUID ảnh đã nén |
| `AuthKey` | string | API key xác thực với service nén ảnh |
| `MaxReSize` | int | Kích thước tối đa chiều dài/rộng (px) sau resize |
| `IsRunImageCompression` | bool | Bật/tắt tính năng nén ảnh — tắt để debug |
| `MaxRetryGetCompress` | int | Số lần retry khi polling kết quả nén |
| `MaxRetryWhenFailed` | int | Số lần retry khi gọi API nén thất bại |

---

### FIRE_BASE_CLIENT

**Type:** `string[]` — `FirebaseHelper.cs`
**Mục đích:** URL Firebase Realtime Database — dùng cho real-time sync trạng thái đơn hàng.

```json
["https://sigo-d32ca-default-rtdb.firebaseio.com/"]
```

> Array để hỗ trợ multi-region Firebase nếu cần. Hiện tại chỉ dùng 1 instance.

---

### Simple String/Bool Keys

| Key | Type | Default | Mô tả |
|-----|------|---------|-------|
| `System_Security_Key_SignalR` | string | — | Security key cho SignalR hub — client phải gửi kèm khi connect |
| `GOOGLE_CLOUD_VISION_CREDENTIAL_PATH` | string | — | Đường dẫn file credentials JSON của Google Cloud Vision API (OCR biển số xe) |
| `ACCESS_REGISTER_URL` | string | — | URL trang đăng ký (redirect sau khi tạo tài khoản) |
| `SYSTEM_ACCESS_CODE` | string | — | Mã bảo mật dùng cho một số API nội bộ |
| `USERLOGIN_ALLOW_CREATE_PASSWORD` | bool | `true` | Cho phép user đặt password (tắt nếu chỉ dùng OTP) |
| `System_Format_Culture` | string | `"vi-VN"` | Culture cho format số, ngày, tiền tệ |
| `URL_API_QRCODE_DISPLAY` | string | `"https://chart.googleapis.com/chart?chs=150x150&cht=qr&chl="` | URL Google Charts API để render QR |
| `RENTAL_SERVICE_CATEGORY_SETTING_FIELDS` | string | — | Danh sách field names (CSV) của category setting JSON |
| `RENTAL_SERVICE_RUN_SUGGEST_VEHICLE` | bool | `true` | Bật engine gợi ý xe tương tự (`RentalService_SuggestVehicleEngine`) |
| `WEBSITE_GLOBAL_APP_SETTING` | JSON | `{}` | Config cho website — key-value tự do, đọc bởi frontend |

---

## Config Caching (CachedDataManagement)

### Architecture

```csharp
// Singleton static class — tất cả config được cache trong memory
public partial class CachedDataManagement
{
    // Refresh tất cả cache khi startup
    public static void RefreshCacheAll();

    // Hook để auto-refresh khi SystemConfig thay đổi
    public static void SystemConfigs_Refresh_AddAction();
}
```

### Pattern cho mỗi cached entity

```csharp
public partial class CachedDataManagement
{
    public static void ConfigDeliveryFees_Refresh();                              // Reload từ DB
    public static ConfigDeliveryFeeInfo[] ConfigDeliveryFees { get; }            // All items
    public static Dictionary<string, ConfigDeliveryFeeInfo> ConfigDeliveryFee_Dic { get; } // by ID
    public static ConfigDeliveryFeeInfo ConfigDeliveryFee_Get_Instance_Id(object sId);     // single by ID
    public static Dictionary<string, ConfigDeliveryFeeInfo> ConfigDeliveryFee_DicCode { get; } // by Code
    public static ConfigDeliveryFeeInfo ConfigDeliveryFee_Get_Instance_Code(object sId);   // single by Code
    public static ConfigDeliveryFeeInfo[] ConfigDeliveryFees_NoDisables { get; } // active only
}
```

### Cached Entity Types (70+)

**Category Config:**
ConfigAddress, ConfigAppVersion, ConfigBank, ConfigBeneficiaryBank, ConfigCompletionFee, ConfigCountry, ConfigCriteriaPriority, ConfigDeliveryFee, ConfigDiscountCodeGroup, ConfigDiscountMethod, ConfigHMACKey, ConfigLandingPage, ConfigLandingPage_File, ConfigLandingPageSetting, ConfigMBBank, ConfigMISAInvoice, ConfigMyOrderGroup, ConfigOrderStatus, ConfigPaymentType, ConfigRentalRequiredItem, ConfigRentalServiceCategory, ConfigRentalServiceRatingComment, ConfigRentalServiceRatingPoint, ConfigReportOrderReason, ConfigReportUserReason, ConfigRSAKey, ConfigSFTP, ConfigServiceCategory_Feature_Mapping, ConfigServiceDocumentType, ConfigServiceImageType, ConfigServiceItemFeature, ConfigSlide, ConfigSlide_File, ConfigSlideScreenMapping, ConfigUserImageType, ConfigUserImageTypeDetail, ConfigUserVerified, ConfigVehicleColor, ConfigVehicleDiscountSuggestion, ConfigVehicleFuelType, ConfigVehicleInsuranceCompany, ConfigVehicleMake, ConfigVehicleModel, ConfigVehicleMultidayRentalDiscount, ConfigVehicleNoOfSeat, ConfigVehicleRentalCancelReason, ConfigVehicleSegment, ConfigVehicleTransmissionType, ConfigVehicleType, ConfigVehicleTypeSub

**Business Data:**
DiscountCode, GetOTPBlacklist, Mioto_VehicleOwner, Mioto_VehicleQuery, NotificationSupportTemplate, NotificationTemplate, RentalServiceItem, RentalServiceItem_Calculating, RentalServiceItemPriorityList, ServiceCategory_DocumentType_Mapping, ServiceCategory_ImageType_InUseMapping, ServiceCategory_ImageType_Mapping, ServiceItem_DateBusyRentalSchedule, ServiceItem_WeekdayRentalPrice, ServiceItem_WeekdaysBusyRentalSchedule, UserDevice, UserLogin_BeneficiaryBank_Mapping, UserLogin_Ext, UserLogin_TaxInfo, UserUseDevice, Vehicle_InsuranceInformation, VietQR_Bank

### Cache Refresh Strategy

- **Startup:** `RefreshCacheAll()` load tất cả vào memory
- **On-demand:** Gọi `{Entity}_Refresh()` khi data thay đổi qua API
- **SystemConfig hook:** `SystemConfigs_Refresh_AddAction()` đăng ký auto-refresh
- **No TTL:** Cache không tự expire — chỉ refresh khi được gọi explicitly
- **Thread-safe:** Các `_Dic` properties sử dụng thread-safe dictionary patterns

---

## Config theo môi trường

| File | Môi trường |
|------|-----------|
| `appsettings.json` | Base config (minimal) |
| `appsettings.Development.json` | Local dev — logging verbose |
| `appsettings.Production.json` | Production |
| `appsettings.Staging.json` | Staging |

> **Quan trọng:** Connection strings và sensitive config KHÔNG nằm trong appsettings.json. Chúng được inject qua:
> - Environment variables
> - Database SystemConfig table
> - `EzyEFConnectionSettingItem` framework (connection management)

### Database Connection Management

```csharp
public static class EzyEFConnectionSettingItem
{
    // Dev mode: dùng hardcoded connection strings
    // Production: dùng encrypted config từ database/env vars
    public static string GetDataConnectionString_Postgres(
        Func<string> fGetConnectionString,
        bool isDevMode);
}
```

**Dev connection examples:**
- AppSystem: `Host=deb-postgresql.allianceitsc.com;Username=postgres;Password=...;Database=Sigo_Dev`
- Business: `Host=192.168.0.21;Username=postgres;Password=...;Database=Sigo_Dev`

---

*Xem thêm: [project-structure.md](../01_ARCHITECTURE/project-structure.md) | [setup-dev.md](./setup-dev.md)*
