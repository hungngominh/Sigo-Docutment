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
| `JwtSettings:Issuer` | Issuer của token | URL hoặc tên app phát hành token (ví dụ: `sigo-api`) |
| `JwtSettings:Audience` | Audience của token | URL hoặc tên app nhận token (ví dụ: `sigo-app`) |
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
**Class:** `SystemConfigKeys` extends `EzySystemConfigKeys`

Tất cả config lưu trong bảng `SystemConfig` (key-value pairs). Truy xuất qua:

```csharp
var value = SystemConfigHelper.GetValueFromConfig(SystemConfigKeys.KEY_NAME);
```

### Danh sách đầy đủ SystemConfigKeys

#### Security & Access

| Key | Mô tả | JSON Schema / Default |
|-----|-------|----------------------|
| `System_Security_Key_SignalR` | Security key cho SignalR hub authentication | `string` — dùng để validate connection |
| `SYSTEM_ACCESS_CODE` | System-wide access code | `string` |
| `ACCESS_REGISTER_URL` | URL cho user registration | `string` — URL |
| `USERLOGIN_ALLOW_CREATE_PASSWORD` | Cho phép user tạo password | `bool` — `true`/`false` |

#### System Settings

| Key | Mô tả | JSON Schema / Default |
|-----|-------|----------------------|
| `System_Format_Culture` | System culture/localization | `string` — `"vi-VN"` |
| `APP_SETTING` | General app settings | `JSON object` |
| `WEBSITE_GLOBAL_APP_SETTING` | Website global settings | `JSON object` |

#### OTP & Authentication

| Key | Mô tả | JSON Schema |
|-----|-------|-------------|
| `Get_OTP_Setting` | OTP configuration | ```json {"OTPLength": 6, "OTPExpireMinutes": 5, "MaxRetry": 5, "BlockMinutes": 30}``` |

#### Vehicle Search & Rental

| Key | Mô tả | JSON Schema |
|-----|-------|-------------|
| `SEARCHING_VEHICLE_SETTING` | Vehicle search configuration | ```json {"MaxDistanceKm": 50, "DefaultPageSize": 20, "EnableGeoSearch": true}``` |
| `RENTAL_SERVICE_SETTING` | Rental service global settings | ```json {"DepositPercent": 30, "PenaltyDepositPercent": 30, "CompletionFeePercentage": 20, "OwnerWithholdingTaxPercent": 0, "HourOwner2Confirm": 3, "HourCustomer2Deposit": 3, "BusinessHourStart": 7, "BusinessHourEnd": 21, "CanAutoCompleteOrder": true, "AutoCompleteOrder_AfterMinutes": 60, "CancelOrderSetting": {"FullRefundWithinMinutes": 15, "NoRefundGreaterThanDays": 7}}``` |
| `RENTAL_SERVICE_ADD_NEW_CAR_SETTING` | Settings khi thêm xe mới | ```json {"RequiredImages": 4, "RequireInsurance": true, "AutoApprove": false}``` |
| `RENTAL_SERVICE_CATEGORY_SETTING_FIELDS` | Category setting fields | ```json {"ServiceFee": 0, "ServiceFeePercentOnByDate": 0, "InsurancePercentPerDay": 0}``` |
| `RENTAL_SERVICE_RUN_SUGGEST_VEHICLE` | Vehicle suggestion engine config | `JSON object` |

#### Quick Order

| Key | Mô tả | JSON Schema |
|-----|-------|-------------|
| `SYSTEM_QUICK_ORDER_REQUEST_SETTING` | Quick order request config | ```json {"AutoCreate": true, "MaxSuggestions": 5, "TimeoutMinutes": 30}``` |

#### Payment & QR

| Key | Mô tả | JSON Schema |
|-----|-------|-------------|
| `URL_API_QRCODE_DISPLAY` | QR code display API URL | `string` — URL |
| `SYSTEM_QRCODE_TRANSFER_SETTING` | QR code transfer settings | `JSON object` |
| `VIET_QR_SETTING` | VietQR integration settings | ```json {"ApiUrl": "...", "ClientId": "...", "ApiKey": "..."}``` |
| `BANK_SMS_DATA_SETTING` | Bank SMS parsing config | ```json {"Patterns": [...], "BankCodes": [...]}``` |
| `USER_WALLET_SETTING` | Wallet limits & rules | ```json {"DailyLimit": 60000000, "TransferUpperLimit": 20000000, "TransferLowerLimit": 10000, "BalanceUpperLimit": 100000000}``` |

#### Notification

| Key | Mô tả | JSON Schema |
|-----|-------|-------------|
| `PUSH_NOTIFICATION_FCM_SETTING` | FCM configuration | ```json {"ServerAPIKey": "AAAA94...", "ServerSenderId": "1063015407697", "AppTitle": "SIGO", "PathFirebaseJson": "/path/to/firebase.json", "PushNotificationMessageUrl": "https://fcm.googleapis.com/...", "TTL": 4500, "Priority": "high"}``` |
| `FIRE_BASE_CLIENT` | Firebase client config | `JSON object` — client-side Firebase config |

#### External Services

| Key | Mô tả | JSON Schema |
|-----|-------|-------------|
| `GOOGLE_CLOUD_VISION_CREDENTIAL_PATH` | Google Cloud Vision API credentials | `string` — file path |
| `GOOGLE_DISTANCE_SETTING` | Google Distance Matrix API | ```json {"ApiKey": "...", "MaxElementsPerRequest": 25}``` |
| `DISTANCE_MATRIX_SETTING` | Distance matrix calculation | `JSON object` |
| `THIRD_PARTY_SETTING` | Third-party integration settings | `JSON object` |
| `USER_ACCOUNT_SETTING` | User account config | `JSON object` |

#### Image Processing

| Key | Mô tả | JSON Schema |
|-----|-------|-------------|
| `IMAGE_COMPRESSION_SETTING` | Image compression config | ```json {"APIUrl": "https://compressimage-api.allianceitsc.com", "ResizeAndCompress": "api/v1/ResizeAndCompress", "Compress": "api/v1/Compress", "GetImageGuids": "api/v1/GetImageGuids", "AuthKey": "...", "MaxReSize": 1200, "IsRunImageCompression": true, "MaxRetryGetCompress": 20, "MaxRetryWhenFailed": 3}``` |

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
// Mỗi entity type tạo 1 partial class với pattern:
public partial class CachedDataManagement
{
    // Refresh cache cho entity này
    public static void ConfigDeliveryFees_Refresh();

    // Get all items
    public static ConfigDeliveryFeeInfo[] ConfigDeliveryFees { get; }

    // Dictionary lookup by ID
    public static Dictionary<string, ConfigDeliveryFeeInfo> ConfigDeliveryFee_Dic { get; }

    // Get single item by ID
    public static ConfigDeliveryFeeInfo ConfigDeliveryFee_Get_Instance_Id(object sId);

    // Dictionary lookup by Code (optional)
    public static Dictionary<string, ConfigDeliveryFeeInfo> ConfigDeliveryFee_DicCode { get; }

    // Get single item by Code (optional)
    public static ConfigDeliveryFeeInfo ConfigDeliveryFee_Get_Instance_Code(object sId);

    // Filtered collection (optional)
    public static ConfigDeliveryFeeInfo[] ConfigDeliveryFees_NoDisables { get; }
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
// Connection strings được quản lý qua framework:
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
