# API: Order

> **Controllers:** `OrderController`, `Order_PaymentController`, `Order_RatingController`, `Order_ListView_RentCarController`, `MyOrder_RentCar_RoleRenter_v2`, `MyOrder_RentCar_RoleOwner`
> **Base:** `/api/v1/Order`, `/api/v1/Order_ListView_RentCar`, `/api/v1/MyOrder_RentCar_*`

## Mục lục
- [Danh sách đơn hàng (Renter)](#danh-sách-đơn-hàng-renter)
- [Danh sách đơn hàng (Owner)](#danh-sách-đơn-hàng-owner)
- [Chi tiết đơn hàng](#chi-tiết-đơn-hàng)
- [Bắt đầu chuyến đi](#bắt-đầu-chuyến-đi)
- [Kết thúc chuyến đi](#kết-thúc-chuyến-đi)
- [Owner Cancel](#owner-cancel)
- [Renter Cancel](#renter-cancel)
- [Thanh toán](#thanh-toán)
- [Đánh giá](#đánh-giá)

---

## Danh sách đơn hàng (Renter)

### `POST /api/v1/MyOrder_RentCar_RoleRenter_v2/List`

**Mô tả:** Danh sách đơn thuê xe của người thuê, nhóm theo trạng thái.

**Phân quyền:** `[Authorize]`

**Thống kê:** 3,457 lượt/30 ngày | avg 1,059ms

**Request Body:** `MyOrder_RentCarParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "OrderStatusGroupCode": "ALL",
  "SearchText": ""
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| PageIndex | int | Không | Trang (default: 1) |
| PageSize | int | Không | Số item/trang (default: 20) |
| OrderStatusGroupCode | string | Không | Filter theo nhóm trạng thái: `ALL`, `PENDING`, `ACTIVE`, `COMPLETED`, `CANCELLED` |
| SearchText | string | Không | Tìm theo tên xe / mã đơn |

### Response — `EzyResultObject<EzyDataSourceResult<MyOrder_RentCarModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      {
        "OrderNumber": "ORD-20260301-001",
        "OrderStatusCode": "PENDING",
        "OrderStatus": "Chờ xác nhận",
        "Name": "Toyota Vios 2022",
        "AvatarUrl": "https://...",
        "FromDate": "01/03/2026 08:00",
        "ToDate": "03/03/2026 08:00",
        "TotalPrice": "1.890.000đ",
        "OwnerName": "Nguyễn Văn A",
        "OwnerAvatarUrl": "https://...",
        "Buttons": [
          { "Text": "Huỷ đơn", "ActionCode": "CANCEL" }
        ]
      }
    ],
    "Total": 15
  }
}
```

---

## Danh sách đơn hàng (Owner)

### `POST /api/v1/MyOrder_RentCar_RoleOwner/List`

**Mô tả:** Danh sách đơn cho thuê xe của chủ xe.

**Phân quyền:** `[Authorize]`

**Thống kê:** 1,829 lượt/30 ngày | avg 201ms

**Request/Response:** Tương tự Renter list, nhưng `CurrentUserCode = "OWNER"` và `Buttons` khác (Confirm, Cancel...).

---

## Chi tiết đơn hàng

### `POST /api/v1/RentalService/GetOrderDetail`

**Mô tả:** Lấy chi tiết đầy đủ một đơn hàng — dùng cho cả Renter và Owner. Xem chi tiết request/response tại [rental-service.md](./rental-service.md#6-chi-tiết-đơn-hàng).

**Phân quyền:** `[Authorize]`

**Hiệu năng:** 3,959 lượt/30 ngày | avg **2,199ms**

---

## Bắt đầu chuyến đi

### `POST /api/v1/Order_ListView_RentCar/Begin`

**Mô tả:** Xác nhận bắt đầu chuyến thuê — giao xe cho renter. Upload ảnh tình trạng xe lúc giao.

**Phân quyền:** `[Authorize]`

**Hiệu năng:** avg **3,075ms**

**Request Body:** `Order_ListView_RentCarParamModel`
```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

**Response:** `EzyResultObject<Order_ListView_RentCarModel>`
```json
{
  "StatusCode": 1,
  "Msg": "Bắt đầu chuyến thành công",
  "Data": {
    "OrderNumber": "ORD-20260301-001",
    "OrderStatusCode": "IN_PROGRESS",
    "OrderStatus": "Đang thuê"
  }
}
```

> **Side effects:**
> - Đơn chuyển sang trạng thái `IN_PROGRESS`
> - Gửi notification cho cả Renter và Owner
> - Block lịch xe trong khoảng thời gian thuê

---

## Kết thúc chuyến đi

### `POST /api/v1/Order_ListView_RentCar/End`

**Mô tả:** Xác nhận kết thúc chuyến thuê — nhận lại xe. Tính phí phát sinh nếu có (trả muộn, vượt km...).

**Phân quyền:** `[Authorize]`

**Hiệu năng:** avg **8,412ms** — bao gồm tính phí phát sinh, cập nhật ví, gửi notification

**Request Body:** `Order_ListView_RentCarParamModel`
```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

**Response:** `EzyResultObject<Order_ListView_RentCarModel>`
```json
{
  "StatusCode": 1,
  "Msg": "Kết thúc chuyến thành công",
  "Data": {
    "OrderNumber": "ORD-20260301-001",
    "OrderStatusCode": "COMPLETED",
    "OrderStatus": "Hoàn tất"
  }
}
```

> **Side effects:**
> - Đơn chuyển sang `COMPLETED`
> - Tính phí phát sinh (ExtraSurcharges) nếu có
> - Trigger chuyển tiền cho Owner (qua EWallet → MBBank)
> - Gửi notification nhắc đánh giá
> - Unblock lịch xe

---

## Owner Cancel

### `POST /api/v1/Order_ListView_RentCar/OwnerCancel`

**Mô tả:** Chủ xe huỷ đơn. Áp dụng penalty nếu huỷ sau khi confirm.

**Phân quyền:** `[Authorize]`

**Hiệu năng:** 89 lượt/30 ngày | avg **8,988ms** | max 31,853ms

**Request Body:** `Order_ListView_RentCarParamModel`
```json
{
  "OrderNumber": "ORD-20260301-001",
  "CancelReasonId": "OWNER_BUSY",
  "CancelReasonDetail": "Xe đang bảo dưỡng"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| OrderNumber | string | Có | Mã đơn hàng |
| CancelReasonId | string | Không | ID lý do huỷ (từ `ConfigVehicleRentalCancelReason`) |
| CancelReasonDetail | string | Không | Chi tiết lý do |

> **Side effects:**
> - Hoàn tiền cho Renter (nếu đã thanh toán)
> - Tăng `OwnerCancelOrderRatio` → ảnh hưởng priority
> - Gửi notification cho Renter

---

## Renter Cancel

### `POST /api/v1/Order_ListView_RentCar/RenterCancel`

**Mô tả:** Người thuê huỷ đơn. Chính sách hoàn tiền theo thời điểm huỷ.

**Phân quyền:** `[Authorize]`

**Hiệu năng:** avg **3,949ms**

**Request Body:** `Order_ListView_RentCarParamModel`
```json
{
  "OrderNumber": "ORD-20260301-001",
  "CancelReasonId": "RENTER_CHANGE_PLAN",
  "CancelReasonDetail": "Thay đổi kế hoạch"
}
```

> **Chính sách hoàn tiền:** Xem `CancelBookingPolicyInfo` trong response của `GetOrderDetail`.

---

## Thanh toán

### `POST /api/v1/Order_Payment/List`

**Mô tả:** Danh sách thanh toán của đơn hàng.

**Phân quyền:** `[Authorize]`

**Request Body:** `Order_PaymentParamModel`
```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

### `POST /api/v1/Order_Payment/FundingWallet`

**Mô tả:** Thanh toán đơn hàng bằng ví Sigo (EWallet).

**Phân quyền:** `[Authorize]`

**Request Body:** `Order_PaymentParamModel`
```json
{
  "OrderNumber": "ORD-20260301-001",
  "Amount": 1890000
}
```

### Response — `EzyResultObject<Order_PaymentModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "PaymentId": "pay-guid",
    "OrderNumber": "ORD-20260301-001",
    "Amount": 1890000,
    "PaymentType": "WALLET",
    "Status": "SUCCESS",
    "PaidAt": "2026-02-26T10:00:00Z"
  }
}
```

### `POST /api/v1/RentalService/OrderPay`

**Mô tả:** Endpoint thanh toán thay thế (gọi từ RentalService flow).

**Hiệu năng:** 28 lượt/30 ngày | avg **2,312ms**

---

## Đánh giá

### `POST /api/v1/Order_Rating/List`

**Mô tả:** Danh sách đánh giá của đơn hàng.

**Phân quyền:** `[Authorize]`

**Request Body:** `Order_RatingParamModel`
```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<Order_RatingModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Data": [
      {
        "RatingId": "rating-guid",
        "OrderNumber": "ORD-20260301-001",
        "Rating": 5,
        "Content": "Xe tốt, chủ xe nhiệt tình",
        "ReviewerName": "Nguyễn Văn B",
        "ReviewerAvatar": "https://...",
        "CreatedDate": "2026-03-04T10:00:00Z",
        "RatingFiles": []
      }
    ],
    "Total": 1
  }
}
```

### `POST /api/v1/Order_Rating_ListView/List`

**Mô tả:** Danh sách đánh giá dạng list view (admin).

### `POST /api/v1/Order_Rating_ListView/UpdateRentalServiceItem_Calculating`

**Mô tả:** Tính lại rating trung bình cho xe (admin trigger).

### `POST /api/v1/Order_Rating_ListView/UpdateUser_Calculating`

**Mô tả:** Tính lại rating trung bình cho user (admin trigger).

---

## Ghi chú kỹ thuật

- **Order flow:** Booking → Confirm → Pay → Begin → End → Rating. Xem chi tiết tại [rental-service.md](./rental-service.md#luồng-nghiệp-vụ-chính)
- **Cancel logic:** Khác nhau tuỳ OrderStatus lúc cancel. Phí penalty tính theo `CancelBookingPolicy`
- **Payment types:** Ví Sigo (WALLET), Tiền mặt (CASH), Chuyển khoản (BANK_TRANSFER)
- **Response wrapper:** `EzyResultObject<T>` — `StatusCode = 1` thành công

---

*Xem thêm: [rental-service.md](./rental-service.md) | [quick-order.md](./quick-order.md) | [core-order.md](../02_MODULES/core-order.md)*
