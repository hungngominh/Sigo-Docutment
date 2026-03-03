# Test Coverage Matrix

> Ma trận cross-reference liên kết Business Rules → Test Scenarios → API Endpoints.
> Dùng cho QC để đảm bảo tất cả business rules đều có test scenarios tương ứng và không bỏ sót endpoint nào.

---

## Cách dùng file này (cho QC và AI agents)

**Câu hỏi: "Luồng cancel refund đã được test chưa?"**
1. Tìm keyword "refund" hoặc "cancel" trong Section 1 → thấy BR-CANCEL-005, BR-CANCEL-006
2. Xem cột "Test Scenario(s)" → RC-07 (100%), RC-11 (70%), RC-08 (0%) — đã cover 3 kịch bản
3. Xem Section 3 "Coverage Gaps" → GAP-004 liên quan, status = ✅ RESOLVED
4. **Kết luận: Đã cover đầy đủ.**

**Câu hỏi: "Endpoint Booking có bao nhiêu test?"**
1. Tìm "Booking" trong Section 2 → thấy `POST RentalService/Booking`
2. Xem cột "Test Scenarios" → B-01 → B-19 (19 scenarios)
3. **Kết luận: 19 scenarios cover 11 business rules.**

---

## 1. Business Rule → Test Scenario Cross-Reference

### Booking Rules (BR-BOOK)

| Rule ID | Rule Description | Test Scenario(s) | API Endpoint | Priority |
|---------|-----------------|-------------------|--------------|----------|
| BR-BOOK-001 | FromDate/ToDate bắt buộc phải truyền | B-02 (thiếu FromDate), B-03 (thiếu ToDate) | Booking | P0 |
| BR-BOOK-002 | FromDate phải nhỏ hơn ToDate (không được bằng hoặc lớn hơn) | B-04 (From>To), B-05 (From==To) | Booking | P0 |
| BR-BOOK-003 | Không được chọn ngày trong quá khứ | B-06 (ngày quá khứ) | Booking | P0 |
| BR-BOOK-004 | Xe phải được duyệt (IsApproved) và đang hoạt động (không bị tạm ngưng) | B-07 (suspended), B-08 (chưa duyệt), D-07 (detail xe suspended) | Booking, Detail | P0 |
| BR-BOOK-005 | Không được trùng lịch booking đã confirmed (ServiceItem_BookedRentalSchedule) | B-09 (overlap booking), C-03 (check overlap), CONC-01 (race condition) | Booking, CheckBefore | P0 |
| BR-BOOK-006 | Không được trùng ngày bận cố định (ServiceItem_DateBusyRentalSchedule) | B-10 (trùng busy date) | Booking | P0 |
| BR-BOOK-007 | Không được trùng ngày bận theo thứ trong tuần (ServiceItem_WeekdaysBusyRentalSchedule) | B-11 (trùng weekday busy) | Booking | P1 |
| BR-BOOK-008 | Số ngày thuê phải >= MinimumRequiredRentalDays | B-12 (ngày < min), C-02 (check min days) | Booking, CheckBefore | P0 |
| BR-BOOK-009 | Phải đăng nhập mới được đặt xe / cập nhật booking | B-13 (no token), U-06 (no token) | Booking, UpdateBookingInfo | P0 |
| BR-BOOK-010 | Không được thuê xe của chính mình (Renter ID != Owner ID) | B-14 (thuê xe mình) | Booking | P0 |
| BR-BOOK-011 | Khoảng cách giao xe không vượt quá maximumDeliveryMileage | B-15 (quá xa), U-05 (distance check) | Booking, UpdateBookingInfo | P1 |
| BR-BOOK-012 | Phải gọi UpdateBookingInfo trước khi Booking (implicit workflow) | B-20 (skip UpdateBookingInfo) | Booking | P1 |
| BR-BOOK-013 | Owner phải xác nhận trong 3 giờ (business hours) → nếu không hệ thống tự huỷ | OC-03 (quá hạn confirm), AC-01 (auto cancel) | OrderConfirm, SystemCancel | P0 |
| BR-BOOK-014 | Renter phải đặt cọc trong 3 giờ (business hours) → nếu không hệ thống tự huỷ | OP-02 (hết hạn cọc), AC-02 (auto cancel) | OrderPay, SystemCancel | P0 |
| BR-BOOK-015 | Nhận xe yêu cầu cả 2 bên xác nhận (two-sided Begin handshake) | OB-01 (owner begin), OB-02 (renter begin), OB-03 (cả hai → INTHETRIP), OB-04 (1 bên) | Begin | P0 |
| BR-BOOK-016 | Chỉ được đánh giá khi đơn ở trạng thái DONE | OR-01 (happy path), OR-02 (chưa DONE) | Review | P0 |
| BR-BOOK-017 | Tự động hoàn thành đơn sau ToDate + 60 phút nếu không có action | AC-03 (auto-complete) | SystemCancel (AutoComplete) | P0 |

