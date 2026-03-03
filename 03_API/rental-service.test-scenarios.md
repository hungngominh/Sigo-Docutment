# Test Scenarios — Rental Service API

> File này chứa test scenarios cho AI tự động tạo test case.
> Mỗi scenario ánh xạ đến business rules trong [booking-flow.md](../04_BUSINESS_FLOWS/booking-flow.md) và [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md).

---

## 1. Tìm kiếm xe — SearchingRentalService/List

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| S-01 | Happy path — tìm xe Hà Nội | FromDate, ToDate hợp lệ, Address = "Hà Nội" | Status: 1, RentalServices[].length > 0, Paging.Total > 0 | P0 | — |
| S-02 | Tìm không có kết quả | Address = "Đảo Hoàng Sa" (vùng không có xe) | Status: 1, RentalServices = [], Paging.Total = 0 | P0 | — |
| S-03 | Không truyền FromDate/ToDate | FromDate = null, ToDate = null | Status: 1, trả danh sách xe (không tính giá theo ngày) | P1 | — |
| S-04 | Filter theo hãng xe | VehicleMakeIds = ["TOYOTA"] | Tất cả xe trả về đều là Toyota | P1 | — |
| S-05 | Filter theo khoảng giá | MinRentalPrice = 500000, MaxRentalPrice = 800000 | Xe trả về có giá trong khoảng | P1 | — |
| S-06 | Filter theo số chỗ | VehicleNoOfSeatIds = ["7_CHO"] | Xe trả về đều 7 chỗ | P1 | — |
| S-07 | Phân trang | PageIndex = 2, PageSize = 10 | Paging.Page = 2, trả ≤ 10 items | P1 | — |
| S-08 | Sắp xếp theo giá tăng dần | OrderBy = ["PRICE_ASC"] | Xe sắp xếp giá từ thấp đến cao | P2 | — |
| S-09 | Chỉ lấy xe điện | IsElectricEngine = true | Tất cả xe đều là xe điện | P2 | — |
| S-10 | Chỉ lấy xe có bảo hiểm | IsHaveInsurance = true | Tất cả xe đều có thông tin bảo hiểm | P2 | — |

---

## 2. Chi tiết xe — SearchingRentalService/Detail

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| D-01 | Happy path — xem chi tiết bằng ID | RentalServiceItemId = valid ID | Status: 1, Data chứa đầy đủ info xe | P0 | — |
| D-02 | Happy path — xem chi tiết bằng Slug | Slug = "toyota-vios-ha-noi" | Status: 1, Data.Slug = input Slug | P0 | — |
| D-03 | Xe không tồn tại | RentalServiceItemId = "invalid-id" | Status: 0 | P0 | — |
| D-04 | Có login + voucher | VoucherCode = valid code | Data.RentalPrice < RentalPriceOriginal | P1 | — |
| D-05 | Có truyền ngày → tính giá | FromDate, ToDate hợp lệ | Data có RentalPrice, TotalPrice, RentalDayCount | P1 | — |
| D-06 | Không truyền ngày | FromDate = null, ToDate = null | Data trả info xe nhưng không có giá theo khoảng | P1 | — |
| D-07 | Xe bị tạm ngưng | RentalServiceItemId = suspended item | Status: 0, msg chứa "tạm ngưng" | P1 | BR-BOOK-004 |
| D-08 | BusySchedules trả về đúng | Xe có lịch bận | Data.BusySchedules chứa danh sách timestamp | P2 | — |

---

## 3. CheckBeforeUpdateBookingInfo

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| C-01 | Happy path — xe còn trống | RentalServiceItemId hợp lệ, ngày trống | IsValid: true, InvalidCode: null | P0 | — |
| C-02 | Ngày thuê < minimum required | Thuê 1 ngày nhưng minimum = 2 | IsValid: false, InvalidCode: "MinimumRentalDayRequired" | P0 | BR-BOOK-008 |
| C-03 | Xe đã có booking | Ngày trùng booking confirmed | IsValid: false | P0 | BR-BOOK-005 |
| C-04 | ErrorData chứa thông tin hữu ích | InvalidCode = "MinimumRentalDayRequired" | ErrorData chứa ValidToDate, MsgInCalendar, WarningMsg | P1 | — |

