# Pricing Calculation — Công thức tính giá chi tiết

## Mục lục
- [Tổng quan công thức](#tổng-quan-công-thức)
- [1. TotalPrice — Tổng tiền đơn hàng](#1-totalprice--tổng-tiền-đơn-hàng)
- [2. SubTotal — Giá thuê sau promotion](#2-subtotal--giá-thuê-sau-promotion)
- [3. Price by Date/Weekday — Giá theo ngày](#3-price-by-dateweekday--giá-theo-ngày)
- [4. Early/Late Hours — Phụ phí giờ sớm/muộn](#4-earlylate-hours--phụ-phí-giờ-sớmmuộn)
- [5. ServiceFee — Phí dịch vụ](#5-servicefee--phí-dịch-vụ)
- [6. Multi-day Discount — Giảm giá thuê nhiều ngày](#6-multi-day-discount--giảm-giá-thuê-nhiều-ngày)
- [7. Voucher Discount — Mã giảm giá](#7-voucher-discount--mã-giảm-giá)
- [8. Insurance Fee — Phí bảo hiểm](#8-insurance-fee--phí-bảo-hiểm)
- [9. Delivery Fee — Phí giao xe](#9-delivery-fee--phí-giao-xe)
- [10. Deposit — Tiền cọc](#10-deposit--tiền-cọc)
- [11. Commission — Phí hoàn thành (PlanProfitAmount)](#11-commission--phí-hoàn-thành-planprofitamount)
- [12. Owner Payment — Tiền chủ xe nhận](#12-owner-payment--tiền-chủ-xe-nhận)
- [13. Owner Withholding Tax — Thuế khấu trừ](#13-owner-withholding-tax--thuế-khấu-trừ)
- [14. Deposit & Refund — Hoàn tiền khi huỷ](#14-deposit--refund--hoàn-tiền-khi-huỷ)
- [Tham số cấu hình tổng hợp](#tham-số-cấu-hình-tổng-hợp)

---

## Tổng quan công thức

```
TotalPrice = SubTotal + TotalInsuranceFee + DeliveryFee - DiscountMoney

Trong đó:
├── SubTotal = TotalOriginalPrice - TotalPromotionMoney
│   ├── TotalOriginalPrice = Σ(RentalPrice per day) + EarlyFee + LateFee
│   └── TotalPromotionMoney = TotalOriginalPrice × DiscountPercent / 100
├── TotalInsuranceFee = TotalPrice × InsurancePercentPerDay / 100
├── DeliveryFee = ConfigDeliveryFee rules
└── DiscountMoney = Voucher calculation (Percent or Money type)
```

**Source code:** `RentCarHelper.cs`, `RentalServiceHelper.cs`

---

## 1. TotalPrice — Tổng tiền đơn hàng

```
TotalPrice = SubTotal + TotalInsuranceFee + DeliveryFee - DiscountMoney
```

**Source:** `RentCarHelper.cs:952`

**Các trường liên quan trên Order:**
- `TotalPrice` — Tổng tiền cuối cùng
- `TotalPriceByDay` = TotalPrice / NumberOfRentalDay
- `PriceByDay` — Giá thuê trung bình/ngày (after all adjustments)

---

## 2. SubTotal — Giá thuê sau promotion

```
SubTotal = TotalOriginalPrice - TotalPromotionMoney
```

**Source:** `RentCarHelper.cs:671, 682`

Trong đó:
- **TotalOriginalPrice** = Tổng giá gốc (sum of daily prices + early/late fees)
- **TotalPromotionMoney** = Giảm giá nhiều ngày (nếu có)

---

## 3. Price by Date/Weekday — Giá theo ngày

Giá thuê mỗi ngày được tính theo **priority cascade**:

```
FOR EACH ngày trong khoảng [FromDate, ToDate]:
    1. Kiểm tra ServiceItem_DateRentalPrice (giá override theo ngày cụ thể)
       → Nếu ngày thuê nằm trong [DatePrice.FromDate, DatePrice.ToDate]:
         RentalPrice = DatePrice.RentalPrice
         PriceInfo = "PriceByDate"

    2. Nếu không có date price → Kiểm tra ServiceItem_WeekdayRentalPrice
       → Nếu có giá cho DayOfWeek (0=Sunday, 6=Saturday):
         RentalPrice = WeekdayPrice.RentalPrice
         PriceInfo = "PriceByWeekDay - {DayOfWeek}"

    3. Fallback: Base price
         RentalPrice = RentalServiceItem.RentalPrice (giá mặc định/ngày)
```

**Source:** `RentCarHelper.cs:90-148, 399-446`

**Entities liên quan:**
- `ServiceItem_DateRentalPrice` — Override giá theo khoảng ngày
- `ServiceItem_WeekdayRentalPrice` — Giá theo thứ trong tuần
- `RentalServiceItem.RentalPrice` — Giá base/ngày

### Cách tính TotalOriginalPrice

```
segments = CalculateRentalSegments(fromDate, toDate, rentalHourStart, rentalHourEnd)

TotalOriginalPrice = 0
FOR EACH segment in segments:
    IF segment.Type == "FullDay":
        TotalOriginalPrice += GetPriceForDate(segment.Date) × 1  // full day
    ELSE IF segment.Type == "GetEarly":
        TotalOriginalPrice += earlyHours × EarlyHourDeliveryFee × 1000
    ELSE IF segment.Type == "ReturnLate":
        TotalOriginalPrice += lateHours × LateHourReturnFee × 1000
```

---

## 4. Early/Late Hours — Phụ phí giờ sớm/muộn

### Rental Segments

```
RentalHourStart = Vehicle_RentalSetting.RentalHourStart ?? 0
RentalHourEnd = Vehicle_RentalSetting.RentalHourEnd ?? 0

IF RentalHour_Using24Hours == true:
    RentalHourStart = fromDate.Hour + (fromDate.Minute / 60.0)
    RentalHourEnd = RentalHourStart  // 24-hour window
```

**Source:** `RentCarHelper.cs:40-48, 170-184`

### Segment Types

| Type | Mô tả | Tính phí |
|------|-------|---------|
| `GetEarly` | Nhận xe trước giờ rental start | `hours × EarlyHourDeliveryFee × 1000` |
| `FullDay` | Ngày thuê đầy đủ (24h) | `GetPriceForDate(date)` |
| `ReturnLate` | Trả xe sau giờ rental end | `hours × LateHourReturnFee × 1000` |

### Config Fields

| Field | Entity | Default | Đơn vị |
|-------|--------|---------|--------|
| `RentalHourStart` | `Vehicle_RentalSetting` | 0 | Giờ (0-23) |
| `RentalHourEnd` | `Vehicle_RentalSetting` | 0 | Giờ (0-23) |
| `RentalHour_Using24Hours` | `Vehicle_RentalSetting` | false | |
| `EarlyHourDeliveryFee` | `Vehicle_RentalSetting` | null | VND/giờ (×1000) |
| `LateHourReturnFee` | `Vehicle_RentalSetting` | null | VND/giờ (×1000) |

---

## 5. ServiceFee — Phí dịch vụ

```
ServiceFee = RentalServiceCategorySettingJsonModel.ServiceFee
```

**Hoặc nếu dùng percentage:**
```
ServiceFee = BasePrice × ServiceFeePercentOnByDate / 100
```

| Config | Source | Default |
|--------|--------|---------|
| `ServiceFee` | `RentalServiceCategorySettingJsonModel` | 0 |
| `ServiceFeePercentOnByDate` | `RentalServiceCategorySettingJsonModel` | 0 |

---

## 6. Multi-day Discount — Giảm giá thuê nhiều ngày

### Selection Logic

```
discountPercent = 0
numOfMinDay = 0

FOR EACH discount IN ConfigVehicleMultidayRentalDiscount (sorted by NumOfMinDay DESC):
    IF discount.NumOfMinDay <= NumberOfRentalDay:
        IF discount.DiscountPercent > discountPercent:
            discountPercent = discount.DiscountPercent
            numOfMinDay = discount.NumOfMinDay
        BREAK  // Lấy discount cao nhất match
```

### Conditions

```
IF Vehicle_RentalSetting.HaveMultidayRentalDiscount == true
AND NOT (IgnoreMultidayDiscount_InMinimumRentalDayRequired == true AND hasActiveMinimumDayRequirement):
    TotalPromotionMoney = TotalOriginalPrice × discountPercent / 100
ELSE:
    TotalPromotionMoney = 0
```

**Source:** `RentCarHelper.cs:614-660`

### Result Fields

| Field | Mô tả |
|-------|-------|
| `PromotionMoney` | = TotalPromotionMoney |
| `PromotionMoneyByDay` | = TotalPromotionMoney / NumberOfRentalDay |
| `PromotionNote` | = "NumOfMinDay {N}. Discount {X}%" |
| `PromotionNumOfMinDay` | = Minimum days to qualify |

---

## 7. Voucher Discount — Mã giảm giá

### 2 loại voucher

```csharp
IF DiscountMethodCode == "Percent":
    DiscountMoney = TotalOriginalPrice × DiscountPercent / 100
    IF MaximumDiscountMoney > 0:
        DiscountMoney = MIN(DiscountMoney, MaximumDiscountMoney)  // Cap

ELSE IF DiscountMethodCode == "Money":
    DiscountMoney = DiscountCode.DiscountMoney  // Fixed amount
    IF DiscountMoney > TotalOriginalPrice:
        DiscountMoney = TotalOriginalPrice  // Không vượt quá tổng
```

**Source:** `RentCarHelper.cs:868-890`

### DiscountCode Entity Fields

| Field | Type | Mô tả |
|-------|------|-------|
| `DiscountMethodCode` | string | `"Percent"` hoặc `"Money"` (từ `ConfigDiscountMethod`) |
| `DiscountPercent` | decimal? | % giảm giá |
| `DiscountMoney` | decimal? | Số tiền giảm cố định |
| `MaximumDiscountMoney` | decimal? | Giới hạn tối đa cho loại Percent |
| `ValidFromDate` | double | Unix timestamp — bắt đầu hiệu lực |
| `ValidToDate` | double | Unix timestamp — hết hiệu lực |
| `MaximumUsedTotal` | int? | Giới hạn tổng lượt dùng |
| `MaximumUsedPerUser` | int? | Giới hạn lượt dùng per user |

### Tracking

- `DiscountCode_Summary` — Tổng hợp sử dụng
- `Order_DiscountCode_Applied_OneTimeUse` — Tracking one-time use per order

---

## 8. Insurance Fee — Phí bảo hiểm

```
TotalInsuranceFee = SubTotal × InsurancePercentPerDay / 100
InsuranceFeeByDay = TotalInsuranceFee / NumberOfRentalDay
```

**Source:** `RentCarHelper.cs:846`

| Config | Source | Default |
|--------|--------|---------|
| `InsurancePercentPerDay` | `RentalServiceCategorySettingJsonModel` | 0 |

**Data source:** `Vehicle_InsuranceInformation` entity — thông tin bảo hiểm xe.

---

## 9. Delivery Fee — Phí giao xe

**Source:** `ConfigDeliveryFeeHelper.cs:80-269`

### ConfigDeliveryFee Entity

| Field | Type | Mô tả |
|-------|------|-------|
| `Operator` | string | Toán tử so sánh |
| `ChargeType` | string | `"ByHour"` hoặc `"ByDay"` |
| `FirstValue` | decimal? | Giá trị so sánh / range start |
| `SecondValue` | decimal? | Range end (cho range operators) |
| `Value` | decimal? | Multiplier (cho ByDay) |

### Operator Types

| Operator | Ý nghĩa |
|----------|---------|
| `GreaterThan` | hours > FirstValue |
| `GreaterThanOrEqual` | hours >= FirstValue |
| `LessThan` | hours < FirstValue |
| `LessThanOrEqual` | hours <= FirstValue |
| `Equal` | hours == FirstValue |
| `NotEqual` | hours != FirstValue |
| `InRangeExclusive` | FirstValue < hours < SecondValue |
| `InRangeInclusiveStart` | FirstValue <= hours < SecondValue |
| `InRangeInclusiveEnd` | FirstValue < hours <= SecondValue |
| `InRangeInclusiveBoth` | FirstValue <= hours <= SecondValue |

### Charge Types

```
IF ChargeType == "ByHour":
    DeliveryFee = hours × HourlyRate × 1000

ELSE IF ChargeType == "ByDay":
    DeliveryFee = PriceByDate × Value
    // Hoặc: PriceByDate × 0.5 cho nửa ngày
```

### Additional Delivery Charges

| Field | Entity | Mô tả |
|-------|--------|-------|
| `EarlyHourDeliveryFee` | `Vehicle_RentalSetting` | Phụ phí giao xe sớm (per hour) |
| `LateHourReturnFee` | `Vehicle_RentalSetting` | Phụ phí trả xe muộn (per hour) |
| `DeliverySurcharge` | `Vehicle_RentalSetting` | Phụ phí theo khoảng cách (optional) |

---

## 10. Deposit — Tiền cọc

```
IF RentalServiceCategorySettingJsonModel.DepositAmount > 0:
    // Fixed deposit amount
    DepositAmount = DepositAmount (configured value)

ELSE:
    // Percentage-based deposit
    DepositAmount = Math.Ceiling(TotalPrice × (DepositPercent ?? 30) / 100)
```

**Source:** `RentalService_SelfdriveCarRental_DetailService.cs:668, 1036`

| Config | Default | Mô tả |
|--------|---------|-------|
| `DepositPercent` | **30%** | % cọc trên tổng tiền |
| `DepositAmount` | 0 | Nếu > 0: fixed amount, nếu = 0: dùng DepositPercent |
| `HaveMinimumDepositPercentAndAmount` | false | Áp dụng deposit tối thiểu |

**Priority:** Vehicle_RentalSetting > RentalServiceCategorySettingJsonModel > System default (30%)

---

## 11. Commission — Phí hoàn thành (PlanProfitAmount)

```
PlanProfitAmount = (SubTotal × CompletionFeePercentage / 100)
                 + ServiceFee
                 - DiscountMoney
                 + (DeliveryFee × CompletionFeePercentage / 100)
```

**Source:** `RentalServiceHelper.cs:1143-1145`

### CompletionFeePercentage Priority

```
1. Nếu Service item có CompletionFeePercentage → dùng giá trị này
2. Nếu không → dùng Owner's CompletionFeePercentage
3. Nếu không → dùng RentalServiceCategorySettingJsonModel.CompletionFeePercentage
4. Nếu không → dùng ConfigCompletionFee system default
```

**Admin override:** `AdminUpdateCompletionFeeOrder()` cho phép admin cập nhật trực tiếp.

---

## 12. Owner Payment — Tiền chủ xe nhận

```
OwnerRemainAmount = DepositAmount - PlanProfitAmount - InsuranceFee
```

**Source:** `RentalServiceHelper.cs:1146-1152`

**Lưu trữ:** `Order_Finance.OwnerRemainAmount`

**Flow:**
1. Renter cọc → DepositAmount ghi nhận
2. Order hoàn tất → `TransferMoneyEngine` tính OwnerRemainAmount
3. Trừ thuế khấu trừ (nếu có)
4. Chuyển vào ví owner qua EWallet

---

## 13. Owner Withholding Tax — Thuế khấu trừ

```
IF OwnerWithholdingTaxPercent > 0 AND TotalPrice > 0:
    taxableAmount = (TotalPrice - DepositAmount) + OwnerRemainAmount
    OwnerWithholdingTaxAmount = taxableAmount × (OwnerWithholdingTaxPercent / 100)
    OwnerRemainAmount -= OwnerWithholdingTaxAmount
```

**Source:** `RentalServiceHelper.cs:1148-1152`

| Config | Source | Default |
|--------|--------|---------|
| `OwnerWithholdingTaxPercent` | `RentalServiceCategorySettingJsonModel` | 0 (không áp thuế) |

**Lưu trữ:** `Order_Finance.OwnerWithholdingTaxAmount`

---

## 14. Deposit & Refund — Hoàn tiền khi huỷ

**Source:** `RentCarHelper.cs:992-1111`
**Method:** `CalcRerturnDepositAmount()`

### Tham số cancel

| Tham số | Source | Default |
|---------|--------|---------|
| `FullRefundWithinMinutes` | `CancelOrderSetting` | **15 phút** |
| `NoRefundGreaterThanDays` | `CancelOrderSetting` | **7 ngày** |
| `PenaltyDepositPercent` | Setting | **30%** |
| `CompletionFeePercentage` | Setting | Theo priority cascade |
| `DepositPercent` | Setting | **30%** |

### Biến tính toán

```
depositPercent = (DepositPercent ?? 30) / 100
penaltyPercent = (PenaltyDepositPercent ?? 30) / 100
refundPercent = 1 - penaltyPercent
servicePercent = CompletionFeePercentage / 100
renterPercent = penaltyPercent - servicePercent

diffDeposit = (now - DepositDoneAt).TotalMinutes
diffRental = (FromDate - now).TotalDays
```

### Kịch bản 1: Hoàn 100% (diffDeposit ≤ FullRefundWithinMinutes)

| | Renter Cancel | Owner Cancel |
|-|---------------|-------------|
| **Renter nhận** | DepositAmount (100%) | DepositAmount (100%) |
| **Owner nhận** | 0 | 0 |
| **Platform nhận** | 0 | 0 |

### Kịch bản 2: Có phạt (diffDeposit > FullRefundWithinMinutes AND diffRental > NoRefundGreaterThanDays)

**Nếu Renter huỷ:**

```
// Khi DepositAmount > 0 (fixed deposit):
RefundAmount_Renter  = DepositAmount × refundPercent
moneyLeft            = DepositAmount - RefundAmount_Renter
RefundAmount_Owner   = DepositAmount × (penaltyPercent - servicePercent)
RefundAmount_Service = moneyLeft - RefundAmount_Owner

// Khi DepositAmount == 0 (percentage-based):
RefundAmount_Renter  = DepositAmount × refundPercent
moneyLeft            = DepositAmount - RefundAmount_Renter
RefundAmount_Owner   = SubTotal × depositPercent × (penaltyPercent - servicePercent)
RefundAmount_Service = moneyLeft - RefundAmount_Owner
```

**Nếu Owner huỷ:**

```
chargeMoney          = DepositAmount × penaltyPercent
renterMoney          = chargeMoney × renterPercent / penaltyPercent
RefundAmount_Renter  = DepositAmount (100% hoàn)
RefundAmount_Service = chargeMoney - renterMoney
RefundAmount_Owner   = -chargeMoney (owner bị trừ)
```

### Kịch bản 3: Không hoàn (diffRental ≤ NoRefundGreaterThanDays — sát ngày)

**Nếu Renter huỷ:**

```
// Khi DepositAmount > 0:
RefundAmount_Renter  = 0 (không hoàn)
RefundAmount_Owner   = DepositAmount × (1 - servicePercent)
RefundAmount_Service = DepositAmount - RefundAmount_Owner

// Khi DepositAmount == 0:
RefundAmount_Renter  = 0
RefundAmount_Owner   = SubTotal × depositPercent × (1 - servicePercent)
RefundAmount_Service = DepositAmount - RefundAmount_Owner
```

**Nếu Owner huỷ:**

```
RefundAmount_Renter  = DepositAmount (100% hoàn)
RefundAmount_Owner   = -DepositAmount (owner trả toàn bộ)
RefundAmount_Service = DepositAmount × servicePercent
```

### Lưu trữ kết quả

| Field trên Order_Finance | Mô tả |
|--------------------------|-------|
| `RenterCancelRefund` | Tiền hoàn cho renter |
| `OwnerCancelRefund` | Tiền owner nhận (âm = bị trừ) |
| `PlanCancelProfitAmount` | Platform profit từ huỷ đơn |
| `ActualRenterCancelRefund` | Hoàn thực tế (sau khi xử lý) |
| `ActualOwnerCancelRefund` | Owner thực tế (sau khi xử lý) |
| `IsDontHaveCancellationFee` | true = miễn phí huỷ |

---

## Tham số cấu hình tổng hợp

### RentalServiceCategorySettingJsonModel (Primary config)

| Field | Type | Default | Mô tả |
|-------|------|---------|-------|
| `DepositPercent` | decimal? | **30** | % cọc |
| `DepositAmount` | decimal? | **0** | Fixed deposit (0 = dùng %) |
| `PenaltyDepositPercent` | decimal? | **30** | % phạt khi huỷ |
| `CompletionFeePercentage` | decimal? | - | % hoa hồng platform |
| `OwnerWithholdingTaxPercent` | decimal? | **0** | % thuế khấu trừ owner |
| `ServiceFee` | decimal? | **0** | Phí dịch vụ cố định |
| `ServiceFeePercentOnByDate` | decimal? | **0** | % phí dịch vụ trên giá ngày |
| `InsurancePercentPerDay` | decimal? | **0** | % bảo hiểm/ngày |
| `RentalHourStart` | int? | - | Giờ bắt đầu thuê |
| `RentalHourEnd` | int? | - | Giờ kết thúc thuê |
| `CancelOrderSetting` | object | - | Nested: `{FullRefundWithinMinutes, NoRefundGreaterThanDays}` |
| `IgnoreMultidayDiscount_InMinimumRentalDayRequired` | bool | false | Bỏ qua giảm giá nhiều ngày |

### Vehicle_RentalSetting (Per-vehicle override)

| Field | Type | Default | Mô tả |
|-------|------|---------|-------|
| `DepositPercent` | decimal? | inherit | Override % cọc cho xe cụ thể |
| `DepositAmount` | decimal? | inherit | Override deposit cố định |
| `PenaltyDepositPercent` | decimal? | inherit | Override % phạt |
| `CompletionFeePercentage` | decimal? | inherit | Override hoa hồng |
| `OwnerWithholdingTaxPercent` | decimal? | inherit | Override thuế |
| `EarlyHourDeliveryFee` | int? | null | VND/giờ nhận sớm (×1000) |
| `LateHourReturnFee` | int? | null | VND/giờ trả muộn (×1000) |
| `DeliverySurcharge` | decimal? | null | Phụ phí giao theo khoảng cách |
| `RentalHourStart` | int? | - | Override giờ bắt đầu |
| `RentalHourEnd` | int? | - | Override giờ kết thúc |
| `RentalHour_Using24Hours` | bool | false | Sử dụng cửa sổ 24h |
| `HaveMultidayRentalDiscount` | bool | false | Bật giảm giá nhiều ngày |
| `HaveMinimumDepositPercentAndAmount` | bool | false | Áp dụng deposit tối thiểu |

### ConfigDeliveryFee (System-level delivery fee rules)

Xem [Section 9](#9-delivery-fee--phí-giao-xe) cho chi tiết.

### ConfigVehicleMultidayRentalDiscount (Discount tiers)

| Field | Type | Mô tả |
|-------|------|-------|
| `NumOfMinDay` | int | Số ngày thuê tối thiểu để qualify |
| `DiscountPercent` | decimal | % giảm giá |

**Ví dụ:**
| NumOfMinDay | DiscountPercent | Ý nghĩa |
|-------------|----------------|---------|
| 3 | 5% | Thuê ≥3 ngày → giảm 5% |
| 7 | 10% | Thuê ≥7 ngày → giảm 10% |
| 14 | 15% | Thuê ≥14 ngày → giảm 15% |
| 30 | 20% | Thuê ≥30 ngày → giảm 20% |

---

## Ví dụ tính toán đầy đủ

```
Input:
- Thuê 5 ngày, từ 01/03 08:00 đến 06/03 08:00
- RentalPrice base: 800.000đ/ngày
- Ngày 01/03 (Thứ 7): WeekdayPrice = 1.000.000đ
- DepositPercent: 30%
- CompletionFeePercentage: 20%
- InsurancePercentPerDay: 5%
- HaveMultidayRentalDiscount: true, 5 ngày → 5%
- Voucher: PERCENT 10%, MaximumDiscountMoney: 200.000đ

Tính:
1. TotalOriginalPrice:
   = 1.000.000 (Sat) + 800.000 (Sun) + 800.000 (Mon) + 800.000 (Tue) + 800.000 (Wed)
   = 4.200.000đ

2. TotalPromotionMoney (multi-day 5%):
   = 4.200.000 × 5 / 100 = 210.000đ

3. SubTotal:
   = 4.200.000 - 210.000 = 3.990.000đ

4. InsuranceFee:
   = 3.990.000 × 5 / 100 = 199.500đ

5. DeliveryFee: 0đ (giả sử không có)

6. VoucherDiscount (PERCENT 10%):
   = 4.200.000 × 10 / 100 = 420.000đ
   → Capped: MIN(420.000, 200.000) = 200.000đ

7. TotalPrice:
   = 3.990.000 + 199.500 + 0 - 200.000 = 3.989.500đ

8. DepositAmount:
   = CEILING(3.989.500 × 30 / 100) = 1.196.850đ

9. PlanProfitAmount (commission):
   = (3.990.000 × 20/100) + 0 - 200.000 + (0 × 20/100)
   = 798.000 - 200.000 = 598.000đ

10. OwnerRemainAmount:
    = 1.196.850 - 598.000 - 199.500 = 399.350đ
```

---

*Xem thêm: [cancel-flow.md](./cancel-flow.md) | [booking-flow.md](./booking-flow.md) | [core-order.md](../02_MODULES/core-order.md)*

---

## QC Verification Checklist

Mỗi formula kèm test values (Given → Then) để QC verify tính đúng:

| # | Formula | Given | Expected Result |
|---|---------|-------|----------------|
| 1 | TotalOriginalPrice | 3 FullDay × 800,000đ + Early 2h × 30,000đ + Late 2h × 30,000đ | 2,520,000đ |
| 2 | MultiDay Discount | TotalOriginalPrice=2,520,000, ≥3 ngày, 5% | PromotionMoney=126,000đ |
| 3 | SubTotal | 2,520,000 - 126,000 | 2,394,000đ |
| 4 | Voucher (Percent 10%, cap 200k) | TotalOriginalPrice=2,520,000 × 10% = 252,000 → cap 200,000 | DiscountMoney=200,000đ |
| 5 | Voucher (Money fixed) | DiscountMoney=100,000 | DiscountMoney=100,000đ |
| 6 | InsuranceFee | SubTotal=2,394,000, InsurancePercent=5% | 119,700đ |
| 7 | TotalPrice | 2,394,000 + 119,700 + 0 - 200,000 | 2,313,700đ |
| 8 | DepositAmount (%) | TotalPrice=2,313,700, DepositPercent=30% | Ceiling(694,110)=694,110đ |
| 9 | DepositAmount (fixed) | DepositAmount=500,000 (configured) | 500,000đ |
| 10 | PlanProfitAmount | SubTotal=2,394,000 × 20% + 0 - 200,000 | 278,800đ |
| 11 | OwnerRemainAmount | 694,110 - 278,800 - 119,700 | 295,610đ |
| 12 | WithholdingTax | taxable=(2,313,700-694,110)+295,610=1,915,200, Tax=2% | 38,304đ |
| 13 | Refund 100% (≤15min) | DepositAmount=694,110, diffDeposit≤15 | RenterRefund=694,110đ |
| 14 | Refund 70% (>15min, >7d) | DepositAmount=694,110, penalty=30% | RenterRefund=485,877đ |

## Edge Cases for QC

| # | Edge Case | Mô tả | Expected Behavior |
|---|-----------|-------|-------------------|
| 1 | Half-day merge | Early=IsHalfDay + Late=IsHalfDay | rentalDayCount += 1 (gộp 2 nửa ngày) |
| 2 | Zero rental days | Chỉ có Early (< NumOfHourToBeOneDay) | rentalDayCount = 1 (tối thiểu) |
| 3 | Voucher > TotalPrice | DiscountMoney=1,000,000 nhưng TotalOriginalPrice=500,000 | DiscountMoney capped = 500,000 (không âm) |
| 4 | ConfigDeliveryFee order trap | Early 5h → match OrderNo=1 (LessThanOrEqual 9) → ByDay 1.0 (NOT 0.5!) | OrderNo=2 sẽ KHÔNG BAO GIỜ match vì OrderNo=1 catch trước |
| 5 | PriceByDate > PriceByWeekDay | Ngày 01/03 có cả DatePrice=1,200,000 và WeekdayPrice=800,000 | Dùng DatePrice=1,200,000 (priority cao hơn) |
| 6 | Using24Hours mode | RentalHour_Using24Hours=true → rentalHourStart=fromDate.Hour | Không có Early/Late segments nếu giờ nhận = giờ trả |
| 7 | ServiceFeePercentOnByDate | Ngày có PriceByDate → cộng thêm ServiceFee% | ServiceFee chỉ áp dụng cho PriceByDate, không cho PriceByWeekDay |
| 8 | OwnerRemainAmount âm | PlanProfitAmount + InsuranceFee > DepositAmount | Giá trị âm hợp lệ → owner nợ platform |