### Cancel Rules (BR-CANCEL)

| Rule ID | Rule Description | Test Scenario(s) | API Endpoint | Priority |
|---------|-----------------|-------------------|--------------|----------|
| BR-CANCEL-001 | Renter chỉ được huỷ khi status thuộc {OWNER2CONFIRM, CUS2DEPOSIT, WAITING2CONFIRMDEPOSIT, WAITING2DEPARTURE} | RC-01 (tại O2C), RC-02 (tại C2D), RC-03 (tại W2CD), RC-09 (INTHETRIP→fail), RC-10 (DONE→fail), RC-11 (70% refund), RC-12 (W2CD cancel) | RenterCancel | P0 |
| BR-CANCEL-002 | Owner chỉ được huỷ khi status thuộc {OWNER2CONFIRM, CUS2DEPOSIT, WAITING2CONFIRMDEPOSIT, WAITING2DEPARTURE} | OCA-01 (owner huỷ O2C) | OwnerCancel | P0 |
| BR-CANCEL-003 | Bắt buộc chọn CancelReasonId khi huỷ | RC-05 (renter thiếu reason), OCC-09 (owner thiếu reason) | RenterCancel, OwnerCancel | P0 |
| BR-CANCEL-004 | Nếu lý do huỷ là "another_reason" → bắt buộc nhập CancelReasonDetail | RC-06 (renter trống detail), OCC-10 (owner trống detail) | RenterCancel, OwnerCancel | P0 |
| BR-CANCEL-005 | Huỷ trong FullRefundWithinMinutes (15 phút) → hoàn 100% cọc cho renter | RC-07 (hoàn 100%), RC-11 (>15p >7d → 70%) | RenterCancel | P0 |
| BR-CANCEL-006 | Huỷ sát ngày (trong NoRefundGreaterThanDays = 7 ngày trước chuyến) → renter không được hoàn | RC-08 (mất cọc) | RenterCancel | P0 |
| BR-CANCEL-007 | Owner huỷ → luôn hoàn 100% cho renter, owner chịu penalty | OCA-02 (hoàn renter), OCA-03 (phạt owner) | OwnerCancel | P0 |
| BR-CANCEL-008 | Owner huỷ → ngày bận được tự động đánh dấu (DateBusyRentalSchedule) tránh bị đặt lại | OCA-04 (đánh dấu bận) | OwnerCancel | P1 |
| BR-CANCEL-009 | Owner huỷ → hệ thống có thể tự tạo QuickOrder cho renter tìm xe thay thế | OCA-05 (auto QuickOrder) | OwnerCancel | P2 |
| BR-CANCEL-010 | System timeout: Owner không confirm trong 3 giờ business hours → auto cancel (SYSTEMCANCEL) | AC-01 (owner timeout) | SystemCancel | P0 |
| BR-CANCEL-011 | System timeout: Renter không cọc trong 3 giờ business hours → auto cancel (SYSTEMCANCEL) | AC-02 (renter timeout) | SystemCancel | P0 |
| BR-CANCEL-012 | Auto-complete: đơn INTHETRIP quá hạn trả xe 60 phút → tự hoàn thành (DONE) | AC-03 (auto-complete) | SystemCancel (AutoComplete) | P0 |
| BR-CANCEL-013 | Cancel ratio owner = (số đơn bị huỷ lỗi owner / tổng đơn) × 100%, tính mỗi 7 ngày bởi CalcCancelOrderRatioJob | BATCH-01 (integration test) | — (background job) | P2 |

