# Test Scenarios — EWallet & Order Lifecycle

> File này chứa test scenarios cho EWallet deposit/payment, order begin/end lifecycle, withdrawal, và financial calculations.
> Mỗi scenario ánh xạ đến business rules trong [pricing-calculation.md](../04_BUSINESS_FLOWS/pricing-calculation.md), [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md), và [ewallet-withdraw.md](../04_BUSINESS_FLOWS/ewallet-withdraw.md).

---

## 1. EWallet Deposit/Payment — Thanh toán cọc qua ví

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|-------------|-------|----------|----------|------|
| EDP-01 | Happy path — renter thanh toán cọc qua ví Sigo | Order status = CUS2DEPOSIT, ví có đủ số dư | OrderNumber hợp lệ, Amount = DepositAmount | Wallet balance giảm đúng DepositAmount, order status → WAITING2DEPARTURE, DepositDoneAt được set | P0 | — |
| EDP-02 | Thanh toán cọc khi ví không đủ số dư | Order status = CUS2DEPOSIT, ví balance < DepositAmount | OrderNumber hợp lệ, Amount = DepositAmount | Status: 0, msg chứa "không đủ số dư" hoặc "insufficient balance" | P0 | — |
| EDP-03 | Ví tự động tạo khi thanh toán lần đầu | User chưa có Wallet trong hệ thống | Gọi FundingWallet lần đầu | Wallet auto-created qua `WalletHelper.GetUserWalletPayment`, WalletType = "Ví chính" | P1 | — |
| EDP-04 | Thanh toán cọc với DepositAmount cố định (fixed) | RentalServiceCategorySettingJsonModel.DepositAmount > 0 | OrderNumber, Amount = DepositAmount (fixed value) | Deposit đúng giá trị cố định, không tính theo %, order → WAITING2DEPARTURE | P1 | pricing-calculation.md Section 10 |
| EDP-05 | Thanh toán cọc theo phần trăm (30% default) | DepositAmount = 0, DepositPercent = 30 | OrderNumber, Amount = Math.Ceiling(TotalPrice × 30 / 100) | DepositAmount = CEILING(TotalPrice × 30%), order → WAITING2DEPARTURE | P0 | pricing-calculation.md Section 10 |
| EDP-06 | Nạp tiền vào ví qua MB Bank transfer | Ví đã tồn tại, user có tài khoản MB Bank | FundingRequest với BankTransactionRef = "MBBANK-..." | WalletAction tạo với Status = Pending, sau RunJob → balance tăng | P1 | — |
| EDP-07 | Nạp tiền vào ví qua VietQR | Ví đã tồn tại | FundingRequest qua VietQR flow | WalletAction tạo, chờ admin confirm hoặc auto-detect | P2 | — |
| EDP-08 | Thanh toán cọc sau khi hết hạn Customer2DepositEndTime | Order status = CUS2DEPOSIT, Customer2DepositEndTime < now | OrderNumber hợp lệ | Status: 0, msg chứa "hết hạn" hoặc "kết thúc", order có thể đã SYSTEMCANCEL | P0 | BR-BOOK-014 |

---