---

## 4. UpdateBookingInfo

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| U-01 | Happy path — tính giá | ID hợp lệ, ngày hợp lệ | Status: 1, Data có RentalPrice, TotalPrice, CanBooking: true | P0 | — |
| U-02 | Áp dụng voucher | VoucherCode hợp lệ | Data.VoucherDiscountPrice > 0, TotalPrice giảm | P0 | — |
| U-03 | Voucher hết lượt | VoucherCode = expired voucher | Không áp dụng giảm giá | P1 | — |
| U-04 | Thay đổi địa chỉ giao xe | DeliveryInfo mới | Data.DeliveryInfo cập nhật, DeliveryFee thay đổi | P1 | — |
| U-05 | Địa chỉ giao xe quá xa | DeliveryInfo xa > maximumDeliveryMileage | CanBooking: false hoặc lỗi distance | P1 | BR-BOOK-011 |
| U-06 | Chưa đăng nhập | Không có token | Status: 0, lỗi unauthorized | P0 | BR-BOOK-009 |

---

## 5. Booking (Đặt xe)

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| B-01 | Happy path — đặt xe thành công | Tất cả input hợp lệ | Status: 1, OrderNumber returned, OrderStatusCode = "OWNER2CONFIRM" | P0 | — |
| B-02 | Thiếu FromDate | FromDate = null | Status: 0, msg = "Vui lòng chọn Ngày đi và Ngày về" | P0 | BR-BOOK-001 |
| B-03 | Thiếu ToDate | ToDate = null | Status: 0, msg = "Vui lòng chọn Ngày đi và Ngày về" | P0 | BR-BOOK-001 |
| B-04 | FromDate > ToDate | FromDate = 2026-03-05, ToDate = 2026-03-01 | Status: 0, msg chứa "không được lớn hơn" | P0 | BR-BOOK-002 |
| B-05 | FromDate == ToDate | Cùng ngày | Status: 0, msg chứa "không được trùng" | P0 | BR-BOOK-002 |
| B-06 | Ngày trong quá khứ | FromDate = 2020-01-01 | Status: 0, msg chứa "quá khứ" | P0 | BR-BOOK-003 |
| B-07 | Xe bị tạm ngưng | IsSuspended = true | Status: 0, msg chứa "tạm ngưng hoặc đã bị xóa" | P0 | BR-BOOK-004 |
| B-08 | Xe chưa duyệt | IsApproved = false | Status: 0, msg = "Không tìm thấy xe" | P0 | BR-BOOK-004 |
| B-09 | Ngày trùng booking khác | Đã có booking confirmed trong khoảng | Status: 0, msg chứa "đã bận" | P0 | BR-BOOK-005 |
| B-10 | Ngày trùng busy date | Owner đánh dấu bận | Status: 0, msg chứa "bận vào" | P0 | BR-BOOK-006 |
| B-11 | Ngày trùng weekday busy | Xe bận thứ 7 hàng tuần | Status: 0, msg chứa "bận vào" | P1 | BR-BOOK-007 |
| B-12 | Số ngày thuê < minimum | Thuê 1 ngày, minimum = 3 | Status: 0, msg chứa "tối thiểu là" | P0 | BR-BOOK-008 |
| B-13 | Chưa đăng nhập | Không có token | Status: 0, msg chứa "đăng nhập" | P0 | BR-BOOK-009 |
| B-14 | Thuê xe của chính mình | Renter ID = Owner ID | Status: 0, msg chứa "không thể thuê dịch vụ cho thuê của chính bạn" | P0 | BR-BOOK-010 |
| B-15 | Giao xe quá xa | Khoảng cách > max | Status: 0, msg chứa "vượt quá" | P1 | BR-BOOK-011 |
| B-16 | Thiếu địa chỉ giao xe | DeliveryAddress trống | Status: 0, msg chứa "Điểm giao nhận" | P1 | — |
| B-17 | Bảo hiểm hết hạn | Xe có insurance expired | Status: 0, msg chứa "bảo hiểm" | P1 | — |
| B-18 | Voucher hợp lệ | VoucherCode = valid | TotalPrice < giá gốc | P1 | — |
| B-19 | Voucher hết lượt dùng | VoucherCode = exhausted | Booking thành công nhưng không giảm giá | P2 | — |
| B-20 | Booking mà không gọi UpdateBookingInfo trước | Gọi POST Booking trực tiếp, skip UpdateBookingInfo | Status: 0, msg chứa "thông tin đặt xe" hoặc thiếu dữ liệu pricing → booking fail do pricing fields = null/0. **Lưu ý**: UpdateBookingInfo set session data (giá, delivery); skip = thiếu data → API reject hoặc tạo đơn lỗi | P1 | BR-BOOK-012 |

