# ADR-0010: Thiết kế Module độc lập (EWallet, Identity, CMS, DynamicReport...)

## Status

Accepted

## Context

Sigo platform có các domain nghiệp vụ có thể tái sử dụng hoặc deploy độc lập:
- **E-Wallet:** Quản lý ví điện tử, giao dịch, rút tiền — có thể dùng cho nhiều app khác
- **Identity:** Xác thực, quản lý user — thường được tái sử dụng
- **CMS:** Quản lý nội dung — dùng chung nhiều platform
- **Dynamic Report:** Tạo báo cáo động — có thể tái sử dụng
- **Traffic Ticket:** Phạt nguội — vertical-specific feature

Nếu tất cả features đều nằm trong monolith duy nhất, sẽ khó:
- Tái sử dụng cho các project khác
- Team làm việc song song
- Deploy/update một feature mà không ảnh hưởng features khác

## Decision Drivers

* **Reusability** — EWallet và Identity là generic modules
* **Independent deployment** — update module không cần redeploy toàn bộ app
* **Team isolation** — team khác nhau có thể làm việc trên module khác nhau
* **Separation of concerns** — mỗi module có data store, business logic, API riêng
* **Gradual extraction** — bắt đầu từ monolith, extract dần từng module

## Considered Options

### Option 1: Module-per-Solution (hiện tại)
Mỗi module là một Visual Studio Solution riêng:
```
SOURCE/
├── AllianceMiddleman_NetCore.sln    # Core API
├── Ezy.Module.EWallet/              # E-Wallet solution
├── Ezy.Module.Identity/             # Identity solution
├── Ezy.Module.CMS/                  # CMS solution
├── Ezy.Module.DynamicReport/        # Report solution
├── Ezy.Module.TrafficTicket/        # Traffic solution
└── Ezy.Module.Hotline/              # Hotline solution
```
- **Pros:** Codebase tách biệt, independent build, dễ reuse
- **Cons:** Compiled DLLs trong Libs/ gây khó khăn debug, versioning

### Option 2: Feature Folders trong Monolith
```
AllianceMiddlemanWebAPI/
├── Features/
│   ├── Wallet/
│   ├── Identity/
│   ├── CMS/
│   └── Reports/
```
- **Pros:** Đơn giản, dễ share code
- **Cons:** Khó tái sử dụng cho project khác, không thể deploy riêng

### Option 3: Full Microservices
- **Pros:** Hoàn toàn độc lập, scale riêng từng service
- **Cons:** Operational complexity rất cao, distributed transactions khó

### Option 4: NuGet Packages
Mỗi module publish như NuGet package
- **Pros:** Versioning rõ ràng, reuse dễ
- **Cons:** Overhead pipeline publish, cần private NuGet registry

## Decision

Chúng ta sẽ dùng **Module-per-Solution** approach, mỗi module là một .NET Solution độc lập. Các module được tích hợp vào Core API thông qua compiled DLLs trong thư mục `Libs/`.

## Rationale

1. **Reusability:** EWallet và Identity là generic, có thể integrate vào project khác chỉ cần copy DLLs
2. **Team independence:** Team A làm EWallet, Team B làm Core API, không conflict
3. **Build isolation:** Mỗi module build riêng, không cần build toàn bộ solution khi sửa module
4. **Gradual adoption:** Có thể extract thêm modules dần dần từ monolith
5. **Versioning:** DLL versioning theo file version

## Cấu trúc mỗi Module (4-layer pattern nhất quán)

```
Ezy.Module.EWallet/
├── Ezy.Module.EWallet/          # Module host + DI registration
├── Ezy.Module.EWallet.API/      # API endpoints (Controllers)
├── Ezy.Module.EWallet.Core/     # Data layer (EF Core, DbContext)
├── Ezy.Module.EWallet.Service/  # Business logic
├── Ezy.Module.EWallet.DataShared/  # DTOs
└── Script/                      # Database migration scripts
```

## Danh sách Modules

| Module | Mục đích | Độc lập |
|--------|---------|---------|
| `Ezy.Module.EWallet` | Ví điện tử, giao dịch, rút tiền | Có thể deploy riêng |
| `Ezy.Module.Identity` | Auth, user profile, device management | Có thể deploy riêng |
| `Ezy.Module.CMS` | Quản lý nội dung (banner, FAQ, news) | Có thể deploy riêng |
| `Ezy.Module.DynamicReport` | Tạo báo cáo động theo cấu hình | Có thể deploy riêng |
| `Ezy.Module.TrafficTicket` | Phạt nguội, vi phạm giao thông | Sigo-specific |
| `Ezy.Module.Hotline` | Hỗ trợ khách hàng qua hotline | Có thể deploy riêng |

## Integration Pattern

```
Core API (AllianceMiddlemanWebAPI)
    │
    ├── References → Ezy.Module.EWallet.dll (via Libs/)
    ├── References → Ezy.Module.Identity.dll
    ├── References → Ezy.Module.CMS.dll
    └── References → Ezy.Module.DynamicReport.dll

// Startup.cs - đăng ký module services
services.AddEWalletModule(configuration);
services.AddIdentityModule(configuration);
services.AddCMSModule(configuration);
```

## Consequences

### Tích cực
- EWallet và Identity có thể tái sử dụng cho các project Ezy khác
- Mỗi module có database scripts riêng (`Script/`)
- Team autonomy khi develop từng module
- Core API có thể chạy mà không cần source code modules

### Tiêu cực
- DLLs trong `Libs/` khó debug (không có source)
- Versioning DLL thủ công, dễ dùng sai version
- Cross-cutting concerns (logging, auth) cần được module theo dõi
- Circular dependency risks giữa modules

### Rủi ro
- DLL hell — version mismatch giữa các modules
- **Giảm thiểu:** Strict versioning, document DLL versions trong changelog
- Module coupling qua shared DTOs
- **Giảm thiểu:** DataShared project cho DTOs public, internal DTOs giữ trong module
- Breaking changes trong module DLL
- **Giảm thiểu:** Semantic versioning, deprecation notices trước khi breaking change

## Related Decisions

- ADR-0002: Layered Architecture (mỗi module follow cùng pattern)
- ADR-0007: Kafka (integration events giữa modules)
- ADR-0010 → ADR-0015: MB Bank (module tích hợp tài chính)

---

*Ngày tạo: 2021-06-01*
