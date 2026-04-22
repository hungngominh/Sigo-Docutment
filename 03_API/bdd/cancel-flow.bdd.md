# BDD Test Scenarios — Cancel Flow (Gherkin)

> File này chứa BDD Gherkin format cho tất cả P0 cancel scenarios.
> Nguồn: [cancel-flow.test-scenarios.md](../cancel-flow.test-scenarios.md) | [cancel-flow.md](../../04_BUSINESS_FLOWS/cancel-flow.md)

---

## Summary Table — P0 Cancel Scenarios

| # | Scenario | Expected | Priority | Rule |
|---|----------|----------|----------|------|
| RCC-01 | Renter huỷ tại OWNER2CONFIRM | → CUSCANCEL, không hoàn tiền | P0 | BR-CANCEL-001 |
| RCC-02 | Renter huỷ tại CUS2DEPOSIT | → CUSCANCEL, không hoàn tiền | P0 | BR-CANCEL-001 |
| RCC-03 | Renter huỷ tại WAITING2CONFIRMDEPOSIT | → CUSCANCEL, cọc pending huỷ | P0 | BR-CANCEL-001 |
| RCC-04 | Renter huỷ trong 15 phút → hoàn 100% | RefundAmount = 100% | P0 | BR-CANCEL-005 |
| RCC-05 | Renter huỷ >15 phút, >7 ngày → hoàn 70% | RefundAmount = 70% | P0 | BR-CANCEL-005/006 |
| RCC-06 | Renter huỷ ≤7 ngày → mất cọc | RefundAmount = 0 | P0 | BR-CANCEL-006 |
| RCC-07 | Thiếu CancelReasonId | Status: 0, lỗi | P0 | BR-CANCEL-003 |
| RCC-08 | "another_reason" trống detail | Status: 0, lỗi | P0 | BR-CANCEL-004 |
| OCC-01 | Owner huỷ tại OWNER2CONFIRM | → OWNERCANCEL | P0 | BR-CANCEL-002 |
| OCC-03 | Owner huỷ trong 15 phút | Hoàn 100%, không phạt owner | P0 | BR-CANCEL-007 |
| OCC-04 | Owner huỷ sau 15 phút | Hoàn 100%, owner bị phạt | P0 | BR-CANCEL-007 |
| OCC-08 | User không phải owner | Status: 0 | P0 | — |
| OCC-09 | Owner thiếu CancelReasonId | Status: 0, lỗi | P0 | BR-CANCEL-003 |
| OCC-10 | Owner "another_reason" trống detail | Status: 0, lỗi | P0 | BR-CANCEL-004 |
| SCC-01 | Owner timeout 3h → SYSTEMCANCEL | Auto cancel | P0 | BR-CANCEL-010 |
| SCC-02 | Renter deposit timeout 3h → SYSTEMCANCEL | Auto cancel | P0 | BR-CANCEL-011 |
| SCC-03 | System cancel → giải phóng lịch | IsBooked = false | P0 | — |
| REF-01 | Hoàn 100% trong 15 phút | RefundAmount = 100% | P0 | BR-CANCEL-005 |
| REF-02 | Hoàn 70% (>15 phút, >7 ngày) | RefundAmount = 70% | P0 | BR-CANCEL-005/006 |
| REF-03 | Không hoàn (≤7 ngày) | RefundAmount = 0 | P0 | BR-CANCEL-006 |
| REF-04 | Owner huỷ → luôn hoàn 100% | RefundAmount = 100% | P0 | BR-CANCEL-007 |

---

## BDD Detail — Renter Cancel (RCC-*)