---

## 5b. SearchVouchers (Tìm voucher)

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| SV-01 | Happy path — tìm voucher hợp lệ | RentalServiceItemId hợp lệ, FromDate/ToDate hợp lệ | Status: 1, Data chứa danh sách voucher áp dụng được, mỗi voucher có DiscountPercent/DiscountAmount | P1 | — |
| SV-02 | Voucher hết hạn bị loại | Có voucher với ApplyTo < now | Voucher hết hạn không xuất hiện trong kết quả | P1 | — |
| SV-03 | Voucher hết lượt dùng bị loại | Voucher có UsageCount >= MaxUsage | Voucher hết lượt không xuất hiện trong kết quả | P1 | — |
| SV-04 | Voucher không áp dụng được cho xe | Voucher chỉ áp dụng cho category khác | Voucher không xuất hiện trong kết quả hoặc IsApplicable = false | P2 | — |

---

## 5c. Concurrent Booking (Race Condition)

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| CONC-01 | 2 renter đặt cùng xe cùng ngày cùng lúc | Renter A + Renter B gọi Booking đồng thời, cùng xe, cùng ngày | Chỉ 1 request thành công (Status: 1), request còn lại Status: 0 msg chứa "đã bận" — không tạo 2 booking | P1 | BR-BOOK-005 |

---

## 6. OrderConfirm (Chủ xe xác nhận)

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| OC-01 | Happy path — confirm thành công | OrderNumber hợp lệ, user = owner | Status: 1, order → CUS2DEPOSIT | P0 | — |
| OC-02 | Không phải owner | User ≠ owner của đơn | Status: 0, msg chứa "không có quyền" | P0 | — |
| OC-03 | Quá thời gian confirm | Owner2ConfirmEndTime < now | Status: 0, msg chứa "kết thúc" | P0 | BR-BOOK-013 |
| OC-04 | Đơn đã bị huỷ | Status = CUSCANCEL | Status: 0, msg chứa "bị thay đổi" | P0 | — |
| OC-05 | Đơn đã được confirm rồi | Status = CUS2DEPOSIT | Status: 0, msg chứa "bị thay đổi" | P1 | — |

---

## 7. OrderCancel

### Renter Cancel

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| RC-01 | Happy path — renter huỷ OWNER2CONFIRM | Status = OWNER2CONFIRM, CancelReasonId có | Status: 1, order → CUSCANCEL | P0 | BR-CANCEL-001 |
| RC-02 | Happy path — renter huỷ CUS2DEPOSIT | Status = CUS2DEPOSIT | Status: 1, order → CUSCANCEL | P0 | BR-CANCEL-001 |
| RC-03 | Happy path — renter huỷ WAITING2DEPARTURE | Status = WAITING2DEPARTURE | Status: 1, tính phí phạt | P0 | BR-CANCEL-001 |
| RC-04 | Không có quyền | User không phải renter | Status: 0, msg chứa "không có quyền" | P0 | — |
| RC-05 | Thiếu CancelReasonId | CancelReasonId = null | Status: 0, msg chứa "chọn lý do" | P0 | BR-CANCEL-003 |
| RC-06 | Lý do "khác" nhưng không nhập chi tiết | CancelReasonCode = "another_reason", CancelReasonDetail trống | Status: 0, msg chứa "nhập lý do" | P0 | BR-CANCEL-004 |
| RC-07 | Huỷ trong FullRefundWithinMinutes | Huỷ ngay sau khi đặt | Hoàn 100% cọc | P0 | BR-CANCEL-005 |
| RC-08 | Huỷ sát ngày | Huỷ trong NoRefundGreaterThanDays | Không hoàn cọc | P0 | BR-CANCEL-006 |
| RC-09 | Đơn đang INTHETRIP | Status = INTHETRIP | Status: 0, msg chứa "bị thay đổi" | P1 | — |
| RC-10 | Đơn đã DONE | Status = DONE | Status: 0 | P1 | — |
| RC-11 | Huỷ sau 15 phút nhưng >7 ngày trước chuyến → hoàn 70% | Status = WAITING2DEPARTURE, đã cọc, thời gian > 15 phút, FromDate - now > 7 ngày | Status: 1, order → CUSCANCEL, RefundAmount = DepositAmount × 70% | P0 | BR-CANCEL-005/006 |
| RC-12 | Renter huỷ khi status = WAITING2CONFIRMDEPOSIT | Status = WAITING2CONFIRMDEPOSIT, cọc đang chờ xác nhận | Status: 1, order → CUSCANCEL, cọc pending được huỷ | P0 | BR-CANCEL-001 |

