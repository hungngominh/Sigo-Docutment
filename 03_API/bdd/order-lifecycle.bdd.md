# BDD Test Scenarios — Order Lifecycle (Gherkin)

> File này chứa BDD Gherkin format cho tất cả P0 order lifecycle scenarios.
> Bao gồm: Order Begin (handshake), Order End + Auto-Complete, EWallet Deposit/Payment, Withdrawal, Financial Calculations.
> Nguồn: [ewallet-order-lifecycle.test-scenarios.md](../ewallet-order-lifecycle.test-scenarios.md)

---

## Summary Table — P0 Lifecycle Scenarios

| # | Scenario | Expected | Priority | Rule |
|---|----------|----------|----------|------|
| OBG-01 | Owner xác nhận giao xe | DeliverVehicleTime set | P0 | BR-BOOK-015 |
| OBG-02 | Renter xác nhận nhận xe | ReceiveVehicleTime set | P0 | BR-BOOK-015 |
| OBG-03 | Cả hai confirm → INTHETRIP | Status → INTHETRIP | P0 | BR-BOOK-015 |
| OBG-04 | Chỉ 1 bên → status không đổi | Status = WAITING2DEPARTURE | P0 | BR-BOOK-015 |
| OBG-05 | User không liên quan → lỗi | Status: 0 | P0 | — |
| OBG-06 | Begin ở status sai → lỗi | Status: 0 | P0 | — |
| OEN-01 | Kết thúc chuyến thành công | Status → DONE | P0 | — |
| OEN-02 | Auto-complete +60 phút | Status → DONE (auto) | P0 | BR-CANCEL-012 |
| OEN-04 | End ở status sai → lỗi | Status: 0 | P0 | — |
| EDP-01 | Thanh toán cọc thành công | Wallet giảm, → WAITING2DEPARTURE | P0 | — |
| EDP-02 | Ví không đủ số dư | Status: 0 | P0 | — |
| EDP-05 | Cọc 30% default | DepositAmount = CEILING(30%) | P0 | pricing Section 10 |
| EDP-08 | Cọc hết hạn | Status: 0 | P0 | BR-BOOK-014 |
| EWD-01 | Rút tiền thành công | NEW → SUCCESS | P0 | — |
| EWD-02 | Rút < 2,000 VND | Auto-cancel | P0 | — |
| EWD-06 | Bank chưa verified | Status: 0 | P0 | — |
| EWD-08 | Luồng trạng thái rút tiền | NEW → PROCESSING → SUCCESS | P0 | — |
| FIN-01 | Tính PlanProfitAmount | 598,000 VND | P0 | pricing Section 11 |
| FIN-02 | Tính OwnerRemainAmount | 399,350 VND | P0 | pricing Section 12 |
| FIN-06 | TransferMoney chuyển đúng | Wallet tăng đúng số | P0 | pricing Section 12 |

---

## BDD Detail — Order Begin (OBG-*)

```gherkin
Feature: Giao nhận xe (Order Begin — Two-Sided Handshake)

  Background:
    Given API endpoint "POST /api/v1/Order_ListView_RentCar/Begin"

  Scenario: OBG-01 — Owner xác nhận giao xe
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-BEGIN01" có status = "WAITING2DEPARTURE"
    And đơn thuộc về "test_owner_01" (owner)
    When owner gọi Begin với OrderNumber = "ORD-BEGIN01"
    Then response StatusCode = 1
    And DeliverVehicleByUsername = "test_owner_01"
    And DeliverVehicleTime = now
    And Order.StatusCode vẫn = "WAITING2DEPARTURE" (chờ renter)

  Scenario: OBG-02 — Renter xác nhận nhận xe
    Given user "test_renter_01" đã đăng nhập
    And đơn hàng "ORD-BEGIN01" có status = "WAITING2DEPARTURE"
    And đơn thuộc về "test_renter_01" (renter)
    When renter gọi Begin với OrderNumber = "ORD-BEGIN01"
    Then response StatusCode = 1
    And ReceiveVehicleByUsername = "test_renter_01"
    And ReceiveVehicleTime = now
    And Order.StatusCode vẫn = "WAITING2DEPARTURE" (chờ owner)

  Scenario: OBG-03 — Cả hai bên đã xác nhận → INTHETRIP
    Given đơn hàng "ORD-BEGIN02" có status = "WAITING2DEPARTURE"
    And owner đã gọi Begin (DeliverVehicleByUsername != null)
    When renter gọi Begin (bên còn lại)
    Then response StatusCode = 1
    And Order.StatusCode = "INTHETRIP"
    And Order.BeginDate được set = now
    And Notification gửi cho cả Owner và Renter "Chuyến đi đã bắt đầu"

  Scenario: OBG-04 — Chỉ 1 bên xác nhận → status không đổi
    Given đơn hàng "ORD-BEGIN03" có status = "WAITING2DEPARTURE"
    And owner đã gọi Begin
    And renter chưa gọi Begin
    When kiểm tra order status
    Then Order.StatusCode = "WAITING2DEPARTURE"
    And DeliverVehicleByUsername có giá trị
    And ReceiveVehicleByUsername = null

  Scenario: OBG-05 — User không liên quan gọi Begin
    Given user "test_renter_02" đã đăng nhập (không phải owner hoặc renter của đơn)
    And đơn hàng "ORD-BEGIN01" thuộc user khác
    When user gọi Begin
    Then response StatusCode = 0
    And response Message chứa "không có quyền"

  Scenario: OBG-06 — Begin ở trạng thái sai
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-BEGIN04" có status = "CUS2DEPOSIT"
    When owner gọi Begin
    Then response StatusCode = 0
    And response Message chứa "bị thay đổi"
```

