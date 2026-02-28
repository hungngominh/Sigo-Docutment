# ADR-0001: Chọn ASP.NET Core 5.0 làm Web API Framework

## Status

Accepted

## Context

AllianceMiddleman (Sigo API) là một nền tảng cho thuê xe với yêu cầu:
- Xử lý hàng nghìn request đồng thời từ ứng dụng mobile và web
- Tích hợp nhiều dịch vụ bên ngoài (MB Bank, MISA, Google OAuth2...)
- Hỗ trợ real-time notification qua WebSocket
- Triển khai trên môi trường Windows Server và Linux (Docker)
- Team phát triển có kinh nghiệm với C# và .NET ecosystem

Cần chọn framework backend phù hợp để xây dựng REST API.

## Decision Drivers

* **Hiệu năng cao** — xử lý concurrent requests từ mobile app
* **Cross-platform** — chạy được trên Windows và Linux (Docker)
* **Tích hợp SignalR** — real-time notification là tính năng bắt buộc
* **Kinh nghiệm team** — team đã quen với C#/.NET
* **Hệ sinh thái phong phú** — ORM, Auth, Logging, Compression tích hợp sẵn
* **Long-term support** — cần ổn định cho production

## Considered Options

### Option 1: ASP.NET Core 5.0
- **Pros:**
  - Hiệu năng cao (top benchmark web frameworks)
  - Cross-platform (Windows, Linux, macOS)
  - SignalR tích hợp sẵn
  - Dependency Injection native
  - Middleware pipeline linh hoạt
  - Team có kinh nghiệm
  - Microsoft hỗ trợ dài hạn
- **Cons:**
  - .NET 5.0 không phải LTS (LTS là .NET 6)
  - Một số thư viện third-party chưa hỗ trợ đầy đủ

### Option 2: Node.js (Express / NestJS)
- **Pros:**
  - Hệ sinh thái NPM rất lớn
  - Non-blocking I/O tốt cho real-time
  - TypeScript hỗ trợ tốt
- **Cons:**
  - Team không có kinh nghiệm với Node.js
  - Single-threaded, cần cluster để tận dụng multi-core
  - Thiếu kinh nghiệm trong team gây rủi ro

### Option 3: Java Spring Boot
- **Pros:**
  - Enterprise-grade, mature ecosystem
  - Hiệu năng tốt
- **Cons:**
  - Team không có kinh nghiệm với Java/Spring
  - Overhead bộ nhớ cao hơn .NET Core
  - Thời gian khởi động chậm hơn

## Decision

Chúng ta sẽ dùng **ASP.NET Core 5.0** làm framework chính cho Web API.

## Rationale

1. **Team expertise:** Team đã có kinh nghiệm sâu với C# và .NET, giảm thiểu rủi ro và tăng tốc phát triển
2. **Tích hợp sinh thái:** Entity Framework Core, SignalR, JWT Auth, Response Compression đều tích hợp sẵn, không cần tích hợp thêm thư viện bên ngoài
3. **Hiệu năng:** ASP.NET Core 5.0 có hiệu năng vượt trội, xử lý tốt concurrent connections
4. **Cross-platform:** Hỗ trợ Docker Linux container cho production, giảm chi phí hosting
5. **Middleware pipeline:** Kiến trúc middleware dễ mở rộng cho JWT, logging, compression, CORS

## Consequences

### Tích cực
- Developer productivity cao do team quen với C#
- SignalR native → real-time notification không cần thư viện thêm
- Dependency injection built-in → code testable và maintainable
- Response Compression (Gzip + Brotli) tích hợp sẵn
- Swagger/OpenAPI tích hợp sẵn cho documentation

### Tiêu cực
- .NET 5.0 không phải LTS → cần nâng cấp lên .NET 6/8 trong tương lai
- Chi phí license thấp hơn Java/Node nhưng tooling (Visual Studio) có thể tốn phí

### Rủi ro
- .NET 5.0 end-of-life → cần lên kế hoạch nâng cấp lên .NET 8 LTS
- **Giảm thiểu:** Lập kế hoạch migration ADR khi cần

## Implementation Notes

- Entry point: `SOURCE/AllianceMiddlemanWebAPI/Program.cs`
- Startup configuration: `SOURCE/AllianceMiddlemanWebAPI/Startup.cs`
- Target framework: `net5.0` trong `.csproj`

## Related Decisions

- ADR-0002: Kiến trúc phân lớp 4 tầng
- ADR-0004: JWT Authentication
- ADR-0008: SignalR Real-time Notification
- ADR-0011: Docker multi-stage deployment

---

*Ngày tạo: 2021-01-01*