## 2. Order Begin — Two-Sided Handshake (Giao nhận xe)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|-------------|-------|----------|----------|------|
| OBG-01 | Owner xác nhận giao xe | Order status = WAITING2DEPARTURE, user = owner | OrderNumber hợp lệ | DeliverVehicleByUsername = owner username, DeliverVehicleTime = now, status vẫn WAITING2DEPARTURE (chờ renter) | P0 | BR-BOOK-015 |
| OBG-02 | Renter xác nhận nhận xe | Order status = WAITING2DEPARTURE, user = renter | OrderNumber hợp lệ | ReceiveVehicleByUsername = renter username, ReceiveVehicleTime = now, status vẫn WAITING2DEPARTURE (chờ owner) | P0 | BR-BOOK-015 |
| OBG-03 | Cả hai bên đã xác nhận → INTHETRIP | Owner đã DeliverVehicle, renter đã ReceiveVehicle | Bên còn lại gọi Begin | Status → INTHETRIP, BeginDate được set, notification gửi cho cả hai | P0 | BR-BOOK-015 |
| OBG-04 | Chỉ một bên xác nhận → status không đổi | Owner đã Begin, renter chưa Begin | Kiểm tra order status | Status vẫn = WAITING2DEPARTURE, DeliverVehicleByUsername có giá trị, ReceiveVehicleByUsername = null | P0 | BR-BOOK-015 |
| OBG-05 | User không liên quan gọi Begin → lỗi | User không phải owner hoặc renter của đơn | OrderNumber hợp lệ, user = stranger | Status: 0, msg chứa "không có quyền" | P0 | — |
| OBG-06 | Begin ở trạng thái sai → lỗi | Order status = CUS2DEPOSIT hoặc OWNER2CONFIRM | OrderNumber hợp lệ | Status: 0, msg chứa "bị thay đổi" hoặc "trạng thái không hợp lệ" | P0 | — |

---

## 3. Order End + Auto-Complete (Kết thúc chuyến + Tự động hoàn tất)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|-------------|-------|----------|----------|------|
| OEN-01 | Happy path — kết thúc chuyến thủ công | Order status = INTHETRIP, user = owner hoặc renter | OrderNumber hợp lệ | Status → DONE, EndDate = now, TransferMoneyEngine triggered, notification gửi nhắc đánh giá | P0 | — |
| OEN-02 | Auto-complete sau ToDate + 60 phút | Order status = INTHETRIP, now > ToDate + 60 phút | AutoCompleteOrderEngine cycle chạy | Status → DONE (auto), EndDate set, TransferMoney flow triggered, completion notification gửi | P0 | BR-CANCEL-012 |
| OEN-03 | Notification cảnh báo trước auto-complete | Order status = INTHETRIP, ToDate <= now < ToDate + 60 phút | AutoCompleteOrderEngine kiểm tra | Notification nhắc Owner + Renter kết thúc chuyến, status vẫn INTHETRIP | P1 | — |
| OEN-04 | End ở trạng thái sai → lỗi | Order status != INTHETRIP (ví dụ WAITING2DEPARTURE) | OrderNumber hợp lệ | Status: 0, msg chứa "bị thay đổi" | P0 | — |
| OEN-05 | Auto-complete xử lý qua nửa đêm (VN time 23:50) | Order ToDate = 23:50 VN time, auto-complete lúc 00:50 ngày hôm sau | AutoCompleteOrderEngine chạy sau midnight | Auto-complete chạy đúng, không bị lỗi timezone, EndDate = 00:50 ngày mới | P1 | — |
| OEN-06 | End triggers TransferMoneyEngine → owner wallet funded | Order DONE, Order_Finance có OwnerRemainAmount > 0 | TransferMoneyEngine xử lý | Owner wallet balance tăng đúng OwnerRemainAmount (sau thuế), WalletTransaction ghi nhận | P0 | pricing-calculation.md Section 12 |
| OEN-07 | Trả xe sớm hơn ToDate | Order status = INTHETRIP, now < ToDate (ví dụ trả trước 1 ngày) | User gọi End sớm | Status → DONE, EndDate = now (< ToDate), TotalPrice và Commission có thể không tính lại (verify hành vi: tính theo booking hay thực tế?) | P1 | — |
| OEN-08 | Trả xe muộn + phí phát sinh | Order status = INTHETRIP, now > ToDate nhưng < ToDate + 60 phút | User gọi End muộn | Status → DONE, EndDate = now, kiểm tra có tính thêm LateHourReturnFee hoặc ExtraSurcharge không | P1 | — |
| OEN-09 | Phí extra surcharge khi kết thúc | Order status = INTHETRIP, có phí phát sinh (nhiên liệu, vệ sinh, hư hỏng) | User gọi End kèm ExtraSurchargeAmount > 0 | Status → DONE, Order_Finance.ExtraSurchargeAmount được ghi nhận, TotalPrice cuối cùng tăng thêm surcharge | P1 | — |

