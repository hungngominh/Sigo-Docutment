# API: Quick Order

> **Controllers:** `QuickOrderRequestController`, `QuickOrderRequestDetailController`, `QuickOrderSuggestionController`, `QuickOrderSuggestionDetailController`
> **Base:** `/api/v1/QuickOrderRequest*`, `/api/v1/QuickOrderSuggestion*`
> **Phân quyền:** `[Authorize]` (tất cả endpoint)

## Mục lục
- [Tổng quan](#tổng-quan)
- [Luồng gọi API](#luồng-gọi-api)
- [QuickOrderRequest — Tạo yêu cầu](#post-apiv1quickorderrequestaddquickorderrequest)
- [QuickOrderRequest — Booking](#post-apiv1quickorderrequestbooking)
- [QuickOrderRequest — Huỷ](#post-apiv1quickorderrequestrentercancelowner-cancel)
- [QuickOrderRequestDetail — Danh sách gợi ý xe](#post-apiv1quickorderrequestdetaillist)
- [QuickOrderRequestDetail — Chọn đặt](#post-apiv1quickorderrequestdetailselect2order_all)
- [QuickOrderSuggestion — Tìm xe rảnh](#post-apiv1quickordersuggestiongetfreerentalcar)
- [QuickOrderSuggestionDetail — Booking trực tiếp](#post-apiv1quickordersuggestiondetailbooking)

---

## Tổng quan

**Quick Order** là luồng đặt xe nhanh qua **admin/staff** thay mặt cho khách — khác với luồng thông thường (user tự đặt qua app):

| Điểm khác biệt | Luồng thường | Quick Order |
|---------------|-------------|-------------|
| Người tạo đơn | User trên app | Admin/Staff |
| Cách tìm xe | User tự search | Staff tìm xe rảnh, gợi ý cho khách |
| Xác nhận | User trực tiếp | Staff xác nhận thay khách |
| Phù hợp | Booking online | Booking qua hotline/zalo |

---

## Luồng gọi API

### Luồng 1: Staff nhận yêu cầu từ khách và tìm xe

```
Bước 1: POST /api/v1/QuickOrderRequest/AddQuickOrderRequest
        → Mục đích: Ghi nhận yêu cầu từ khách (ngày, địa điểm, loại xe)
        → Input: QuickOrderRequestInfoModel (FromDate, ToDate, Address, VehicleType...)
        → Output: QuickOrderRequestId (string)

Bước 2: POST /api/v1/QuickOrderSuggestion/GetFreeRentalCar
        → Mục đích: Tìm danh sách xe rảnh phù hợp với yêu cầu
        → Input: QuickOrderSuggestionModel (QuickOrderRequestId)
        → Output: QuickOrderSuggestionDetailModel[] — danh sách xe gợi ý

Bước 3: POST /api/v1/QuickOrderSuggestionDetail/AddMulti
        → Mục đích: Lưu danh sách xe gợi ý cho yêu cầu
        → Input: QuickOrderSuggestionDetailsModel (danh sách xe được chọn)

Bước 4: POST /api/v1/QuickOrderSuggestionDetail/UpdatePrice
        → Mục đích: Điều chỉnh giá nếu cần (staff có thể tuỳ chỉnh)
        → Input: QuickOrderSuggestionDetailsModel
```

### Luồng 2: Staff xác nhận booking với khách

```
Bước 1: POST /api/v1/QuickOrderRequestDetail/List
        → Mục đích: Xem lại tất cả xe đã gợi ý cho yêu cầu
        → Input: QuickOrderRequestId

Bước 2a (chọn 1 xe): POST /api/v1/QuickOrderSuggestionDetail/Booking
        → Mục đích: Đặt luôn 1 xe cụ thể
        → Input: SuggestionDetailId
        → Output: OrderModel (đơn hàng đã tạo)

Bước 2b (chọn tất cả): POST /api/v1/QuickOrderRequestDetail/Select2Order_All
        → Mục đích: Chọn tất cả xe gợi ý để đặt hàng loạt
        → Output: "Chọn đặt thành công"

Bước 3 (tuỳ chọn): POST /api/v1/QuickOrderRequest/Booking
        → Mục đích: Chốt đặt từ phía request (khác với booking từ suggestion detail)
        → Input: QuickOrderRequestParamModel
```

**Side effects khi booking thành công:**
- Tạo đơn hàng thực sự trong hệ thống
- Gửi notification cho chủ xe
- Block lịch xe

### Luồng 3: Huỷ yêu cầu

```
Bước 1 (nếu staff huỷ): POST /api/v1/QuickOrderRequest/OwnerCancel
        → Mục đích: Staff/owner huỷ yêu cầu từ phía mình
        → Input: QuickOrderRequestId + CancelReason
        → Output: "Hủy chuyến thành công"

Bước 1 (nếu khách huỷ): POST /api/v1/QuickOrderRequest/RenterCancel
        → Mục đích: Khách huỷ yêu cầu
        → Input: QuickOrderRequestId + CancelReason
        → Output: "Hủy chuyến thành công"
```

**Xử lý lỗi trong luồng:**

| Bước | Lỗi có thể xảy ra | Xử lý |
|------|-------------------|-------|
| AddQuickOrderRequest | Thiếu thông tin bắt buộc | Kiểm tra response, hiển thị lỗi |
| Booking | avg 13,450ms — timeout | Tăng timeout client lên 30s |
| RenterCancel | avg 42,667ms — rất chậm | ⚠️ Tăng timeout lên 60s, cần điều tra |

---

## `POST /api/v1/QuickOrderRequest/AddQuickOrderRequest`

**Mô tả:** Tạo yêu cầu thuê xe nhanh — ghi nhận thông tin từ khách (qua hotline/zalo).

**Vai trò trong luồng:** Bước 1 trong [Luồng 1](#luồng-1-staff-nhận-yêu-cầu-từ-khách-và-tìm-xe)

**Hiệu năng:** 14 lượt/30 ngày | avg **2,816ms** 🔴

### Request Body — `QuickOrderRequestInfoModel`
```json
{
  "FromDate": 1740787200.0,
  "ToDate": 1740960000.0,
  "Address": "Hà Nội",
  "VehicleType": "Car",
  "NumberOfSeat": 5,
  "CustomerName": "Nguyễn Văn A",
  "CustomerPhone": "0901234567",
  "Note": "Cần xe có GPS"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| FromDate | double | Có | Ngày nhận xe (Unix timestamp) |
| ToDate | double | Có | Ngày trả xe (Unix timestamp) |
| Address | string | Có | Địa điểm nhận xe |
| CustomerName | string | Có | Tên khách hàng |
| CustomerPhone | string | Có | SĐT liên hệ |
| NumberOfSeat | int | Không | Số chỗ ngồi yêu cầu |
| Note | string | Không | Ghi chú thêm |

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Data": "quick-order-request-guid"
}
```

---

## `POST /api/v1/QuickOrderRequest/UpdateQuickOrderRequest`

**Mô tả:** Cập nhật thông tin yêu cầu (khi khách thay đổi ngày/địa điểm).

### Request Body — `QuickOrderRequestInfoModel`
Tương tự `AddQuickOrderRequest` + thêm `QuickOrderRequestId`.

### Response — `EzyResultObject<QuickOrderRequestModel>`

---

## `POST /api/v1/QuickOrderRequest/GetRequestInfoFormDefault/{id}`

**Mô tả:** Lấy thông tin form mặc định theo ID request.

**Route param:** `{id}` — QuickOrderRequestId

### Response — `EzyResultObject<ResultCheckFormAPIModel>`

---

## `POST /api/v1/QuickOrderRequest/CloneRequest`

**Mô tả:** Nhân bản một yêu cầu (tạo yêu cầu mới từ yêu cầu cũ — dùng khi khách muốn đặt lại).

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Tạo yêu cầu thành công",
  "Data": "new-quick-order-request-guid"
}
```

---

## `POST /api/v1/QuickOrderRequest/CreateRenter`

**Mô tả:** Tạo tài khoản renter mới từ thông tin khách (dùng khi khách chưa có tài khoản hệ thống).

### Request Body — `QuickOrder_CreateRenterModel`
```json
{
  "FullName": "Nguyễn Văn A",
  "MobilePhone": "0901234567"
}
```

### Response — `EzyResultObject<StaffModel>`

---

## `POST /api/v1/QuickOrderRequest/Booking`

**Mô tả:** Chốt booking từ quick order request.

**Vai trò trong luồng:** Bước 3 trong [Luồng 2](#luồng-2-staff-xác-nhận-booking-với-khách)

**Hiệu năng:** 9 lượt/30 ngày | avg **13,450ms** 🔴 | max 22,395ms — cần tối ưu khẩn

### Request Body — `QuickOrderRequestParamModel`
```json
{
  "QuickOrderRequestId": "quick-order-guid"
}
```

### Response — `EzyResultObject<QuickOrderRequestModel>`
```json
{
  "StatusCode": 1,
  "Msg": "Đặt xe thành công",
  "Data": {
    "QuickOrderRequestId": "quick-order-guid",
    "OrderNumber": "ORD-20260301-001",
    "Status": "Booked"
  }
}
```

> ⚠️ **Timeout:** avg 13s — client cần set timeout ≥ 30s.

---

## `POST /api/v1/QuickOrderRequest/RenterCancel` / `OwnerCancel`

**Mô tả:** Huỷ yêu cầu quick order.

**Vai trò trong luồng:** Bước 1 trong [Luồng 3](#luồng-3-huỷ-yêu-cầu)

**Hiệu năng:**
- `RenterCancel`: avg **42,667ms** 🔴 | max 74,582ms — **API chậm nhất hệ thống, cần điều tra**
- `OwnerCancel`: avg 8,988ms 🔴

### Request Body — `QuickOrderRequestParamModel`
```json
{
  "QuickOrderRequestId": "quick-order-guid",
  "CancelReasonId": "REASON_001",
  "CancelReasonDetail": "Khách thay đổi kế hoạch"
}
```

### Response — `EzyResultObject<QuickOrderRequestModel>`
```json
{
  "StatusCode": 1,
  "Msg": "Hủy chuyến thành công"
}
```

> ⚠️ **`RenterCancel` timeout:** Client cần set timeout ≥ 90s. Cần điều tra stored procedure gây chậm.

---

## `POST /api/v1/QuickOrderRequest/List`

**Mô tả:** Lấy danh sách quick order requests (admin/staff management).

### Request Body — `QuickOrderRequestParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "Status": "Pending"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<QuickOrderRequestModel>>`

---

## `POST /api/v1/QuickOrderRequestDetail/List`

**Mô tả:** Lấy danh sách xe đã gợi ý cho một quick order request.

**Vai trò trong luồng:** Bước 1 trong [Luồng 2](#luồng-2-staff-xác-nhận-booking-với-khách)

**Hiệu năng:** 394 lượt/30 ngày | avg **3,105ms** 🔴 | p95 9,418ms

### Request Body — `QuickOrderRequestDetailParamModel`
```json
{
  "QuickOrderRequestId": "quick-order-guid",
  "PageIndex": 1,
  "PageSize": 20
}
```

### Response — `EzyResultObject<EzyDataSourceResult<QuickOrderRequestDetailModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 5,
    "Data": [
      {
        "DetailId": "detail-guid",
        "VehicleName": "Toyota Vios 2022",
        "PlateNumber": "29A-12345",
        "DailyPrice": 700000,
        "IsSelected": false,
        "Status": "Available"
      }
    ]
  }
}
```

---

## `POST /api/v1/QuickOrderRequestDetail/Select2Order_All`

**Mô tả:** Chọn tất cả xe gợi ý để tiến hành đặt hàng loạt.

**Vai trò trong luồng:** Bước 2b trong [Luồng 2](#luồng-2-staff-xác-nhận-booking-với-khách)

### Request Body — `QuickOrderRequestDetailParamModel`
```json
{
  "QuickOrderRequestId": "quick-order-guid"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Chọn đặt thành công"
}
```

---

## `POST /api/v1/QuickOrderRequestDetail/SelectTopN_2Order`

**Mô tả:** Chọn N xe đầu tiên (tốt nhất) trong danh sách gợi ý để đặt.

### Request Body — `QuickOrderRequestDetailParamModel`
```json
{
  "QuickOrderRequestId": "quick-order-guid",
  "TopN": 3
}
```

---

## `POST /api/v1/QuickOrderSuggestion/List`

**Mô tả:** Lấy danh sách suggestion set cho các quick order requests.

### Request Body — `QuickOrderSuggestionParamModel`
```json
{
  "QuickOrderRequestId": "quick-order-guid"
}
```

---

## `POST /api/v1/QuickOrderSuggestion/GetFreeRentalCar`

**Mô tả:** Tìm danh sách xe rảnh phù hợp với yêu cầu — API core của Quick Order.

**Vai trò trong luồng:** Bước 2 trong [Luồng 1](#luồng-1-staff-nhận-yêu-cầu-từ-khách-và-tìm-xe)

### Request Body — `QuickOrderSuggestionModel`
```json
{
  "QuickOrderRequestId": "quick-order-guid"
}
```

### Response — `EzyResultObject<QuickOrderSuggestionDetailModel[]>`
```json
{
  "StatusCode": 1,
  "Data": [
    {
      "SuggestionDetailId": "sugg-detail-guid",
      "RentalServiceItemId": "service-item-guid",
      "VehicleName": "Toyota Vios 2022",
      "PlateNumber": "29A-12345",
      "DailyPrice": 700000,
      "Address": "Cầu Giấy, Hà Nội",
      "Distance": "2.5 km",
      "IsAvailable": true
    }
  ]
}
```

---

## `POST /api/v1/QuickOrderSuggestionDetail/List`

**Mô tả:** Lấy danh sách chi tiết suggestion đã lưu.

---

## `POST /api/v1/QuickOrderSuggestionDetail/AddMulti`

**Mô tả:** Lưu danh sách xe gợi ý vào suggestion.

**Vai trò trong luồng:** Bước 3 trong [Luồng 1](#luồng-1-staff-nhận-yêu-cầu-từ-khách-và-tìm-xe)

### Request Body — `QuickOrderSuggestionDetailsModel`
```json
{
  "QuickOrderSuggestionId": "suggestion-guid",
  "Details": [
    { "RentalServiceItemId": "service-item-guid-1" },
    { "RentalServiceItemId": "service-item-guid-2" }
  ]
}
```

---

## `POST /api/v1/QuickOrderSuggestionDetail/UpdatePrice`

**Mô tả:** Cập nhật giá cho từng xe trong suggestion (staff có thể điều chỉnh giá).

**Vai trò trong luồng:** Bước 4 trong [Luồng 1](#luồng-1-staff-nhận-yêu-cầu-từ-khách-và-tìm-xe)

### Request Body — `QuickOrderSuggestionDetailsModel`
```json
{
  "Details": [
    { "SuggestionDetailId": "sugg-detail-guid", "AdjustedPrice": 650000 }
  ]
}
```

---

## `POST /api/v1/QuickOrderSuggestionDetail/Booking`

**Mô tả:** Đặt xe trực tiếp từ một suggestion detail.

**Vai trò trong luồng:** Bước 2a trong [Luồng 2](#luồng-2-staff-xác-nhận-booking-với-khách)

### Request Body — `QuickOrderSuggestionDetailParamModel`
```json
{
  "SuggestionDetailId": "sugg-detail-guid"
}
```

### Response — `EzyResultObject<OrderModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "OrderNumber": "ORD-20260301-001",
    "OrderStatus": "PENDING",
    "TotalPrice": 2100000
  }
}
```

---

## `POST /api/v1/QuickOrderSuggestionDetail/GetDetail`

**Mô tả:** Lấy chi tiết một suggestion (bao gồm thông tin xe công khai — gọi `GetPublicRentalServiceDetailAsync`).

### Response — `EzyResultObject<QuickOrderSuggestionDetailModel>`

---

## Ghi chú kỹ thuật

- **Quick Order vs thông thường:** Quick Order tạo đơn thay mặt khách — không cần khách có tài khoản (dùng `CreateRenter` nếu cần).
- **`RenterCancel` avg 42s:** Cần điều tra SP gây bottleneck — nghi ngờ logic hoàn tiền/wallet phức tạp.
- **`Booking` avg 13s:** Tương tự, cần tối ưu SP.
- **Ngày tháng:** Unix timestamp (double) — nhất quán với `RentalService`.
