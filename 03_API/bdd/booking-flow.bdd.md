# BDD Test Scenarios — Booking Flow (Gherkin)

> File này chứa BDD Gherkin format cho tất cả P0 booking scenarios.
> Nguồn: [rental-service.test-scenarios.md](../rental-service.test-scenarios.md) | [booking-flow.md](../../04_BUSINESS_FLOWS/booking-flow.md)

---

## Summary Table — P0 Booking Scenarios

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| B-01 | Happy path — đặt xe | Tất cả input hợp lệ | Status: 1, OrderNumber returned | P0 | — |
| B-02 | Thiếu FromDate | FromDate = null | Status: 0, msg lỗi | P0 | BR-BOOK-001 |
| B-03 | Thiếu ToDate | ToDate = null | Status: 0, msg lỗi | P0 | BR-BOOK-001 |
| B-04 | FromDate > ToDate | FromDate > ToDate | Status: 0 | P0 | BR-BOOK-002 |
| B-05 | FromDate == ToDate | Cùng ngày | Status: 0 | P0 | BR-BOOK-002 |
| B-06 | Ngày trong quá khứ | FromDate past | Status: 0 | P0 | BR-BOOK-003 |
| B-07 | Xe bị tạm ngưng | IsSuspended = true | Status: 0 | P0 | BR-BOOK-004 |
| B-08 | Xe chưa duyệt | IsApproved = false | Status: 0 | P0 | BR-BOOK-004 |
| B-09 | Ngày trùng booking khác | Overlap confirmed | Status: 0 | P0 | BR-BOOK-005 |
| B-10 | Ngày trùng busy date | DateBusy overlap | Status: 0 | P0 | BR-BOOK-006 |
| B-12 | Số ngày < minimum | MinimumDays violation | Status: 0 | P0 | BR-BOOK-008 |
| B-13 | Chưa đăng nhập | No token | Status: 0 | P0 | BR-BOOK-009 |
| B-14 | Thuê xe của chính mình | Renter = Owner | Status: 0 | P0 | BR-BOOK-010 |
| OC-01 | Owner confirm thành công | Valid owner + status | Status: 1, → CUS2DEPOSIT | P0 | — |
| OC-02 | Không phải owner | User ≠ owner | Status: 0 | P0 | — |
| OC-03 | Quá thời gian confirm | Timeout expired | Status: 0 | P0 | BR-BOOK-013 |
| OC-04 | Đơn đã bị huỷ | Status = CUSCANCEL | Status: 0 | P0 | — |
| OP-01 | Thanh toán cọc thành công | Status = CUS2DEPOSIT | Status: 1, → WAITING2DEPARTURE | P0 | — |
| OP-02 | Hết hạn cọc | Timeout expired | Status: 0 | P0 | BR-BOOK-014 |
| OP-03 | Sai status | Status invalid | Status: 0 | P0 | — |
| OP-04 | Không phải renter | User ≠ renter | Status: 0 | P0 | — |

---

## BDD Detail — Booking (B-*)

