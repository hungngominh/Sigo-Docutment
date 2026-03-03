# Test Scenarios — Cancel Flow

> File này chứa test scenarios cho Cancel Flow của Sigo API (AllianceMiddleman).
> Mỗi scenario ánh xạ đến business rules trong [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md).

---

## 1. Renter Cancel (Khách thuê huỷ đơn)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| RCC-01 | Renter huỷ tại OWNER2CONFIRM (chưa cọc) | Order status = OWNER2CONFIRM, chưa có giao dịch cọc | CancelReasonId hợp lệ, user = renter | Status: 1, order → CUSCANCEL, không cần hoàn tiền | P0 | BR-CANCEL-001 |
| RCC-02 | Renter huỷ tại CUS2DEPOSIT (chưa cọc) | Order status = CUS2DEPOSIT, renter chưa thanh toán cọc | CancelReasonId hợp lệ, user = renter | Status: 1, order → CUSCANCEL, không cần hoàn tiền | P0 | BR-CANCEL-001 |
| RCC-03 | Renter huỷ tại WAITING2CONFIRMDEPOSIT | Order status = WAITING2CONFIRMDEPOSIT, cọc đang chờ xác nhận | CancelReasonId hợp lệ, user = renter | Status: 1, order → CUSCANCEL, cọc pending được huỷ | P0 | BR-CANCEL-001 |
| RCC-04 | Renter huỷ tại WAITING2DEPARTURE trong FullRefundWithinMinutes (15 phút) | Order status = WAITING2DEPARTURE, đã cọc, thời gian từ lúc cọc < 15 phút | CancelReasonId hợp lệ, user = renter | Status: 1, order → CUSCANCEL, RefundAmount = 100% DepositAmount | P0 | BR-CANCEL-005 |
| RCC-05 | Renter huỷ tại WAITING2DEPARTURE sau 15 phút, >7 ngày trước ngày thuê | Order status = WAITING2DEPARTURE, đã cọc, thời gian > 15 phút, FromDate - now > 7 ngày | CancelReasonId hợp lệ, user = renter | Status: 1, order → CUSCANCEL, RefundAmount = DepositAmount × 70% | P0 | BR-CANCEL-005/006 |
| RCC-06 | Renter huỷ tại WAITING2DEPARTURE trong vòng 7 ngày trước ngày thuê | Order status = WAITING2DEPARTURE, đã cọc, FromDate - now ≤ 7 ngày | CancelReasonId hợp lệ, user = renter | Status: 1, order → CUSCANCEL, RefundAmount = 0 (mất toàn bộ cọc) | P0 | BR-CANCEL-006 |
| RCC-07 | Huỷ mà không có CancelReasonId | Order status = OWNER2CONFIRM | CancelReasonId = null, user = renter | Status: 0, msg chứa "chọn lý do huỷ" | P0 | BR-CANCEL-003 |
| RCC-08 | Huỷ với lý do "another_reason" nhưng không nhập CancelReasonDetail | Order status = OWNER2CONFIRM | CancelReasonCode = "another_reason", CancelReasonDetail = null/empty | Status: 0, msg chứa "nhập lý do huỷ chi tiết" | P0 | BR-CANCEL-004 |
| RCC-09 | Huỷ khi đơn đang INTHETRIP | Order status = INTHETRIP (đang trong chuyến) | CancelReasonId hợp lệ, user = renter | Status: 0, msg chứa "bị thay đổi" (không cho phép huỷ ở trạng thái này) | P1 | — |
| RCC-10 | Huỷ khi đơn đã DONE | Order status = DONE (đã hoàn thành) | CancelReasonId hợp lệ, user = renter | Status: 0, msg chứa "bị thay đổi" (không cho phép huỷ ở trạng thái này) | P1 | — |

---

