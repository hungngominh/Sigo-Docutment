# ADR-0003: Chọn SQL Server làm Database chính, PostgreSQL hỗ trợ

## Status

Accepted

## Context

Sigo API cần lựa chọn database để lưu trữ:
- Dữ liệu nghiệp vụ cốt lõi: Order, User, Vehicle, Payment, Wallet (100+ entities)
- Giao dịch tài chính yêu cầu ACID compliance
- Dữ liệu cấu hình hệ thống (50+ Config* tables)
- Hỗ trợ stored procedures cho báo cáo phức tạp

Hệ thống cần đảm bảo tính toàn vẹn dữ liệu cao cho domain tài chính (thanh toán, ví điện tử).

## Decision Drivers

* **ACID compliance** — bắt buộc cho giao dịch tài chính và order processing
* **Stored procedures** — báo cáo phức tạp yêu cầu stored proc
* **Team familiarity** — team có kinh nghiệm với SQL Server
* **EF Core support** — cần ORM hỗ trợ tốt
* **Scalability** — cần xử lý tải tăng trưởng
* **Windows ecosystem** — hệ thống hiện tại trên Windows Server

## Considered Options

### Option 1: SQL Server (Primary) + PostgreSQL (Secondary)
- **Pros:**
  - ACID đầy đủ, mature và proven
  - Stored procedures mạnh mẽ
  - EF Core hỗ trợ xuất sắc
  - Team có kinh nghiệm
  - Integration với Windows ecosystem
  - SQL Server Management Studio quen thuộc
  - PostgreSQL làm option dự phòng/module phụ
- **Cons:**
  - Chi phí license SQL Server (Enterprise)
  - Phức tạp hơn khi duy trì 2 database engine

### Option 2: Chỉ PostgreSQL
- **Pros:**
  - Open source, không tốn license
  - ACID compliance
  - JSONB, PostGIS, full-text search tích hợp
  - Cross-platform
- **Cons:**
  - Team ít kinh nghiệm với PostgreSQL
  - Stored procedures khác syntax (PL/pgSQL)
  - Cần migration nếu đã có data trên SQL Server

### Option 3: MySQL
- **Pros:**
  - Open source, phổ biến
  - Team quen dùng
- **Cons:**
  - JSON support yếu hơn PostgreSQL
  - Stored procedures kém mạnh hơn SQL Server
  - ACID trong một số engine (MyISAM) không đầy đủ

### Option 4: MongoDB
- **Pros:**
  - Schema flexible cho product attributes
  - Horizontal scaling tốt
- **Cons:**
  - Multi-document transactions phức tạp hơn
  - Team ít kinh nghiệm NoSQL
  - Không phù hợp cho financial data

## Decision

Chúng ta sẽ dùng **SQL Server** làm database chính cho toàn bộ business data, và hỗ trợ **PostgreSQL** như database phụ cho một số module hoặc khi có nhu cầu cụ thể.

## Rationale

1. **ACID cho tài chính:** SQL Server cung cấp transaction isolation đầy đủ, quan trọng cho Order và Payment
2. **Stored Procedures:** Business logic báo cáo phức tạp được implement qua SP, SQL Server có SP mạnh và quen thuộc
3. **Team expertise:** Không tốn thời gian học công nghệ mới, giảm rủi ro production
4. **Dual DbContext:** Tách `AppSystemDataContext` (system data) và `BusinessDataContext` (business data) giúp manage cả hai database
5. **PostgreSQL option:** Giữ khả năng dùng PostgreSQL cho các module mới hoặc future migration

## Consequences

### Tích cực
- Tính toàn vẹn dữ liệu đảm bảo cho financial transactions
- Stored procedures tận dụng được cho báo cáo phức tạp (BangTinhCongNo, TongHopKhaiThue)
- Team productive ngay từ đầu
- Connection pooling và query optimization quen thuộc
- SSMS tooling cho DBA

### Tiêu cực
- Chi phí license SQL Server Enterprise cao
- Dual database làm phức tạp infrastructure
- SQL Server nặng hơn PostgreSQL về resource

### Rủi ro
- Vendor lock-in với Microsoft SQL Server
- **Giảm thiểu:** EF Core abstraction layer cho phép migrate nếu cần
- Chi phí license tăng khi scale
- **Giảm thiểu:** Cân nhắc chuyển sang PostgreSQL hoàn toàn khi có cơ hội

## Cấu trúc DbContext

```csharp
// System data (accounts, staff, settings)
AppSystemDataContext : DbContext

// Business data (orders, payments, vehicles, users)
BusinessDataContext : DbContext
```

## Related Decisions

- ADR-0005: Entity Framework Core ORM
- ADR-0006: Redis Caching (bổ sung cho SQL Server)
- ADR-0002: Layered Architecture (data layer)

---

*Ngày tạo: 2021-01-01*
