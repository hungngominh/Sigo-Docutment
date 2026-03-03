# Documentation — Pending Questions

> File này lưu các câu hỏi cần chủ dự án trả lời để hoàn thiện documentation.
> Khi có câu trả lời, hãy update file tương ứng và xoá câu hỏi khỏi file này.

---

## 1. `sp_GetDataReportWebsite4BCT_Json` (SP #8)

**File:** `01_ARCHITECTURE/stored-procedures.md` (line ~675-693)

**Vấn đề:** SP hiện tại trả về hardcoded 0 cho tất cả 9 fields (TotalNewOrder, TotalCompletedOrder...).

**Đã xác nhận (2026-03-03):**
- [x] BCT = **Bộ Công Thương** — báo cáo dùng để khai báo thông tin sàn TMĐT theo quy định
- [x] SP trả về hardcoded 0 là **đúng theo thiết kế** — dữ liệu báo cáo BCT hiện tại dùng thể bào (placeholder), chưa cần implement logic thực

**Hành động:** Cập nhật `stored-procedures.md` ghi chú SP #8 là placeholder cho báo cáo Bộ Công Thương. ✅

---

## 2. 4 SP chưa deploy lên PostgreSQL

**File:** `01_ARCHITECTURE/stored-procedures.md`

**SPs:**
- `sp_GetMessageQueueReport_Json`
- `sp_GetSMSQueueReport_Json`
- `sp_GetReduceWebSupport_Url_Json`
- `sp_GetIPAddressFromLog_PropertyChanged_Today_Json`

**Đã xác nhận (2026-03-03):** Không cần quan tâm — các SP này không thuộc scope hiện tại.

**Hành động:** Đóng, không cần document thêm. ✅

---

## 3. SLA Targets

**File cần tạo:** `05_PERFORMANCE/sla-targets.md`

**Trạng thái:** ⏳ Chưa có SLA/response time targets chính thức. Chủ dự án chưa định nghĩa.

**Cần quyết định (khi sẵn sàng):**
- [ ] API response time mục tiêu (ví dụ: P95 < 500ms)
- [ ] Uptime target (ví dụ: 99.9%)
- [ ] Slow API threshold (khi nào alert?)
- [ ] Background engine max latency

---

## 4. Runbook — Khắc phục sự cố

**File:** `06_OPERATIONS/runbook.md`

**Trạng thái:** ⏳ Chưa có quy trình chi tiết. Chủ dự án chưa định nghĩa.

**Cần bổ sung (khi sẵn sàng):**
- [ ] "MB Bank chuyển tiền thất bại" — retry logic, fallback thủ công, escalation path
- [ ] "Kafka/message tắc" — cách clear backlog, resume consumers, monitor lag

---

*Cập nhật lần cuối: 2026-03-03*