```gherkin
Feature: Đặt xe (Booking)

  Background:
    Given user "test_renter_01" đã đăng nhập
    And API endpoint "POST /api/v1/RentalService/Booking"

  Scenario: B-01 — Renter đặt xe thành công với thông tin hợp lệ
    Given xe "vehicle_active_01" đang active và đã duyệt (IsApproved = true)
    And FromDate = "2026-03-10 08:00" và ToDate = "2026-03-12 20:00"
    And không có booking nào trùng khoảng ngày trên
    And renter đã gọi UpdateBookingInfo trước đó
    When renter gọi POST /api/v1/RentalService/Booking
    Then response StatusCode = 1
    And response Data chứa OrderNumber dạng "ORD-XXXXXX"
    And Order.StatusCode = "OWNER2CONFIRM"
    And ServiceItem_BookedRentalSchedule.IsBooked = true cho ngày 10/03 → 12/03
    And Notification gửi đến Owner thông báo có đơn mới
    And Owner2ConfirmEndTime được set (tính theo business hours)

  Scenario: B-02 — Thiếu FromDate
    Given xe "vehicle_active_01" đang active
    When renter gọi Booking với FromDate = null, ToDate = "2026-03-12 20:00"
    Then response StatusCode = 0
    And response Message = "Vui lòng chọn Ngày đi và Ngày về"

  Scenario: B-03 — Thiếu ToDate
    Given xe "vehicle_active_01" đang active
    When renter gọi Booking với FromDate = "2026-03-10 08:00", ToDate = null
    Then response StatusCode = 0
    And response Message = "Vui lòng chọn Ngày đi và Ngày về"

  Scenario: B-04 — FromDate lớn hơn ToDate
    Given xe "vehicle_active_01" đang active
    When renter gọi Booking với FromDate = "2026-03-15 08:00", ToDate = "2026-03-10 20:00"
    Then response StatusCode = 0
    And response Message chứa "không được lớn hơn"

  Scenario: B-05 — FromDate bằng ToDate (cùng ngày giờ)
    Given xe "vehicle_active_01" đang active
    When renter gọi Booking với FromDate = "2026-03-10 08:00", ToDate = "2026-03-10 08:00"
    Then response StatusCode = 0
    And response Message chứa "không được trùng"

  Scenario: B-06 — Ngày trong quá khứ
    Given xe "vehicle_active_01" đang active
    When renter gọi Booking với FromDate = "2020-01-01 08:00", ToDate = "2020-01-03 20:00"
    Then response StatusCode = 0
    And response Message chứa "quá khứ"

  Scenario: B-07 — Xe bị tạm ngưng
    Given xe "vehicle_suspended" có IsSuspended = true
    When renter gọi Booking với thông tin hợp lệ
    Then response StatusCode = 0
    And response Message chứa "tạm ngưng hoặc đã bị xóa"

  Scenario: B-08 — Xe chưa được duyệt
    Given xe "vehicle_not_approved" có IsApproved = false
    When renter gọi Booking với thông tin hợp lệ
    Then response StatusCode = 0
    And response Message = "Không tìm thấy xe"

  Scenario: B-09 — Ngày trùng với booking đã confirmed
    Given xe "vehicle_active_01" đã có booking confirmed từ 10/03 đến 12/03
    When renter gọi Booking với FromDate = "2026-03-11", ToDate = "2026-03-13"
    Then response StatusCode = 0
    And response Message chứa "đã bận"

  Scenario: B-10 — Ngày trùng busy date do owner đánh dấu
    Given xe "vehicle_active_02" có DateBusyRentalSchedule ngày 15/03
    When renter gọi Booking với FromDate = "2026-03-14", ToDate = "2026-03-16"
    Then response StatusCode = 0
    And response Message chứa "bận vào"

  Scenario: B-12 — Số ngày thuê nhỏ hơn minimum
    Given xe "vehicle_active_02" có MinimumRequiredRentalDays = 3
    When renter gọi Booking với FromDate = "2026-03-10 08:00", ToDate = "2026-03-11 20:00" (1 ngày)
    Then response StatusCode = 0
    And response Message chứa "tối thiểu là"

  Scenario: B-13 — Chưa đăng nhập
    Given request không có Authorization header
    When gọi POST /api/v1/RentalService/Booking
    Then response StatusCode = 0
    And response Message chứa "đăng nhập"

  Scenario: B-14 — Thuê xe của chính mình
    Given user "test_owner_renter" vừa là owner vừa là renter
    And xe thuộc sở hữu của "test_owner_renter"
    When user gọi Booking cho xe của chính mình
    Then response StatusCode = 0
    And response Message chứa "không thể thuê dịch vụ cho thuê của chính bạn"
```

---

## BDD Detail — OrderConfirm (OC-*)