---

## BDD Detail — Order End + Auto-Complete (OEN-*)

```gherkin
Feature: Kết thúc chuyến (Order End + Auto-Complete)

  Background:
    Given API endpoint "POST /api/v1/Order_ListView_RentCar/End"

  Scenario: OEN-01 — Kết thúc chuyến thủ công thành công
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-END01" có status = "INTHETRIP"
    When owner gọi End với OrderNumber = "ORD-END01"
    Then response StatusCode = 1
    And Order.StatusCode = "DONE"
    And Order.EndDate = now
    And TransferMoneyEngine được triggered
    And Notification gửi nhắc cả 2 bên đánh giá

  Scenario: OEN-02 — Auto-complete sau ToDate + 60 phút
    Given đơn hàng "ORD-END02" có status = "INTHETRIP"
    And Order.ToDate = "2026-03-12 20:00"
    And now > "2026-03-12 21:00" (quá 60 phút)
    When AutoCompleteOrderEngine chạy cycle
    Then Order.StatusCode = "DONE"
    And Order.IsSystemAutoDone = true
    And Order.EndDate được set tự động
    And TransferMoneyEngine triggered → chuyển tiền cho owner
    And Notification gửi completion cho cả 2 bên

  Scenario: OEN-04 — End ở trạng thái sai
    Given user "test_owner_01" đã đăng nhập
    And đơn hàng "ORD-END04" có status = "WAITING2DEPARTURE"
    When owner gọi End
    Then response StatusCode = 0
    And response Message chứa "bị thay đổi"
```

---

## BDD Detail — EWallet Deposit/Payment (EDP-*)

```gherkin
Feature: Thanh toán cọc qua ví (EWallet Deposit)

  Background:
    Given API endpoint "POST /api/v1/RentalService/OrderPay"

  Scenario: EDP-01 — Renter thanh toán cọc thành công qua ví Sigo
    Given user "test_renter_01" đã đăng nhập
    And đơn hàng "ORD-PAY01" có status = "CUS2DEPOSIT"
    And DepositAmount = 1,196,850 VND
    And ví renter có balance ≥ 1,196,850 VND
    When renter gọi OrderPay với Amount = 1,196,850
    Then response StatusCode = 1
    And Wallet balance giảm 1,196,850 VND
    And Order.StatusCode = "WAITING2DEPARTURE"
    And Order.DepositDoneAt = now

  Scenario: EDP-02 — Thanh toán khi ví không đủ số dư
    Given user "test_renter_02" đã đăng nhập
    And đơn hàng "ORD-PAY02" có status = "CUS2DEPOSIT"
    And DepositAmount = 1,196,850 VND
    And ví renter có balance = 500,000 VND (không đủ)
    When renter gọi OrderPay
    Then response StatusCode = 0
    And response Message chứa "không đủ số dư"

  Scenario: EDP-05 — Tính cọc 30% default
    Given đơn hàng "ORD-PAY05" có TotalPrice = 3,989,500 VND
    And DepositPercent = 30 (default), DepositAmount = 0 (không fixed)
    When hệ thống tính DepositAmount
    Then DepositAmount = CEILING(3,989,500 × 30 / 100) = 1,196,850 VND

  Scenario: EDP-08 — Thanh toán sau khi hết hạn
    Given user "test_renter_01" đã đăng nhập
    And đơn hàng "ORD-PAY08" có Customer2DepositEndTime < now
    When renter gọi OrderPay
    Then response StatusCode = 0
    And response Message chứa "hết hạn" hoặc "kết thúc"
```

---

## BDD Detail — EWallet Withdrawal (EWD-*)

