# DB Optimization Notes

> Ghi chú từ thư mục `SOURCE/DB_OPTIMIZATION/`

## Mục lục
- [Tổng quan vấn đề](#tổng-quan-vấn-đề)
- [API cần tối ưu](#api-cần-tối-ưu-ưu-tiên-cao)
- [Các cải tiến đã thực hiện](#các-cải-tiến-đã-thực-hiện)
- [Kế hoạch tiếp theo](#kế-hoạch-tiếp-theo)

---

## Tổng quan vấn đề

Dựa trên phân tích log từ `Sigo_Live_Log` (8.37M records, 25/02/2026):

| Chỉ số | Giá trị |
|--------|---------|
| Tổng lượt gọi/30 ngày | ~680,000 |
| API chậm nhất (avg) | 42,667ms (`QuickOrderRequest/RenterCancel`) |
| API có tải cao + chậm | `SearchingRentalService/UpdateBookingInfo` (8,762 lượt, avg 2,806ms) |
| API search chính | `SearchingRentalService/List` (26,785 lượt, avg 1,781ms, max 335s) |

---

## API cần tối ưu (ưu tiên cao)

### 1. `SearchingRentalService/List` 🔴
- **Lượt gọi:** 26,785/30 ngày (API business quan trọng nhất)
- **Avg:** 1,781ms | **Max:** 335,733ms (!!!)
- **Hướng tối ưu:**
  - [ ] Thêm index trên các cột filter (địa điểm, ngày, loại xe)
  - [ ] Cache kết quả tìm kiếm phổ biến (Redis)
  - [ ] Phân tích execution plan query

### 2. `SearchingRentalService/UpdateBookingInfo` 🔴
- **Lượt gọi:** 8,762/30 ngày
- **Avg:** 2,806ms | **P95:** 7,228ms | **Max:** 45,044ms
- **Hướng tối ưu:**
  - [ ] Tách logic phức tạp thành async
  - [ ] Review N+1 query

### 3. `QuickOrderRequest/RenterCancel` 🔴
- **Avg:** 42,667ms — chậm nhất hệ thống
- **Lượt gọi thấp** (5 lượt/30 ngày) nhưng UX rất tệ
- **Hướng tối ưu:**
  - [ ] Điều tra stored procedure liên quan
  - [ ] Có thể do lock/deadlock khi cancel

### 4. `Order_ListView_RentCar/OwnerCancel` 🔴
- **Avg:** 8,988ms | **Max:** 31,853ms
- **Hướng tối ưu:**
  - [ ] Review transaction scope
  - [ ] Async notification sau khi cancel

### 5. `Traffic_Vehicle/CheckTrafficTicket` 🔴
- **Avg:** 5,432ms | **Max:** 34,447ms
- **Nguyên nhân:** Phụ thuộc external API
- **Hướng tối ưu:**
  - [ ] Thêm timeout + circuit breaker
  - [ ] Cache kết quả kiểm tra trong 24h

### 6. `RentalService/GetOrderDetail` 🔴
- **Avg:** 2,199ms | **Min:** 98ms (dao động lớn)
- **Hướng tối ưu:**
  - [ ] Xem lại các JOIN, có thể reduce SELECT fields

---

## Các cải tiến đã thực hiện

> Nguồn: `SOURCE/DB_OPTIMIZATION/` — Phân tích ngày 2026-02-03, PostgreSQL 16.2

### Phase 1: Critical Indexes (ảnh hưởng lớn nhất)

| # | Index | Table | Cải thiện |
|---|-------|-------|-----------|
| 1 | `idx_busy_schedule_rental_dates` | `ServiceItem_DateBusyRentalSchedule` | 177ms → 0.5ms (**350x**), tiết kiệm 20.9h CPU/ngày |
| 2 | `idx_rental_view_history_main` | `RentalServiceItemViewHistory` | Cache hit 0.6% → 95%+, 600MB/query → 5-10MB |
| 3 | `idx_rental_view_history_viewat` | `RentalServiceItemViewHistory` (partial) | DISTINCT query tối ưu, 583MB → <10MB |
| 4 | `idx_order_unhide_phone` | `Order` (partial, WHERE DepositDoneAt IS NOT NULL) | 814ms → 16-80ms |
| 5 | `idx_order_statusCode_id` | `Order` | Đã hoạt động tốt (0.243ms) |
| 6 | `idx_notification_entity_type` | `NotificationMessage` | NOT EXISTS subquery tối ưu |

### Phase 2: Code Fixes

| # | Fix | File | Cải thiện |
|---|-----|------|-----------|
| 1 | `UpdateBULK` → `Update` | `OrderService_UserAction.cs:81` | 423,864ms → 20-50ms (**850-2000x**) |
| 2 | Sửa WHERE clause SP | `sp_GetOrdersNeedUnhidePhone_Json` | `(NOW() - DepositDoneAt) >= INTERVAL` → `DepositDoneAt <= (NOW() - INTERVAL)` để dùng index |

### Phase 3: Đang chờ thực hiện

| # | Task | Ưu tiên | Ghi chú |
|---|------|---------|---------|
| 1 | `MBBank_APICall_Log` — tạo index composite | HIGH | 196K calls, cache 2.87%, cần xác định WHERE clause |
| 2 | `sp_set_busy_rental_schedule` — viết lại function | HIGH | 19.7s/call × 1,713 calls = 9.4h CPU |
| 3 | Archive log tables > 3 tháng | MEDIUM | MBBank_APICall_Log, RentalServiceItemViewHistory |

### Kết quả đo lường

| Metric | Trước | Sau (dự kiến) | Cải thiện |
|--------|-------|---------------|-----------|
| Avg Query Time (Top 10) | 19,762ms | 200-500ms | **40-98x** |
| Cache Hit Rate | 10-20% | 90-95% | **5-9x** |
| Disk Reads | 600 MB/query | 5-10 MB/query | **60-120x** |
| CPU Time/Day | 51.7 giờ | 0.5-1 giờ | **50-100x** |
| Bulk UPDATE | 423,864ms | 20-50ms | **850-2000x** |

---

## Kế hoạch tiếp theo

### Ưu tiên cao (trong tuần)

1. **MBBank_APICall_Log indexing**
   ```sql
   CREATE INDEX CONCURRENTLY idx_mbbank_log_composite
   ON dbo."MBBank_APICall_Log" ("APIType", "ClientMessageId", "Log_CreatedDate" DESC);
   ```

2. **FK indexes cho query JOIN chains**
   ```sql
   CREATE INDEX idx_rental_service_id ON dbo."RentalService_SelfdriveCarRental" ("Id", "CarId");
   CREATE INDEX idx_vehicle_id_model ON dbo."Vehicle" ("Id", "VehicleModelId");
   CREATE INDEX idx_order_rental_service ON dbo."Order" ("RentalServiceItemId");
   ```

3. **Review `sp_set_busy_rental_schedule_for_busy_canceled_order`**
   - 1,713 calls × 19.7s = 9.4h CPU/ngày
   - Có thể thay bằng materialized view + batch update

### Ưu tiên trung bình (trong tháng)

4. **Archive dữ liệu cũ** — Partition `MBBank_APICall_Log` theo tháng, archive > 3 tháng
5. **ANALYZE tables** sau khi tạo index:
   ```sql
   ANALYZE dbo."ServiceItem_DateBusyRentalSchedule";
   ANALYZE dbo."Order";
   ANALYZE dbo."RentalServiceItemViewHistory";
   ANALYZE dbo."MBBank_APICall_Log";
   ```

### Rollback plan

```sql
-- Xóa index nếu gây vấn đề
DROP INDEX CONCURRENTLY dbo.idx_busy_schedule_rental_dates;
-- Code: quay lại repo.UpdateBULK(listU) nếu cần
```

Chi tiết đầy đủ: `SOURCE/DB_OPTIMIZATION/ACTION_PLAN.md` | `SOURCE/DB_OPTIMIZATION/OPTIMIZATION_REPORT.md`

---

*Xem báo cáo chi tiết: [API_Performance_Report.html](./API_Performance_Report.html)*
