# Architecture Overview - AllianceMiddleman

> **Framework:** ASP.NET Core 5.0
> **Deployment:** Docker / Jenkins CI/CD
> **Database:** SQL Server + PostgreSQL (hỗ trợ)
> **Cache:** Redis

---

## Mục lục

- [Tổng quan hệ thống](#tổng-quan-hệ-thống)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Kiến trúc phân lớp](#kiến-trúc-phân-lớp)
- [Luồng xử lý request](#luồng-xử-lý-request)
- [Danh sách module](#danh-sách-module)
- [Công nghệ sử dụng](#công-nghệ-sử-dụng)
- [Background Engines](#background-engines)
- [Tích hợp bên ngoài](#tích-hợp-bên-ngoài)

---

## Tổng quan hệ thống

[Mô tả 2–3 câu về mục đích hệ thống]

```
Client (Mobile/Web)
        │
        ▼
AllianceMiddlemanWebAPI   ◄── JWT Auth / OAuth2
        │
   ┌────┴────┐
   │ Service │  (AllianceMiddlemanWebAPI.Service)
   └────┬────┘
        │
   ┌────┴────┐
   │  Core   │  (AllianceMiddlemanWebAPI.Core — EF Core)
   └────┬────┘
        │
   ┌────┴────┐
   │   DB    │  SQL Server / PostgreSQL
   └─────────┘
```

---

## Cấu trúc thư mục

```
SOURCE/
├── AllianceMiddlemanWebAPI/          # Web API Host (Controllers, Startup)
│   ├── Controllers/
│   │   ├── AppSystem/                # Account, Staff, Notification...
│   │   ├── MainBusiness/             # Order, User, Vehicle, Payment...
│   │   └── Categories/               # Config & lookup tables
│   └── Hubs/                         # SignalR hubs
│
├── AllianceMiddlemanWebAPI.Core/     # Data Access Layer
│   └── Data/
│       ├── AppSystem/                # System entities & context
│       ├── BusinessData/             # Business entities & context
│       └── Stores/                   # Stored procedures
│
├── AllianceMiddlemanWebAPI.DataShared/  # Shared DTOs / contracts
│
├── AllianceMiddlemanWebAPI.Service/     # Business Logic Layer
│   ├── Services/
│   │   ├── AppSystem/
│   │   └── MainBusiness/
│   ├── Engines/                      # Background async engines
│   └── Hubs/                         # SignalR hub implementations
│
├── Ezy.Module.EWallet/               # Module ví điện tử
├── Ezy.Module.Identity/              # Module identity
├── Ezy.Module.CMS/                   # Module quản lý nội dung
├── Ezy.Module.DynamicReport/         # Module báo cáo động
└── Ezy.Module.TrafficTicket/         # Module phạt nguội
```

---

## Kiến trúc phân lớp

| Lớp             | Project                              | Vai trò                              |
|-----------------|--------------------------------------|--------------------------------------|
| **API Layer**   | `AllianceMiddlemanWebAPI`            | Nhận request, xác thực, trả response |
| **Service Layer** | `AllianceMiddlemanWebAPI.Service`  | Business logic, validation           |
| **Data Layer**  | `AllianceMiddlemanWebAPI.Core`       | EF Core, queries, stored procedures  |
| **Shared DTOs** | `AllianceMiddlemanWebAPI.DataShared` | Models dùng chung giữa các lớp       |

---

## Luồng xử lý request

```
Request
  │
  ▼
[JWT Middleware] ── Validate token
  │
  ▼
[Controller] ── Nhận + validate input
  │
  ▼
[Service] ── Business logic
  │
  ▼
[EF Core Context] ── Query DB / Stored Procedure
  │
  ▼
[Response] ── EzyResultObject<T>
```

---

## Danh sách module

| Module                   | Mô tả                          | Solution riêng |
|--------------------------|--------------------------------|----------------|
| `Ezy.Module.EWallet`     | Ví điện tử, giao dịch          | Có             |
| `Ezy.Module.Identity`    | Quản lý danh tính người dùng   | Có             |
| `Ezy.Module.CMS`         | Quản lý nội dung               | Có             |
| `Ezy.Module.DynamicReport` | Báo cáo động               | Có             |
| `Ezy.Module.TrafficTicket` | Phạt nguội, vi phạm giao thông | Có          |

---

## Công nghệ sử dụng

| Công nghệ           | Mục đích                          |
|---------------------|----------------------------------|
| ASP.NET Core 5.0    | Web API framework                |
| Entity Framework Core 5.0 | ORM, code-first            |
| SQL Server          | Database chính                   |
| Redis               | Caching (StackExchange.Redis)    |
| Kafka               | Message streaming (Confluent.Kafka) |
| SignalR             | Thông báo real-time              |
| JWT Bearer          | Xác thực API                     |
| Docker              | Containerization                 |
| Jenkins             | CI/CD pipeline                   |

---

## Background Engines

Các engine chạy nền trong `/Services/Engines/`:

| Engine                          | Mô tả                             |
|---------------------------------|----------------------------------|
| `AutoApprovedOrder_PaymentEngine` | Tự động duyệt thanh toán đơn hàng |
| `AutoCompleteOrderEngine`       | Tự động hoàn thành đơn hàng      |
| `NotificationBatchJobEngine`    | Gửi thông báo theo lô            |
| `AutoWithdrawEngine`            | Tự động xử lý rút tiền           |
| `ImageCompressionEngine`        | Nén ảnh tự động                  |
| `ProactiveNotificationHasNewUser` | Thông báo chủ động người dùng mới |
| *(và 20+ engines khác)*         |                                  |

---

## Tích hợp bên ngoài

| Hệ thống      | Mục đích                        |
|---------------|---------------------------------|
| MB Bank       | Tích hợp ngân hàng MB           |
| MISA Invoice  | Xuất hóa đơn điện tử            |
| Google OAuth2 | Đăng nhập bằng Google           |
| Google Maps   | Tích hợp bản đồ                 |
| Mioto API     | Tích hợp nền tảng thuê xe Mioto |
| OTP Service   | Gửi OTP xác thực                |

---

*Cập nhật lần cuối: {ngày}*
