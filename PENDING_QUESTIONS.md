# Documentation — Pending Questions

> File này lưu các câu hỏi cần chủ dự án trả lời để hoàn thiện documentation.
> Khi có câu trả lời, hãy update file tương ứng và xoá câu hỏi khỏi file này.

---

## 1. `sp_GetDataReportWebsite4BCT_Json` (SP #8)

**File:** `01_ARCHITECTURE/stored-procedures.md` (line ~675-693)

**Vấn đề:** SP hiện tại trả về hardcoded 0 cho tất cả 9 fields (TotalNewOrder, TotalCompletedOrder...).

**Cần trả lời:**
- [ ] SP này cần implement logic gì? (query nào, bảng nào, điều kiện gì?)
- [ ] 9 fields cụ thể lấy dữ liệu từ đâu?
- [ ] BCT = Bộ Công Thương? Báo cáo này dùng cho mục đích gì?

---

## 2. 4 SP chưa deploy lên PostgreSQL

**File:** `01_ARCHITECTURE/stored-procedures.md`

**SPs:**
- `sp_GetMessageQueueReport_Json`
- `sp_GetSMSQueueReport_Json`
- `sp_GetReduceWebSupport_Url_Json`
- `sp_GetIPAddressFromLog_PropertyChanged_Today_Json`

**Trạng thái:** Có trong hệ thống (entity class) nhưng chưa được chạy lên DB PostgreSQL.

**Cần trả lời:**
- [ ] Khi nào dự kiến deploy?
- [ ] Logic SQL của từng SP là gì?

## 3. SLA Targets (cần tạo)

**File cần tạo:** `05_PERFORMANCE/sla-targets.md`

**Vấn đề:** Chưa có SLA/response time targets chính thức.

**Cần quyết định:**
- [ ] API response time mục tiêu (ví dụ: P95 < 500ms)
- [ ] Uptime target (ví dụ: 99.9%)
- [ ] Slow API threshold (khi nào alert?)
- [ ] Background engine max latency

---

## 4. Runbook — Khắc phục sự cố

**File:** `06_OPERATIONS/runbook.md`

**Các mục cần bổ sung bước khắc phục chi tiết:**
- [ ] "MB Bank chuyển tiền thất bại" — cần thêm: retry logic, fallback thủ công, escalation path
- [ ] "Kafka/message tắc" — cần thêm: cách clear backlog, resume consumers, monitor lag

**Ghi chú:** Chủ dự án chưa có quy trình chi tiết cho 2 mục này.

---

*Cập nhật lần cuối: khi tạo file*