### Owner Cancel

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| OCA-01 | Happy path — owner huỷ | Status ∈ valid set, CancelReasonId có | Status: 1, order → OWNERCANCEL | P0 | BR-CANCEL-002 |
| OCA-02 | Hoàn 100% cho renter | Đơn đã cọc | RenterCancelRefund = 100% deposit | P0 | BR-CANCEL-007 |
| OCA-03 | Owner bị penalty | Owner huỷ | OwnerCancelRefund < 0 | P0 | BR-CANCEL-007 |
| OCA-04 | Ngày bận được đánh dấu | Owner huỷ đơn ngày 01-03/03 | ServiceItem_DateBusyRentalSchedule tạo cho 01-03/03 | P1 | BR-CANCEL-008 |
| OCA-05 | Auto tạo QuickOrder | Config bật QuickOrder_RentCar_AutoCreate | QuickOrder mới được tạo cho renter | P2 | BR-CANCEL-009 |

---

## 8. OrderBegin (Nhận xe)

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| OB-01 | Owner confirm giao xe | User = owner, status = WAITING2DEPARTURE | DeliverVehicleByUsername + DeliverVehicleTime set | P0 | BR-BOOK-015 |
| OB-02 | Renter confirm nhận xe | User = renter, status = WAITING2DEPARTURE | ReceiveVehicleByUsername + ReceiveVehicleTime set | P0 | BR-BOOK-015 |
| OB-03 | Cả 2 đã confirm → INTHETRIP | Cả owner + renter đã begin | Status → INTHETRIP | P0 | BR-BOOK-015 |
| OB-04 | Chỉ 1 bên confirm | Chỉ owner begin | Status vẫn WAITING2DEPARTURE | P0 | BR-BOOK-015 |
| OB-05 | Sai status | Status != WAITING2DEPARTURE | Status: 0, msg chứa "bị thay đổi" | P0 | — |
| OB-06 | Không có quyền | User không phải owner/renter | Status: 0, msg chứa "không có quyền" | P0 | — |

---

## 9. OrderEnd (Trả xe)

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| OE-01 | Happy path — kết thúc chuyến | Status = INTHETRIP, user có quyền | Status: 1, order → DONE | P0 | — |
| OE-02 | Sai status | Status != INTHETRIP | Status: 0, msg chứa "bị thay đổi" | P0 | — |
| OE-03 | Không có quyền | User không phải owner/renter | Status: 0, msg chứa "không có quyền" | P0 | — |

---

## 10. OrderReview (Đánh giá)

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| OR-01 | Happy path — đánh giá | Status = DONE, ReviewRating = 5 | Status: 1 | P0 | BR-BOOK-016 |
| OR-02 | Đơn chưa DONE | Status = INTHETRIP | Status: 0, msg chứa "bị thay đổi" | P0 | BR-BOOK-016 |
| OR-03 | User không liên quan | User không phải owner/renter | Status: 0, msg chứa "không có quyền" | P0 | — |

---