```gherkin
Feature: Renter huỷ đơn (RenterCancel)

  Background:
    Given user "test_renter_01" đã đăng nhập
    And API endpoint "POST /api/v1/Order_ListView_RentCar/RenterCancel"

  Scenario: RCC-01 — Renter huỷ tại OWNER2CONFIRM (chưa cọc)
    Given đơn hàng "ORD-CANCEL01" có status = "OWNER2CONFIRM"
    And đơn chưa có giao dịch cọc
    When renter gọi RenterCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "CUSCANCEL"
    And không có giao dịch hoàn tiền (chưa cọc)
    And ServiceItem_BookedRentalSchedule.IsBooked = false cho các ngày của đơn
    And Notification gửi đến Owner thông báo đơn bị huỷ

  Scenario: RCC-02 — Renter huỷ tại CUS2DEPOSIT (chưa cọc)
    Given đơn hàng "ORD-CANCEL02" có status = "CUS2DEPOSIT"
    And renter chưa thanh toán cọc
    When renter gọi RenterCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "CUSCANCEL"
    And không có giao dịch hoàn tiền

  Scenario: RCC-03 — Renter huỷ tại WAITING2CONFIRMDEPOSIT
    Given đơn hàng "ORD-CANCEL03" có status = "WAITING2CONFIRMDEPOSIT"
    And cọc đang chờ xác nhận
    When renter gọi RenterCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "CUSCANCEL"
    And giao dịch cọc pending được huỷ/rollback

  Scenario: RCC-04 — Renter huỷ trong FullRefundWithinMinutes → hoàn 100%
    Given đơn hàng "ORD-CANCEL04" có status = "WAITING2DEPARTURE"
    And đơn đã cọc DepositAmount = 1,200,000 VND
    And thời gian từ lúc cọc (DepositDoneAt) < 15 phút
    When renter gọi RenterCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "CUSCANCEL"
    And RefundAmount = 1,200,000 VND (100% DepositAmount)
    And Wallet balance renter tăng 1,200,000 VND

  Scenario: RCC-05 — Renter huỷ >15 phút, >7 ngày trước chuyến → hoàn 70%
    Given đơn hàng "ORD-CANCEL05" có status = "WAITING2DEPARTURE"
    And đơn đã cọc DepositAmount = 1,200,000 VND
    And thời gian từ lúc cọc > 15 phút
    And FromDate - now > 7 ngày
    When renter gọi RenterCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "CUSCANCEL"
    And RefundAmount = 840,000 VND (70% × 1,200,000)
    And Wallet balance renter tăng 840,000 VND

  Scenario: RCC-06 — Renter huỷ trong vòng 7 ngày trước chuyến → mất cọc
    Given đơn hàng "ORD-CANCEL06" có status = "WAITING2DEPARTURE"
    And đơn đã cọc DepositAmount = 1,200,000 VND
    And FromDate - now ≤ 7 ngày
    When renter gọi RenterCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "CUSCANCEL"
    And RefundAmount = 0 (mất toàn bộ cọc)
    And Wallet balance renter không thay đổi

  Scenario: RCC-07 — Huỷ thiếu CancelReasonId
    Given đơn hàng "ORD-CANCEL07" có status = "OWNER2CONFIRM"
    When renter gọi RenterCancel với CancelReasonId = null
    Then response StatusCode = 0
    And response Message chứa "chọn lý do huỷ"

  Scenario: RCC-08 — Huỷ với "another_reason" nhưng trống detail
    Given đơn hàng "ORD-CANCEL08" có status = "OWNER2CONFIRM"
    When renter gọi RenterCancel với:
      | Field              | Value          |
      | CancelReasonCode   | another_reason |
      | CancelReasonDetail | (empty)        |
    Then response StatusCode = 0
    And response Message chứa "nhập lý do huỷ chi tiết"
```

---

## BDD Detail — Owner Cancel (OCC-*)

```gherkin
Feature: Chủ xe huỷ đơn (OwnerCancel)

  Background:
    Given API endpoint "POST /api/v1/Order_ListView_RentCar/OwnerCancel"

  Scenario: OCC-01 — Owner huỷ tại OWNER2CONFIRM
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-OCANCEL01" có status = "OWNER2CONFIRM", chưa có cọc
    When owner gọi OwnerCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "OWNERCANCEL"
    And không có giao dịch hoàn tiền (chưa cọc)
    And Notification gửi đến Renter thông báo đơn bị huỷ bởi chủ xe

  Scenario: OCC-03 — Owner huỷ trong 15 phút → hoàn 100%, không phạt
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-OCANCEL03" có status = "WAITING2DEPARTURE", đã cọc
    And thời gian từ lúc cọc < 15 phút
    When owner gọi OwnerCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "OWNERCANCEL"
    And Renter nhận hoàn 100% DepositAmount
    And Owner không bị phạt (OwnerCancelPenalty = 0)

  Scenario: OCC-04 — Owner huỷ sau 15 phút → hoàn 100% cho renter, owner bị phạt
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-OCANCEL04" có status = "WAITING2DEPARTURE", đã cọc
    And thời gian từ lúc cọc > 15 phút
    When owner gọi OwnerCancel với CancelReasonId hợp lệ
    Then response StatusCode = 1
    And Order.StatusCode = "OWNERCANCEL"
    And Renter nhận hoàn 100% DepositAmount
    And Owner bị phạt OwnerCancelPenalty
    And User_Calculating.CancelCount tăng cho owner

  Scenario: OCC-08 — User không phải owner
    Given user "test_owner_02" đã đăng nhập (không phải owner của đơn)
    And đơn hàng "ORD-OCANCEL01" thuộc owner khác
    When user gọi OwnerCancel
    Then response StatusCode = 0
    And response Message chứa "không có quyền"

  Scenario: OCC-09 — Owner thiếu CancelReasonId
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-OCANCEL05" có status = "OWNER2CONFIRM"
    When owner gọi OwnerCancel với CancelReasonId = null
    Then response StatusCode = 0
    And response Message chứa "chọn lý do huỷ"

  Scenario: OCC-10 — Owner chọn "another_reason" nhưng trống detail
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-OCANCEL06" có status = "OWNER2CONFIRM"
    When owner gọi OwnerCancel với:
      | Field              | Value          |
      | CancelReasonCode   | another_reason |
      | CancelReasonDetail | (empty)        |
    Then response StatusCode = 0
    And response Message chứa "nhập lý do huỷ chi tiết"
```