## 2. Owner Cancel (Chủ xe huỷ đơn)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OCC-01 | Owner huỷ tại OWNER2CONFIRM | Order status = OWNER2CONFIRM, chưa có cọc | CancelReasonId hợp lệ, user = owner | Status: 1, order → OWNERCANCEL, không cần hoàn tiền | P0 | BR-CANCEL-002 |
| OCC-02 | Owner huỷ tại CUS2DEPOSIT | Order status = CUS2DEPOSIT, renter chưa cọc | CancelReasonId hợp lệ, user = owner | Status: 1, order → OWNERCANCEL, không cần hoàn tiền | P0 | — |
| OCC-03 | Owner huỷ tại WAITING2DEPARTURE, trong 15 phút | Order status = WAITING2DEPARTURE, đã cọc, thời gian từ lúc cọc < 15 phút | CancelReasonId hợp lệ, user = owner | Status: 1, order → OWNERCANCEL, hoàn 100% cọc cho renter, không phạt owner | P0 | BR-CANCEL-007 |
| OCC-04 | Owner huỷ tại WAITING2DEPARTURE, sau 15 phút | Order status = WAITING2DEPARTURE, đã cọc, thời gian từ lúc cọc > 15 phút | CancelReasonId hợp lệ, user = owner | Status: 1, order → OWNERCANCEL, hoàn 100% cọc cho renter, owner bị phạt (OwnerCancelPenalty) | P0 | BR-CANCEL-007 |
| OCC-05 | Owner huỷ → tạo DateBusyRentalSchedule | Order có FromDate = 01/03, ToDate = 05/03 | CancelReasonId hợp lệ, user = owner | ServiceItem_DateBusyRentalSchedule được tạo cho ngày 01/03 - 05/03 (chặn lịch bận) | P1 | BR-CANCEL-008 |
| OCC-06 | Owner huỷ → auto tạo QuickOrder cho renter | Config QuickOrder_RentCar_AutoCreate = true | CancelReasonId hợp lệ, user = owner | QuickOrder mới được tạo tự động cho renter với cùng thông tin thuê xe | P2 | BR-CANCEL-009 |
| OCC-07 | Owner huỷ → IsOwnerFault và cancel ratio | Owner đã có lịch sử huỷ đơn | CancelReasonId hợp lệ, user = owner | IsOwnerFault = true, User_Calculating.CancelRatio được cập nhật tăng | P1 | BR-CANCEL-013 |
| OCC-08 | User không phải owner cố huỷ đơn với quyền owner | Order thuộc về owner A, user = owner B (khác người) | CancelReasonId hợp lệ, user ≠ owner của đơn | Status: 0, msg chứa "không có quyền" | P0 | — |
| OCC-09 | Owner huỷ thiếu CancelReasonId | Order status = OWNER2CONFIRM | CancelReasonId = null, user = owner | Status: 0, msg chứa "chọn lý do huỷ" | P0 | BR-CANCEL-003 |
| OCC-10 | Owner chọn "another_reason" nhưng không nhập chi tiết | Order status = OWNER2CONFIRM | CancelReasonCode = "another_reason", CancelReasonDetail = null/empty, user = owner | Status: 0, msg chứa "nhập lý do huỷ chi tiết" | P0 | BR-CANCEL-004 |

---

## 3. System Auto-Cancel (Hệ thống tự động huỷ)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| SCC-01 | OWNER2CONFIRM timeout (3h giờ hành chính) | Order status = OWNER2CONFIRM, Owner2ConfirmEndTime ≤ now | Job chạy kiểm tra timeout | Order → SYSTEMCANCEL, thông báo gửi cho cả 2 bên | P0 | BR-CANCEL-010 |
| SCC-02 | CUS2DEPOSIT timeout (3h giờ hành chính) | Order status = CUS2DEPOSIT, Customer2DepositEndTime ≤ now | Job chạy kiểm tra timeout | Order → SYSTEMCANCEL, thông báo gửi cho cả 2 bên | P0 | BR-CANCEL-011 |
| SCC-03 | System cancel → giải phóng lịch booking | Order có BookedRentalSchedule cho ngày 01/03 - 05/03 | Job thực hiện SYSTEMCANCEL | ServiceItem_BookedRentalSchedule.IsBooked = false cho các ngày tương ứng | P0 | — |
| SCC-04 | System cancel → khôi phục mã giảm giá | Order đã sử dụng voucher/discount code | Job thực hiện SYSTEMCANCEL | OneTimeUse.IsCanceled = true, DiscountCode_Summary.UsedCount giảm 1 | P1 | — |
| SCC-05 | System cancel qua nửa đêm (business hours boundary) | Order tạo lúc 20:00, Owner2ConfirmEndTime tính 3h business hours → ngày hôm sau 10:00 sáng | Job chạy kiểm tra timeout lúc 10:00 sáng hôm sau | Order → SYSTEMCANCEL (timeout được tính đúng qua boundary giờ hành chính) | P1 | — |

---