```gherkin
Feature: Chủ xe xác nhận đơn (OrderConfirm)

  Background:
    Given API endpoint "POST /api/v1/RentalService/OrderConfirm"

  Scenario: OC-01 — Owner xác nhận đơn thành công
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-TEST01" có status = "OWNER2CONFIRM"
    And Owner2ConfirmEndTime > now (chưa quá hạn)
    And đơn hàng thuộc về "test_owner_01"
    When owner gọi OrderConfirm với OrderNumber = "ORD-TEST01"
    Then response StatusCode = 1
    And Order.StatusCode = "CUS2DEPOSIT"
    And Customer2DepositEndTime được set (3 giờ business hours)
    And Notification gửi đến Renter nhắc đặt cọc

  Scenario: OC-02 — User không phải owner
    Given user "test_renter_01" đã đăng nhập (không phải owner)
    And đơn hàng "ORD-TEST01" thuộc owner khác
    When user gọi OrderConfirm
    Then response StatusCode = 0
    And response Message chứa "không có quyền"

  Scenario: OC-03 — Quá thời gian confirm
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-TEST02" có Owner2ConfirmEndTime < now (đã quá hạn)
    When owner gọi OrderConfirm
    Then response StatusCode = 0
    And response Message chứa "kết thúc"

  Scenario: OC-04 — Đơn đã bị huỷ trước đó
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-TEST03" có status = "CUSCANCEL"
    When owner gọi OrderConfirm
    Then response StatusCode = 0
    And response Message chứa "bị thay đổi"
```

---

## BDD Detail — OrderPay (OP-*)

```gherkin
Feature: Renter thanh toán cọc (OrderPay)

  Background:
    Given API endpoint "POST /api/v1/RentalService/OrderPay"

  Scenario: OP-01 — Thanh toán cọc thành công
    Given user "test_renter_01" đã đăng nhập
    And đơn hàng "ORD-TEST01" có status = "CUS2DEPOSIT"
    And Customer2DepositEndTime > now
    And ví renter có đủ số dư >= DepositAmount
    When renter gọi OrderPay với OrderNumber = "ORD-TEST01"
    Then response StatusCode = 1
    And Order.StatusCode = "WAITING2DEPARTURE"
    And Order.DepositDoneAt được set = now
    And Wallet balance giảm đúng DepositAmount
    And Notification gửi đến Owner thông báo renter đã cọc

  Scenario: OP-02 — Hết hạn đặt cọc
    Given user "test_renter_01" đã đăng nhập
    And đơn hàng "ORD-TEST04" có Customer2DepositEndTime < now
    When renter gọi OrderPay
    Then response StatusCode = 0
    And response Message chứa "hết" hoặc "kết thúc"

  Scenario: OP-03 — Sai trạng thái đơn
    Given user "test_renter_01" đã đăng nhập
    And đơn hàng "ORD-TEST05" có status = "OWNER2CONFIRM" (chưa được confirm)
    When renter gọi OrderPay
    Then response StatusCode = 0
    And response Message chứa "bị thay đổi"

  Scenario: OP-04 — User không phải renter của đơn
    Given user "test_renter_02" đã đăng nhập (không phải renter của đơn)
    And đơn hàng "ORD-TEST01" thuộc renter khác
    When user gọi OrderPay
    Then response StatusCode = 0
    And response Message chứa "không có quyền"
```

---

*Tổng cộng: **21 P0 scenarios** ở BDD Gherkin format cho Booking Flow (B-01→B-14, OC-01→OC-04, OP-01→OP-04).*

> **Lưu ý:** P1/P2 scenarios (B-11, B-15→B-19, OC-05) **không có BDD format** — đây là intentional.
> P1/P2 scenarios được document dạng bảng tóm tắt trong [rental-service.test-scenarios.md](../rental-service.test-scenarios.md).
> Khi cần BDD cho P1/P2, tham khảo format template trong Appendix A của file trên.

---

*Xem thêm: [cancel-flow.bdd.md](./cancel-flow.bdd.md) | [order-lifecycle.bdd.md](./order-lifecycle.bdd.md)*
