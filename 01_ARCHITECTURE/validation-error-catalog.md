# Validation Rules & Error Catalog

> Tài liệu đầy đủ validation rules, error messages, discount logic, timezone handling.

---

## Mục lục
- [1. Order Booking Validation Rules](#1-order-booking-validation-rules)
- [2. Cancel Validation Rules](#2-cancel-validation-rules)
- [3. TextDisplayKeys Error Message Catalog](#3-textdisplaykeys-error-message-catalog)
- [4. Discount Code Validation & Release Logic](#4-discount-code-validation--release-logic)
- [5. Timezone Handling Patterns](#5-timezone-handling-patterns)
- [6. Rental Service Availability Validation](#6-rental-service-availability-validation)

---

## 1. Order Booking Validation Rules

**File:** `AllianceMiddlemanWebAPI.Service/Services/MainBusiness/RentalServiceItem/RentalServiceItemService_Booking.cs`
**Method:** `CheckCanBookRentalService(TParam param)`

### BR-BOOK: Booking Rules

| Rule ID | Condition | Error Key | Default Message |
|---------|-----------|-----------|-----------------|
| BR-BOOK-001 | FromDate hoặc ToDate null | `msg_error_no_booking_fromdate_todate` | Vui lòng chọn Ngày đi và Ngày về |
| BR-BOOK-002 | FromDate > ToDate | `msg_error_booking_fromdate_greater_todate` | Ngày đi không được lớn hơn Ngày về |
| BR-BOOK-003 | FromDate == ToDate | `msg_error_booking_fromdate_equal_todate` | Ngày đi không được trùng với Ngày về |
| BR-BOOK-004 | FromDate hoặc ToDate < Now | `msg_error_booking_from_date_to_date_incorrect` | Ngày đi hoặc Ngày về không được là thời gian quá khứ |
| BR-BOOK-005 | Không chọn địa chỉ giao xe | `msg_error_no_booking_address` | Vui lòng chọn Điểm giao nhận xe |
| BR-BOOK-006 | Khoảng cách giao xe > max | `msg_error_delivery_distance_exceed_maximum` | Điểm giao nhận xe vượt quá [Distance] |
| BR-BOOK-007 | Lời nhắn rỗng | `msg_error_message_to_owner_is_empty` | Vui lòng nhập lời nhắn cho chủ xe |
| BR-BOOK-008 | Chưa đăng nhập | `msg_error_must_login_to_book_rental_service` | Bạn phải đăng nhập |
| BR-BOOK-009 | Owner thuê xe chính mình | `msg_error_user_rent_their_own_service` | Chủ xe không thể thuê chính xe của mình |
| BR-BOOK-010 | Service chưa approved | (inline) | Dịch vụ chưa được duyệt |
| BR-BOOK-011 | Service bị suspended | (inline) | Dịch vụ đang bị đình chỉ |
| BR-BOOK-012 | Service bị deactivated | (inline) | Dịch vụ đã bị vô hiệu |
| BR-BOOK-013 | Xe đang bận (booked) | `msg_error_booking_rental_date_incorrect` | Dịch vụ đã bận. Vui lòng chọn lại ngày thuê |
| BR-BOOK-014 | Xe bận theo thứ trong tuần | (dynamic) | Xe bận vào các ngày {weekdays} |
| BR-BOOK-015 | Xe bận theo ngày cụ thể | (dynamic) | Xe bận từ {date1} đến {date2} |
| BR-BOOK-016 | Số ngày thuê < yêu cầu tối thiểu | `msg_error_rental_day_count_not_met_require` | Ngày thuê không đủ yêu cầu tối thiểu |
| BR-BOOK-017 | Bảo hiểm xe hết hạn | `msg_error_car_insurance_expired` | Bảo hiểm xe đã hết hạn |

### Implementation Details

**BR-BOOK-013 (Booked schedule overlap check):**
```csharp
// 3 điều kiện overlap:
(c.FromDate <= fromDateUTC && fromDateUTC <= c.ToDate) ||  // pickup trong booking
(c.FromDate <= toDateUTC && toDateUTC <= c.ToDate) ||      // return trong booking
(fromDateUTC <= c.FromDate && c.ToDate <= toDateUTC)       // booking nằm trong rental
// VÀ c.IsBooked == true
```

**BR-BOOK-014 (Weekday busy — dynamic message):**
```
Lấy ServiceItem_WeekdaysBusyRentalSchedule WHERE IsBusy == true
Loop mỗi ngày thuê: check DayOfWeek có nằm trong weekDays bận không
Message: hiển thị danh sách thứ bận (Thứ 2, Thứ 4...)
```

**BR-BOOK-016 (Minimum rental day):**
```csharp
// Lấy MinimumRentalDayRequired cho khoảng thời gian thuê
// Convert dates bằng UI_TimezoneOffset
// So sánh NumberOfRentalDay < MinimumRentalDay
```

### Validation Test Scenarios (Sample Request → Response)

**Test 1 — BR-BOOK-002: FromDate > ToDate**
```json
// Request POST /api/v1/RentalServiceItem/BookRentalService
{
  "FromDate": "2024-03-15T10:00:00+07:00",
  "ToDate": "2024-03-14T10:00:00+07:00",
  "RentalServiceItemId": 500,
  "DeliveryAddressLatitude": 10.7769,
  "DeliveryAddressLongitude": 106.7009,
  "MessageToOwner": "Cho tôi thuê xe"
}
// Response (HTTP 200, business error)
{ "Status": 0, "Message": "Ngày đi không được lớn hơn Ngày về", "Data": null }
```

**Test 2 — BR-BOOK-009: Owner thuê xe chính mình**
```json
// Request (user login = owner of vehicle)
{
  "FromDate": "2024-03-20T08:00:00+07:00",
  "ToDate": "2024-03-22T20:00:00+07:00",
  "RentalServiceItemId": 500
}
// Response
{ "Status": 0, "Message": "Chủ xe không thể thuê chính xe của mình", "Data": null }
```

**Test 3 — BR-BOOK-013: Xe đang bận (overlap)**
```json
// Xe 500 đã có booking: 18/03 00:00 UTC → 20/03 00:00 UTC
// Request: thuê 19/03 → 21/03 (overlap case 1: fromDate nằm trong booking)
{
  "FromDate": "2024-03-19T08:00:00+07:00",
  "ToDate": "2024-03-21T20:00:00+07:00",
  "RentalServiceItemId": 500
}
// Response
{ "Status": 0, "Message": "Dịch vụ đã bận. Vui lòng chọn lại ngày thuê", "Data": null }
```

**Test 4 — BR-BOOK-017: Bảo hiểm hết hạn**
```json
// Xe 500 có HaveInsurance=true, CoverageEndDate=2024-03-10
// Request: thuê 11/03 → 13/03
{
  "FromDate": "2024-03-11T08:00:00+07:00",
  "ToDate": "2024-03-13T20:00:00+07:00",
  "RentalServiceItemId": 500
}
// Response
{ "Status": 0, "Message": "Bảo hiểm xe đã hết hạn", "Data": null }
```

**Test 5 — Success (tất cả validation pass)**
```json
// Request
{
  "FromDate": "2024-03-20T08:00:00+07:00",
  "ToDate": "2024-03-22T20:00:00+07:00",
  "RentalServiceItemId": 500,
  "DeliveryAddressLatitude": 10.7769,
  "DeliveryAddressLongitude": 106.7009,
  "DeliveryAddress": "123 Lê Lợi, Q1, HCM",
  "MessageToOwner": "Cho tôi thuê xe ngày 20-22/3",
  "UI_TimezoneOffset": -420
}
// Response
{
  "Status": 1,
  "Message": "",
  "Data": {
    "OrderNumber": "ORD-20240320-042",
    "StatusCode": "OWNER2CONFIRM",
    "TotalPrice": 2400000,
    "DepositAmount": 720000
  }
}
```

**File:** `AllianceMiddlemanWebAPI.Service/Services/MainBusiness/Order/OrderService_UserAction.cs`

### Order Status Codes (từ DB — ConfigOrderStatus)

| # | Code | Name (Vietnamese) | ColorCode | OrderNo |
|---|------|-------------------|-----------|---------|
| 1 | `OWNER2CONFIRM` | Chờ xác nhận | #FFA500 | 1 |
| 2 | `CUS2DEPOSIT` | Chờ thanh toán | #FFD700 | 2 |
| 3 | `WAITING2CONFIRMDEPOSIT` | Chờ xác nhận cọc | #DAA520 | 3 |
| 4 | `WAITING2DEPARTURE` | Chờ xuất phát | #4169E1 | 4 |
| 5 | `INTHETRIP` | Đang trong chuyến | #32CD32 | 5 |
| 6 | `DONE` | Hoàn thành | #228B22 | 6 |
| 7 | `CUSCANCEL` | Khách huỷ | #DC143C | 7 |
| 8 | `OWNERCANCEL` | Chủ xe huỷ | #B22222 | 8 |
| 9 | `SYSTEMCANCEL` | Hệ thống huỷ | #808080 | 9 |

### Order Status Flow
```
OWNER2CONFIRM ──(owner confirm)──► CUS2DEPOSIT ──(renter pay)──► WAITING2CONFIRMDEPOSIT
       │                                │                               │
       │                                │                         (confirm deposit)
       │                                │                               │
       ▼                                ▼                               ▼
  OWNERCANCEL                      CUSCANCEL                   WAITING2DEPARTURE
  CUSCANCEL                        OWNERCANCEL                       │
  SYSTEMCANCEL                     SYSTEMCANCEL              (begin trip)
                                                                     │
                                                                     ▼
                                                                INTHETRIP
                                                                     │
                                                              (end trip / auto)
                                                                     │
                                                                     ▼
                                                                   DONE
```

### Owner Cancel Rules (CheckCanOwnerCancel)

| Rule | Condition | Error Key | Message |
|------|-----------|-----------|---------|
| BR-CANCEL-OWN-001 | Owner không có quyền (permission check) | `msg_error_no_permission_to_cancel_booking` | Bạn không có quyền để từ chối đơn hàng |
| BR-CANCEL-OWN-002 | Status không cho phép cancel | `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. |

**Owner chỉ cancel được ở các status:**
- `OWNER2CONFIRM` — Đang chờ owner xác nhận
- `CUS2DEPOSIT` — Đang chờ renter cọc
- `WAITING2CONFIRMDEPOSIT` — Đang chờ xác nhận cọc
- `WAITING2DEPARTURE` — Đang chờ xuất phát

### Renter Cancel Rules (CheckCanRenterCancel)

| Rule | Condition | Error Key | Message |
|------|-----------|-----------|---------|
| BR-CANCEL-REN-001 | Renter không có quyền | `msg_error_renter_no_permission_to_cancel_booking` | Bạn không có quyền hủy chuyến |
| BR-CANCEL-REN-002 | Status không cho phép | `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. |

**Renter chỉ cancel được ở các status:**
- `OWNER2CONFIRM`
- `CUS2DEPOSIT`
- `WAITING2CONFIRMDEPOSIT`
- `WAITING2DEPARTURE`

### System Auto-Cancel

**Trigger conditions:**
- `Owner2ConfirmEndTime <= DateTime.UtcNow` → Owner không confirm kịp
- `Customer2DepositEndTime <= DateTime.UtcNow` → Renter không cọc kịp

**Actions:**
1. Status → `SYSTEMCANCEL`
2. `ReleaseDiscountCode(orderIds)` → giải phóng mã giảm giá
3. Notification cho cả 2 bên

### Other Action Validations

| Action | Method | Valid Status | Error Key |
|--------|--------|-------------|-----------|
| Owner Confirm | CheckCanOwnerConfirm | OWNER2CONFIRM | `msg_error_no_permission_to_confirm_booking` |
| Renter Pay Deposit | CheckCanRenterPay | CUS2DEPOSIT, WAITING2CONFIRMDEPOSIT | `msg_error_no_permission_to_pay_booking` |
| Begin Trip | CheckCanBeginTrip | WAITING2DEPARTURE | `msg_error_no_perrmission_booking_begin_trip` |
| End Trip | CheckCanEndTrip | INTHETRIP | `msg_error_no_perrmission_booking_end_trip` |
| Deposit Timeout | (auto) | CUS2DEPOSIT | `msg_error_booking_cus_to_deposit_time_expired_renter` |
| Owner Confirm Timeout | (auto) | OWNER2CONFIRM | `msg_error_wait_booking_time_has_expired` |

### Cancel Refund Summary (Quick Reference)

> Chi tiết đầy đủ: [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md)

| Ai cancel | Khi nào | Renter nhận | Owner nhận | Penalty |
|-----------|---------|-------------|------------|---------|
| Renter | OWNER2CONFIRM (chưa cọc) | — | — | 0 |
| Renter | CUS2DEPOSIT (chưa cọc) | — | — | 0 |
| Renter | WAITING2DEPARTURE, ≤1h | 70% deposit | 30% deposit | 30% cho renter |
| Renter | WAITING2DEPARTURE, >1h-7d | 70% deposit | 30% deposit | 30% cho renter |
| Owner | OWNER2CONFIRM | — | — | Tỉ lệ cancel tăng |
| Owner | WAITING2DEPARTURE | 100% deposit | 0 | Hoàn toàn cho renter |
| System | Timeout | 100% deposit | 0 | Auto-release discount codes |

**Refund formulas (từ `helper-algorithms.md` section 4.4):**
```
PlanProfitAmount = SubTotal × CompletionFee% + ServiceFee - DiscountMoney
                   + DeliveryFee × CompletionFee%
OwnerRemainAmount = DepositAmount - PlanProfitAmount - InsuranceFee
OwnerCancelRefund = (DB field, tính theo cancel policy)
RenterCancelRefund = DepositAmount - OwnerCancelRefund
```

---

## 3. TextDisplayKeys Error Message Catalog

**File:** `AllianceMiddlemanWebAPI.Core/Services/TextDisplayKeys.cs`

> Tất cả error messages được quản lý qua `TextDisplayHelper.GetValue(key, defaultMessage)`.
> Admin có thể thay đổi message qua CMS mà không cần deploy lại code.

### Booking & Order Errors

| Key | Default Vietnamese | Context |
|-----|-------------------|---------|
| `msg_error_no_booking_fromdate_todate` | Vui lòng chọn Ngày đi và Ngày về | Booking form |
| `msg_error_booking_fromdate_greater_todate` | Ngày đi không được lớn hơn Ngày về | Booking form |
| `msg_error_booking_fromdate_equal_todate` | Ngày đi không được trùng với Ngày về | Booking form |
| `msg_error_booking_from_date_to_date_incorrect` | Ngày đi hoặc Ngày về không được là thời gian quá khứ | Booking form |
| `msg_error_no_booking_address` | Vui lòng chọn Điểm giao nhận xe | Booking form |
| `msg_error_delivery_distance_exceed_maximum` | Điểm giao nhận xe vượt quá [Distance] | Delivery check |
| `msg_error_message_to_owner_is_empty` | Vui lòng nhập lời nhắn cho chủ xe | Booking form |
| `msg_error_booking_rental_date_incorrect` | Dịch vụ đã bận. Vui lòng chọn lại ngày thuê | Availability check |
| `msg_error_rental_day_count_not_met_require` | Ngày thuê không đủ yêu cầu tối thiểu | Min days check |
| `msg_error_order_not_found` | Không tìm thấy mã đơn hàng | Order lookup |

### Authentication & Permission Errors

| Key | Default Vietnamese | Context |
|-----|-------------------|---------|
| `msg_error_must_login_to_book_rental_service` | Bạn phải đăng nhập | Auth guard |
| `msg_error_user_rent_their_own_service` | Chủ xe không thể thuê chính xe của mình | Self-rental block |
| `msg_error_no_permission_to_confirm_booking` | Bạn không có quyền xác nhận đơn hàng | Owner action |
| `msg_error_no_permission_to_pay_booking` | Bạn không có quyền thanh toán | Renter action |
| `msg_error_no_permission_to_cancel_booking` | Bạn không có quyền để từ chối đơn hàng | Owner cancel |
| `msg_error_renter_no_permission_to_cancel_booking` | Bạn không có quyền hủy chuyến | Renter cancel |
| `msg_error_not_car_owner` | Bạn không phải chủ xe | Owner verify |

### Status & Timeout Errors

| Key | Default Vietnamese | Context |
|-----|-------------------|---------|
| `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. | Concurrent update |
| `msg_error_wait_booking_time_has_expired` | Thời gian chờ chủ xe xác nhận đã hết | Owner timeout |
| `msg_error_booking_cus_to_deposit_time_expired_renter` | Thời gian đặt cọc đã hết | Deposit timeout |

### Trip Errors

| Key | Default Vietnamese | Context |
|-----|-------------------|---------|
| `msg_error_no_perrmission_booking_begin_trip` | Bạn không có quyền bắt đầu chuyến | Trip start |
| `msg_error_no_perrmission_booking_end_trip` | Bạn không có quyền kết thúc chuyến | Trip end |

### Voucher/Discount Errors

| Key | Default Vietnamese | Context |
|-----|-------------------|---------|
| `msg_error_voucher_rich_limit` | Mã khuyến mãi không hợp lệ hoặc đã hết hiệu lực, vui lòng kiểm tra lại | Discount apply |

### Insurance Errors

| Key | Default Vietnamese | Context |
|-----|-------------------|---------|
| `msg_error_car_insurance_expired` | Bảo hiểm xe đã hết hạn | Insurance check |

### API Error Response Format

Tất cả validation errors trả về cùng một format:

```json
{
  "Status": 0,
  "Message": "Error message text (từ TextDisplayHelper)",
  "Data": null
}
```

- `Status: 0` = lỗi, `Status: 1` = thành công
- `Message` = nội dung từ `TextDisplayHelper.GetValue(key, defaultMessage)`
- Admin có thể thay đổi message text qua CMS → thay đổi phản ánh realtime
- Nếu error key không tồn tại trong CMS → dùng `defaultMessage` hardcode trong code

---

## 4. Discount Code Validation & Release Logic

**File:** `RentalServiceItemService_Booking.cs`
**Method:** `CheckDiscountCodeCanUse(string voucherCode)`

### Validation Flow

```
Step 1: Tìm DiscountCode trong DB
        WHERE Code == voucherCode AND !IsDisable

Step 2: Check thời hạn
        ValidFromDate <= Now <= ValidToDate

Step 3: Check giới hạn sử dụng per user
        Nếu MaximumUsedPerUser == 1:
          → Query Order_DiscountCode_Applied_OneTimeUse
          → User đã dùng code này chưa? (WHERE !IsCanceled)
          → Nếu đã dùng → reject

Step 4: Check tổng giới hạn
        Query DiscountCode_Summary
        Nếu IsReachLimit == true → reject

Step 5: Nếu fail bất kỳ step → Error:
        msg_error_voucher_rich_limit
```

### Discount Code Types

| Type | Logic | Ví dụ |
|------|-------|-------|
| Percentage | `discountMoney = totalPrice × DiscountPercent / 100` | 10% giảm |
| Fixed Amount | `discountMoney = DiscountMoney` | Giảm 200k |
| Maximum Cap | `Math.Min(discountMoney, MaximumDiscountMoney)` | Tối đa 500k |

### GetDiscountMoney() — Computation Detail

```
Method: RentCarHelper.GetDiscountMoney(decimal totalPrice, DiscountCode code)

Step 1: Xác định DiscountMethodCode
        Nếu code.DiscountMethodCode == "PERCENT":
          discountMoney = totalPrice × code.DiscountPercent / 100
        Nếu code.DiscountMethodCode == "MONEY":
          discountMoney = code.DiscountMoney

Step 2: Áp dụng cap (MaximumDiscountMoney)
        Nếu code.MaximumDiscountMoney != null VÀ code.MaximumDiscountMoney > 0:
          discountMoney = Math.Min(discountMoney, code.MaximumDiscountMoney)

Step 3: Return discountMoney

Ví dụ: totalPrice=2,400,000, DiscountMethodCode="PERCENT",
        DiscountPercent=15, MaximumDiscountMoney=200,000
  → Step 1: 2,400,000 × 15% = 360,000
  → Step 2: Min(360,000, 200,000) = 200,000 ✓
  → Giảm 200,000 VNĐ (bị cap)

Ví dụ: totalPrice=800,000, DiscountMethodCode="MONEY",
        DiscountMoney=100,000, MaximumDiscountMoney=null
  → Step 1: 100,000 (fixed)
  → Step 2: no cap
  → Giảm 100,000 VNĐ
```

### DiscountCode Entity (từ DB — 27 columns quan trọng)

| Column | Type | Mô tả |
|--------|------|-------|
| Code | varchar | Mã giảm giá (unique) |
| DiscountMethodCode | varchar | "PERCENT" hoặc "MONEY" |
| DiscountPercent | numeric | % giảm (nếu PERCENT) |
| DiscountMoney | numeric | Số tiền giảm (nếu MONEY) |
| MaximumDiscountMoney | numeric | Giảm tối đa (cap) |
| MaximumUsedTotal | integer | Tổng lượt dùng tối đa |
| MaximumUsedPerUser | integer | Lượt dùng/user (thường = 1) |
| ValidFromDate | timestamptz | Bắt đầu hiệu lực |
| ValidToDate | timestamptz | Hết hiệu lực |
| IsDisable | boolean | Vô hiệu hoá |

### DiscountCode_Summary Entity (từ DB — 13 columns)

| Column | Type | Mô tả |
|--------|------|-------|
| DiscountCodeId | bigint | FK → DiscountCode |
| UsedCount | integer | Số lần đã sử dụng |
| IsReachLimit | boolean | Đã hết lượt dùng |

### Order_DiscountCode_Applied_OneTimeUse (từ DB — 18 columns)

| Column | Type | Mô tả |
|--------|------|-------|
| OrderId | bigint | FK → Order |
| DiscountCodeId | bigint | FK → DiscountCode |
| UserLoginId | bigint | FK → UserLogin |
| IsCanceled | boolean | Đã huỷ → giải phóng lượt dùng |

### Discount Code Filters (Search)

Khi hiển thị discount trên search results:
```
IsActive == true
Code starts with "S_RENTCAR_" hoặc "S_RENT_CAR_"
DiscountPercent > 0 (percentage type) HOẶC DiscountMoney > 0 (fixed type)
```

### Release Logic (Khi cancel/system cancel)

**Method:** `ReleaseDiscountCode(long[] orderIds)`

```
1. Tìm Order_DiscountCode_Applied_OneTimeUse cho các orderIds
2. Mark IsCanceled = true
3. Cập nhật DiscountCode_Summary: giảm UsedCount
4. Nếu IsReachLimit == true VÀ UsedCount < MaximumUsed:
   → IsReachLimit = false (re-enable code)
```

---

## 5. Timezone Handling Patterns

### UI_TimezoneOffset Convention

**Định nghĩa:** Integer, đơn vị phút, offset từ UTC

```
Vietnam (UTC+7): UI_TimezoneOffset = -420
UTC:             UI_TimezoneOffset = 0
```

**Công thức chuyển đổi:**
```
Client time = UTC + (-UI_TimezoneOffset) phút
            = UTC - UI_TimezoneOffset
            = UTC.AddMinutes(-UI_TimezoneOffset)

UTC time = Client time + UI_TimezoneOffset
         = ClientTime.AddMinutes(UI_TimezoneOffset)
```

**Lưu ý:** Dấu âm (-420) nghĩa là client time = UTC + 420 phút = UTC + 7h

### Pattern 1: EzyDateTimeHelper (Query)

```csharp
// Convert client date → UTC cho DB query
DateTime fromDateUTC = EzyDateTimeHelper.DateTime_FromClientToServer_SQL(param.FromDate).Value;
DateTime toDateUTC = EzyDateTimeHelper.DateTime_FromClientToServer_SQL(param.ToDate).Value;
```

**Dùng ở:** Booking validation, availability check, search pipeline

### Pattern 2: RentalServiceHelper (Business hours)

```csharp
// Convert UTC → client time cho business logic
if (now == null) now = DateTime.UtcNow.AddMinutes(-UI_TimezoneOffset.Value);
// → now = client local time

// Convert result back → UTC
result = result.Value.AddMinutes(UI_TimezoneOffset.Value);
// → result = UTC time
```

**Dùng ở:** `GetEndTime()` tính deadline trong business hours

### Pattern 3: BuildDictPriceByDate (Price lookup)

```csharp
// Convert UTC → Vietnam time (+7h = +420 phút)
fromDate = fromDate.Value.AddMinutes(420);  // Hardcoded VN timezone
toDate = toDate.Value.AddMinutes(420);
```

**Lưu ý:** Giá thuê (DateRentalPrice) được cấu hình theo giờ VN, nên cần convert sang cùng timezone để lookup chính xác.

### Pattern 4: DateTimeConvertHelper

```csharp
// Epoch milliseconds → formatted string
public static string DoubleToDateString(double? epochMilliseconds,
    string timezoneId = null, string format = "yyyy-MM-dd HH:mm:ss")
{
    var utcTime = DateTimeOffset.FromUnixTimeMilliseconds((long)epochMilliseconds).UtcDateTime;
    var tz = GetTimeZone(timezoneId);  // "Asia/Ho_Chi_Minh" ↔ "SE Asia Standard Time"
    var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
    return localTime.ToString(format);
}
```

### Pattern 5: GeneralHelper (UI display)

```csharp
// Hiển thị datetime cho UI
public static string MsgShowDateTimeUTC(DateTime? DateFrom, DateTime? DateTo,
    int? UI_TimezoneOffset, string format = "yyyy-MM-dd HH:mm:ss")
{
    // Apply: AddMinutes(-UI_TimezoneOffset)
    // → Chuyển UTC → client local time cho hiển thị
}
```

### Timezone Flow Summary

```
┌─────────┐     ┌───────────┐     ┌────────────┐     ┌─────────┐
│ Client   │────►│ API param │────►│ Server UTC │────►│ DB UTC  │
│ (VN +7h) │     │ (client)  │     │ (-offset)  │     │ (UTC)   │
└─────────┘     └───────────┘     └────────────┘     └─────────┘

                ┌────────────┐     ┌───────────┐     ┌─────────┐
                │ DB UTC     │────►│ +offset   │────►│ Client  │
                │ (UTC)      │     │ (convert)  │     │ display │
                └────────────┘     └───────────┘     └─────────┘
```

**Nguyên tắc:**
- DB lưu UTC
- API nhận client time → convert sang UTC trước khi query/save
- API trả về UTC → client convert sang local time
- Exception: `DateRentalPrice` cấu hình theo giờ VN → hardcode +420 khi lookup

### Ví dụ cụ thể: Booking từ 08:00 → 20:00 ngày 15/03/2026 (giờ VN)

```
Client gửi:
  FromDate = 2026-03-15 08:00:00 (VN, UTC+7)
  ToDate   = 2026-03-15 20:00:00 (VN, UTC+7)
  UI_TimezoneOffset = -420

Pattern 1 (Query → UTC):
  fromDateUTC = 2026-03-15 08:00:00 + (-420 phút) = 2026-03-15 01:00:00 UTC
  toDateUTC   = 2026-03-15 20:00:00 + (-420 phút) = 2026-03-15 13:00:00 UTC

Pattern 2 (Business hours: tính deadline Owner confirm 3h trong giờ hành chính 7:00-21:00):
  now_UTC = 2026-03-15 02:30:00 UTC
  now_VN  = now_UTC + (-(-420)) = 2026-03-15 09:30:00 VN
  deadline = 09:30 + 3h = 12:30 VN (trong giờ hành chính → OK)
  deadline_UTC = 12:30 + (-420) = 2026-03-15 05:30:00 UTC

Pattern 3 (Price lookup: giá ngày 15/03):
  fromDate_VN = fromDateUTC.AddMinutes(420) = 2026-03-15 08:00 VN
  → Lookup ServiceItem_DateRentalPrice WHERE FromDate <= 2026-03-15 AND ToDate >= 2026-03-15
  → Tìm thấy giá 800.000đ/ngày

Kết quả trả về client:
  FromDate (UTC) = 2026-03-15 01:00:00
  → Client convert: + 420 phút = 2026-03-15 08:00:00 VN ✓
```

### Pattern 6: Stored Procedure timezone (PostgreSQL)

Một số SPs sử dụng `AT TIME ZONE` trực tiếp trong SQL:

```sql
-- sp_Get_OrderOverTimeButDontEnd_Json:
-- Auto-complete vào cuối ngày VN (≥23:50)
EXTRACT(HOUR FROM NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh') * 60
+ EXTRACT(MINUTE FROM NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh') >= 1430
-- 1430 phút = 23h50 VN

-- sp_GetOwnerForDiscountNotification_Json:
-- Calendar week theo VN timezone
date_trunc('week', NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh')
```

**Lưu ý:** DB columns `FromDate`/`ToDate` dùng `timestamptz` (timestamp with time zone), PostgreSQL tự xử lý timezone khi compare. Các SP chỉ cần `AT TIME ZONE` khi cần logic theo giờ VN cụ thể (như "cuối ngày VN").

---

## 6. Rental Service Availability Validation

**File:** `RentalServiceItemService_Booking.cs`
**Method:** `CheckRentalServiceNotBusy(TParam param)`

### Three-Level Busy Check

**Level 1: Booked Schedule (confirmed bookings)**
```csharp
// Check ServiceItem_BookedRentalSchedule
// Overlap conditions (3 cases):
// 1. fromDateUTC nằm trong [booking.FromDate, booking.ToDate]
// 2. toDateUTC nằm trong [booking.FromDate, booking.ToDate]
// 3. Booking nằm hoàn toàn trong [fromDateUTC, toDateUTC]
// AND IsBooked == true
// → Error: msg_error_booking_rental_date_incorrect
```

**Level 2: Weekday Busy (thứ bận hàng tuần)**
```csharp
// Check ServiceItem_WeekdaysBusyRentalSchedule WHERE IsBusy == true
// Loop mỗi ngày thuê: check DayOfWeek có trong weekDays bận
// → Dynamic error showing busy weekdays
```

**Level 3: Date-Specific Busy (ngày bận cụ thể)**
```csharp
// Check ServiceItem_DateBusyRentalSchedule
// Overlap: !(fromDateClient > ToDate || toDateClient < FromDate)
// AND IsBusy == true
// → Dynamic error showing busy date ranges by month
```

### Insurance Validation

```csharp
// Nếu Vehicle_RentalSetting.HaveInsurance == true:
//   Tìm Vehicle_InsuranceInformation
//   Check: IsVerified == true
//   Check: CoverageStartDate <= fromDateUTC
//   Check: toDateUTC <= CoverageEndDate (+1 day - 1 second)
//   Fail → không hiển thị xe trong search results
```

---

*Xem thêm: [booking-flow.md](../04_BUSINESS_FLOWS/booking-flow.md) | [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md) | [helper-algorithms.md](./helper-algorithms.md)*

---

## QC Quick Reference: Error Code Lookup

Bảng flat alphabetical tất cả error keys — QC dùng để tra cứu nhanh expected error message cho mỗi test case:

| Error Key | Default Message | Trigger Rule |
|-----------|----------------|-------------|
| `msg_error_booking_cus_to_deposit_time_expired_renter` | Thời gian đặt cọc đã hết | BR-BOOK-014 (deposit timeout) |
| `msg_error_booking_from_date_to_date_incorrect` | Ngày đi hoặc Ngày về không được là thời gian quá khứ | BR-BOOK-004 |
| `msg_error_booking_fromdate_equal_todate` | Ngày đi không được trùng với Ngày về | BR-BOOK-003 |
| `msg_error_booking_fromdate_greater_todate` | Ngày đi không được lớn hơn Ngày về | BR-BOOK-002 |
| `msg_error_booking_rental_date_incorrect` | Dịch vụ đã bận. Vui lòng chọn lại ngày thuê | BR-BOOK-013 |
| `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. | Status concurrent update |
| `msg_error_car_insurance_expired` | Bảo hiểm xe đã hết hạn | BR-BOOK-017 |
| `msg_error_delivery_distance_exceed_maximum` | Điểm giao nhận xe vượt quá [Distance] | BR-BOOK-006 |
| `msg_error_message_to_owner_is_empty` | Vui lòng nhập lời nhắn cho chủ xe | BR-BOOK-007 |
| `msg_error_must_login_to_book_rental_service` | Bạn phải đăng nhập | BR-BOOK-008 |
| `msg_error_no_booking_address` | Vui lòng chọn Điểm giao nhận xe | BR-BOOK-005 |
| `msg_error_no_booking_fromdate_todate` | Vui lòng chọn Ngày đi và Ngày về | BR-BOOK-001 |
| `msg_error_no_permission_to_cancel_booking` | Bạn không có quyền để từ chối đơn hàng | BR-CANCEL-OWN-001 |
| `msg_error_no_permission_to_confirm_booking` | Bạn không có quyền xác nhận đơn hàng | Owner action |
| `msg_error_no_permission_to_pay_booking` | Bạn không có quyền thanh toán | Renter action |
| `msg_error_no_perrmission_booking_begin_trip` | Bạn không có quyền bắt đầu chuyến | Trip start |
| `msg_error_no_perrmission_booking_end_trip` | Bạn không có quyền kết thúc chuyến | Trip end |
| `msg_error_not_car_owner` | Bạn không phải chủ xe | Owner verify |
| `msg_error_order_not_found` | Không tìm thấy mã đơn hàng | Order lookup |
| `msg_error_rental_day_count_not_met_require` | Ngày thuê không đủ yêu cầu tối thiểu | BR-BOOK-016 |
| `msg_error_renter_no_permission_to_cancel_booking` | Bạn không có quyền hủy chuyến | BR-CANCEL-REN-001 |
| `msg_error_user_rent_their_own_service` | Chủ xe không thể thuê chính xe của mình | BR-BOOK-009 |
| `msg_error_voucher_rich_limit` | Mã khuyến mãi không hợp lệ hoặc đã hết hiệu lực | Voucher validation |
| `msg_error_wait_booking_time_has_expired` | Thời gian chờ chủ xe xác nhận đã hết | BR-BOOK-013 (owner timeout) |

## Expanded Test Request Templates

### Booking Rules (BR-BOOK-001 to BR-BOOK-017)

**BR-BOOK-001: Thiếu FromDate/ToDate**
```json
{ "FromDate": null, "ToDate": null, "RentalServiceItemId": 500 }
→ { "Status": 0, "Message": "Vui lòng chọn Ngày đi và Ngày về" }
```

**BR-BOOK-002: FromDate > ToDate**
```json
{ "FromDate": "2026-03-15T10:00:00+07:00", "ToDate": "2026-03-14T10:00:00+07:00", "RentalServiceItemId": 500 }
→ { "Status": 0, "Message": "Ngày đi không được lớn hơn Ngày về" }
```

**BR-BOOK-003: FromDate == ToDate**
```json
{ "FromDate": "2026-03-15T08:00:00+07:00", "ToDate": "2026-03-15T08:00:00+07:00", "RentalServiceItemId": 500 }
→ { "Status": 0, "Message": "Ngày đi không được trùng với Ngày về" }
```

**BR-BOOK-004: Past dates**
```json
{ "FromDate": "2020-01-01T08:00:00+07:00", "ToDate": "2020-01-03T20:00:00+07:00", "RentalServiceItemId": 500 }
→ { "Status": 0, "Message": "...quá khứ..." }
```

**BR-BOOK-005: Thiếu địa chỉ giao**
```json
{ "FromDate": "2026-03-20T08:00:00+07:00", "ToDate": "2026-03-22T20:00:00+07:00", "RentalServiceItemId": 500, "DeliveryAddress": "" }
→ { "Status": 0, "Message": "Vui lòng chọn Điểm giao nhận xe" }
```

**BR-BOOK-006: Khoảng cách giao xe quá xa**
```json
{ "FromDate": "...", "ToDate": "...", "RentalServiceItemId": 500, "DeliveryAddressLatitude": 21.0285, "DeliveryAddressLongitude": 105.8542 }
→ { "Status": 0, "Message": "Điểm giao nhận xe vượt quá..." }
```

**BR-BOOK-007: Lời nhắn rỗng**
```json
{ "FromDate": "...", "ToDate": "...", "RentalServiceItemId": 500, "MessageToOwner": "" }
→ { "Status": 0, "Message": "Vui lòng nhập lời nhắn cho chủ xe" }
```

**BR-BOOK-008: Chưa đăng nhập**
```json
// Request without Authorization header
→ { "Status": 0, "Message": "Bạn phải đăng nhập" }
```

**BR-BOOK-009: Thuê xe chính mình**
```json
// Login as owner of vehicle 500
{ "RentalServiceItemId": 500, "FromDate": "...", "ToDate": "..." }
→ { "Status": 0, "Message": "Chủ xe không thể thuê chính xe của mình" }
```

**BR-BOOK-010: Xe chưa approved**
```json
{ "RentalServiceItemId": 999, "FromDate": "...", "ToDate": "..." }
// Vehicle 999: IsApproved=false
→ { "Status": 0, "Message": "Không tìm thấy xe" }
```

**BR-BOOK-011: Xe bị suspended**
```json
{ "RentalServiceItemId": 888, "FromDate": "...", "ToDate": "..." }
// Vehicle 888: IsSuspended=true
→ { "Status": 0, "Message": "...tạm ngưng..." }
```

**BR-BOOK-013: Overlap booked schedule**
```json
// Vehicle 500 booked: 18/03 → 20/03
{ "RentalServiceItemId": 500, "FromDate": "2026-03-19T08:00:00+07:00", "ToDate": "2026-03-21T20:00:00+07:00" }
→ { "Status": 0, "Message": "Dịch vụ đã bận. Vui lòng chọn lại ngày thuê" }
```

**BR-BOOK-014: Weekday busy**
```json
// Vehicle 500: Saturday = busy
{ "RentalServiceItemId": 500, "FromDate": "2026-03-21T08:00:00+07:00", "ToDate": "2026-03-22T20:00:00+07:00" }
// 21/03 = Saturday → busy
→ { "Status": 0, "Message": "...bận vào..." }
```

**BR-BOOK-016: Minimum rental days**
```json
// Vehicle minimum = 3 days, renter requests 1 day
{ "RentalServiceItemId": 500, "FromDate": "2026-03-20T08:00:00+07:00", "ToDate": "2026-03-21T08:00:00+07:00" }
→ { "Status": 0, "Message": "Ngày thuê không đủ yêu cầu tối thiểu" }
```

**BR-BOOK-017: Bảo hiểm hết hạn**
```json
// Vehicle 500: HaveInsurance=true, CoverageEndDate=2026-03-10
{ "RentalServiceItemId": 500, "FromDate": "2026-03-11T08:00:00+07:00", "ToDate": "2026-03-13T20:00:00+07:00" }
→ { "Status": 0, "Message": "Bảo hiểm xe đã hết hạn" }
```

### Cancel Rules

**BR-CANCEL-003: Thiếu CancelReasonId**
```json
{ "OrderNumber": "ORD-001", "CancelReasonId": null }
→ { "Status": 0, "Message": "...chọn lý do..." }
```

**BR-CANCEL-004: Lý do "khác" nhưng thiếu chi tiết**
```json
{ "OrderNumber": "ORD-001", "CancelReasonId": 99, "CancelReasonCode": "another_reason", "CancelReasonDetail": "" }
→ { "Status": 0, "Message": "...nhập lý do..." }
```

**Success (tất cả validation pass)**
```json
{ "FromDate": "2026-03-20T08:00:00+07:00", "ToDate": "2026-03-22T20:00:00+07:00", "RentalServiceItemId": 500,
  "DeliveryAddressLatitude": 10.7769, "DeliveryAddressLongitude": 106.7009, "DeliveryAddress": "123 Lê Lợi, Q1, HCM",
  "MessageToOwner": "Cho tôi thuê xe", "UI_TimezoneOffset": -420 }
→ { "Status": 1, "Data": { "OrderNumber": "ORD-20260320-042", "StatusCode": "OWNER2CONFIRM", "TotalPrice": 2400000, "DepositAmount": 720000 } }
```