## 11. OrderPay (Thanh toán)

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| OP-01 | Happy path — thanh toán thành công | Status = CUS2DEPOSIT | Status: 1, order → WAITING2DEPARTURE | P0 | — |
| OP-02 | Hết hạn cọc | Customer2DepositEndTime < now | Status: 0, msg chứa "hết" | P0 | BR-BOOK-014 |
| OP-03 | Sai status | Status != CUS2DEPOSIT/WAITING2CONFIRMDEPOSIT | Status: 0, msg chứa "bị thay đổi" | P0 | — |
| OP-04 | Không phải renter | User ≠ renter | Status: 0, msg chứa "không có quyền" | P0 | — |

---

## 12. System Auto-Cancel

| # | Scenario | Input | Expected | Priority | Rule |
|---|----------|-------|----------|----------|------|
| AC-01 | Owner không confirm kịp | OWNER2CONFIRM + Owner2ConfirmEndTime <= now | Order → SYSTEMCANCEL | P0 | BR-CANCEL-010 |
| AC-02 | Renter không cọc kịp | CUS2DEPOSIT + Customer2DepositEndTime <= now | Order → SYSTEMCANCEL | P0 | BR-CANCEL-011 |
| AC-03 | Auto-complete quá hạn trả xe | INTHETRIP + ToDate + 60 phút | Order → DONE (auto) | P0 | BR-CANCEL-012 |

---

*Tổng cộng: **85 test scenarios** covering happy paths, negative cases, edge cases, concurrent booking, voucher search, và business rule validations.*

---

## Appendix A: BDD Test Templates (Gherkin)

> Copy các template sau cho AI tạo automated test hoặc manual test scripts.

### Template — Search Vehicle

```gherkin
Feature: Search Rental Vehicles

  Scenario: S-01 Happy path — tìm xe theo địa điểm
    Given user is on the search page
    And API endpoint "POST /api/v1/SearchingRentalService/List"
    When user searches with:
      | Field    | Value              |
      | Address  | Hà Nội             |
      | FromDate | {tomorrow 08:00}   |
      | ToDate   | {tomorrow+3 20:00} |
    Then response Status = 1
    And response Data.Data is not empty
    And each vehicle has RentalPrice > 0

  Scenario: S-03 Search without dates
    Given user is on the search page
    When user searches with:
      | Field    | Value  |
      | Address  | Hà Nội |
      | FromDate | null   |
      | ToDate   | null   |
    Then response Status = 1
    And vehicles are returned without per-day pricing
```

### Template — Booking Order

```gherkin
Feature: Book a Vehicle

  Background:
    Given user is authenticated as Renter
    And a valid RentalServiceItemId exists with available schedule

  Scenario: B-01 Happy path — đặt xe thành công
    When user books with valid FromDate, ToDate, DeliveryAddress
    Then response Status = 1
    And response Data.OrderNumber is not null
    And order status = "OWNER2CONFIRM"
    And Owner receives notification "Đơn hàng mới"

  Scenario: B-04 FromDate > ToDate
    When user books with FromDate = "2026-03-05" and ToDate = "2026-03-01"
    Then response Status = 0
    And response Message contains "không được lớn hơn"

  Scenario: B-14 Thuê xe của chính mình
    Given user is the owner of the vehicle
    When user books their own vehicle
    Then response Status = 0
    And response Message contains "không thể thuê dịch vụ cho thuê của chính bạn"
```

### Template — Cancel Order (Renter)

```gherkin
Feature: Cancel Order — Renter

  Background:
    Given user is authenticated as Renter
    And an order exists with OrderNumber = {orderNumber}

  Scenario: RC-07 Huỷ trong FullRefundWithinMinutes — hoàn 100%
    Given order was created less than 15 minutes ago
    And order status = "OWNER2CONFIRM"
    When renter cancels with valid CancelReasonId
    Then response Status = 1
    And order status = "CUSCANCEL"
    And renter receives 100% deposit refund

  Scenario: RC-08 Huỷ sát ngày — không hoàn cọc
    Given order FromDate is within NoRefundGreaterThanDays (7 days)
    And order status = "WAITING2DEPARTURE"
    When renter cancels with valid CancelReasonId
    Then response Status = 1
    And order status = "CUSCANCEL"
    And renter refund = 0
```

### Template — System Auto-Complete

