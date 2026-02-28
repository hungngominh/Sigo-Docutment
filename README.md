# AllianceMiddleman — Tài liệu hệ thống

> **Framework:** ASP.NET Core 5.0 | **DB:** SQL Server + PostgreSQL | **Deploy:** Docker / Jenkins

---

## Reading Guide — Đọc gì trước?

| Bạn là ai | Bắt đầu từ | Tiếp theo |
|-----------|-----------|-----------|
| **Developer mới** | [overview.md](./01_ARCHITECTURE/overview.md) → [project-structure.md](./01_ARCHITECTURE/project-structure.md) | [base-service-pattern.md](./01_ARCHITECTURE/base-service-pattern.md) → [setup-dev.md](./06_OPERATIONS/setup-dev.md) |
| **Backend dev (thêm CRUD)** | [base-service-pattern.md](./01_ARCHITECTURE/base-service-pattern.md#tạo-service-mới--step-by-step) | [database-design.md](./01_ARCHITECTURE/database-design.md) → [validation-error-catalog.md](./01_ARCHITECTURE/validation-error-catalog.md) |
| **Dev tích hợp (MB Bank, MISA...)** | [integrations.md](./02_MODULES/integrations.md) | [integrations/mbbank.md](./03_API/integrations/mbbank.md) → [ewallet.md](./02_MODULES/ewallet.md) |
| **QA / Tester** | [rental-service.test-scenarios.md](./03_API/rental-service.test-scenarios.md) | [booking-flow.md](./04_BUSINESS_FLOWS/booking-flow.md) → [cancel-flow.md](./04_BUSINESS_FLOWS/cancel-flow.md) |
| **AI Agent / LLM** | [llms.txt](./llms.txt) → [SYSTEM_MAP.md](./SYSTEM_MAP.md) | Theo links trong SYSTEM_MAP |
| **DevOps** | [deployment.md](./01_ARCHITECTURE/deployment.md) → [setup-dev.md](./06_OPERATIONS/setup-dev.md) | [config-keys.md](./06_OPERATIONS/config-keys.md) → [runbook.md](./06_OPERATIONS/runbook.md) |
| **Business analyst** | [booking-flow.md](./04_BUSINESS_FLOWS/booking-flow.md) → [pricing-calculation.md](./04_BUSINESS_FLOWS/pricing-calculation.md) | [order-status-machine.md](./04_BUSINESS_FLOWS/order-status-machine.md) → [cancel-flow.md](./04_BUSINESS_FLOWS/cancel-flow.md) |
| **Debugging sự cố** | [runbook.md](./06_OPERATIONS/runbook.md) | [background-engines.md](./01_ARCHITECTURE/background-engines.md) → [stored-procedures.md](./01_ARCHITECTURE/stored-procedures.md) |

---

## Mục lục

### [01. Kiến trúc hệ thống](./01_ARCHITECTURE/)
| File | Nội dung |
|------|---------|
| [overview.md](./01_ARCHITECTURE/overview.md) | Tổng quan layers, tech stack, luồng request |
| [project-structure.md](./01_ARCHITECTURE/project-structure.md) | **Solution structure, DI configuration, startup pipeline** |
| [base-service-pattern.md](./01_ARCHITECTURE/base-service-pattern.md) | **CRUD framework, EzyResultObject, pagination, tạo service mới** |
| [database-design.md](./01_ARCHITECTURE/database-design.md) | Schema, entities, EF Core mappings, indexes, stored procedures |
| [stored-procedures.md](./01_ARCHITECTURE/stored-procedures.md) | **17 SPs chi tiết: parameters, return types, actual SQL logic, callers** |
| [helper-algorithms.md](./01_ARCHITECTURE/helper-algorithms.md) | **6 helper classes: pricing engine (with examples), search pipeline, delivery fee (DB data), FCM, wallet** |
| [background-engines.md](./01_ARCHITECTURE/background-engines.md) | 29+ background engines, batch jobs, chi tiết logic |
| [notification-system.md](./01_ARCHITECTURE/notification-system.md) | **FCM, SignalR hub, notification templates, scheduling** |
| [file-upload.md](./01_ARCHITECTURE/file-upload.md) | **Google Drive, image compression, SFTP, static files** |
| [security-middleware.md](./01_ARCHITECTURE/security-middleware.md) | **JWT, CORS, authorization, error handling, request logging, 2FA** |
| [validation-error-catalog.md](./01_ARCHITECTURE/validation-error-catalog.md) | **Validation rules, error messages, order status flow, discount logic, timezone patterns** |
| [deployment.md](./01_ARCHITECTURE/deployment.md) | Docker, Jenkins, môi trường |

### [02. Modules](./02_MODULES/)
| File | Nội dung |
|------|---------|
| [core-appsystem.md](./02_MODULES/core-appsystem.md) | Hệ thống account, staff, device |
| [core-user.md](./02_MODULES/core-user.md) | User management, verification |
| [core-vehicle.md](./02_MODULES/core-vehicle.md) | Vehicle entities, insurance |
| [core-order.md](./02_MODULES/core-order.md) | Order lifecycle, payment |
| [core-rental-service.md](./02_MODULES/core-rental-service.md) | Rental service, service items |
| [core-notification.md](./02_MODULES/core-notification.md) | Notification module |
| [ewallet.md](./02_MODULES/ewallet.md) | Ví điện tử, giao dịch, wallet integrity |
| [identity.md](./02_MODULES/identity.md) | Quản lý danh tính người dùng |
| [integrations.md](./02_MODULES/integrations.md) | Tổng quan tích hợp bên thứ 3 |
| [cms.md](./02_MODULES/cms.md) | Quản lý nội dung |
| [dynamic-report.md](./02_MODULES/dynamic-report.md) | Báo cáo động |
| [traffic-ticket.md](./02_MODULES/traffic-ticket.md) | Phạt nguội, vi phạm giao thông |
| [hotline.md](./02_MODULES/hotline.md) | OMI Call tổng đài |

### [03. API Reference](./03_API/)
| File | Domain |
|------|--------|
| [auth.md](./03_API/auth.md) | Account, OTP, Login, Register |
| [rental-service.md](./03_API/rental-service.md) | RentalService, SearchingRentalService |
| [rental-service.test-scenarios.md](./03_API/rental-service.test-scenarios.md) | 75 test scenarios + BDD templates cho AI tạo test case |
| [cancel-flow.test-scenarios.md](./03_API/cancel-flow.test-scenarios.md) | 36 cancel flow test scenarios |
| [ewallet-order-lifecycle.test-scenarios.md](./03_API/ewallet-order-lifecycle.test-scenarios.md) | 34 wallet + order lifecycle test scenarios |
| [test-coverage-matrix.md](./03_API/test-coverage-matrix.md) | QC cross-reference: scenario → business rule → code |
| [order.md](./03_API/order.md) | Order, Payment, Confirm, Cancel |
| [user.md](./03_API/user.md) | User, Profile, Device |
| [notification.md](./03_API/notification.md) | Notification, Hub, Support |
| [vehicle.md](./03_API/vehicle.md) | Vehicle, Insurance, Setting |
| [service-item.md](./03_API/service-item.md) | ServiceItem, DatePrice, Schedule |
| [quick-order.md](./03_API/quick-order.md) | QuickOrderRequest, Suggestion |
| [categories.md](./03_API/categories.md) | Tất cả Config* controllers, actual data (order status, delivery fee, discounts) |
| [reports.md](./03_API/reports.md) | BangTinhCongNo, TongHopKhaiThue |
| [ewallet.md](./03_API/ewallet.md) | EWallet — Ví điện tử, nạp/rút/chuyển tiền |
| [traffic-ticket.md](./03_API/traffic-ticket.md) | TrafficTicket — Tra cứu phạt nguội |
| [hotline.md](./03_API/hotline.md) | Hotline — OMI Call integration |
| [cms.md](./03_API/cms.md) | CMS — Blog, News, Topic, URL Record |
| [dynamic-report.md](./03_API/dynamic-report.md) | DynamicReport — Báo cáo động |
| [integrations/mbbank.md](./03_API/integrations/mbbank.md) | MB Bank — OAuth2, RSA, TransferFund, polling |
| [integrations/misa-invoice.md](./03_API/integrations/misa-invoice.md) | MISA Invoice |
| [integrations/mioto.md](./03_API/integrations/mioto.md) | Mioto — Đồng bộ xe, giá, chuyến |
| [integrations/vietqr.md](./03_API/integrations/vietqr.md) | VietQR — Tra cứu tài khoản, KYC |

### [04. Business Flows](./04_BUSINESS_FLOWS/)
| File | Luồng |
|------|-------|
| [booking-flow.md](./04_BUSINESS_FLOWS/booking-flow.md) | Đặt xe → thanh toán → nhận xe |
| [cancel-flow.md](./04_BUSINESS_FLOWS/cancel-flow.md) | Renter/Owner cancel, hoàn tiền (với formulas) |
| [pricing-calculation.md](./04_BUSINESS_FLOWS/pricing-calculation.md) | **14 công thức tính giá chi tiết, ví dụ minh hoạ** |
| [order-status-machine.md](./04_BUSINESS_FLOWS/order-status-machine.md) | **State machine đơn hàng đầy đủ** |
| [owner-onboarding.md](./04_BUSINESS_FLOWS/owner-onboarding.md) | Chủ xe đăng ký dịch vụ |
| [ewallet-withdraw.md](./04_BUSINESS_FLOWS/ewallet-withdraw.md) | Rút tiền ví |

### [05. Performance](./05_PERFORMANCE/)
| File | Nội dung |
|------|---------|
| [API_Performance_Report.html](./05_PERFORMANCE/API_Performance_Report.html) | Báo cáo hiệu năng API (dashboard, charts) |
| [top10-api-report.md](./05_PERFORMANCE/top10-api-report.md) | Tài liệu chi tiết 10 API gọi nhiều nhất |
| [db-optimization-notes.md](./05_PERFORMANCE/db-optimization-notes.md) | Ghi chú tối ưu DB |

### [06. Operations](./06_OPERATIONS/)
| File | Nội dung |
|------|---------|
| [setup-dev.md](./06_OPERATIONS/setup-dev.md) | Cài đặt môi trường local |
| [config-keys.md](./06_OPERATIONS/config-keys.md) | **Đầy đủ: appsettings, SystemConfigKeys, CachedDataManagement** |
| [runbook.md](./06_OPERATIONS/runbook.md) | Xử lý sự cố thường gặp |

### [07. Architecture Decision Records](./07_ADR/)
| ADR | Tiêu đề | Trạng thái |
|-----|---------|-----------|
| [0001](./07_ADR/0001-aspnet-core-5-framework.md) | Chọn ASP.NET Core 5.0 làm Web API Framework | Accepted |
| [0002](./07_ADR/0002-layered-architecture.md) | Kiến trúc phân lớp 4 tầng | Accepted |
| [0003](./07_ADR/0003-sql-server-primary-database.md) | SQL Server primary + PostgreSQL secondary | Accepted |
| [0004](./07_ADR/0004-jwt-authentication.md) | JWT Bearer Token + OAuth2 nội bộ | Accepted |
| [0005](./07_ADR/0005-entity-framework-core.md) | Entity Framework Core 5.0 làm ORM | Accepted |
| [0006](./07_ADR/0006-redis-caching.md) | Redis làm Caching Layer | Accepted |
| [0007](./07_ADR/0007-kafka-message-streaming.md) | Apache Kafka cho Message Streaming | Accepted |
| [0008](./07_ADR/0008-signalr-realtime-notification.md) | SignalR cho Real-time Notification | Accepted |
| [0009](./07_ADR/0009-background-engines-pattern.md) | Background Engines Pattern (IHostedService) | Accepted |
| [0010](./07_ADR/0010-modular-design.md) | Thiết kế Module độc lập | Accepted |
| [0011](./07_ADR/0011-docker-multistage-deployment.md) | Docker Multi-stage Build Deployment | Accepted |
| [0012](./07_ADR/0012-jenkins-cicd-pipeline.md) | Jenkins CI/CD + SVN Revision Tagging | Accepted |
| [0013](./07_ADR/0013-api-versioning-strategy.md) | Chiến lược API Versioning (v1/v2) | Accepted |
| [0014](./07_ADR/0014-standardized-response-wrapper.md) | Response chuẩn hoá EzyResultObject | Accepted |
| [0015](./07_ADR/0015-mbbank-payment-integration.md) | Tích hợp MB Bank API cho thanh toán | Accepted |

### AI Entry Points & QC
| File | Mục đích |
|------|---------|
| [llms.txt](./llms.txt) | Điểm vào cho AI agents — tóm tắt hệ thống + deep-dive links |
| [SYSTEM_MAP.md](./SYSTEM_MAP.md) | Bản đồ hệ thống cho AI navigation — entity, flow, cross-refs |

---

## Quick Reference — Cho Developer mới

### Code Patterns
- **Tạo API CRUD mới:** Xem [base-service-pattern.md](./01_ARCHITECTURE/base-service-pattern.md#tạo-service-mới--step-by-step)
- **Response format:** `EzyResultObject<T>` — Status 1=OK, 0=Error
- **Pagination:** `EzyDataSourceResult<T>` — Data + TotalCount
- **Entity base:** `IEFBaseEntity` — Id (long), IsDeleted, Log_* fields

### Architecture Decisions
- **Service không dùng standard DI** — dùng `CreateServiceInstance<T>()` factory
- **Cache 70+ config tables** vào memory — `CachedDataManagement`
- **Background engines** dùng custom framework (`Ezy.Module.Engine.dll`), không phải Hangfire
- **MB Bank polling** (không webhook) — `AutoUpdateWithdrawStatus_MBBankEngine`

### Pricing & Business
- **Deposit:** 30% mặc định, có thể fixed amount
- **Commission:** Priority cascade (item → owner → category → system)
- **Cancel refund:** 100% trong 15 phút, phạt 30% sau đó, 0% sát ngày (7 ngày)
- **Business hours:** 7:00 - 21:00, timeout 3 giờ

---

*Cập nhật lần cuối: 01/03/2026*