---

## 2. Endpoint → Validation Rules Applied

| Endpoint | Validation Rules áp dụng | Test Scenarios covering |
|----------|--------------------------|------------------------|
| `POST SearchingRentalService/List` | Không có validation nghiêm ngặt (AllowAnonymous); FromDate/ToDate optional; filter optional | S-01 → S-10 |
| `POST SearchingRentalService/Detail` | Phải có RentalServiceItemId hoặc Slug; xe phải IsApproved, không bị Suspended/Deactive (BR-BOOK-004) | D-01 → D-08 |
| `POST SearchingRentalService/CheckBeforeUpdateBookingInfo` | Kiểm tra xe còn trống (BR-BOOK-005); minimum rental days (BR-BOOK-008) | C-01 → C-04 |
| `POST SearchingRentalService/UpdateBookingInfo` | Phải đăng nhập (BR-BOOK-009); delivery distance (BR-BOOK-011); voucher validation | U-01 → U-06 |
| `POST RentalService/Booking` | FromDate/ToDate required (BR-BOOK-001); FromDate < ToDate (BR-BOOK-002); no past dates (BR-BOOK-003); xe approved/active (BR-BOOK-004); no overlap booked (BR-BOOK-005); no overlap busy dates (BR-BOOK-006); no overlap weekday busy (BR-BOOK-007); minimum rental days (BR-BOOK-008); phải đăng nhập (BR-BOOK-009); cannot rent own (BR-BOOK-010); delivery distance (BR-BOOK-011) | B-01 → B-19 |
| `POST RentalService/OrderConfirm` | User phải là owner; status = OWNER2CONFIRM; chưa quá Owner2ConfirmEndTime (BR-BOOK-013) | OC-01 → OC-05 |
| `POST RentalService/OrderPay` | User phải là renter; status thuộc {CUS2DEPOSIT, WAITING2CONFIRMDEPOSIT}; chưa quá Customer2DepositEndTime (BR-BOOK-014) | OP-01 → OP-04 |
| `POST Order_ListView_RentCar/Begin` | User phải là owner hoặc renter; status = WAITING2DEPARTURE; two-sided handshake (BR-BOOK-015) | OB-01 → OB-06 |
| `POST Order_ListView_RentCar/End` | User phải là owner hoặc renter; status = INTHETRIP | OE-01 → OE-03, OEN-07 → OEN-09 |
| `POST Order_ListView_RentCar/RenterCancel` | User phải là renter; status thuộc valid set (BR-CANCEL-001); CancelReasonId required (BR-CANCEL-003); CancelReasonDetail nếu "another_reason" (BR-CANCEL-004); tính hoàn tiền theo policy (BR-CANCEL-005, BR-CANCEL-006) | RC-01 → RC-12 |
| `POST Order_ListView_RentCar/OwnerCancel` | User phải là owner; status thuộc valid set (BR-CANCEL-002); CancelReasonId required (BR-CANCEL-003); CancelReasonDetail nếu "another_reason" (BR-CANCEL-004); hoàn 100% cho renter + penalty owner (BR-CANCEL-007); đánh dấu ngày bận (BR-CANCEL-008); auto QuickOrder (BR-CANCEL-009) | OCA-01 → OCA-05, OCC-09, OCC-10 |
| `POST RentalService/OrderReview` | User phải là owner hoặc renter; status = DONE (BR-BOOK-016) | OR-01 → OR-03 |
| `AutoCancelOverTimeOrderEngine` (System) | Owner timeout 3h (BR-CANCEL-010); Renter deposit timeout 3h (BR-CANCEL-011); Auto-complete 60 min (BR-CANCEL-012) | AC-01 → AC-03 |
| `POST RentalService/InsertNewRentalService` | Owner phải đăng nhập; biển số xe unique; thông tin xe bắt buộc | OVM-01 → OVM-04 |
| `POST RentalService/Submit2Review` | Xe phải ở trạng thái DRAFT; đủ thông tin bắt buộc | OVM-05 → OVM-07 |
| `POST RentalService/UpdateRentalServiceStatus` | Owner phải sở hữu xe; status transition hợp lệ (ACTIVE/SUSPENDED/DEACTIVE) | OVM-08 → OVM-11 |
| `POST RentalService/GetCancelOrderInfo` | Order phải tồn tại; trả refund policy cho status hiện tại | OVM-12, OVM-13 |
| `POST RentalService/OrderConfirmHasPay` | Status = WAITING2CONFIRMDEPOSIT; admin/owner xác nhận | OVM-14, OVM-15 |
| `POST SearchingRentalService/GetSettingApp` | AllowAnonymous; trả cấu hình app | OVM-16 |
| `POST SearchingRentalService/SaveLog_UserClick_Rent` | Fire-and-forget; ghi log click | OVM-17 |
| `GET SearchingRentalService/GetRentalService_SelfdriveCarRental_Alias` | AllowAnonymous; trả danh sách slug SEO | OVM-18 |
| `POST SearchingRentalService/SearchVouchers` | Trả danh sách voucher áp dụng được cho xe + ngày | SV-01 → SV-04 |