```gherkin
Feature: System Auto-Complete Order

  Scenario: AC-03 Auto-complete sau 60 phút quá hạn
    Given order status = "INTHETRIP"
    And order ToDate was 60+ minutes ago
    When AutoCompleteOrderEngine runs
    Then order status = "DONE"
    And order IsSystemAutoDone = true
    And TransferMoney flow is triggered
    And both Renter and Owner receive completion notification
```

### Scenario → Business Rule Cross-Reference

| Scenario Range | Business Rule | Document |
|---------------|---------------|----------|
| B-01 → B-19 | BR-BOOK-001 → BR-BOOK-011 | [booking-flow.md](../04_BUSINESS_FLOWS/booking-flow.md) |
| RC-01 → RC-10 | BR-CANCEL-001 → BR-CANCEL-006 | [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md) |
| OCA-01 → OCA-05 | BR-CANCEL-002, BR-CANCEL-007 → 009 | [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md) |
| AC-01 → AC-03 | BR-CANCEL-010 → BR-CANCEL-012 | [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md) |
| OP-01 → OP-04 | BR-BOOK-014 | [booking-flow.md](../04_BUSINESS_FLOWS/booking-flow.md) |
| OB-01 → OB-06 | BR-BOOK-015 | [booking-flow.md](../04_BUSINESS_FLOWS/booking-flow.md) |
| OR-01 → OR-03 | BR-BOOK-016 | [booking-flow.md](../04_BUSINESS_FLOWS/booking-flow.md) |

---

## Appendix B: Request/Response JSON Examples

> Cho AI agents sinh test code. Mỗi endpoint gồm full request body + expected response.

### POST /api/v1/SearchingRentalService/List — Search Vehicles

**Request:**
```json
{
  "FromDate": "2026-03-10T08:00:00",
  "ToDate": "2026-03-12T20:00:00",
  "Address": "Hà Nội",
  "Latitude": 21.028511,
  "Longitude": 105.804817,
  "PageIndex": 1,
  "PageSize": 20,
  "VehicleMakeIds": [],
  "VehicleNoOfSeatIds": [],
  "MinRentalPrice": null,
  "MaxRentalPrice": null,
  "IsElectricEngine": null,
  "IsHaveInsurance": null,
  "OrderBy": []
}
```

**Response (success):**
```json
{
  "Status": 1,
  "Message": "",
  "Data": {
    "Data": [
      {
        "RentalServiceItemId": "123456",
        "Slug": "toyota-vios-ha-noi",
        "VehicleMakeName": "Toyota",
        "VehicleModelName": "Vios",
        "NoOfSeat": 4,
        "RentalPrice": 650000,
        "TotalPrice": 1950000,
        "RentalDayCount": 3,
        "Address": "Hai Bà Trưng, Hà Nội",
        "Latitude": 21.028,
        "Longitude": 105.805,
        "ImageUrl": "https://...",
        "IsHaveInsurance": true,
        "Rating": 4.8,
        "TripCount": 25
      }
    ],
    "TotalCount": 42
  }
}
```

### POST /api/v1/RentalService/Booking — Create Booking

**Request:**
```json
{
  "RentalServiceItemId": "123456",
  "FromDate": "2026-03-10T08:00:00",
  "ToDate": "2026-03-12T20:00:00",
  "DeliveryAddress": "12 Trần Hưng Đạo, Hoàn Kiếm, Hà Nội",
  "DeliveryLatitude": 21.025,
  "DeliveryLongitude": 105.855,
  "VoucherCode": "",
  "UI_TimezoneOffset": -420,
  "Note": ""
}
```

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Response (success — B-01):**
```json
{
  "Status": 1,
  "Message": "",
  "Data": {
    "OrderNumber": "ORD-240310-001",
    "OrderId": "789012",
    "StatusCode": "OWNER2CONFIRM",
    "TotalPrice": 1950000,
    "DepositAmount": 585000,
    "Owner2ConfirmEndTime": "2026-03-10T14:00:00Z"
  }
}
```

**Response (error — B-02 thiếu FromDate):**
```json
{
  "Status": 0,
  "Message": "Vui lòng chọn Ngày đi và Ngày về",
  "Data": null
}
```

