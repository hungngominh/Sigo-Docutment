# Architecture Decision Records — AllianceMiddleman

Thư mục này chứa các Architecture Decision Records (ADR) ghi lại những quyết định kiến trúc quan trọng của hệ thống **AllianceMiddleman (Sigo API)**.

---

## Quy ước

- **Đặt tên file:** `NNNN-tieu-de-ngan.md` (số thứ tự 4 chữ số)
- **Trạng thái:** Proposed → Accepted → Deprecated → Superseded / Rejected
- **Tạo ADR mới:** Copy `template.md`, điền nội dung, tạo PR để review

---

## Vòng đời ADR

```
Proposed → Accepted → Deprecated → Superseded
              ↓
           Rejected
```

---

## Danh sách ADR

| ADR | Tiêu đề | Trạng thái | Ngày |
|-----|---------|-----------|------|
| [0001](./0001-aspnet-core-5-framework.md) | Chọn ASP.NET Core 5.0 làm Web API Framework | Accepted | 2021-01-01 |
| [0002](./0002-layered-architecture.md) | Kiến trúc phân lớp 4 tầng (API / Service / Core / DataShared) | Accepted | 2021-01-01 |
| [0003](./0003-sql-server-primary-database.md) | Chọn SQL Server làm database chính, PostgreSQL hỗ trợ | Accepted | 2021-01-01 |
| [0004](./0004-jwt-authentication.md) | Xác thực bằng JWT Bearer Token + OAuth2 nội bộ | Accepted | 2021-01-01 |
| [0005](./0005-entity-framework-core.md) | Dùng Entity Framework Core 5.0 làm ORM | Accepted | 2021-01-01 |
| [0006](./0006-redis-caching.md) | Dùng Redis làm Caching Layer | Accepted | 2021-06-01 |
| [0007](./0007-kafka-message-streaming.md) | Dùng Apache Kafka cho Message Streaming | Accepted | 2021-06-01 |
| [0008](./0008-signalr-realtime-notification.md) | Dùng SignalR cho Real-time Notification | Accepted | 2021-06-01 |
| [0009](./0009-background-engines-pattern.md) | Pattern Background Engines (IHostedService) cho xử lý bất đồng bộ | Accepted | 2021-06-01 |
| [0010](./0010-modular-design.md) | Thiết kế module độc lập (EWallet, Identity, CMS, DynamicReport...) | Accepted | 2021-06-01 |
| [0011](./0011-docker-multistage-deployment.md) | Triển khai bằng Docker multi-stage build | Accepted | 2021-09-01 |
| [0012](./0012-jenkins-cicd-pipeline.md) | CI/CD Pipeline dùng Jenkins + SVN revision tagging | Accepted | 2021-09-01 |
| [0013](./0013-api-versioning-strategy.md) | Chiến lược versioning API (v1 / v2) | Accepted | 2022-01-01 |
| [0014](./0014-standardized-response-wrapper.md) | Response chuẩn hoá với EzyResultObject | Accepted | 2021-01-01 |
| [0015](./0015-mbbank-payment-integration.md) | Tích hợp MB Bank API cho xử lý thanh toán | Accepted | 2022-01-01 |

---

## Hướng dẫn tạo ADR mới

1. Copy file [template.md](./template.md) sang `NNNN-ten-quyet-dinh.md`
2. Điền đầy đủ các mục: Context, Decision Drivers, Options, Decision, Consequences
3. Cập nhật bảng danh sách ADR ở README này
4. Submit PR cho ít nhất 2 senior engineer review

---

## Tài nguyên tham khảo

- [Documenting Architecture Decisions — Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- [MADR Template](https://adr.github.io/madr/)
- [ADR GitHub Organization](https://adr.github.io/)

---

*Cập nhật lần cuối: 25/02/2026*
