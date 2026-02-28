# Runbook — Xử lý sự cố thường gặp

## Mục lục
- [API chậm / timeout](#api-chậm--timeout)
- [Lỗi authentication](#lỗi-authentication)
- [Background engine không chạy](#background-engine-không-chạy)
- [MB Bank chuyển tiền thất bại](#mb-bank-chuyển-tiền-thất-bại)
- [Redis cache lỗi](#redis-cache-lỗi)
- [Kafka message tắc](#kafka-message-tắc)

---

## API chậm / timeout

**Dấu hiệu:** Response time > 5,000ms, user phản hồi app lag.

**Kiểm tra:**
1. Xem log DB: `SELECT * FROM dbo."Log_APIRequest" WHERE "TotalRunTime" > 5000 ORDER BY "Log_CreatedDate" DESC LIMIT 20`
2. Kiểm tra SQL Server: query đang chờ lock không
3. Kiểm tra Redis: kết nối có ổn không

**API chậm đã biết (cần theo dõi):**
- `SearchingRentalService/List` — avg 1,781ms, max 335s
- `QuickOrderRequest/RenterCancel` — avg 42,667ms
- Xem đầy đủ: [db-optimization-notes.md](../05_PERFORMANCE/db-optimization-notes.md)

---

## Lỗi authentication

**Dấu hiệu:** 401 Unauthorized, user không đăng nhập được.

**Kiểm tra:**
1. OAuth2 server (`http://localhost:12391`) có chạy không
2. JWT SecretKey có khớp giữa identity server và API không
3. Token có hết hạn không

**Fix nhanh:**
```bash
# Restart identity server (chạy trên Docker)
docker restart sigo-api

# Hoặc rollback về image cũ nếu cần
docker pull thanghatien/sigo-api-dev:1.0.0-{SVN_REVISION}
docker stop sigo-api && docker rm sigo-api
docker run -d \
  --name sigo-api \
  -p 80:80 -p 443:443 \
  --env-file /etc/sigo/production.env \
  thanghatien/sigo-api-dev:1.0.0-{SVN_REVISION}
```

---

## Background engine không chạy

**Dấu hiệu:** Đơn hàng không tự hoàn thành, không gửi được notification batch.

**Kiểm tra:**
1. Log app: tìm `Engine` trong log
2. Kiểm tra `FirstRunHostedService` có khởi tạo xong không

**Restart:**
```bash
# Restart toàn bộ app
docker restart alliance-middleman
```

---

## MB Bank chuyển tiền thất bại

**Dấu hiệu:** Chủ xe không nhận được tiền, `AutoWithdrawEngine` báo lỗi.

**Kiểm tra:**
1. Xem log: tìm `AutoWithdraw` errors
2. Kiểm tra cấu hình MB Bank trong `ConfigMBBank`
3. Check số dư tài khoản công ty

**Xử lý thủ công:**
- Vào admin, tìm `WithdrawRequest` status = `Failed`
- Retry thủ công qua `MBBank/TransferFund`

---

## Redis cache lỗi

**Dấu hiệu:** App chậm toàn bộ, log có `RedisConnectionException`.

**Kiểm tra:**
```bash
redis-cli ping
```

**Fix:**
- Restart Redis
- App tự fallback về DB (chậm hơn nhưng vẫn chạy được)

---

## Kafka message tắc

**Dấu hiệu:** Notification gửi chậm, email queue tắc nghẽn.

**Kiểm tra:**
```bash
# Kiểm tra Kafka consumer lag (cần kafka-consumer-groups CLI)
kafka-consumer-groups --bootstrap-server {KAFKA_BOOTSTRAP_SERVERS} \
  --describe --group sigo-notification-group

# Hoặc kiểm tra notification queue in-memory (app dùng Channel<T>)
# App sử dụng bounded channel (capacity: 1000, policy: DropOldest)
# Monitor qua log: tìm keyword "NotificationService" hoặc "PushNotify"

# Kiểm tra email/SMS queue qua SP
psql -h {DB_HOST} -U {DB_USER} -d {DB_NAME} -c \
  "SELECT * FROM sp_GetMessageQueueReport_Json();"
psql -h {DB_HOST} -U {DB_USER} -d {DB_NAME} -c \
  "SELECT * FROM sp_GetSMSQueueReport_Json();"
```

> **Lưu ý:** App chủ yếu dùng in-memory `Channel<sp_GetNotifyMessage4Push_Json_Result>` cho notification queue (không qua Kafka). Kafka config key: `Kafka:BootstrapServers` trong env vars.

---

## Liên hệ hỗ trợ

| Vấn đề | Team/Phụ trách | Ghi chú |
|--------|---------------|---------|
| DB / Query | DBA Team — Alliance IT | PostgreSQL: `deb-postgresql.allianceitsc.com`, Production: `103.57.211.148` |
| Infrastructure | DevOps — Alliance IT | Jenkins: `jenkins-dev.allianceitsc.com:442`, Docker Hub: `thanghatien/sigo-api-dev` |
| Payment / MB Bank | Backend Team | Config: `ConfigMBBank` table, Log: `MBBank_APICall_Log` |
| MISA Invoice | Backend Team | Config: `ConfigMISAInvoice` table, Log: `Log_MISAInvoice_PublishHSM` |