### POST /api/v1/Order_ListView_RentCar/RenterCancel — Cancel Order

**Request:**
```json
{
  "OrderNumber": "ORD-240310-001",
  "CancelReasonId": "55",
  "CancelReasonCode": "",
  "CancelReasonDetail": ""
}
```

**Response (success — RC-07 full refund):**
```json
{
  "Status": 1,
  "Message": "Huỷ đơn thành công",
  "Data": {
    "OrderNumber": "ORD-240310-001",
    "StatusCode": "CUSCANCEL",
    "RefundAmount": 585000,
    "RefundPercent": 100
  }
}
```

### POST /api/v1/RentalService/OrderConfirm — Owner Confirm

**Request:**
```json
{
  "OrderNumber": "ORD-240310-001"
}
```

**Response (success — OC-01):**
```json
{
  "Status": 1,
  "Message": "",
  "Data": {
    "OrderNumber": "ORD-240310-001",
    "StatusCode": "CUS2DEPOSIT",
    "Customer2DepositEndTime": "2026-03-10T17:00:00Z"
  }
}
```

### POST /api/v1/RentalService/OrderPay — Renter Pay Deposit

**Request:**
```json
{
  "OrderNumber": "ORD-240310-001",
  "Amount": 585000
}
```

**Response (success — OP-01):**
```json
{
  "Status": 1,
  "Message": "",
  "Data": {
    "OrderNumber": "ORD-240310-001",
    "StatusCode": "WAITING2DEPARTURE",
    "DepositDoneAt": "2026-03-10T15:30:00Z"
  }
}
```

---

## Appendix C: Test Infrastructure Guide

> Hướng dẫn setup cho AI agents hoặc QC khi sinh/chạy test.

### Authentication

```
1. Đăng nhập: POST /api/v1/User/Login { "Phone": "0901234567", "Password": "..." }
   → Response.Data.Token = JWT Bearer token
2. Gắn header: Authorization: Bearer {token}
3. Test account "test_no_token": gửi request KHÔNG có Authorization header
```

### Test Data Seeding

```
Test data được liệt kê trong test-coverage-matrix.md Section 4.
Các accounts test: test_renter_01, test_owner_01, etc.
Các xe test: vehicle_active_01, vehicle_suspended, etc.

Cách seed:
- Option A: Dùng SQL insert trực tiếp vào DB test (preferred cho integration test)
- Option B: Gọi API tạo data tuần tự (Login → InsertNewRentalService → Submit2Review → admin approve)
- Option C: Restore DB snapshot có sẵn test data
```

### Concurrent Test (CONC-01)

```csharp
// C# example cho race condition test:
var tasks = new[] {
    Task.Run(() => httpClient_RenterA.PostAsync("/api/v1/RentalService/Booking", bodyA)),
    Task.Run(() => httpClient_RenterB.PostAsync("/api/v1/RentalService/Booking", bodyB))
};
var results = await Task.WhenAll(tasks);
// Assert: exactly 1 success (Status=1), 1 failure (Status=0, "đã bận")
```

### Assertion Patterns

```
- "msg chứa X"  → response.Message.Contains("X")     // dùng Contains()
- "msg = X"      → response.Message == "X"              // dùng Equals()
- "Status: 1"    → response.Status == 1                 // application-level, HTTP vẫn 200
- "Status: 0"    → response.Status == 0                 // application-level error, HTTP vẫn 200
- B-13 "no token" → HTTP 401 hoặc Status 0 tuỳ middleware config
```

### Scenario ID Convention

```
File                                    | ID Prefix | BDD Alias
rental-service.test-scenarios.md        | OB-*      | OBG-* (trong bdd/order-lifecycle.bdd.md)
cancel-flow.test-scenarios.md           | RCC-*     | RCC-* (giống nhau)
ewallet-order-lifecycle.test-scenarios  | OEN-*     | OEN-* (giống nhau)
owner-vehicle-management.test-scenarios | OVM-*     | — (chưa có BDD)

Quy tắc: Khi cross-reference, luôn dùng ID từ file .test-scenarios.md gốc.
BDD files có thể dùng alias prefix khác → mapping ở bảng trên.
```