---

## BDD Detail — System Auto-Cancel (SCC-*)

```gherkin
Feature: Hệ thống tự động huỷ (System Auto-Cancel)

  Scenario: SCC-01 — Owner không confirm trong 3 giờ business hours
    Given đơn hàng "ORD-SCANCEL01" có status = "OWNER2CONFIRM"
    And Owner2ConfirmEndTime ≤ now
    When AutoCancelOverTimeOrderEngine chạy cycle kiểm tra
    Then Order.StatusCode = "SYSTEMCANCEL"
    And Notification gửi cho Owner ("đơn bị huỷ do quá hạn xác nhận")
    And Notification gửi cho Renter ("đơn bị huỷ do chủ xe không xác nhận")
    And ServiceItem_BookedRentalSchedule.IsBooked = false cho các ngày

  Scenario: SCC-02 — Renter không cọc trong 3 giờ business hours
    Given đơn hàng "ORD-SCANCEL02" có status = "CUS2DEPOSIT"
    And Customer2DepositEndTime ≤ now
    When AutoCancelOverTimeOrderEngine chạy cycle kiểm tra
    Then Order.StatusCode = "SYSTEMCANCEL"
    And Notification gửi cho cả 2 bên
    And ServiceItem_BookedRentalSchedule.IsBooked = false

  Scenario: SCC-03 — System cancel giải phóng lịch booking
    Given đơn hàng "ORD-SCANCEL03" đang bị SYSTEMCANCEL
    And đơn có BookedRentalSchedule cho ngày 01/03 → 05/03
    When hệ thống xử lý cancel
    Then ServiceItem_BookedRentalSchedule.IsBooked = false cho ngày 01/03 → 05/03
    And xe có thể được đặt lại cho các ngày đã giải phóng
```

---

## BDD Detail — Refund Calculation (REF-*)

```gherkin
Feature: Tính tiền hoàn trả khi huỷ (Refund Calculation)

  Background:
    Given CalcRerturnDepositAmount() được gọi khi huỷ đơn có cọc

  Scenario: REF-01 — Hoàn 100% trong 15 phút
    Given renter huỷ tại WAITING2DEPARTURE
    And DepositAmount = 1,200,000 VND
    And thời gian từ DepositDoneAt < FullRefundWithinMinutes (15 phút)
    When CalcRerturnDepositAmount() tính toán
    Then RefundPercent = 100
    And RefundAmount = 1,200,000 VND
    And Renter wallet tăng 1,200,000 VND

  Scenario: REF-02 — Hoàn 70% (>15 phút, >7 ngày trước chuyến)
    Given renter huỷ tại WAITING2DEPARTURE
    And DepositAmount = 1,200,000 VND
    And thời gian từ DepositDoneAt > 15 phút
    And FromDate - now > NoRefundGreaterThanDays (7 ngày)
    When CalcRerturnDepositAmount() tính toán
    Then RefundPercent = 70
    And RefundAmount = 840,000 VND (1,200,000 × 70%)
    And Renter wallet tăng 840,000 VND

  Scenario: REF-03 — Không hoàn (≤7 ngày trước chuyến)
    Given renter huỷ tại WAITING2DEPARTURE
    And DepositAmount = 1,200,000 VND
    And FromDate - now ≤ 7 ngày
    When CalcRerturnDepositAmount() tính toán
    Then RefundPercent = 0
    And RefundAmount = 0
    And Renter mất toàn bộ cọc 1,200,000 VND

  Scenario: REF-04 — Owner huỷ → luôn hoàn 100% cho renter
    Given owner huỷ đơn có cọc DepositAmount = 1,200,000 VND
    And bất kể thời gian huỷ
    When CalcRerturnDepositAmount() tính toán cho owner cancel
    Then RefundAmount_Renter = 1,200,000 VND (100%)
    And Owner chịu OwnerCancelPenalty
```

---

*Tổng cộng: **21 P0 scenarios** ở BDD Gherkin format cho Cancel Flow (RCC-01→RCC-08, OCC-01→OCC-10, SCC-01→SCC-03, REF-01→REF-04).*

> **Lưu ý:** P1/P2 scenarios (RCC-09, RCC-10, OCC-02, OCC-05→OCC-07, SCC-04, SCC-05, REF-05→REF-08, SE-*) **không có BDD format** — đây là intentional.
> P1/P2 scenarios được document dạng bảng tóm tắt trong [cancel-flow.test-scenarios.md](../cancel-flow.test-scenarios.md).
> Khi cần BDD cho P1/P2, tham khảo format Gherkin trong file BDD này và apply cùng pattern.

---

*Xem thêm: [booking-flow.bdd.md](./booking-flow.bdd.md) | [order-lifecycle.bdd.md](./order-lifecycle.bdd.md)*