---

## 4. EWallet Withdrawal — Rút tiền về ngân hàng

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|-------------|-------|----------|----------|------|
| EWD-01 | Happy path — owner rút tiền về ngân hàng | Ví có đủ số dư, tài khoản ngân hàng IsVerified = true | WithdrawRequest với Amount hợp lệ | WithdrawRequest tạo status = NEW, AutoWithdrawEngine xử lý → PROCESSING → SUCCESS, tiền về tài khoản ngân hàng | P0 | ewallet-withdraw.md |
| EWD-02 | Rút số tiền < 2,000 VND → auto-cancel | Ví có balance > 0 nhưng < 2,000 VND | WithdrawRequest với Amount < 2,000 | Auto-cancel với IsIgnore = true, msg: "Số tiền muốn rút phải lớn hơn hoặc bằng 2000" | P0 | ewallet-withdraw.md |
| EWD-03 | Rút vượt giới hạn hàng ngày (60,000,000 VND) | Đã rút gần đạt daily limit | WithdrawRequest với Amount khiến tổng > 60M VND/ngày | Status: 0, lỗi vượt quá giới hạn rút hàng ngày (DailyLimit từ ConfigWalletType) | P1 | ewallet-withdraw.md |
| EWD-04 | Chuyển khoản nội bộ INHOUSE (cùng MB Bank) | Tài khoản ngân hàng đích = MB Bank | WithdrawRequest, bank = MBBANK | TransferType = INHOUSE, xử lý qua MBBank_TransferFundService_AutoWithdraw | P1 | ewallet-withdraw.md |
| EWD-05 | Chuyển khoản liên ngân hàng FAST (qua Napas) | Tài khoản ngân hàng đích != MB Bank | WithdrawRequest, bank = Vietcombank/... | TransferType = FAST, xử lý qua Napas gateway | P1 | ewallet-withdraw.md |
| EWD-06 | Rút tiền khi tài khoản ngân hàng chưa verified | Tài khoản ngân hàng IsVerified = false hoặc không có UserLogin_BeneficiaryBank_Mapping | WithdrawRequest | Status: 0, lỗi yêu cầu xác minh tài khoản ngân hàng trước khi rút | P0 | ewallet-withdraw.md |
| EWD-07 | Nhiều yêu cầu rút tiền đồng thời (concurrent) | Ví có balance = 500,000, 2 request cùng lúc mỗi request 400,000 | 2 WithdrawRequest gửi đồng thời | Chỉ 1 request thành công, request còn lại lỗi "không đủ số dư" (AvailableBalance trừ pending withdrawals) | P2 | — |
| EWD-08 | Luồng trạng thái rút tiền: NEW → PROCESSING → SUCCESS | WithdrawRequest đã tạo | AutoWithdrawEngine chạy từng bước | Status chuyển NEW → PROCESSING (approved) → SUCCESS (bank confirmed), WalletTransaction ghi nhận, SMS/Push gửi | P0 | ewallet-withdraw.md |

---

