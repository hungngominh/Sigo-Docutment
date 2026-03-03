# Test Scenarios — Owner Vehicle Management

> File này chứa test scenarios cho các endpoints quản lý xe của chủ xe (Owner) chưa có coverage.
> Bao gồm: đăng ký xe mới, gửi duyệt, thay đổi trạng thái, và các utility endpoints.

---

## 1. InsertNewRentalService (Đăng ký xe mới)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OVM-01 | Happy path — đăng ký xe mới thành công | User = owner đã verified, xe chưa tồn tại | Đầy đủ thông tin xe: LicensePlate, VehicleMakeId, VehicleModelId, NoOfSeat, Address, hình ảnh | Status: 1, Data chứa RentalServiceItemId mới, trạng thái xe = DRAFT (chưa duyệt) | P0 | — |
| OVM-02 | Thiếu thông tin bắt buộc (biển số) | User = owner | LicensePlate = null/empty, các field khác hợp lệ | Status: 0, msg chứa lỗi thiếu biển số xe | P0 | — |
| OVM-03 | Biển số xe đã tồn tại trong hệ thống | User = owner, xe khác đã đăng ký cùng biển số | LicensePlate = biển số đã tồn tại | Status: 0, msg chứa "đã tồn tại" hoặc "trùng" | P0 | — |
| OVM-04 | User chưa đăng nhập | Không có Authorization token | Thông tin xe hợp lệ | Status: 0, unauthorized | P0 | — |

---

## 2. Submit2Review (Gửi xe để duyệt)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OVM-05 | Happy path — gửi duyệt thành công | Xe ở trạng thái DRAFT, đủ thông tin bắt buộc (hình ảnh, giấy tờ) | RentalServiceItemId hợp lệ | Status: 1, trạng thái xe → PENDING_REVIEW, admin nhận notification | P0 | — |
| OVM-06 | Xe thiếu thông tin bắt buộc để duyệt | Xe DRAFT nhưng thiếu hình ảnh hoặc giấy tờ xe | RentalServiceItemId | Status: 0, msg chứa danh sách thông tin còn thiếu | P0 | — |
| OVM-07 | Xe đã ở trạng thái duyệt rồi | Xe đã là ACTIVE/APPROVED | RentalServiceItemId | Status: 0, msg chứa "đã được duyệt" hoặc trạng thái không hợp lệ | P1 | — |

---

## 3. UpdateRentalServiceStatus (Thay đổi trạng thái xe)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OVM-08 | Owner tạm ngưng xe (ACTIVE → SUSPENDED) | Xe đang ACTIVE, user = owner | StatusCode = "SUSPENDED" | Status: 1, xe.IsSuspended = true, xe không xuất hiện trong kết quả tìm kiếm | P0 | — |
| OVM-09 | Owner kích hoạt lại xe (SUSPENDED → ACTIVE) | Xe đang SUSPENDED, user = owner | StatusCode = "ACTIVE" | Status: 1, xe.IsSuspended = false, xe xuất hiện lại trong kết quả tìm kiếm | P0 | — |
| OVM-10 | Owner huỷ đăng ký xe (DEACTIVE) | Xe đang ACTIVE, không có booking pending | StatusCode = "DEACTIVE" | Status: 1, xe bị deactive, không thể đặt xe này nữa | P1 | — |
| OVM-11 | Thay đổi trạng thái khi có booking pending | Xe đang ACTIVE, có booking OWNER2CONFIRM hoặc WAITING2DEPARTURE | StatusCode = "SUSPENDED" | Status: 0, msg chứa "có đơn đang xử lý" hoặc cho phép nhưng cảnh báo | P1 | — |

---

## 4. GetCancelOrderInfo (Lấy thông tin trước khi huỷ)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OVM-12 | Happy path — lấy thông tin huỷ đúng | Order status = WAITING2DEPARTURE, đã cọc | OrderNumber hợp lệ | Status: 1, Data chứa RefundPolicy (% hoàn, số tiền hoàn dự kiến), CancelReasons list, DepositAmount | P0 | — |
| OVM-13 | Đơn không ở trạng thái cho phép huỷ | Order status = DONE hoặc INTHETRIP | OrderNumber hợp lệ | Status: 0, msg chứa "không thể huỷ" hoặc trả policy trống | P1 | — |

---

## 5. OrderConfirmHasPay (Xác nhận đơn khi khách đã thanh toán)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OVM-14 | Happy path — xác nhận đã nhận cọc | Order status = WAITING2CONFIRMDEPOSIT, admin/owner xác nhận | OrderNumber hợp lệ | Status: 1, order → WAITING2DEPARTURE, DepositDoneAt được set | P0 | — |
| OVM-15 | Xác nhận ở trạng thái sai | Order status != WAITING2CONFIRMDEPOSIT | OrderNumber hợp lệ | Status: 0, msg chứa "bị thay đổi" hoặc trạng thái không hợp lệ | P1 | — |

---

## 6. GetSettingApp (Lấy cấu hình app)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OVM-16 | Smoke test — lấy cấu hình app | — | GET /api/v1/SearchingRentalService/GetSettingApp | Status: 1, Data chứa cấu hình app (BusinessHourStart, BusinessHourEnd, DepositPercent, CancelPolicy, ...) | P1 | — |

---

## 7. SaveLog_UserClick_Rent (Ghi log click thuê xe)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OVM-17 | Fire-and-forget — ghi log thành công | User đã đăng nhập | RentalServiceItemId, ActionType = "CLICK_RENT" | Status: 1 (fire-and-forget), log được ghi trong DB (verify bằng query DB sau đó) | P2 | — |

---

## 8. GetRentalService_SelfdriveCarRental_Alias (SEO Alias)

| # | Scenario | Precondition | Input | Expected | Priority | Rule |
|---|----------|--------------|-------|----------|----------|------|
| OVM-18 | Lấy danh sách alias/slug | Có xe đã active với slug được tạo | GET request | Status: 1, Data chứa danh sách slug (ví dụ: "toyota-vios-ha-noi", "honda-city-hcm"), mỗi slug unique | P2 | — |

---

## Tham chiếu chéo (Cross-references)

| Tài liệu | Nội dung liên quan |
|-----------|-------------------|
| [rental-service.md](./rental-service.md) | API endpoints chi tiết cho RentalService controller |
| [owner-onboarding.md](../04_BUSINESS_FLOWS/owner-onboarding.md) | Luồng đăng ký xe và duyệt xe |
| [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md) | Business rules cho GetCancelOrderInfo |
| [order-status-machine.md](../04_BUSINESS_FLOWS/order-status-machine.md) | State machine cho OrderConfirmHasPay |
| [test-coverage-matrix.md](./test-coverage-matrix.md) | Ma trận coverage — 8 endpoints mới được cover |

---

*Tổng cộng: **18 test scenarios** covering owner vehicle management, submit review, status changes, cancel info, payment confirmation, settings, logging, và SEO alias.*
