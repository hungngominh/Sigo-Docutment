# Core Module: Order — Quản lý đơn hàng

> **Project:** `AllianceMiddlemanWebAPI` | **Domain:** Nghiệp vụ cốt lõi

## Mục lục
- [Tổng quan](#tổng-quan)
- [Controllers](#controllers)
- [Entities](#entities)
- [API Endpoints](#api-endpoints)
- [Order Status Flow](#order-status-flow)
- [Quick Order](#quick-order)
- [Hiệu năng](#hiệu-năng)

---

## Tổng quan

Module Order quản lý toàn bộ vòng đời đơn hàng thuê xe, từ khi tạo đến khi hoàn tất và đánh giá. Là trung tâm kết nối giữa Rental Service, Payment, User, Vehicle và Notification.

**Trạng thái đơn hàng:** Được quản lý qua `ConfigOrderStatus` — có 10+ trạng thái khác nhau.

---

## Controllers

### Quản lý đơn hàng

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `OrderController` | `api/v1/Order` | CRUD đơn hàng cơ bản |
| `Order_ListViewController` | `api/v1/Order_ListView` | Danh sách đơn dạng list view |
| `Order_ListViewController_RentCar` | `api/v1/Order_ListView_RentCar` | List view chuyên cho thuê xe: Begin/End/Cancel |
| `MyOrder_RentCarController` | `api/v1/MyOrder_RentCar_RoleOwner` / `_RoleRenter` | Đơn hàng của tôi theo vai trò |

### Thanh toán & Đánh giá

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `Order_PaymentController` | `api/v1/Order_Payment` | Thanh toán đơn hàng |
| `Order_RatingController` | `api/v1/Order_Rating` | Đánh giá sau chuyến |
| `Order_ReceiveVehicle_ImageController` | `api/v1/Order_ReceiveVehicle_Image` | Ảnh nhận/trả xe |
| `Order_VehicleController` | `api/v1/Order_Vehicle` | Xe gán cho đơn |
| `Order_Vehicle_InsuranceSubmittedController` | `api/v1/Order_Vehicle_InsuranceSubmitted` | Bảo hiểm nộp theo đơn |
| `Order_SMS_Mapping` | `api/v1/Order_SMS_Mapping` | Map SMS ngân hàng → đơn |

### Quick Order

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `QuickOrderRequestController` | `api/v1/QuickOrderRequest` | Yêu cầu đặt xe nhanh |
| `QuickOrderRequestDetailController` | `api/v1/QuickOrderRequestDetail` | Chi tiết yêu cầu |
| `QuickOrderSuggestionController` | `api/v1/QuickOrderSuggestion` | Gợi ý xe |
| `QuickOrderSuggestionDetailController` | `api/v1/QuickOrderSuggestionDetail` | Chi tiết gợi ý |

---

## Entities

### Order (đơn hàng chính)

| Entity | Mô tả |
|--------|-------|
| `Order` | Đơn hàng: renter, owner, dates, status, total price |
| `Order_ChangeStatus` | Lịch sử thay đổi trạng thái (audit trail) |
| `Order_Finance` | Chi tiết tài chính: base price, fees, discounts, total |
| `Order_Setting` | Cài đặt riêng per-order (delivery, options) |

### Thanh toán

| Entity | Mô tả |
|--------|-------|
| `Order_Payment` | Phương thức thanh toán + trạng thái |
| `Order_SMS_CompanyBankAccountActivity_Mapping` | Map SMS bank → payment confirmation |
| `Order_DiscountCode_Applied_OneTimeUse` | Mã giảm giá đã dùng |

### Đánh giá

| Entity | Mô tả |
|--------|-------|
| `Order_Rating` | Rating + comment từ renter/owner |
| `Order_Rating_File` | Ảnh đính kèm đánh giá |
| `Order_ReportHistory` | Báo cáo sự cố trong chuyến |

### Xe & Ảnh

| Entity | Mô tả |
|--------|-------|
| `Order_Vehicle` | Xe được gán cho đơn |
| `Order_Vehicle_Address` | Địa chỉ nhận/trả xe |
| `Order_Vehicle_InsuranceSubmitted` | Bảo hiểm nộp kèm |
| `Order_ReceiveVehicle_Image` | Metadata ảnh nhận/trả |
| `Order_ReceiveVehicle_Image_File` | File ảnh thực tế |

### Quick Order

| Entity | Mô tả |
|--------|-------|
| `QuickOrderRequest` | Yêu cầu đặt nhanh: location, dates, budget |
| `QuickOrderRequestDetail` | Chi tiết yêu cầu (loại xe, tính năng) |
| `QuickOrderSuggestion` | Gợi ý xe phù hợp (hệ thống tạo) |
| `QuickOrderSuggestionDetail` | Chi tiết xe gợi ý |

---

## API Endpoints

### Quản lý đơn hàng

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Order/List` | Danh sách đơn hàng | Required |
| POST | `/api/v1/Order/Detail` | Chi tiết đơn | Required |
| POST | `/api/v1/Order_ListView_RentCar/Begin` | Bắt đầu chuyến (nhận xe) | Required |
| POST | `/api/v1/Order_ListView_RentCar/End` | Kết thúc chuyến (trả xe) | Required |
| POST | `/api/v1/Order_ListView_RentCar/OwnerCancel` | Owner huỷ đơn | Required |
| POST | `/api/v1/Order_ListView_RentCar/RenterCancel` | Renter huỷ đơn | Required |
| POST | `/api/v1/MyOrder_RentCar_RoleOwner/List` | Đơn của tôi (vai trò Owner) | Required |
| POST | `/api/v1/MyOrder_RentCar_RoleRenter/List` | Đơn của tôi (vai trò Renter) | Required |

### Thanh toán

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Order_Payment/List` | Danh sách thanh toán | Required |
| POST | `/api/v1/Order_Payment/Add` | Thêm thanh toán | Required |
| POST | `/api/v1/Order_Payment/Update` | Cập nhật trạng thái | Required |

### Đánh giá

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Order_Rating/List` | Danh sách đánh giá | Required |
| POST | `/api/v1/Order_Rating/Submit` | Gửi đánh giá | Required |

### Quick Order

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/QuickOrderRequest/List` | Danh sách yêu cầu | Required |
| POST | `/api/v1/QuickOrderRequest/Add` | Tạo yêu cầu đặt nhanh | Required |
| POST | `/api/v1/QuickOrderRequest/RenterCancel` | Huỷ yêu cầu | Required |
| POST | `/api/v1/QuickOrderSuggestion/List` | Gợi ý xe | Required |

---

## Order Status Flow

```
                    ┌──────── RenterCancel ────── Cancelled
                    │
WaitingConfirm ─────┤
    (Booking)       │──── OwnerCancel ─────────── Cancelled
                    │
                    └──── OwnerConfirm ──► Confirmed
                                              │
                                  ┌───── RenterCancel ──► Cancelled (refund)
                                  │
                                  └──── Payment ──────► Paid
                                                         │
                                              ┌──── Cancel ──► Cancelled (refund)
                                              │
                                              └──── Begin ──► InProgress
                                                                  │
                                                           ┌──── Cancel ──► Cancelled (partial refund)
                                                           │
                                                           └──── End ──► Completed
                                                                            │
                                                                     ┌──── Rating
                                                                     │
                                                                     └──── Auto-complete
                                                                           (AutoCompleteOrderEngine)
```

**Trạng thái chính:**

| Status | Ý nghĩa | Ai trigger |
|--------|---------|-----------|
| `WaitingConfirm` | Chờ owner xác nhận | Renter booking |
| `Confirmed` | Owner đã confirm | Owner |
| `Paid` | Đã thanh toán | Renter |
| `InProgress` | Đang thuê (đã nhận xe) | Renter + Owner |
| `Completed` | Hoàn tất (đã trả xe) | Owner hoặc Auto-engine |
| `Cancelled` | Huỷ (bởi renter hoặc owner) | Either side |

---

## Quick Order

Quick Order là flow rút gọn cho renter: thay vì tự tìm và chọn xe, renter mô tả yêu cầu và hệ thống gợi ý xe phù hợp.

```
Renter → QuickOrderRequest/Add
    { location, dateFrom, dateTo, vehicleType, budget }
         │
         ▼
AutoCreateQuickOrderEngine (Background)
    → Tìm xe phù hợp
    → Tạo QuickOrderSuggestion
    → Push notification cho renter
         │
         ▼
Renter → Chọn xe từ gợi ý → Tạo Order thông thường
```

---

## Hiệu năng

| Endpoint | Avg (ms) | Max (ms) | Trạng thái |
|---------|----------|----------|-----------|
| `Order_ListView_RentCar/OwnerCancel` | **8,988** | 31,853 | CRITICAL |
| `QuickOrderRequest/RenterCancel` | **42,667** | — | CRITICAL |
| `RentalService/GetOrderDetail` | **2,199** | — | WARNING |

**`OwnerCancel` — 8,988ms:** Transaction scope quá rộng (cancel + refund + notification + status update trong cùng transaction).

**`QuickOrderRequest/RenterCancel` — 42,667ms:** Deadlock khi cancel. Cần investigate lock ordering.

---

*Xem thêm: [rental-service.md](./core-rental-service.md) | [cancel-flow.md](../04_BUSINESS_FLOWS/cancel-flow.md) | [ewallet.md](./ewallet.md)*
