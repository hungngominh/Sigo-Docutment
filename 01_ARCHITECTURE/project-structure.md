# Project Structure & DI Configuration

## Mục lục
- [Solution Structure](#solution-structure)
- [Project Dependencies](#project-dependencies)
- [Startup Pipeline](#startup-pipeline)
- [DI Registrations](#di-registrations)
- [External DLL Dependencies](#external-dll-dependencies)
- [FirstRun Initialization](#firstrun-initialization)

---

## Solution Structure

**Solution file:** `SOURCE/AllianceMiddleman_NetCore.sln`
**Framework:** .NET 5.0

### Core Platform (4 projects)

| Project | Assembly Name | Type | Vai trò |
|---------|--------------|------|---------|
| `AllianceMiddlemanWebAPI` | AllianceMiddlemanWebAPI | Web (Sdk.Web) | API gateway, host, controllers, Startup.cs |
| `AllianceMiddlemanWebAPI.Core` | AllianceMiddlemanWebAPI.Core | Library | Data access layer — DbContext, entities, EF mappings |
| `AllianceMiddlemanWebAPI.Service` | AllianceMiddlemanWebAPI.Shared | Library | Business logic — services, engines, helpers |
| `AllianceMiddlemanWebAPI.DataShared` | AllianceMiddlemanWebAPI.DataShared | Library | Shared models, DTOs, constants, enums |

> **Lưu ý:** Project `AllianceMiddlemanWebAPI.Service` có assembly name `AllianceMiddlemanWebAPI.Shared` (legacy naming).

### Feature Modules

Mỗi module là một solution con, cấu trúc giống nhau:

```
Ezy.Module.{Name}/
├── Ezy.Module.{Name}/           # Web API host (nếu deploy standalone)
├── Ezy.Module.{Name}.API/       # Controllers
├── Ezy.Module.{Name}.Core/      # DbContext, entities
├── Ezy.Module.{Name}.Shared/    # Services, business logic (assembly = .Shared)
└── Ezy.Module.{Name}.DataShared/# DTOs, models
```

| Module | Số project | Vai trò |
|--------|-----------|---------|
| **Ezy.Module.CMS** | 5 | Content Management — blog, topics, landing pages |
| **Ezy.Module.EWallet** | 5 | Ví điện tử — wallet, transactions, actions |
| **Ezy.Module.TrafficTicket** | 5 | Kiểm tra phạt nguội xe |
| **Ezy.Module.DynamicReport** | 5 | Báo cáo động, configurable templates |
| **Ezy.Module.Identity** | 4 | Identity utilities, helpers, caching |
| **Ezy.Module.Hotline** | 3 | Tổng đài OMI Call integration |
| **PgQuery** | 1 | PostgreSQL query utility |

**Tổng: 32 projects** trong solution.

### Project Reference Flow

```
AllianceMiddlemanWebAPI (Host)
├── references → AllianceMiddlemanWebAPI.Core
├── references → AllianceMiddlemanWebAPI.Service (Shared)
├── references → AllianceMiddlemanWebAPI.DataShared
├── references → Ezy.Module.CMS.API
├── references → Ezy.Module.EWallet.API
├── references → Ezy.Module.TrafficTicket.API
├── references → Ezy.Module.DynamicReport.API
├── references → Ezy.Module.Hotline.API
└── references → Ezy.Module.Identity

AllianceMiddlemanWebAPI.Service (Shared)
├── references → AllianceMiddlemanWebAPI.Core
├── references → AllianceMiddlemanWebAPI.DataShared
└── references → External DLLs (Ezy.APIService.*, Ezy.Module.*)

AllianceMiddlemanWebAPI.Core
├── references → AllianceMiddlemanWebAPI.DataShared
└── references → External DLLs (EF Core, Npgsql)
```

---

## Startup Pipeline

**File:** `AllianceMiddlemanWebAPI/Startup.cs`

### ConfigureServices() — DI Registration Order

```csharp
// 1. Cache service
services.AddCacheService();

// 2. Notification hosted service (singleton)
services.Configure<NotificationOptions>(config);
services.AddSingleton<INotificationService, NotificationService>();
services.AddHostedService(sp => sp.GetRequiredService<INotificationService>());

// 3. First run initialization
services.AddHostedService<FirstRunHostedService>();

// 4. Response compression (Gzip + Brotli)
services.AddResponseCompression(options => {
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = new[] { "application/json", "text/plain", "text/html" };
});

// 5. CORS
services.AddCors();

// 6. Authentication — JWT Bearer
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = JwtSecurityKey.GetTokenValidationParameters();
        options.Events = new JwtBearerEvents {
            OnAuthenticationFailed = JwtBearerEventHelper.OnAuthenticationFailed,
            OnTokenValidated = JwtBearerEventHelper.OnTokenValidated,
            OnMessageReceived = context => {
                // SignalR: lấy token từ query string
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/NotificationHub"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

// 7. Controllers + Newtonsoft JSON
services.AddControllers().AddNewtonsoftJson();

// 8. File upload size (unlimited)
services.Configure<FormOptions>(options => {
    options.MultipartBodyLengthLimit = long.MaxValue;
});

// 9. SignalR
services.AddConnections();
services.AddSignalR();
```

### Configure() — Middleware Pipeline Order

```csharp
// 1. Developer exception page (dev only)
app.UseDeveloperExceptionPage();

// 2. HTTPS redirect
app.UseHttpsRedirection();

// 3. Static files (lần 1 — /Views/Home)
app.SetupUseStaticFiles();

// 4. Response compression
app.UseResponseCompression();

// 5. Routing
app.UseRouting();

// 6. CORS — Allow all
app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true)
    .AllowCredentials());

// 7. Authentication
app.UseAuthentication();

// 8. Authorization
app.UseAuthorization();

// 9. Forwarded headers (reverse proxy)
app.UseForwardedHeaders();

// 10. Endpoints
app.UseEndpoints(endpoints => {
    endpoints.MapControllers();
    endpoints.MapGet("/deployment", /* assembly metadata */);
    endpoints.MapHub<NotificationHub>("/NotificationHub");
});

// 11. Static files (lần 2)
app.UseStaticFiles();
```

### Health Check Endpoints

| Endpoint | Response | Mô tả |
|----------|----------|-------|
| `GET /health/ready` | 200 "READY" hoặc 503 "NOT READY" | Kiểm tra `AppReadiness.IsReady` |
| `GET /health/live` | 200 "ALIVE" | Always returns OK |
| `GET /deployment` | JSON assembly metadata + commit hash | Build info |

---

## DI Registrations

### Hosted Services

| Service | Lifetime | Mô tả |
|---------|----------|-------|
| `NotificationService` | Singleton | System.Threading.Channels queue + workers |
| `FirstRunHostedService` | Hosted | One-time startup initialization |

### Framework Registrations (via ProjectFrameWorkManagement)

Đăng ký services qua `EzyAPIStartupService.StartupService()` — tự động scan và register tất cả services implement `ISQLFrameWork`:

```csharp
// Pattern: Service implements ISQLFrameWork → auto-registered
public class ESCEmailTemplateService : EFBaseCategoryService<...>, IEmailTemplateService, ISQLFrameWork
```

**Registration pattern cho các service:**
- Tất cả services kế thừa từ `EFBaseCategoryService<T,...>` hoặc implement `ISQLFrameWork`
- Framework tự resolve qua `CreateServiceInstance<TService>()` trong engines
- Services KHÔNG dùng standard DI container, mà dùng factory pattern qua framework

### Cached Data (Singleton-like via static)

```csharp
CachedDataManagement.RefreshCacheAll()
// Load tất cả config tables vào static dictionaries
// 70+ entity types được cache: ConfigAddress, ConfigBank, ConfigDeliveryFee, ...
```

Xem chi tiết: [config-keys.md](../06_OPERATIONS/config-keys.md)

---

## External DLL Dependencies

### Core Ezy Framework

| DLL | Vai trò |
|-----|---------|
| `Ezy.APIService.Core.dll` | Core API services, startup registration |
| `Ezy.APIService.CoreUtilities.dll` | Utility functions |
| `Ezy.APIService.Shared.dll` | Shared contracts, interfaces |
| `Ezy.APIService.SharedController.dll` | Base controller classes |
| `Ezy.APIService.AuthShared.dll` | JWT token validation, security key management |
| `Ezy.Module.Library.dll` | Common library (Ezy.Module.Library.Utilities) |
| `Ezy.Module.MSSQLRepository.dll` | Database connection management (`EzyEFConnectionSettingItem`) |

### Base Infrastructure

| DLL | Vai trò |
|-----|---------|
| `Ezy.Module.BaseCache.dll` | Caching base classes |
| `Ezy.Module.BaseData.dll` | Data base classes (`EzyBaseParamModel`, `EzyBasePagingParamModel`) |
| `Ezy.Module.BaseMSSQLData.dll` | MSSQL data access |
| `Ezy.Module.BaseService.dll` | Base service classes (`EFBaseCategoryService`) |
| `Ezy.Module.Controller.dll` | Base controllers (`BaseCategoryController`) |
| `Ezy.Module.DataShared.dll` | Shared data models |
| `Ezy.Module.Engine.dll` | Background engine framework (`EzyEngineEntityAsync`) |
| `Ezy.Module.Infrastructure.dll` | Infrastructure utilities |
| `Ezy.Module.RedisCaching.dll` | Redis caching layer |

### Service-Specific

| DLL | Vai trò |
|-----|---------|
| `Ezy.APIService.AppSystemAPI/Core/Service.dll` | App system management |
| `Ezy.APIService.LogChange.dll` | Audit trail, change logging |
| `Ezy.APIService.LogService.dll` | Application logging |
| `Ezy.APIService.EmailMessage.dll` | Email service |
| `Ezy.APIService.ExcelHelper.dll` | Excel export (EPPlus) |
| `Ezy.ApiService.NotifyCore/API/Service.dll` | Notification framework |
| `Ezy.ProjectAPIMgmt.dll` | API management framework |

### Third-Party

| DLL | Version | Vai trò |
|-----|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 5.x | PostgreSQL EF Core provider |
| `Z.EntityFramework.Extensions.EFCore.dll` | - | Bulk operations (BulkInsert, BulkUpdate) |
| `System.Linq.Dynamic.Core.dll` | - | Dynamic LINQ queries |
| `RestSharp.dll` | - | HTTP client cho external APIs |
| `Renci.SshNet.dll` | - | SFTP client |
| `FirebaseAdmin.dll` | - | Firebase Cloud Messaging |
| `Google.Apis.Auth.dll` | - | Google OAuth2 |
| `Magick.NET-Q16-AnyCPU.dll` | - | Image processing |
| `EPPlus.dll` | - | Excel file generation |
| `HtmlAgilityPack.dll` | - | HTML parsing (CMS) |
| `Confluent.Kafka.dll` | - | Apache Kafka client |
| `Newtonsoft.Json` | - | JSON serialization |
| `StackExchange.Redis` | - | Redis client |
| `Nito.AsyncEx` | - | Async coordination primitives |

---

## FirstRun Initialization

`FirstRunHostedService.StartAsync()` chạy một lần khi app khởi động:

```csharp
// Thứ tự thực hiện:
1. EzyAPIStartupService.StartupService()
   // → Scan và register tất cả services implement ISQLFrameWork
   // → Initialize database connections

2. CachedDataManagement.RefreshCacheAll()
   // → Load 70+ config tables vào memory cache
   // → Build Dictionary<key, entity> cho fast lookup

3. EzyFA2AuthenticatorManager.Register()
   // → Setup 2FA authentication

4. ProjectEngineHelper.StartAllEngines(isAutoRun)
   // → Register và start tất cả background engines
   // → Mỗi engine có isAutoStartEngine flag

5. FormatHelper.InitCulture()
   // → Set culture = vi-VN
   // → Number format, date format conventions

6. ApplicationSettingInfo.Instance initialization
   // → Detect server OS (Windows/Linux)
   // → Set application-wide settings
```

### Engine Registration Pattern

```csharp
// ProjectEngineAsyncHelper.StartAllEngines()
private void PushNotificationEngine_Register() {
    var engine = new PushNotificationEngine();
    RegisterEngine(engine, isAutoStartEngine);
}

private void AutoWithdrawEngine_Register() {
    var engine = new AutoWithdrawEngine();
    RegisterEngine(engine, isAutoStartEngine);
}
// ... repeat cho tất cả 28+ engines
```

### Module Engine Startup

```csharp
// Mỗi module có engine startup riêng
ProjectEngineAsyncHelper.StartAllEngines(isStart);     // Main engines
ProjectEngineHelper.StartAllEngines(isStart);           // Sync engines
AutoNotification.Engines.StartAllEngines(isStart);      // Notification module
TrafficTicket.Engines.StartAllEngines(isStart);         // TrafficTicket module
```

**Control flag:** `IsDontRunEngine` — nếu true, engines register nhưng không auto-start (dùng cho dev/debug).

---

*Xem thêm: [overview.md](./overview.md) | [base-service-pattern.md](./base-service-pattern.md) | [background-engines.md](./background-engines.md)*
