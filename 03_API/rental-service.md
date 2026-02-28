# API: Rental Service

> **Controllers:**
> - `RentalServiceController` → `/api/v1/RentalService` _(Authorize)_
> - `RentalServiceController_v2` → `/api/v2/RentalService` _(Authorize)_
> - `SearchingRentalServiceController` → `/api/v1/SearchingRentalService` _(AllowAnonymous phần lớn)_
> - `RentalService_SelfdriveCarRentalController` → `/api/v1/RentalService_SelfdriveCarRental` _(Authorize, Admin)_
>
> **Mặc định:** Tất cả request đều tự động gán `RentalServiceCategoryCode = "RENT_CAR"` nếu không truyền.

---

## Mục lục

- [Tìm kiếm xe](#1-tìm-kiếm-xe)
- [Chi tiết xe (public)](#2-chi-tiết-xe-public)
- [Cập nhật thông tin booking](#3-cập-nhật-thông-tin-booking)
- [Kiểm tra trước khi cập nhật booking](#4-kiểm-tra-trước-khi-cập-nhật-booking)
- [Đặt xe (Booking)](#5-đặt-xe-booking)
- [Chi tiết đơn hàng](#6-chi-tiết-đơn-hàng)
- [Xác nhận đơn](#7-xác-nhận-đơn)
- [Huỷ đơn](#8-huỷ-đơn)
- [Quản lý xe của tôi (Owner)](#9-quản-lý-xe-của-tôi-owner)
- [Các API phụ trợ](#10-các-api-phụ-trợ)

---

## 1. Tìm kiếm xe

### `POST /api/v1/SearchingRentalService/List`

**Mô tả:** Tìm kiếm danh sách xe cho thuê theo điều kiện (ngày, địa điểm, filter). Đây là API nặng tải nhất của module.

**Phân quyền:** `[AllowAnonymous]` — không cần đăng nhập.

**Hiệu năng:** 26,785 lượt/30 ngày | avg **1,781ms** 🔴 | max 335,733ms 🔴

**Request Body:** `RentalServiceParamModel`

```json
{
  "FromDate": 1740787200.0,
  "ToDate": 1740960000.0,
  "Address": "Hà Nội",
  "SearchAddress": "Hà Nội",
  "Latitude": 21.0278,
  "Longitude": 105.8342,
  "PageIndex": 1,
  "PageSize": 20,
  "OrderBy": ["PRICE_ASC"],
  "VehicleNoOfSeatIds": ["5_CHO"],
  "VehicleTransmissionTypeIds": ["SO_TU_DONG"],
  "VehicleMakeIds": ["TOYOTA"],
  "VehicleModelIds": [],
  "VehicleColorIds": [],
  "FuelTypeIds": [],
  "RentalSettings": [],
  "FeatureIds": [],
  "DocumentIds": [],
  "RentalRequiredItemIds": [],
  "VehicleTypeSubIds": [],
  "VehicleSegmentIds": [],
  "MinRentalPrice": null,
  "MaxRentalPrice": null,
  "VehicleMinYearModel": null,
  "VehicleMaxYearModel": null,
  "IsElectricEngine": null,
  "IsHaveInsurance": null,
  "IsHaveDeliverySurcharge": null,
  "IsHaveSecurity": null,
  "SearchVersion": null
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| FromDate | double (Unix timestamp) | Có | Ngày giờ nhận xe |
| ToDate | double (Unix timestamp) | Có | Ngày giờ trả xe |
| Address / SearchAddress | string | Không | Địa chỉ tìm kiếm |
| Latitude / Longitude | decimal | Không | Toạ độ tìm kiếm |
| PageIndex | int | Không | Trang (default: 1) |
| PageSize | int | Không | Số item/trang (default: 20) |
| OrderBy | string[] | Không | Tiêu chí sắp xếp |
| VehicleNoOfSeatIds | string[] | Không | Lọc số chỗ ngồi |
| VehicleTransmissionTypeIds | string[] | Không | Lọc hộp số |
| VehicleMakeIds | string[] | Không | Lọc hãng xe |
| VehicleModelIds | string[] | Không | Lọc model xe |
| MinRentalPrice / MaxRentalPrice | decimal | Không | Lọc khoảng giá |
| VehicleMinYearModel / VehicleMaxYearModel | int | Không | Lọc năm sản xuất |
| IsElectricEngine | bool? | Không | Chỉ lấy xe điện |
| IsHaveInsurance | bool? | Không | Chỉ lấy xe có bảo hiểm |
| IsHaveDeliverySurcharge | bool? | Không | Chỉ lấy xe có giao tận nơi |
| IsHaveSecurity | bool? | Không | Chỉ lấy xe yêu cầu tài sản thế chấp |
| FeatureIds | string[] | Không | Lọc theo tiện ích |
| RentalRequiredItemIds | string[] | Không | Lọc theo tài sản thế chấp |

**Response:** `EzyResultObject<RentalServiceSearchingModel>`

```json
{
  "Status": 1,
  "Msg": null,
  "Data": {
    "RentalServices": [
      {
        "ID_GUID": "abc-123",
        "Slug": "toyota-vios-ha-noi",
        "Name": "Toyota Vios 2022",
        "AvatarUrl": "https://...",
        "ImageUrls": ["https://..."],
        "Address": "Cầu Giấy, Hà Nội",
        "Distance": "2.5 km",
        "Rating": "4.8",
        "ServedCount": 120,
        "NumberOfSeat": 5,
        "RentalPriceByDay": "700.000đ/ngày",
        "RentalPrice": "2.100.000đ",
        "RentalPriceOriginal": "2.400.000đ",
        "CanBooking": true,
        "CanChangeDeliveryAddress": true,
        "RentalDate": "01/03 - 03/03",
        "Voucher": "GIAM10"
      }
    ],
    "Option": {
      "OrderByList": [],
      "NoOfSeatList": [],
      "RentalPrice": { "Min": 300000, "Max": 3000000 },
      "CreatedYear": { "Min": 2015, "Max": 2025 },
      "VehicleTransmissionTypeList": [],
      "VehicleMakeList": [],
      "FeatureList": [],
      "PopularPlaces": [],
      "NeedReloadOption": false
    },
    "Paging": {
      "Total": 100,
      "Page": 1,
      "PageSize": 20
    },
    "RentalDayCount_Text": "3 ngày"
  }
}
```

---

## 2. Chi tiết xe (public)

### `POST /api/v1/SearchingRentalService/Detail`

**Mô tả:** Lấy toàn bộ thông tin chi tiết một xe để hiển thị trang đặt xe. Bao gồm: thông tin xe, chủ xe, giá, lịch bận, chính sách, đánh giá.

**Phân quyền:** `[AllowAnonymous]` — không cần đăng nhập. Nếu đã đăng nhập sẽ trả thêm thông tin cá nhân hoá (giá có voucher, trạng thái CanBooking...).

**Hiệu năng:** 21,341 lượt/30 ngày | avg **1,982ms** 🔴 | p95 4,018ms 🔴

**Request Body:** `RentalServiceParamModel`

```json
{
  "RentalServiceItemId": "abc-123",
  "Slug": "toyota-vios-ha-noi",
  "FromDate": 1740787200.0,
  "ToDate": 1740960000.0,
  "Latitude": 21.0278,
  "Longitude": 105.8342,
  "DeliveryInfo": {
    "UserSearchAddress": "123 Xuân Thuỷ, Hà Nội",
    "UserSearchLat": 21.034,
    "UserSearchLng": 105.781
  },
  "VoucherCode": null
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| RentalServiceItemId | string | Có (hoặc Slug) | ID của xe |
| Slug | string | Có (hoặc Id) | Slug URL của xe |
| FromDate / ToDate | double | Không | Nếu truyền sẽ tính giá theo khoảng ngày |
| Latitude / Longitude | decimal | Không | Toạ độ người dùng (tính khoảng cách) |
| DeliveryInfo | object | Không | Địa chỉ nhận xe mong muốn |
| VoucherCode | string | Không | Mã voucher để tính giá sau giảm |

**Response:** `EzyResultObject<RentalServicePublicModel>`

```json
{
  "Status": 1,
  "Data": {
    "ID_GUID": "abc-123",
    "Slug": "toyota-vios-ha-noi",
    "Name": "Toyota Vios 2022",
    "AvatarUrl": "https://...",
    "ImageUrls": ["https://..."],
    "Address": "Cầu Giấy, Hà Nội",
    "Rating": "4.8",
    "ServedCount": 120,
    "NumberOfSeat": 5,
    "PlateNumber": "29A-12345",
    "RentalPriceByDay": "700.000đ/ngày",
    "RentalPriceByDayOriginal": "800.000đ/ngày",
    "RentalPrice": "2.100.000đ",
    "TotalPrice": "2.100.000đ",
    "RentalDayCount": "3",
    "CanBooking": true,
    "CanChangeDeliveryAddress": true,
    "RentalDate": "01/03 - 03/03",
    "MessageToOwner": null,
    "FullDescription": "Mô tả chi tiết xe...",
    "InsuranceDescription": "Có bảo hiểm...",
    "InsuranceInfo": { "Id": "...", "Name": "Bảo hiểm xe tự lái", "Description": "..." },
    "Term": "Điều khoản...",
    "Features": [{ "Id": "...", "Name": "GPS", "Icon": "...", "IconUrl": "..." }],
    "Documents": [{ "Id": "...", "Name": "Bằng lái xe", "IconUrl": "..." }],
    "Securities": [{ "Id": "...", "Name": "Xe máy/Tài sản cầm cố", "IconUrl": "..." }],
    "RenterDocuments": ["Bằng lái xe hạng B2"],
    "Characteristics": [],
    "BusySchedules": [1740787200.0, 1740873600.0],
    "OwnerInfo": {
      "Id": "owner-1",
      "Name": "Nguyễn Văn A",
      "AvatarUrl": "https://...",
      "Rating": "4.9",
      "ServedCount": 200,
      "JoinedWhen": "Khoảng 2 năm trước",
      "ReplyWithin": "Trong vài giờ",
      "IsVerified": true,
      "IsDriverLicenseVerified": true
    },
    "Vouchers": [],
    "Review": {
      "Rating": "4.8",
      "TotalRatingCount": 50,
      "Reviews": []
    },
    "CancelBookingPolicy": {},
    "CancelBookingPolicyInfo": {},
    "PaymentInstructionInfo": {},
    "GeneralProvisionInfo": {},
    "BookingInfoItems": [],
    "ExtraSurcharges": [],
    "DeliveryInfo": {
      "UserTakeDeliveryAddress": "Hà Nội",
      "OwnerShipDeliveryAddress": "Cầu Giấy, Hà Nội",
      "OwnerShipDeliveryFee": "Miễn phí",
      "OwnerShipCanSelect": true,
      "IsUserTakeSelected": false
    },
    "PopularPlaces": [],
    "ThingsToKnow": [],
    "RentalPriceDetails": []
  }
}
```

---

## 3. Cập nhật thông tin booking

### `POST /api/v1/SearchingRentalService/UpdateBookingInfo`

**Mô tả:** Tính toán lại giá và thông tin booking khi người dùng thay đổi ngày, địa chỉ nhận xe, hoặc voucher trên trang chi tiết xe. Gọi mỗi khi user thay đổi điều kiện đặt xe.

**Phân quyền:** `[Authorize]`

**Hiệu năng:** 8,762 lượt/30 ngày | avg **2,806ms** 🔴 | p95 7,228ms | max 45,044ms

**Request Body:** `RentalServiceParamModel`

```json
{
  "RentalServiceItemId": "abc-123",
  "FromDate": 1740787200.0,
  "ToDate": 1740960000.0,
  "VoucherCode": "GIAM10",
  "DeliveryAddress": "123 Xuân Thuỷ, Hà Nội",
  "DeliveryInfo": {
    "UserSearchAddress": "123 Xuân Thuỷ, Hà Nội",
    "UserSearchLat": 21.034,
    "UserSearchLng": 105.781,
    "IsUserTakeSelected": true
  },
  "MessageToOwner": "Tôi sẽ đến đúng giờ"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| RentalServiceItemId | string | Có | ID của xe |
| FromDate / ToDate | double | Có | Ngày giờ nhận/trả xe |
| VoucherCode | string | Không | Mã voucher |
| DeliveryAddress | string | Không | Địa chỉ nhận xe (text) |
| DeliveryInfo | object | Không | Thông tin địa chỉ nhận xe chi tiết |
| MessageToOwner | string | Không | Lời nhắn gửi chủ xe |

**Response:** `EzyResultObject<RentalServiceBookingInfoModel>`

```json
{
  "Status": 1,
  "Data": {
    "RentalDate": "01/03/2026 - 03/03/2026",
    "RentalDayCount": "3 ngày",
    "DeliveryAddress": "123 Xuân Thuỷ, Hà Nội",
    "DeliveryFee": "Miễn phí",
    "RentalPriceByDay": "700.000đ/ngày",
    "RentalPriceByDayOriginal": "800.000đ/ngày",
    "RentalPrice": "2.100.000đ",
    "RentalPriceOriginal": "2.400.000đ",
    "VoucherCode": "GIAM10",
    "VoucherDiscountPrice": "-210.000đ",
    "TotalPrice": "1.890.000đ",
    "CanBooking": true,
    "RentalPriceDetails": [
      {
        "Title": "Đơn giá thuê",
        "PriceText": "700.000đ/ngày",
        "Code": "RENTAL_PRICE",
        "Group": 1,
        "OrderNo": 1
      }
    ],
    "BookingInfoItems": [],
    "ExtraSurcharges": [],
    "DeliveryInfo": {
      "UserTakeDeliveryAddress": "123 Xuân Thuỷ, Hà Nội",
      "OwnerShipDeliveryAddress": "Cầu Giấy, Hà Nội",
      "IsUserTakeSelected": true
    },
    "CancelBookingPolicyInfo": {},
    "PaymentInstructionInfo": {}
  }
}
```

---

## 4. Kiểm tra trước khi cập nhật booking

### `POST /api/v1/SearchingRentalService/CheckBeforeUpdateBookingInfo`

**Mô tả:** Kiểm tra các điều kiện hợp lệ trước khi gọi `UpdateBookingInfo` hoặc trước khi đặt xe (xe còn trống, ngày hợp lệ, user đủ điều kiện...).

**Phân quyền:** `[AllowAnonymous]`

**Hiệu năng:** 9,578 lượt/30 ngày | avg **784ms** 🟡

**Request Body:** `RentalServiceParamModel`

```json
{
  "RentalServiceItemId": "abc-123",
  "FromDate": 1740787200.0,
  "ToDate": 1740960000.0
}
```

**Response:** `EzyResultObject<CheckBeforeUpdateBookingInfoResultModel>`

```json
{
  "Status": 1,
  "Data": {
    "IsValid": true,
    "InvalidCode": null,
    "ErrorData": null
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| IsValid | bool | `true` = hợp lệ, có thể tiếp tục |
| InvalidCode | string | Mã lỗi nếu không hợp lệ (ví dụ: `CAR_BUSY`, `DATE_INVALID`) |
| ErrorData | Dictionary | Dữ liệu bổ sung để UI hiển thị thông báo lỗi tuỳ mã |

---

## 5. Đặt xe (Booking)

### `POST /api/v1/RentalService/Booking`

**Mô tả:** Tạo đơn đặt xe. Sau khi đặt thành công, hệ thống tự động lấy chi tiết đơn (`NeedGetOrderDetailAfterBooking = true`). Bước này cần được gọi sau khi đã `UpdateBookingInfo` thành công.

**Phân quyền:** `[Authorize]`

**Hiệu năng:** 2,648 lượt/30 ngày | avg **2,065ms** 🔴

**Request Body:** `RentalServiceParamModel`

```json
{
  "RentalServiceItemId": "abc-123",
  "FromDate": 1740787200.0,
  "ToDate": 1740960000.0,
  "VoucherCode": "GIAM10",
  "MessageToOwner": "Tôi sẽ đến đúng giờ",
  "DeliveryAddress": "123 Xuân Thuỷ, Hà Nội",
  "DeliveryInfo": {
    "UserSearchAddress": "123 Xuân Thuỷ, Hà Nội",
    "UserSearchLat": 21.034,
    "UserSearchLng": 105.781,
    "IsUserTakeSelected": true
  }
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| RentalServiceItemId | string | Có | ID xe cần đặt |
| FromDate / ToDate | double | Có | Thời gian thuê (Unix timestamp) |
| VoucherCode | string | Không | Mã voucher giảm giá |
| MessageToOwner | string | Không | Lời nhắn gửi chủ xe |
| DeliveryAddress | string | Không | Địa chỉ nhận xe |
| DeliveryInfo | object | Không | Chi tiết địa chỉ nhận xe |

**Response:** `EzyResultObject<RentalServiceOrderInfoModel>` (khi `NeedGetOrderDetailAfterBooking = true`)

```json
{
  "Status": 1,
  "Msg": "Đặt xe thành công",
  "Data": {
    "OrderNumber": "ORD-20260301-001",
    "OrderStatusCode": "PENDING",
    "OrderStatus": "Chờ xác nhận",
    "FromDate": "01/03/2026 08:00",
    "ToDate": "03/03/2026 08:00",
    "RentalDate_Text": "01/03 - 03/03/2026",
    "EntDate": "26/02/2026",
    "TotalPrice": "1.890.000đ",
    "DepositAmount": "945.000đ",
    "AmountRemain": "945.000đ",
    "PaymentTypeName": "Tiền mặt",
    "CurrentUserCode": "RENTER",
    "CanBooking": false,
    "Buttons": [],
    "HeaderInfo": { "Title": "Chờ chủ xe xác nhận", "CanShow": true }
  }
}
```

> **Lưu ý logic nghiệp vụ:**
> - `IPAddress` được tự động lấy từ request và gán vào param trước khi gọi service.
> - Sau khi tạo đơn, hệ thống tự fetch lại chi tiết đơn và trả về ngay trong response.
> - Đơn đặt sẽ ở trạng thái `PENDING` (chờ chủ xe xác nhận).

---

## 6. Chi tiết đơn hàng

### `POST /api/v1/RentalService/GetOrderDetail`

**Mô tả:** Lấy thông tin chi tiết của một đơn hàng (dành cho cả Renter và Owner). Trả về đầy đủ thông tin hiển thị màn hình theo dõi đơn.

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

**Response:** `EzyResultObject<RentalServiceOrderInfoModel>`

```json
{
  "Status": 1,
  "Data": {
    "OrderNumber": "ORD-20260301-001",
    "OrderStatusCode": "PENDING",
    "OrderStatus": "Chờ xác nhận",
    "CurrentUserCode": "RENTER",
    "Name": "Toyota Vios 2022",
    "AvatarUrl": "https://...",
    "FromDate": "01/03/2026 08:00",
    "FromDate_Format_1": "Chủ nhật, 01 tháng 03, 2026\n08:00",
    "FromDate_Format_2": "08:00, 01 tháng 03, 2026",
    "ToDate": "03/03/2026 08:00",
    "ToDate_Format_1": "Thứ ba, 03 tháng 03, 2026\n08:00",
    "EntDate_Format_2": "10:30, 26 tháng 02, 2026",
    "TotalPrice": "1.890.000đ",
    "DepositAmount": "945.000đ",
    "AmountRemain": "945.000đ",
    "PaymentTypeName": "Tiền mặt",
    "Hotline": "1900xxxx",
    "UserName": "Nguyễn Văn B",
    "OwnerInfo": { "Name": "Nguyễn Văn A", "AvatarUrl": "..." },
    "AnotherUserInfo": { "Name": "Nguyễn Văn A", "MobilePhone": "09xxxxxxxx" },
    "Buttons": [
      { "Text": "Huỷ đơn", "ActionCode": "CANCEL" }
    ],
    "HeaderInfo": { "Title": "Chờ chủ xe xác nhận", "CanShow": true },
    "PaymentInfos": [],
    "RentalPriceDetails": []
  }
}
```

---

## 7. Xác nhận đơn

### `POST /api/v1/RentalService/OrderConfirm`

**Mô tả:** Chủ xe xác nhận chấp nhận đơn đặt xe từ khách.

**Phân quyền:** `[Authorize]` — chỉ chủ xe (Owner) mới có quyền.

**Hiệu năng:** avg **8,829ms** 🔴 — điểm nghẽn cổ chai, cần tối ưu.

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

**Response:** `EzyResultObject<object>`

```json
{
  "Status": 1,
  "Msg": "Cập nhật thành công"
}
```

> **Lưu ý:**
> - Sau khi confirm, đơn chuyển sang trạng thái `CONFIRMED`.
> - Hệ thống gửi notification cho Renter.
> - Xem thêm `OrderConfirmHasPay` — xác nhận khi khách đã thanh toán cọc.

### `POST /api/v1/RentalService/OrderConfirmHasPay`

**Mô tả:** Xác nhận đơn trong trường hợp khách đã thanh toán trước.

**Phân quyền:** `[Authorize]`

---

## 8. Huỷ đơn

### `POST /api/v1/RentalService/OrderCancel`

**Mô tả:** Huỷ đơn đặt xe. Cả Renter và Owner đều có thể huỷ (tuỳ trạng thái đơn và phân quyền). Logic hoàn tiền/tiền cọc được xử lý ở tầng service.

**Phân quyền:** `[Authorize]`

**Hiệu năng:** 208 lượt/30 ngày | avg **3,975ms** 🔴

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001",
  "CancelReasonId": "REASON_001",
  "CancelReasonDetail": "Tôi có việc đột xuất"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| OrderNumber | string | Có | Số đơn hàng |
| CancelReasonId | string | Không | ID lý do huỷ (từ danh mục) |
| CancelReasonDetail | string | Không | Mô tả chi tiết lý do |

**Response:** `EzyResultObject<object>`

```json
{
  "Status": 1,
  "Msg": "Hủy thành công"
}
```

### `POST /api/v1/RentalService/GetCancelOrderInfo`

**Mô tả:** Lấy thông tin hiển thị màn hình xác nhận huỷ (chính sách hoàn tiền, lý do huỷ...). Gọi trước khi hiển thị popup xác nhận huỷ.

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

---

## 9. Quản lý xe của tôi (Owner)

### `POST /api/v1/RentalService/List`

**Mô tả:** Lấy danh sách xe cho thuê của chủ xe hiện tại (Owner).

**Phân quyền:** `[Authorize]`

**Response:** `EzyResultObject<MyRentalServiceModel[]>`

```json
{
  "Status": 1,
  "Data": [
    {
      "RentalServiceId": "svc-001",
      "CarId": "car-001",
      "Model": "Toyota Vios",
      "PlateNumber": "29A-12345",
      "AvatarUrl": "https://...",
      "ShortAddress": "Cầu Giấy, Hà Nội",
      "Status": "ACTIVE",
      "RentalPrice": 700000,
      "RentalPrice_Txt": "700.000đ/ngày",
      "Rate": 4.8,
      "Rate_Txt": "4.8",
      "ServedCount": 120,
      "ViewsCount": 500,
      "DaysCount": 45,
      "CanEdit": true,
      "CanView": true,
      "CanSubmit2Review": false,
      "RentalStatus": {
        "StatusName": "Đang hoạt động",
        "StatusColorCode": "#00AA00",
        "StatusCode": "ACTIVE",
        "StatusToChange": []
      }
    }
  ]
}
```

### `POST /api/v2/RentalService/List`

**Mô tả:** Phiên bản v2 — trả về danh sách xe nhóm theo trạng thái (Đang hoạt động, Đang chờ duyệt...).

**Response:** `EzyResultObject<MyRentalServiceGroupResultModel>`

```json
{
  "Status": 1,
  "Data": {
    "Groups": [
      {
        "Name": "Đang hoạt động",
        "RentalServiceItems": [ ]
      },
      {
        "Name": "Chờ duyệt",
        "RentalServiceItems": [ ]
      }
    ]
  }
}
```

### `POST /api/v1/RentalService/Detail`

**Mô tả:** Lấy chi tiết xe của Owner để chỉnh sửa (khác với `SearchingRentalService/Detail` dành cho Renter).

**Request Body:**

```json
{
  "RentalServiceItemId": "abc-123"
}
```

**Response:** `EzyResultObject<RentalServiceModel>` — bao gồm đầy đủ: `Address`, `Features`, `Images`, `Documents`, `DailyRentalPrice`, `RentalSetting`, `CurrentWorkingAndBusyDate`, `VehicleInfo`, v.v.

### `POST /api/v1/RentalService/GetMyOrders`

**Mô tả:** Lấy danh sách đơn hàng của tôi (tổng hợp cả Renter và Owner).

### `POST /api/v1/RentalService/GetMyOrder_RentCar_RoleOwner`

**Mô tả:** Lấy danh sách đơn hàng với vai trò là chủ xe.

### `POST /api/v1/RentalService/GetMyOrder_RentCar_RoleRenter`

**Mô tả:** Lấy danh sách đơn hàng với vai trò là người thuê xe.

---

## 10. Các API phụ trợ

### `POST /api/v1/RentalService/OrderBegin`

**Mô tả:** Xác nhận nhận/giao xe — bắt đầu chuyến đi. **Cả Owner và Renter** đều gọi API này (2-sided handshake). Khi cả 2 bên đã confirm → status chuyển sang `INTHETRIP`.

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001",
  "ID_GUID": "abc-123"
}
```

**Response:** `EzyResultObject<RentalServiceOrderInfoModel>`

```json
{
  "Status": 1,
  "Msg": "Cập nhật thành công",
  "Data": {
    "OrderNumber": "ORD-20260301-001",
    "OrderStatusCode": "WAITING2DEPARTURE",
    "Order_Vehicle": {
      "DeliverVehicleByUsername": "Nguyễn Văn A",
      "DeliverVehicleTime": "01/03/2026 08:00",
      "ReceiveVehicleByUsername": null,
      "ReceiveVehicleTime": null
    }
  }
}
```

> **Lưu ý:**
> - Owner gọi → set `DeliverVehicleByUsername` + `DeliverVehicleTime`
> - Renter gọi → set `ReceiveVehicleByUsername` + `ReceiveVehicleTime`
> - Khi cả 2 đã set → status → `INTHETRIP`
> - Chỉ gọi được khi status = `WAITING2DEPARTURE`

### `POST /api/v1/RentalService/OrderEnd`

**Mô tả:** Xác nhận kết thúc chuyến (trả xe). Sau khi hoàn thành, hệ thống tính tiền và chuyển khoản cho owner.

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001",
  "ID_GUID": "abc-123"
}
```

**Response:** `EzyResultObject<object>`

```json
{
  "Status": 1,
  "Msg": "Cập nhật thành công"
}
```

> **Lưu ý:**
> - Chỉ gọi được khi status = `INTHETRIP`
> - Sau khi End → status → `DONE`
> - Nếu không ai gọi End sau ToDate + 60 phút → `AutoCompleteOrderEngine` tự hoàn thành

### `POST /api/v1/RentalService/OrderPay`

**Mô tả:** Thanh toán đặt cọc đơn hàng. Hỗ trợ ví điện tử, MB Bank, VietQR.

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001",
  "ID_GUID": "abc-123"
}
```

**Response:** `EzyResultObject<object>`

```json
{
  "Status": 1,
  "Msg": "Thanh toán thành công"
}
```

> **Lưu ý:**
> - Chỉ gọi được khi status ∈ `[CUS2DEPOSIT, WAITING2CONFIRMDEPOSIT]`
> - Phải thanh toán trước `Customer2DepositEndTime` (mặc định 3 giờ business hours)
> - Sau khi thanh toán → `DepositDoneAt` được set, status → `WAITING2DEPARTURE`

### `POST /api/v1/RentalService/OrderReview`

**Mô tả:** Đánh giá sau khi kết thúc chuyến. Cả renter và owner đều có thể đánh giá.

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001",
  "ReviewRating": 5,
  "ReviewContent": "Xe tốt, chủ xe nhiệt tình"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| OrderNumber | string | Có | Số đơn hàng |
| ReviewRating | int | Có | Điểm đánh giá (1-5) |
| ReviewContent | string | Không | Nội dung đánh giá |

**Response:** `EzyResultObject<object>`

```json
{
  "Status": 1,
  "Msg": "Đánh giá thành công"
}
```

> **Lưu ý:** Chỉ gọi được khi status = `DONE`

### `POST /api/v1/RentalService/GetCancelOrderInfo`

**Mô tả:** Lấy thông tin hiển thị màn hình xác nhận huỷ (chi phí huỷ, hoàn tiền, lý do). Gọi trước khi hiển thị popup xác nhận huỷ.

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

**Response:** `EzyResultObject<object>`

```json
{
  "Status": 1,
  "Data": {
    "CancelBookingPrices": [
      {
        "Title": "Khách hàng đã thanh toán",
        "PriceText": "945.000đ"
      },
      {
        "Title": "Chi phí hủy chuyến",
        "PriceText": "283.500đ"
      },
      {
        "Title": "Tiền hoàn lại",
        "PriceText": "661.500đ"
      }
    ],
    "Message": "Bạn sẽ bị mất 30% tiền cọc nếu huỷ chuyến"
  }
}
```

### `POST /api/v1/SearchingRentalService/SearchVouchers`

**Mô tả:** Tìm kiếm voucher áp dụng được cho một xe cụ thể.

**Phân quyền:** `[AllowAnonymous]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "RentalServiceItemId": "abc-123",
  "FromDate": 1740787200.0,
  "ToDate": 1740960000.0
}
```

**Response:** `EzyResultObject<RentalServiceVoucherPublicModel[]>`

```json
{
  "Status": 1,
  "Data": [
    {
      "Id": "voucher-001",
      "AvatarUrl": "https://...",
      "Title": "Giảm 10% đơn hàng",
      "DiscountType": "PERCENT",
      "DiscountPercent": "10%",
      "DiscountMoney": null,
      "MaximumDiscountMoney": "500.000đ",
      "DiscountTitle": "Giảm 10%",
      "Description": "Áp dụng cho đơn từ 1.000.000đ",
      "ApplyFrom": "01/03/2026",
      "ApplyTo": "31/03/2026",
      "Code": "GIAM10",
      "DiscountDescription": "Giảm tối đa 500.000đ"
    }
  ]
}
```

### `POST /api/v1/RentalService/GetMyOrders`

**Mô tả:** Lấy cấu hình tabs danh sách đơn hàng (tổng hợp cả Renter và Owner). Trả về danh sách tabs với số lượng đơn theo từng status.

**Phân quyền:** `[Authorize]`

**Response:** `EzyResultObject<AppScreenUIControlSettingOptionModel>`

```json
{
  "Status": 1,
  "Data": {
    "PageConfig": {
      "Contents": [
        { "Title": "Chờ xác nhận (3)", "RequestData": { "StatusCodes": "OWNER2CONFIRM" } },
        { "Title": "Chờ cọc (1)", "RequestData": { "StatusCodes": "CUS2DEPOSIT,WAITING2CONFIRMDEPOSIT" } },
        { "Title": "Sắp khởi hành (2)", "RequestData": { "StatusCodes": "WAITING2DEPARTURE" } },
        { "Title": "Đang thuê (1)", "RequestData": { "StatusCodes": "INTHETRIP" } },
        { "Title": "Hoàn thành (15)", "RequestData": { "StatusCodes": "DONE" } }
      ]
    }
  }
}
```

### `POST /api/v1/RentalService/GetMyOrder_RentCar_RoleOwner`

**Mô tả:** Lấy danh sách đơn hàng với vai trò chủ xe. Cấu trúc response tương tự `GetMyOrders`.

**Phân quyền:** `[Authorize]`

### `POST /api/v1/RentalService/GetMyOrder_RentCar_RoleRenter`

**Mô tả:** Lấy danh sách đơn hàng với vai trò người thuê. Cấu trúc response tương tự `GetMyOrders`.

**Phân quyền:** `[Authorize]`

### `POST /api/v1/SearchingRentalService/GetSettingApp`

**Mô tả:** Lấy cấu hình app (feature flags, thông báo...). Thường gọi khi khởi động app.

**Phân quyền:** `[AllowAnonymous]` — bypass JWT error (cho phép gọi kể cả token invalid)

**Request Body:** `RentalService_GetSettingModel`

```json
{
  "AppId": "com.sigo.app",
  "AppVersion": "2.5.0",
  "AppVersionBuild": "250",
  "BrandModel": "iPhone 15",
  "BrandName": "Apple",
  "OSName": "iOS",
  "OSVer": "17.0",
  "DeviceNumber": "device-uuid",
  "FCMTokenKey": "fcm-token"
}
```

### `POST /api/v1/SearchingRentalService/SaveLog_UserClick_Rent`

**Mô tả:** Ghi log hành vi người dùng nhấn vào xe (analytics/tracking). Fire-and-forget, không cần chờ response.

**Phân quyền:** `[AllowAnonymous]`

### `GET /api/v1/SearchingRentalService/GetRentalService_SelfdriveCarRental_Alias`

**Mô tả:** Lấy danh sách alias/slug mapping cho tất cả xe (dùng cho SEO/routing phía client).

**Phân quyền:** `[AllowAnonymous]`

### `POST /api/v1/RentalService/InsertNewRentalService`

**Mô tả:** Tạo mới một dịch vụ cho thuê xe (bước đầu tiên trong luồng đăng ký xe). Tạo ra RentalServiceItem với `IsAddNew = true`.

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "RentalServiceCategoryCode": "RENT_CAR"
}
```

**Response:** `EzyResultObject<RentalServiceModel>`

```json
{
  "Status": 1,
  "Data": {
    "Id": "svc-new-001",
    "RentalServiceCategoryCode": "RENT_CAR",
    "IsAddNew": true,
    "IsApproved": null,
    "Images": [],
    "Features": [],
    "Documents": [],
    "DailyRentalPrice": [],
    "RentalSetting": {}
  }
}
```

### `POST /api/v1/RentalService/Submit2Review`

**Mô tả:** Chủ xe gửi xe để duyệt sau khi hoàn tất nhập liệu. Chuyển `IsAddNew = false`, `IsWaiting2Approve = true`.

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "RentalServiceItemId": "svc-new-001",
  "RentalServiceCategoryCode": "RENT_CAR"
}
```

**Response:** `EzyResultObject<RentalServiceModel>`

```json
{
  "Status": 1,
  "Msg": "Gửi duyệt thành công"
}
```

### `POST /api/v1/RentalService/UpdateRentalServiceStatus`

**Mô tả:** Cập nhật trạng thái xe (Tạm dừng, Kích hoạt lại...).

**Phân quyền:** `[Authorize]`

**Request Body:** `RentalServiceParamModel`

```json
{
  "RentalServiceItemId": "svc-001",
  "RentalServiceCategoryCode": "RENT_CAR",
  "RentalServiceStatus": "Suspended"
}
```

| RentalServiceStatus | Mô tả |
|---------------------|-------|
| `Active` | Kích hoạt lại |
| `Suspended` | Tạm ngưng |
| `Deactive` | Huỷ kích hoạt |

**Response:** `EzyResultObject<RentalServiceModel>`

```json
{
  "Status": 1,
  "Msg": "Cập nhật thành công"
}
```

---

## Luồng nghiệp vụ chính

### Luồng đặt xe (Renter)

```
1. SearchingRentalService/List        → Tìm xe
2. SearchingRentalService/Detail      → Xem chi tiết xe
3. CheckBeforeUpdateBookingInfo       → Kiểm tra xe còn trống
4. UpdateBookingInfo                  → Tính giá (gọi lại khi đổi ngày/địa chỉ/voucher)
5. RentalService/Booking              → Đặt xe → Trả về OrderDetail
6. RentalService/GetOrderDetail       → Theo dõi đơn
7. RentalService/OrderCancel          → Huỷ nếu cần
8. RentalService/OrderReview          → Đánh giá sau chuyến
```

### Luồng quản lý đơn (Owner)

```
1. GetMyOrder_RentCar_RoleOwner       → Xem danh sách đơn
2. GetOrderDetail                     → Xem chi tiết đơn
3. OrderConfirm / OrderConfirmHasPay  → Xác nhận đơn
4. OrderBegin                         → Xác nhận giao xe
5. OrderEnd                           → Xác nhận nhận lại xe
6. OrderReview                        → Đánh giá khách thuê
```

### Luồng đăng ký xe mới (Owner)

```
1. v2/RentalService/GetVehicleCreationData  → Lấy dữ liệu form tạo xe
2. RentalService/InsertNewRentalService     → Tạo xe mới
3. [Các API GetImage, GetAddress, GetFeature, GetDocument, GetSetting, GetDailyRentalPrice]
   → Lần lượt lấy/cập nhật từng phần thông tin xe
4. RentalService/Submit2Review             → Gửi duyệt
```

---

## Error Codes & Validation Rules

### Booking Validation (RentalService/Booking, UpdateBookingInfo)

| Error Key | Message mặc định | Điều kiện trigger |
|-----------|------------------|-------------------|
| `msg_error_no_public_rental_service_found` | Không tìm thấy xe. Bạn vui lòng chọn xe khác | Không tìm thấy xe matching hoặc xe bị tạm ngưng/xoá |
| `msg_error_no_booking_fromdate_todate` | Vui lòng chọn Ngày đi và Ngày về | FromDate hoặc ToDate là null |
| `msg_error_booking_fromdate_greater_todate` | Ngày đi không được lớn hơn Ngày về | FromDate > ToDate |
| `msg_error_booking_fromdate_equal_todate` | Ngày đi không được trùng với Ngày về | FromDate == ToDate |
| `msg_error_booking_from_date_to_date_incorrect` | Ngày thuê không được là ngày quá khứ. Vui lòng chọn lại | FromDate hoặc ToDate < DateTime.Now |
| `msg_error_no_booking_address` | Vui lòng chọn Điểm giao nhận xe | DeliveryAddress trống |
| `msg_error_message_to_owner_is_empty` | Vui lòng nhập lời nhắn cho chủ xe | MessageToOwner trống |
| `msg_error_booking_rental_date_incorrect` | Dịch vụ đã bận. Vui lòng chọn lại ngày thuê | Trùng ServiceItem_BookedRentalSchedule |
| `msg_error_rental_day_count_not_met_require` | Số ngày cần thuê tối thiểu là #RentalDayCount# | RentalDayCount < MinimumRequiredRentalDays |
| `msg_error_must_login_to_book_rental_service` | Vui lòng đăng nhập để có thể sử dụng dịch vụ | User chưa đăng nhập |
| `msg_error_user_rent_their_own_service` | Bạn không thể thuê dịch vụ cho thuê của chính bạn | Renter ID == Owner ID |
| `msg_error_delivery_distance_exceed_maximum` | Điểm giao nhận xe vượt quá #Distance#. Vui lòng chọn địa chỉ khác | Khoảng cách giao xe > maximumDeliveryMileage |
| `msg_error_voucher_rich_limit` | Voucher đã đạt giới hạn sử dụng | Mã giảm giá đã hết lượt |
| (dynamic) | Dịch vụ bận vào [thứ] | Ngày thuê trùng ServiceItem_WeekdaysBusyRentalSchedule |
| (dynamic) | Dịch vụ bận vào [ngày cụ thể] | Ngày thuê trùng ServiceItem_DateBusyRentalSchedule |

**Service suspension checks** — Xe bị trả lỗi `msg_error_no_public_rental_service_found` khi:
- `IsApproved != true` (chưa duyệt)
- `IsAddNew == true` (mới tạo, chưa submit)
- `IsSuspended == true` (đang tạm ngưng)
- `IsDeactive == true` (đã huỷ kích hoạt)

### CheckBeforeUpdateBookingInfo — InvalidCode

| InvalidCode | Điều kiện | ErrorData |
|-------------|-----------|-----------|
| `MinimumRentalDayRequired` | RentalDayCount < MaximumRequiredRentalDays | `ValidToDate`, `MsgInCalendar`, `WarningMsg { Title, Description }` |

### Order Confirm Validation

| Error Key | Message mặc định | Điều kiện |
|-----------|------------------|-----------|
| `msg_error_no_permission_to_confirm_booking` | Bạn không có quyền để xác nhận đơn hàng | User không có permission Booking_Rental_Service_Owner_Can_Confirm_Permission |
| `msg_error_wait_booking_time_has_expired` | Thời gian chờ chủ xe xác nhận đã kết thúc | Owner2ConfirmEndTime <= DateTime.Now |
| `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. | Order status != OWNER2CONFIRM |

### Order Cancel Validation

| Error Key | Message mặc định | Điều kiện |
|-----------|------------------|-----------|
| `msg_error_no_permission_to_cancel_booking` | Bạn không có quyền để từ chối đơn hàng | Owner không có permission |
| `msg_error_renter_no_permission_to_cancel_booking` | Bạn không có quyền hủy chuyến | Renter không có permission |
| `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. | Status không thuộc [OWNER2CONFIRM, CUS2DEPOSIT, WAITING2CONFIRMDEPOSIT, WAITING2DEPARTURE] |
| (validation) | Vui lòng chọn lý do muốn hủy chuyến | CancelReasonId trống |
| (validation) | Vui lòng nhập lý do muốn hủy chuyến | CancelReasonCode == "another_reason" nhưng CancelReasonDetail trống |

### Order Begin/End Validation

| Error Key | Message mặc định | Điều kiện |
|-----------|------------------|-----------|
| `msg_error_no_perrmission_booking_begin_trip` | Bạn không có quyền bắt đầu chuyến đi | User không có permission (Owner hoặc Renter) |
| `msg_error_no_perrmission_booking_end_trip` | Bạn không có quyền kết thúc chuyến đi | User không có permission (Owner hoặc Renter) |
| `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. | Begin: status != WAITING2DEPARTURE; End: status != INTHETRIP |

### Order Review Validation

| Error Key | Message mặc định | Điều kiện |
|-----------|------------------|-----------|
| `msg_error_can_not_review_order` | Bạn không có quyền để đánh giá đơn hàng | User không phải owner lẫn renter |
| `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. | Status != DONE |

### Payment Validation

| Error Key | Message mặc định | Điều kiện |
|-----------|------------------|-----------|
| `msg_error_no_permission_to_pay_booking` | Bạn không có quyền để thanh toán đơn hàng | Renter không có permission |
| `msg_error_booking_cus_to_deposit_time_expired_renter` | Thời gian đặt cọc đã hết. Vui lòng thuê lại dịch vụ | Customer2DepositEndTime <= DateTime.Now |
| `msg_error_booking_status_changed` | [Warning]Trạng thái đơn hàng đã bị thay đổi. | Status không thuộc [CUS2DEPOSIT, WAITING2CONFIRMDEPOSIT] |

### Các lỗi khác

| Error Key | Message mặc định | Điều kiện |
|-----------|------------------|-----------|
| `msg_error_order_not_found` | Không tìm thấy đơn hàng | OrderNumber không tồn tại |
| `msg_error_not_car_owner` | Bạn không phải là chủ xe, không thể xem thông tin này | User không phải owner của xe |
| `msg_error_car_insurance_expired` | Bảo hiểm xe đã hết hạn | Vehicle insurance hết hạn |

### Auto Cancel (System)

| Điều kiện | Kết quả |
|-----------|---------|
| Status == OWNER2CONFIRM AND Owner2ConfirmEndTime <= now | Tự động huỷ → SYSTEMCANCEL |
| Status == CUS2DEPOSIT AND Customer2DepositEndTime <= now | Tự động huỷ → SYSTEMCANCEL |

> **Timeout mặc định:** `HourOwner2Confirm` = 3 giờ, `HourCustomer2Deposit` = 3 giờ (chỉ tính trong business hours: 7:00 - 21:00).

---

## Order Status Constants

| Status Code | Tên hiển thị | Mô tả |
|-------------|-------------|-------|
| `OWNER2CONFIRM` | Đợi chủ dịch vụ xác nhận | Đơn mới tạo, chờ owner confirm |
| `CUS2DEPOSIT` | Chờ đặt cọc | Owner đã confirm, chờ renter cọc |
| `WAITING2CONFIRMDEPOSIT` | Chờ xác nhận cọc | Renter đã chuyển cọc, chờ confirm |
| `WAITING2DEPARTURE` | Chờ khởi hành | Đã thanh toán, chờ ngày nhận xe |
| `INTHETRIP` | Đang trong chuyến | Đã nhận xe, đang thuê |
| `DONE` | Hoàn thành | Đã trả xe, chuyến kết thúc |
| `CUSCANCEL` | Khách hàng hủy chuyến | Renter huỷ |
| `OWNERCANCEL` | Người cho thuê hủy cho thuê | Owner huỷ |
| `SYSTEMCANCEL` | Hệ thống hủy chuyến | Timeout tự động huỷ |

```
OWNER2CONFIRM ──► CUS2DEPOSIT ──► WAITING2CONFIRMDEPOSIT ──► WAITING2DEPARTURE ──► INTHETRIP ──► DONE
      │                │                    │                        │                   │
      └─► OWNERCANCEL  └─► CUSCANCEL       └─► CUSCANCEL           └─► CUSCANCEL       └─► DONE (auto)
      └─► SYSTEMCANCEL └─► SYSTEMCANCEL     └─► OWNERCANCEL        └─► OWNERCANCEL
      └─► CUSCANCEL
```

---

## Ghi chú kỹ thuật

- **`RentalServiceCategoryCode`:** Tất cả controller tự động gán `"RENT_CAR"` nếu client không truyền. Đây là hằng số phân loại dịch vụ.
- **Ngày tháng:** Truyền dưới dạng **Unix timestamp (double)** — không phải ISO 8601. Ví dụ: `1740787200.0` = 2026-03-01 00:00:00 UTC.
- **`IPAddress`:** Các API Booking và Search tự động lấy IP từ request, không cần client truyền.
- **`SearchVersion`:** Field dự phòng cho A/B test phiên bản thuật toán tìm kiếm.
- **Response wrapper:** Tất cả API trả về `EzyResultObject<T>` với `Status = 1` là thành công, `Status = 0` là lỗi, `Status = -1` là lỗi hệ thống, `Status = -2` là unauthorized.