```gherkin
Feature: Rút tiền về ngân hàng (EWallet Withdrawal)

  Background:
    Given API endpoint "POST /api/v1/WalletAction/Withdraw"

  Scenario: EWD-01 — Owner rút tiền thành công
    Given user "test_owner_01" đã đăng nhập
    And ví có balance = 2,000,000 VND
    And tài khoản ngân hàng đã IsVerified = true
    When owner gọi WithdrawRequest với Amount = 1,000,000
    Then WithdrawRequest được tạo với Status = "NEW"
    And AutoWithdrawEngine xử lý → Status = "PROCESSING"
    And Bank xác nhận → Status = "SUCCESS"
    And Wallet balance giảm 1,000,000 VND
    And WalletTransaction ghi nhận

  Scenario: EWD-02 — Rút < 2,000 VND bị auto-cancel
    Given user "test_owner_01" đã đăng nhập
    And ví có balance = 1,500 VND
    When owner gọi WithdrawRequest với Amount = 1,500
    Then WithdrawRequest bị auto-cancel với IsIgnore = true
    And Message: "Số tiền muốn rút phải lớn hơn hoặc bằng 2000"

  Scenario: EWD-06 — Rút khi bank chưa verified
    Given user "test_owner_01" đã đăng nhập
    And tài khoản ngân hàng IsVerified = false
    When owner gọi WithdrawRequest
    Then response StatusCode = 0
    And Message chứa yêu cầu xác minh tài khoản ngân hàng

  Scenario: EWD-08 — Luồng trạng thái đầy đủ: NEW → PROCESSING → SUCCESS
    Given WithdrawRequest đã tạo với Status = "NEW"
    When AutoWithdrawEngine chạy:
      | Step | Action | Status |
      | 1 | Engine pick up request | NEW → PROCESSING |
      | 2 | Bank API call (MBBank TransferFund) | PROCESSING |
      | 3 | Bank confirm thành công | PROCESSING → SUCCESS |
    Then WalletTransaction type = Transfer
    And Amount = WithdrawAmount
    And Push notification gửi "Rút tiền thành công"
```

---

## BDD Detail — Financial Calculations (FIN-*)

```gherkin
Feature: Tính toán tài chính (Financial Calculations)

  Scenario: FIN-01 — Tính hoa hồng platform (PlanProfitAmount)
    Given Order hoàn tất (DONE)
    And SubTotal = 3,990,000 VND
    And CompletionFeePercentage = 20%
    And ServiceFee = 0
    And DiscountMoney = 200,000 VND
    And DeliveryFee = 0
    When PlanProfitAmount được tính:
      PlanProfitAmount = (SubTotal × CompletionFee / 100) + ServiceFee - DiscountMoney + (DeliveryFee × CompletionFee / 100)
    Then PlanProfitAmount = (3,990,000 × 20/100) + 0 - 200,000 + 0 = 598,000 VND

  Scenario: FIN-02 — Tính tiền chủ xe nhận (OwnerRemainAmount)
    Given DepositAmount = 1,196,850 VND
    And PlanProfitAmount = 598,000 VND
    And InsuranceFee = 199,500 VND
    When OwnerRemainAmount được tính:
      OwnerRemainAmount = DepositAmount - PlanProfitAmount - InsuranceFee
    Then OwnerRemainAmount = 1,196,850 - 598,000 - 199,500 = 399,350 VND

  Scenario: FIN-06 — TransferMoneyEngine chuyển đúng số tiền vào ví owner
    Given Order.StatusCode = "DONE"
    And Order_Finance.OwnerRemainAmount = 399,350 VND (sau thuế)
    When TransferMoneyEngine xử lý đơn
    Then Owner wallet balance tăng 399,350 VND
    And WalletTransaction.Type = "Transfer"
    And WalletTransaction.Amount = 399,350 VND
    And WalletTransaction.Description chứa OrderNumber
```

---

*Tổng cộng: **20 P0 scenarios** ở BDD Gherkin format cho Order Lifecycle (OBG-01→OBG-06, OEN-01→OEN-04, EDP-01→EDP-08, EWD-01→EWD-08, FIN-01→FIN-06).*

> **Lưu ý:** P1/P2 scenarios (EDP-03, EDP-04, EDP-06, EDP-07, OEN-03, OEN-05, OEN-07→OEN-09, EWD-03→EWD-05, EWD-07, FIN-03→FIN-05) **không có BDD format** — đây là intentional.
> P1/P2 scenarios được document dạng bảng tóm tắt trong [ewallet-order-lifecycle.test-scenarios.md](../ewallet-order-lifecycle.test-scenarios.md).
> Khi cần BDD cho P1/P2, tham khảo format Gherkin trong file BDD này và apply cùng pattern.

---

*Xem thêm: [booking-flow.bdd.md](./booking-flow.bdd.md) | [cancel-flow.bdd.md](./cancel-flow.bdd.md)*