---

## 3. Coverage Gaps

### Business rules chưa có test scenario rõ ràng

| Gap ID | Mô tả | Rule liên quan | Status | Resolution |
|--------|--------|----------------|--------|------------|
| GAP-001 | BR-BOOK-012 (phải gọi UpdateBookingInfo trước Booking) không có test case explicit | BR-BOOK-012 | ✅ RESOLVED | Thêm B-20 vào rental-service.test-scenarios.md |
| GAP-002 | BR-CANCEL-013 (CalcCancelOrderRatioJob) không có test scenario | BR-CANCEL-013 | ✅ RESOLVED | Thêm BATCH-01 vào ewallet-order-lifecycle.test-scenarios.md |
| GAP-003 | Cancel rules BR-CANCEL-003/004 thiếu test cho OwnerCancel | BR-CANCEL-003, BR-CANCEL-004 | ✅ RESOLVED | Thêm OCC-09, OCC-10 vào cancel-flow.test-scenarios.md |
| GAP-004 | Chưa có test cho refund kịch bản 2 (>15 phút, >7 ngày) | BR-CANCEL-005, BR-CANCEL-006 | ✅ RESOLVED | Thêm RC-11 vào rental-service.test-scenarios.md |
| GAP-005 | Không có test cho WAITING2CONFIRMDEPOSIT cancel | BR-CANCEL-001, BR-CANCEL-002 | ✅ RESOLVED | Thêm RC-12 vào rental-service.test-scenarios.md |
| GAP-006 | OrderEnd thiếu edge cases (trả sớm, trả muộn, surcharge) | — | ✅ RESOLVED | Thêm OEN-07, OEN-08, OEN-09 vào ewallet-order-lifecycle.test-scenarios.md |
| GAP-007 | SearchVouchers endpoint không có test | — | ✅ RESOLVED | Thêm SV-01→SV-04 vào rental-service.test-scenarios.md |
| GAP-008 | Concurrent booking race condition không có test | BR-BOOK-005 | ✅ RESOLVED | Thêm CONC-01 vào rental-service.test-scenarios.md |
| GAP-009 | B-17 bảo hiểm hết hạn chỉ P2 nhưng nghiêm trọng | — | ✅ RESOLVED | Nâng B-17 từ P2 → P1 |

### Endpoints chưa có test coverage

| Endpoint | Status | Resolution |
|----------|--------|------------|
| `POST SearchingRentalService/GetSettingApp` | ✅ COVERED | OVM-16 |
| `POST SearchingRentalService/SaveLog_UserClick_Rent` | ✅ COVERED | OVM-17 |
| `GET SearchingRentalService/GetRentalService_SelfdriveCarRental_Alias` | ✅ COVERED | OVM-18 |
| `POST RentalService/InsertNewRentalService` | ✅ COVERED | OVM-01→OVM-04 |
| `POST RentalService/Submit2Review` | ✅ COVERED | OVM-05→OVM-07 |
| `POST RentalService/UpdateRentalServiceStatus` | ✅ COVERED | OVM-08→OVM-11 |
| `POST RentalService/GetCancelOrderInfo` | ✅ COVERED | OVM-12, OVM-13 |
| `POST RentalService/OrderConfirmHasPay` | ✅ COVERED | OVM-14, OVM-15 |

