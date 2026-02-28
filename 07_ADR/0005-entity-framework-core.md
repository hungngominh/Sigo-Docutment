# ADR-0005: Dùng Entity Framework Core 5.0 làm ORM

## Status

Accepted

## Context

Sigo API có 100+ database entities trải rộng qua hai DbContext (`AppSystemDataContext` và `BusinessDataContext`). Team cần quyết định cách thức tương tác với SQL Server:

- Viết raw SQL / Stored Procedures
- Dùng ORM
- Dùng micro-ORM (Dapper)

Hệ thống có cả queries đơn giản (CRUD) lẫn queries phức tạp (báo cáo, aggregation).

## Decision Drivers

* **Developer productivity** — giảm boilerplate SQL code
* **Type safety** — compile-time checking, tránh lỗi typo
* **LINQ support** — querying với C# syntax
* **Migration management** — schema versioning
* **ASP.NET Core integration** — DI tích hợp native
* **Performance** — đủ tốt cho production load

## Considered Options

### Option 1: Entity Framework Core 5.0
- **Pros:**
  - LINQ queries type-safe
  - Code-first migrations
  - Change tracking
  - Native ASP.NET Core DI
  - Hỗ trợ cả SQL Server và PostgreSQL
  - Team quen thuộc
- **Cons:**
  - Performance thấp hơn raw SQL với queries phức tạp
  - Generated SQL đôi khi không optimal
  - Learning curve cho advanced features (owned entities, shadow properties)

### Option 2: Dapper (Micro-ORM)
- **Pros:**
  - Hiệu năng gần raw SQL
  - Kiểm soát hoàn toàn query
  - Nhẹ, đơn giản
- **Cons:**
  - Phải viết SQL thuần (tốn thời gian)
  - Không có change tracking
  - Không có migration management
  - Dễ xảy ra SQL injection nếu không cẩn thận

### Option 3: Raw SQL + Stored Procedures
- **Pros:**
  - Hiệu năng tối đa
  - DBA có thể tối ưu
- **Cons:**
  - Boilerplate code cao
  - Khó refactor khi schema thay đổi
  - Không có type safety

### Option 4: Kết hợp EF Core + Dapper
- **Pros:**
  - EF Core cho CRUD đơn giản, Dapper cho queries phức tạp
  - Best of both worlds
- **Cons:**
  - Phức tạp hơn, cần maintain 2 patterns

## Decision

Chúng ta sẽ dùng **Entity Framework Core 5.0** làm ORM chính cho CRUD operations và queries thông thường. Với queries phức tạp và báo cáo, sử dụng **Stored Procedures** đã định nghĩa trong `EEzyStoredProcedureNames.cs`.

## Rationale

1. **Team productivity:** EF Core giảm đáng kể boilerplate, team .NET quen thuộc
2. **Type safety:** LINQ expressions compile-time checked, tránh lỗi runtime
3. **Dual database:** Cùng EF Core code chạy được trên cả SQL Server và PostgreSQL (chỉ thay DbContext provider)
4. **Migration:** Code-first migrations quản lý schema changes có lịch sử
5. **Stored Procedures cho reports:** Báo cáo phức tạp (aggregations lớn) dùng SP để tối ưu hiệu năng database-side
6. **Z.EntityFramework.Extensions:** Dùng thư viện này cho bulk operations (insert/update hàng nghìn records)

## Cấu trúc DbContext thực tế

```csharp
// 2 DbContext tách biệt
public class AppSystemDataContext : DbContext
{
    // Accounts, Staff, Devices, EmailTemplates, Settings
}

public class BusinessDataContext : DbContext
{
    // 100+ business entities:
    // Orders, Payments, Users, Vehicles, RentalServices,
    // Notifications, Wallets, Configs...
}
```

## Consequences

### Tích cực
- CRUD operations nhanh chóng với ít code
- Schema migration có version control
- Change tracking tự động detect dirty entities
- LINQ queries refactor-safe (rename property → compile error ngay)
- DI registration đơn giản: `services.AddDbContext<BusinessDataContext>(...)`

### Tiêu cực
- N+1 query problem nếu không dùng `.Include()` đúng cách
- Generated SQL có thể không optimal với complex queries
- DbContext lifetime management cần cẩn thận (scoped vs singleton)
- Migration conflicts khi nhiều developer thay đổi entities cùng lúc

### Rủi ro
- Performance degradation với queries phức tạp
- **Giảm thiểu:** Profiler để detect slow queries, dùng SP cho báo cáo
- Lazy loading N+1 problems
- **Giảm thiểu:** Disable lazy loading by default, dùng explicit `.Include()`
- Context pool exhaustion
- **Giảm thiểu:** Dùng `AddDbContextPool<>()` và monitoring

## Related Decisions

- ADR-0002: Layered Architecture (Core/Data layer)
- ADR-0003: SQL Server + PostgreSQL (EF Core providers)
- ADR-0006: Redis Caching (kết hợp cache-aside pattern)

---

*Ngày tạo: 2021-01-01*
