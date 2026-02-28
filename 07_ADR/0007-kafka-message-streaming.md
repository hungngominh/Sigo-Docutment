# ADR-0007: Dùng Apache Kafka cho Message Streaming

## Status

Accepted

## Context

Sigo API cần xử lý các event bất đồng bộ có volume lớn:
- Payment events từ MB Bank callback
- Notification broadcasting tới nhiều user
- Order status change events
- Audit trail cho các giao dịch tài chính
- Integration events giữa các module (EWallet, Identity, Core API)

Xử lý đồng bộ (synchronous) các sự kiện này trong request cycle sẽ làm tăng response latency và tạo coupling giữa các components.

## Decision Drivers

* **Decoupling** — producers và consumers độc lập
* **High throughput** — xử lý hàng nghìn events/giây
* **Durability** — events được persist, không mất khi consumer down
* **Replay capability** — có thể replay events từ offset cụ thể
* **Multiple consumers** — nhiều services có thể consume cùng một topic
* **Ordering** — đảm bảo thứ tự xử lý trong partition

## Considered Options

### Option 1: Apache Kafka (Confluent.Kafka)
- **Pros:**
  - High throughput (millions messages/sec)
  - Durable log-based storage
  - Replay events từ bất kỳ offset
  - Consumer groups cho horizontal scaling
  - Confluent.Kafka library cho .NET
  - Proven at scale
- **Cons:**
  - Phức tạp hơn RabbitMQ
  - Cần ZooKeeper (hoặc KRaft mode)
  - Learning curve cao
  - Overkill nếu volume thấp

### Option 2: RabbitMQ
- **Pros:**
  - Đơn giản hơn Kafka
  - AMQP protocol chuẩn
  - Management UI tốt
  - .NET library mature (MassTransit, EasyNetQ)
- **Cons:**
  - Messages không persistent theo mặc định
  - Không replay được events đã processed
  - Throughput thấp hơn Kafka
  - Fan-out kém efficient hơn Kafka

### Option 3: Azure Service Bus
- **Pros:**
  - Managed service, không cần maintain infrastructure
  - Dead letter queue tích hợp sẵn
- **Cons:**
  - Vendor lock-in Azure
  - Chi phí cao ở volume lớn
  - Latency cao hơn on-premise

### Option 4: Database-based Queue (Outbox Pattern)
- **Pros:**
  - Không cần thêm infrastructure
  - ACID với business transaction
- **Cons:**
  - Database polling overhead
  - Không scale tốt
  - Không phải giải pháp message broker thực sự

## Decision

Chúng ta sẽ dùng **Apache Kafka** với **Confluent.Kafka** .NET client cho message streaming và event-driven communication giữa các components.

## Rationale

1. **Volume dự kiến cao:** Payment events, notification events có thể lên tới hàng chục nghìn/giờ khi user base tăng
2. **Durability:** Giao dịch tài chính không được phép mất event → Kafka persist log không mất data
3. **Replay:** Khi có bug hoặc consumer down, có thể replay từ offset cũ để reprocess
4. **Multi-consumer:** Notification service, audit service, analytics service đều consume cùng order events
5. **Module integration:** Kafka làm integration bus giữa các module (EWallet, Core API)

## Cấu hình thực tế

```json
// appsettings.json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "sigo-api-consumer"
  }
}
```

## Hệ thống events chính

| Topic | Producer | Consumers |
|-------|---------|-----------|
| `order.created` | RentalService | NotificationEngine, EWallet |
| `order.confirmed` | RentalService | NotificationEngine |
| `payment.received` | MBBank | OrderEngine, WalletEngine |
| `notification.send` | API Controllers | PushNotificationEngine |
| `withdrawal.requested` | EWallet | AutoWithdrawEngine |

## Consequences

### Tích cực
- API response time không bị block bởi notification/audit processing
- Fault tolerance: nếu consumer down, events vẫn còn trong Kafka khi recover
- Event sourcing ready: Kafka topics có thể dùng như event store
- Analytics: dễ pipe events vào data pipeline

### Tiêu cực
- Thêm infrastructure phức tạp (Kafka cluster, ZooKeeper)
- Eventual consistency: một số operations không còn synchronous
- Debugging khó hơn khi trace qua async events
- Operational knowledge: team cần học Kafka administration

### Rủi ro
- Consumer lag khi volume tăng đột biến
- **Giảm thiểu:** Consumer group với nhiều instances, monitoring lag metrics
- Message ordering violation nếu dùng nhiều partitions
- **Giảm thiểu:** Dùng entity ID làm partition key để đảm bảo ordering per entity
- Schema evolution phức tạp
- **Giảm thiểu:** Thiết kế events backward-compatible, dùng Avro/Schema Registry nếu cần

## Related Decisions

- ADR-0009: Background Engines (consume Kafka events)
- ADR-0010: Modular Design (Kafka là integration bus giữa modules)

---

*Ngày tạo: 2021-06-01*