---

## 4. Test Data Requirements

> **TL;DR:** Cần **7 user accounts** (5 renter/owner + 1 admin + 1 no-token), **6 xe** (active/suspended/not-approved/insurance-expired/electric), **3 voucher** (valid/exhausted/expired), **3 wallet balances**. Chi tiết bên dưới.

### Dữ liệu cần chuẩn bị theo nhóm scenario

| Scenario Group | Test Data cần có | Chi tiết |
|----------------|-----------------|----------|
| **Search (S-*)** | Xe đã duyệt tại nhiều khu vực | Ít nhất 2 xe ở Hà Nội (Toyota, Honda), 1 xe 7 chỗ, 1 xe điện, 1 xe có bảo hiểm. Không cần xe ở "Đảo Hoàng Sa" (để test empty result) |
| **Detail (D-*)** | Xe với nhiều trạng thái khác nhau | 1 xe active có đầy đủ info + BusySchedules, 1 xe bị tạm ngưng (IsSuspended = true), 1 xe chưa duyệt (IsApproved = false). Voucher hợp lệ cho xe active |
| **CheckBefore (C-*)** | Xe có lịch bận + cấu hình minimum days | 1 xe có booking confirmed trong khoảng ngày cụ thể, 1 xe có MinimumRequiredRentalDays = 2 |
| **UpdateBooking (U-*)** | User accounts + voucher + delivery config | 1 renter account có token, 1 voucher hợp lệ, 1 voucher hết lượt. Xe có maximumDeliveryMileage = 10km, địa chỉ giao > 10km |
| **Booking (B-*)** | Đầy đủ dữ liệu cho tất cả validations | 2 user accounts (renter + owner khác nhau), 1 user vừa là renter vừa là owner (test B-14). Xe active, xe suspended, xe chưa duyệt. Booking confirmed cho ngày test. DateBusy + WeekdayBusy records. Insurance expired record |
| **OrderConfirm (OC-*)** | Đơn ở status OWNER2CONFIRM | 1 đơn mới tạo (Owner2ConfirmEndTime trong tương lai), 1 đơn quá hạn confirm, 1 đơn đã bị renter huỷ trước. 2 accounts: owner hợp lệ + user khác |
| **OrderPay (OP-*)** | Đơn ở status CUS2DEPOSIT + ví tiền | 1 đơn chờ cọc (Customer2DepositEndTime trong tương lai), 1 đơn hết hạn cọc. Renter account có đủ số dư ví |
| **Begin (OB-*)** | Đơn ở status WAITING2DEPARTURE | 1 đơn chờ nhận xe. 2 accounts: owner + renter. Test cần gọi tuần tự: owner begin → renter begin |
| **End (OE-*)** | Đơn ở status INTHETRIP | 1 đơn đang trong chuyến |
| **Review (OR-*)** | Đơn ở status DONE | 1 đơn đã hoàn thành. 2 accounts + 1 account không liên quan |
| **RenterCancel (RC-*)** | Đơn ở nhiều status + cancel reasons | Đơn ở status OWNER2CONFIRM, CUS2DEPOSIT, WAITING2DEPARTURE, INTHETRIP, DONE. Cancel reasons list (bao gồm "another_reason"). Đơn đã cọc xong < 15 phút, đơn cọc > 15 phút, đơn sát ngày đi < 7 ngày |
| **OwnerCancel (OCA-*)** | Đơn đã cọc + config QuickOrder | Đơn đã cọc (DepositDoneAt != null). Config `QuickOrder_RentCar_AutoCreate.Code = "owner_cancel"`. Owner account |
| **SystemCancel (AC-*)** | Đơn quá hạn timeout | 1 đơn OWNER2CONFIRM có Owner2ConfirmEndTime <= now. 1 đơn CUS2DEPOSIT có Customer2DepositEndTime <= now. 1 đơn INTHETRIP có ToDate + 60 phút <= now |

