# Module: Identity — Quản lý danh tính

> **Solution:** `Ezy.Module.Identity` | **Trạng thái:** Active (Core Infrastructure)

## Mục lục
- [Tổng quan](#tổng-quan)
- [Cấu trúc](#cấu-trúc)
- [Vai trò trong hệ thống](#vai-trò-trong-hệ-thống)
- [Thành phần chính](#thành-phần-chính)
- [Configuration Keys](#configuration-keys)
- [Tích hợp với Core API](#tích-hợp-với-core-api)

---

## Tổng quan

Module Identity là **nền tảng infrastructure** dùng chung cho toàn bộ hệ thống Ezy. Không có controller API riêng — đây là module cung cấp:

- **Utilities:** Helper functions cho string, datetime, SQL, expression
- **Caching infrastructure:** Quản lý cached data cho identity/auth
- **Permission system:** Định nghĩa các function keys và quyền truy cập
- **Audit logging:** Track property changes trong database
- **Shared constants:** Config keys, screen codes, text display keys

Module này được compile thành DLL và reference bởi `AllianceMiddlemanWebAPI` và các modules khác qua `Libs/`.

> **Lưu ý:** Authentication flow (JWT issue/validate) nằm ở OAuth2 server nội bộ (`localhost:12391`), không phải trong module này. Module Identity cung cấp infrastructure layer bên dưới.

---

## Cấu trúc

```
Ezy.Module.Identity/
│
├── Ezy.Module.Identity.Core/
│   ├── Data/
│   │   └── DataContextRegistrar.cs          # EF Core context registration
│   ├── DataCommon/
│   │   ├── CategoryScreenCodes.cs           # Screen/UI code constants
│   │   ├── CategoryScreenCodes_2.cs         # Extended screen codes
│   │   ├── Interfaces.cs                    # Core interfaces
│   │   ├── SystemFunctionKeys.cs            # Function key constants
│   │   └── SystemFunctionSettingParentTypes.cs  # Parent type definitions
│   ├── DataInfo/
│   │   └── Cached/
│   │       └── CachedDataManagement_Support.cs  # Cached data helpers
│   ├── Repository/
│   │   └── StoreRepository_Json.cs          # JSON stored procedure repository
│   ├── Services/
│   │   ├── IDataCachedService.cs            # Caching service interface
│   │   ├── SQLLogPropertyChangeHelper.cs    # DB property change logger
│   │   └── TextDisplayKeys.cs              # UI text keys
│   └── Utilities/
│       ├── EzyDateTimeHelper.cs            # DateTime utilities
│       ├── EzyExpressionHelper.cs          # LINQ expression helpers
│       ├── EzyStringHelper.cs              # String utilities
│       ├── IOHelper.cs                     # File I/O helpers
│       └── SQLQueryHelper.cs              # SQL query builder helpers
│
├── Ezy.Module.Identity.DataShared/
│   ├── Common/
│   │   ├── CommonContants.cs              # Shared constants
│   │   ├── ConfigSimpleTypes.cs           # Simple config type definitions
│   │   ├── FunctionPermissionKeys.cs      # Permission key constants
│   │   ├── SystemConfigKeys.cs            # appsettings key constants
│   │   └── SystemTextDisplaySettingKeys.cs # Text display setting keys
│   └── Services/Base/
│       ├── ESCReturnResult.cs             # Standard return result type
│       ├── JHLBaseModel.cs               # Base model class
│       └── ModelInterfaces.cs            # Model interface definitions
│
└── Ezy.Module.Identity.Shared/
    ├── Helper/
    │   ├── DebXmlSerializeHelper.cs       # XML serialization debug helper
    │   ├── FormatHelper.cs               # Data formatting utilities
    │   ├── GeneralHelper.cs              # General purpose helpers
    │   ├── GoogleChatHelper.cs           # Google Chat notification helper
    │   ├── SQLDataContextHelper.cs       # SQL context helper
    │   └── UIDataHelper.cs              # UI data processing helper
    └── Services/IdentityAddFile/
        ├── ITrafficTicket_BatchJob.cs    # Batch job interface
        └── TrafficTicket_BatchJob.cs     # TrafficTicket batch implementation
```

---

## Vai trò trong hệ thống

```
AllianceMiddlemanWebAPI
    │
    ├── references → Ezy.Module.Identity.dll
    │                   │
    │                   ├── SystemConfigKeys    → đọc appsettings keys
    │                   ├── FunctionPermissionKeys → kiểm tra quyền
    │                   ├── EzyStringHelper     → xử lý string
    │                   ├── EzyDateTimeHelper   → xử lý datetime
    │                   ├── SQLLogPropertyChangeHelper → audit log
    │                   └── IDataCachedService  → cached data
    │
    ├── references → Ezy.Module.EWallet.dll (cũng dùng Identity)
    ├── references → Ezy.Module.CMS.dll (cũng dùng Identity)
    └── references → Ezy.Module.DynamicReport.dll (cũng dùng Identity)
```

---

## Thành phần chính

### Utilities

#### `EzyStringHelper`
Helper xử lý string thường dùng trong toàn hệ thống:
- Normalize phone number (chuẩn hoá SĐT Việt Nam)
- Slug generation (cho SEO URL)
- Sanitize input
- String comparison utilities

#### `EzyDateTimeHelper`
Helper xử lý datetime theo timezone Việt Nam (UTC+7):
- Parse/format datetime theo chuẩn VN
- Calculate date ranges (ngày bận lịch)
- Timezone conversion

#### `EzyExpressionHelper`
Helper tạo dynamic LINQ expressions:
- Build Where predicates từ filter params
- Dynamic ordering
- Dùng trong generic repository pattern

#### `SQLQueryHelper`
Helper build SQL queries:
- Pagination (OFFSET/FETCH)
- Dynamic WHERE clauses
- Sort order generation

---

### Caching Infrastructure

#### `IDataCachedService`
Interface cho cached data management:
- Cache các config/lookup data thường dùng
- TTL management
- Cache invalidation

#### `CachedDataManagement_Support`
Implementation helpers cho cache management:
- Cache update patterns
- Stale data detection

---

### Audit & Permission

#### `SQLLogPropertyChangeHelper`
Track và ghi log thay đổi property trong database:
- So sánh before/after values
- Ghi vào `SystemBusinessChangeTracking` (AppSystem)
- Dùng cho compliance và debug

#### `SystemFunctionKeys`
Constants định nghĩa các function trong hệ thống:
```csharp
// Ví dụ (tên thực tế trong code)
public static class SystemFunctionKeys
{
    public const string WALLET_TOPUP = "wallet.topup";
    public const string WALLET_WITHDRAW = "wallet.withdraw";
    public const string ORDER_CONFIRM = "order.confirm";
    // ...
}
```

#### `FunctionPermissionKeys`
Constants cho role-based access control:
- Admin permissions
- Staff permissions
- Owner permissions
- User permissions

---

### Return Result

#### `ESCReturnResult`
Standard return type dùng trong service layer:
```csharp
public class ESCReturnResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
    public string ErrorCode { get; set; }
}
```

---

## Configuration Keys

`SystemConfigKeys.cs` định nghĩa tất cả keys đọc từ `appsettings.json`:

| Key Constant | appsettings path | Mục đích |
|-------------|-----------------|---------|
| `JWT_SECRET_KEY` | `JwtSettings:SecretKey` | JWT signing key |
| `JWT_ISSUER` | `JwtSettings:Issuer` | JWT issuer |
| `JWT_AUDIENCE` | `JwtSettings:Audience` | JWT audience |
| `OAUTH2_URL` | `USER_AUTO_LOGIN_URL` | OAuth2 server URL |
| `REDIS_CONNECTION` | `ConnectionStrings:Redis` | Redis connection |
| `DB_CONNECTION` | `ConnectionStrings:DefaultConnection` | SQL Server connection |
| `GOOGLE_CLIENT_ID` | `Google:ClientId` | Google OAuth client ID |
| `GOOGLE_CLIENT_SECRET` | `Google:ClientSecret` | Google OAuth secret |

> Xem đầy đủ tại: [../06_OPERATIONS/config-keys.md](../06_OPERATIONS/config-keys.md)

---

## Tích hợp với Core API

Module Identity được tích hợp vào Core API qua compiled DLL trong `SOURCE/Libs/`:

```
SOURCE/Libs/
├── Ezy.Module.Identity.dll
├── Ezy.Module.Identity.Core.dll
├── Ezy.Module.Identity.DataShared.dll
└── Ezy.Module.Identity.Shared.dll
```

**Đăng ký trong Startup.cs:**
```csharp
services.AddIdentityModule(configuration);
// → Đăng ký IDataCachedService, SQLLogPropertyChangeHelper,
//   và các utilities cần thiết
```

---

*Xem thêm: [auth.md](../03_API/auth.md) | [ADR-0004](../07_ADR/0004-jwt-authentication.md) | [ADR-0010](../07_ADR/0010-modular-design.md)*
