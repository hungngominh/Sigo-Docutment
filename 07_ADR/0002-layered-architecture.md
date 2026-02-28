# ADR-0002: Kiến trúc phân lớp 4 tầng (API / Service / Core / DataShared)

## Status

Accepted

## Context

Hệ thống Sigo API cần xử lý domain nghiệp vụ phức tạp bao gồm:
- 100+ entities trong database
- 50+ controllers với business logic đa dạng
- 27+ background engines chạy song song
- 6 feature modules độc lập (EWallet, Identity, CMS...)
- Nhiều developer làm việc đồng thời

Cần quyết định cấu trúc tổ chức code để đảm bảo maintainability và scalability.

## Decision Drivers

* **Separation of concerns** — tách biệt rõ ràng presentation, business, data
* **Testability** — có thể unit test từng layer độc lập
* **Team collaboration** — nhiều developer làm việc song song không conflict
* **Maintainability** — dễ tìm hiểu và sửa đổi code
* **Reusability** — logic dùng chung không bị duplicate

## Considered Options

### Option 1: Layered Architecture 4 tầng
```
AllianceMiddlemanWebAPI         ← API Layer (Controllers, Auth, SignalR)
AllianceMiddlemanWebAPI.Service ← Service Layer (Business Logic, Engines)
AllianceMiddlemanWebAPI.Core    ← Data Layer (EF Core, DbContext, SP)
AllianceMiddlemanWebAPI.DataShared ← Shared DTOs
```
- **Pros:** Rõ ràng, quen thuộc với team .NET, dễ navigate
- **Cons:** Có thể tạo "God Service" nếu không discipline

### Option 2: Clean Architecture (Hexagonal)
```
Domain (entities + interfaces)
Application (use cases)
Infrastructure (EF Core, external services)
Presentation (API controllers)
```
- **Pros:** Dependency inversion mạnh, domain thuần túy
- **Cons:** Phức tạp hơn, overkill cho team và timeline hiện tại

### Option 3: Vertical Slice Architecture
- Tổ chức theo feature thay vì layer
- **Pros:** Mỗi feature tự đủ, ít coupling giữa features
- **Cons:** Khó chia sẻ logic chung, team chưa quen

## Decision

Chúng ta sẽ dùng **Layered Architecture 4 tầng** với phân chia projects rõ ràng.

## Rationale

1. **Quen thuộc:** Team đã có kinh nghiệm với layered architecture trong .NET
2. **Rõ ràng:** Mỗi layer có trách nhiệm cụ thể, dễ explain cho member mới
3. **Phù hợp timeline:** Clean Architecture đòi hỏi thiết kế cẩn thận hơn và có thể chậm delivery
4. **DTO isolation:** `DataShared` project tách biệt DTOs, tránh circular dependencies
5. **Module support:** Pattern này dễ mở rộng sang các module độc lập (EWallet, Identity...)

## Consequences

### Tích cực
- Separation of concerns rõ ràng
- Controller nhẹ (thin controller) — chỉ gọi service và trả response
- Service layer chứa toàn bộ business logic
- Data layer có thể được thay thế mà không ảnh hưởng business logic
- DTOs tách biệt khỏi entities

### Tiêu cực
- Có nguy cơ tạo "pass-through" layers (service chỉ gọi thẳng repository)
- Nếu không discipline, service có thể trở thành "God Service"
- Một thay đổi nhỏ có thể cần thay đổi qua nhiều layers

### Rủi ro
- Business logic rò rỉ sang controller layer
- **Giảm thiểu:** Code review enforce không có business logic trong controller
- Service layer quá lớn
- **Giảm thiểu:** Tách service theo domain (AppSystem, MainBusiness, Categories...)

## Cấu trúc thực tế

```
SOURCE/
├── AllianceMiddlemanWebAPI/
│   └── Controllers/
│       ├── AppSystem/       # Account, Staff, Notification
│       ├── MainBusiness/    # Order, User, Vehicle, Payment
│       ├── Categories/      # Config* (50+ controllers)
│       └── Reports/
│
├── AllianceMiddlemanWebAPI.Service/
│   ├── Services/
│   │   ├── AppSystem/
│   │   └── MainBusiness/
│   └── Engines/             # 27+ IHostedService
│
├── AllianceMiddlemanWebAPI.Core/
│   └── Data/
│       ├── AppSystem/
│       ├── BusinessData/    # 100+ entities
│       └── Stores/          # Stored procedures
│
└── AllianceMiddlemanWebAPI.DataShared/
    └── DTOs/
```

## Related Decisions

- ADR-0001: ASP.NET Core 5.0 Framework
- ADR-0005: Entity Framework Core ORM
- ADR-0009: Background Engines Pattern
- ADR-0010: Modular Design

---

*Ngày tạo: 2021-01-01*