### User Accounts cần tạo

| Account | Role | Mục đích |
|---------|------|----------|
| `test_renter_01` | Renter | Người thuê chính cho hầu hết scenarios |
| `test_renter_02` | Renter | Người thuê phụ (test concurrent, test user không liên quan) |
| `test_owner_01` | Owner | Chủ xe chính — sở hữu xe test |
| `test_owner_02` | Owner | Chủ xe phụ (test không có quyền confirm đơn của owner_01) |
| `test_owner_renter` | Owner + Renter | Account vừa là chủ xe vừa là người thuê (test BR-BOOK-010: thuê xe của chính mình) |
| `test_admin` | Admin | Quản trị viên — dùng cho admin APIs (RentalService_SelfdriveCarRental) |
| `test_no_token` | — | Request không có Authorization header (test BR-BOOK-009) |

### Xe (RentalServiceItem) cần tạo

| Xe | Trạng thái | Cấu hình đặc biệt |
|----|-----------|-------------------|
| `vehicle_active_01` | Active, IsApproved = true | MinimumRequiredRentalDays = 1, maximumDeliveryMileage = 15km, có bảo hiểm còn hạn |
| `vehicle_active_02` | Active, IsApproved = true | MinimumRequiredRentalDays = 3, có WeekdayBusy (thứ 7), có DateBusy một số ngày cụ thể |
| `vehicle_suspended` | IsSuspended = true | Dùng cho B-07, D-07 |
| `vehicle_not_approved` | IsApproved = false | Dùng cho B-08 |
| `vehicle_insurance_expired` | Active, insurance hết hạn | Dùng cho B-17 |
| `vehicle_electric` | Active, IsElectricEngine = true | Dùng cho S-09 |

### Discount Codes (Voucher)

| Code | Trạng thái | Ghi chú |
|------|-----------|---------|
| `VOUCHER_VALID_10` | Active, còn lượt dùng | Giảm 10%, tối đa 500.000đ |
| `VOUCHER_EXHAUSTED` | Active, hết lượt dùng (UsageCount >= MaxUsage) | Dùng cho U-03, B-19 |
| `VOUCHER_EXPIRED` | Hết hạn (ApplyTo < now) | Dùng cho edge case |

### Wallet Balances (Ví điện tử)

| Account | Số dư | Ghi chú |
|---------|-------|---------|
| `test_renter_01` | >= 2.000.000đ | Đủ để cọc cho đơn test |
| `test_renter_02` | 0đ | Test trường hợp không đủ số dư (nếu applicable) |
| `test_owner_01` | >= 0đ | Owner cần ví để nhận tiền / chịu penalty |

---

## 5. Tổng hợp thống kê

| Metric | Giá trị |
|--------|---------|
| Tổng Business Rules | 30 (17 BR-BOOK + 13 BR-CANCEL) |
| Business Rules covered | 30 / 30 (100%) |
| Tổng Test Scenarios | 179 (85 rental + 38 cancel + 38 ewallet + 18 vehicle-mgmt) |
| Endpoints có test coverage | 22 / 22 |
| Coverage gaps đã xác định | 9 — tất cả ✅ RESOLVED |
| Endpoints chưa có test | 0 — tất cả ✅ COVERED |
| BDD Gherkin scenarios | 62 P0 (21 booking + 21 cancel + 20 lifecycle) |
| Priority P0 scenarios | 60 |
| Priority P1 scenarios | 25 |
| Priority P2 scenarios | 10 |

---

*Cross-reference từ: [rental-service.test-scenarios.md](./rental-service.test-scenarios.md) | [cancel-flow.test-scenarios.md](./cancel-flow.test-scenarios.md) | [ewallet-order-lifecycle.test-scenarios.md](./ewallet-order-lifecycle.test-scenarios.md) | [owner-vehicle-management.test-scenarios.md](./owner-vehicle-management.test-scenarios.md) | [booking-flow.md](../04_BUSINESS_FLOWS/booking-flow.md) | [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md) | [rental-service.md](./rental-service.md)*