## 4. Refund Calculation (Tính tiền hoàn trả)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| REF-01 | Hoàn 100% trong 15 phút (renter huỷ) | Renter huỷ tại WAITING2DEPARTURE, thời gian từ lúc cọc < FullRefundWithinMinutes (15 phút) | Renter cancel | RefundAmount = 100% × DepositAmount, RefundPercent = 100 | P0 | BR-CANCEL-005 |
| REF-02 | Hoàn 70% (renter huỷ, >15 phút, >7 ngày) | Renter huỷ tại WAITING2DEPARTURE, thời gian > 15 phút, FromDate - now > NoRefundGreaterThanDays (7 ngày) | Renter cancel | RefundAmount = DepositAmount × 70%, RefundPercent = 70 | P0 | BR-CANCEL-005/006 |
| REF-03 | Không hoàn (renter huỷ, ≤7 ngày) | Renter huỷ tại WAITING2DEPARTURE, FromDate - now ≤ 7 ngày | Renter cancel | RefundAmount = 0, RefundPercent = 0, renter mất toàn bộ cọc | P0 | BR-CANCEL-006 |
| REF-04 | Owner huỷ → luôn hoàn 100% cho renter | Owner huỷ tại bất kỳ trạng thái nào có cọc | Owner cancel | RefundAmount = 100% × DepositAmount cho renter, bất kể thời gian | P0 | BR-CANCEL-007 |
| REF-05 | Tính platform commission khi huỷ | Order có PlatformCommission, renter đã cọc | Cancel xảy ra | PlatformCommission được tính lại/hoàn trả tương ứng với RefundPercent | P1 | — |
| REF-06 | Hoàn tiền với DepositAmount cố định (không phải %) | Order có DepositAmount = 500,000 VND (giá trị tuyệt đối, không tính theo % TotalPrice) | Renter cancel trong 15 phút | RefundAmount = 500,000 VND (tính trên DepositAmount thực tế, không phải DepositPercent × TotalPrice) | P1 | — |
| REF-07 | Làm tròn số tiền hoàn (decimal precision) | DepositAmount = 333,333 VND, RefundPercent = 70% | Renter cancel | RefundAmount = 233,333 VND (kiểm tra cách làm tròn: floor/round/ceil) | P2 | — |
| REF-08 | Boundary FullRefundWithinMinutes (đúng 15 phút) | Thời gian từ lúc cọc = chính xác 15 phút (biên giới) | Renter cancel tại đúng phút thứ 15 | Xác định rõ: RefundAmount = 100% hay áp dụng mức phạt (edge case boundary) | P1 | BR-CANCEL-005 |

---

## 5. Side Effects (Tác động phụ khi huỷ)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| SE-01 | Huỷ → giải phóng lịch booking | Order có ServiceItem_BookedRentalSchedule cho ngày 01/03 - 05/03, IsBooked = true | Cancel (bất kỳ loại nào) | ServiceItem_BookedRentalSchedule.IsBooked = false cho tất cả ngày của đơn | P0 | — |
| SE-02 | Huỷ đơn có discount → khôi phục mã giảm giá | Order đã áp dụng voucher, DiscountCode_OneTimeUse record tồn tại | Cancel (bất kỳ loại nào) | DiscountCode_OneTimeUse.IsCanceled = true, DiscountCode_Summary.UsedCount giảm 1, mã giảm giá có thể dùng lại | P0 | — |
| SE-03 | Owner huỷ → tạo DateBusyRentalSchedule cho ngày thuê | Order có FromDate = 01/03, ToDate = 05/03, owner huỷ | Owner cancel | ServiceItem_DateBusyRentalSchedule được tạo cho khoảng ngày 01/03 - 05/03, chặn đặt xe trong thời gian đó | P1 | BR-CANCEL-008 |
| SE-04 | Huỷ → gửi thông báo cho cả 2 bên | Order có renter và owner | Cancel (bất kỳ loại nào) | Notification gửi cho renter (huỷ thành công/đơn bị huỷ), notification gửi cho owner (đơn bị huỷ/huỷ thành công) | P1 | — |
| SE-05 | Huỷ → cập nhật User_Calculating | Order đã hoàn thành cancel | Cancel xảy ra | User_Calculating được cập nhật: CancelCount tăng, CancelRatio tính lại cho user thực hiện huỷ | P2 | BR-CANCEL-013 |

---

*Tong cong: **38 test scenarios** covering renter cancel, owner cancel, system auto-cancel, refund calculation, va side effects cho Cancel Flow.*