## 5. Financial Calculations — Tính toán tài chính

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|-------------|-------|----------|----------|------|
| FIN-01 | Tính hoa hồng platform (PlanProfitAmount) | Order hoàn tất, CompletionFeePercentage = 20% | SubTotal = 3,990,000, ServiceFee = 0, DiscountMoney = 200,000, DeliveryFee = 0 | PlanProfitAmount = (3,990,000 × 20/100) + 0 - 200,000 + (0 × 20/100) = 598,000 | P0 | pricing-calculation.md Section 11 |
| FIN-02 | Tính tiền chủ xe nhận (OwnerRemainAmount) | Order hoàn tất, DepositAmount, PlanProfitAmount, InsuranceFee đã tính | DepositAmount = 1,196,850, PlanProfitAmount = 598,000, InsuranceFee = 199,500 | OwnerRemainAmount = 1,196,850 - 598,000 - 199,500 = 399,350 | P0 | pricing-calculation.md Section 12 |
| FIN-03 | Khấu trừ thuế thu nhập (Withholding Tax) | OwnerWithholdingTaxPercent > 0 | TotalPrice = 3,989,500, DepositAmount = 1,196,850, OwnerRemainAmount = 399,350 | taxableAmount = (3,989,500 - 1,196,850) + 399,350 = 3,192,000, OwnerWithholdingTaxAmount = taxableAmount × WithholdingTaxPercent / 100, OwnerRemainAmount giảm tương ứng | P1 | pricing-calculation.md Section 13 |
| FIN-04 | Commission priority cascade (item → owner → category → system) | Service item có CompletionFeePercentage = 15%, owner có 20%, category có 25% | Order với service item đó | CompletionFeePercentage = 15% (ưu tiên item level), PlanProfitAmount tính theo 15% | P1 | pricing-calculation.md Section 11 |
| FIN-05 | Tính toán tài chính khi có voucher discount | Voucher loại Percent = 10%, MaximumDiscountMoney = 200,000 | TotalOriginalPrice = 4,200,000 | DiscountMoney = MIN(4,200,000 × 10%, 200,000) = 200,000, TotalPrice giảm 200,000, PlanProfitAmount trừ DiscountMoney | P1 | pricing-calculation.md Section 7, 11 |
| FIN-06 | TransferMoneyEngine chuyển đúng số tiền vào ví owner | Order DONE, Order_Finance đã tính xong | TransferMoneyEngine xử lý | Owner wallet balance tăng = OwnerRemainAmount (sau thuế), WalletTransaction type = Transfer, Amount = OwnerRemainAmount | P0 | pricing-calculation.md Section 12 |

---

## 6. Background Job — CalcCancelOrderRatioJob

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|-------------|-------|----------|----------|------|
| BATCH-01 | CalcCancelOrderRatioJob integration test | Owner đã huỷ 2 đơn trong 30 ngày qua, tổng đơn = 10 | Job chạy (chạy mỗi 7 ngày) | User_Calculating.CancelRatio = 20% (2/10), User_Calculating.CancelCount = 2, Owner được đánh dấu nếu vượt ngưỡng cảnh báo | P2 | BR-CANCEL-013 |

---

## Tham chiếu chéo (Cross-references)

| Tài liệu | Nội dung liên quan |
|-----------|-------------------|
| [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md) | Kịch bản hoàn tiền khi huỷ đơn (refund scenarios) — xem thêm RC-07, RC-08 trong [rental-service.test-scenarios.md](./rental-service.test-scenarios.md) |
| [pricing-calculation.md](../04_BUSINESS_FLOWS/pricing-calculation.md) | Công thức chi tiết: TotalPrice, SubTotal, Commission, OwnerRemainAmount, Withholding Tax |
| [ewallet-withdraw.md](../04_BUSINESS_FLOWS/ewallet-withdraw.md) | Luồng rút tiền: AutoWithdrawEngine, MB Bank integration, INHOUSE/FAST transfer types |
| [order-status-machine.md](../04_BUSINESS_FLOWS/order-status-machine.md) | State machine đầy đủ: OWNER2CONFIRM → CUS2DEPOSIT → WAITING2DEPARTURE → INTHETRIP → DONE |
| [ewallet.md](./ewallet.md) | API endpoints EWallet: Wallet/List, TopUp, Transfer, Action/Funding, Action/Withdrawal |
| [order.md](./order.md) | API endpoints Order: Begin, End, Payment, Cancel |

---

*Tổng cộng: **38 test scenarios** covering EWallet deposit/payment, order begin handshake, order end + auto-complete, withdrawal flow, financial calculations, và background job integration.*
