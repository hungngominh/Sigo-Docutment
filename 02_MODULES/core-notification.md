# Core Module: Notification — Thông báo & Hỗ trợ

> **Project:** `AllianceMiddlemanWebAPI` | **Domain:** Nghiệp vụ cốt lõi

## Mục lục
- [Tổng quan](#tổng-quan)
- [Controllers](#controllers)
- [Entities](#entities)
- [API Endpoints](#api-endpoints)
- [Kiến trúc notification](#kiến-trúc-notification)
- [Support Chat](#support-chat)
- [Proactive Notification](#proactive-notification)

---

## Tổng quan

Module Notification đảm nhận:
1. **Push Notification** — gửi thông báo đẩy tới mobile app (FCM/APNs)
2. **Real-time Notification** — push qua SignalR WebSocket
3. **In-app Notification** — danh sách notification trong app
4. **Support Chat** — chat hỗ trợ giữa user và staff
5. **Proactive Notification** — thông báo chủ động do hệ thống trigger

**Hiệu năng highlight:**
- `NotificationSender/SendMessage` → **0.48ms** (async queue pattern)
- `Notification/List` → **42ms** (đã tối ưu)
- `Notification/TotalNew` → **80ms**

---

## Controllers

### Notification

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `NotificationController` | `api/v1/Notification` | CRUD notifications cho user |
| `NotificationTemplateController` | `api/v1/NotificationTemplate` | Quản lý mẫu notification |
| `NotificationHubConnectionController` | `api/v1/NotificationHubConnection` | Quản lý SignalR connections |
| `NotificationMessage4AdminController` | `api/v1/NotificationMessage4Admin` | Notifications cho admin |
| `ProactiveNotificationController` | `api/v1/ProactiveNotification` | Notification chủ động |
| `ProactiveNotificationDetailController` | `api/v1/ProactiveNotificationDetail` | Chi tiết proactive |

### Support Chat

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `NotificationSupportSessionController` | `api/v1/NotificationSupportSession` | Phiên chat hỗ trợ |
| `NotificationSupportMessageController` | `api/v1/NotificationSupportMessage` | Tin nhắn trong phiên |
| `NotificationSupportActionController` | `api/v1/NotificationSupportAction` | Action buttons trong chat |
| `NotificationSupportTemplateController` | `api/v1/NotificationSupportTemplate` | Mẫu hỗ trợ |
| `NotificationSupportTemplateActionController` | `api/v1/NotificationSupportTemplateAction` | Action trong mẫu |
| `NotificationSupportTemplateMessageController` | `api/v1/NotificationSupportTemplateMessage` | Message trong mẫu |

### Sender (AppSystem)

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `NotificationSenderController` | `api/v1/NotificationSender` | Gửi notification (internal use) |

---

## Entities

### Notification

| Entity | Mô tả |
|--------|-------|
| `NotificationMessage` | Tin nhắn thông báo: title, body, type, isRead, targetUser |
| `NotificationTemplate` | Mẫu notification: title template, body template, type, channel |
| `PushNotificationHistory` | Lịch sử push đã gửi (success/fail, device, timestamp) |

### Proactive Notification

| Entity | Mô tả |
|--------|-------|
| `ProactiveNotification` | Thông báo chủ động: condition, target audience, schedule |
| `ProactiveNotificationDetail` | Chi tiết điều kiện trigger |

### Support Chat

| Entity | Mô tả |
|--------|-------|
| `NotificationSupportSession` | Phiên chat: user, staff assigned, status (open/closed) |
| `NotificationSupportMessage` | Tin nhắn: sender, content, timestamp, attachments |
| `NotificationSupportAction` | Action button: label, actionType, actionData |
| `NotificationSupportTemplate` | Mẫu hỗ trợ (FAQ responses, guided flows) |
| `NotificationSupportTemplateAction` | Action gắn với mẫu |
| `NotificationSupportTemplateMessage` | Message gắn với mẫu |

---

## API Endpoints

### Notification (User-facing)

| Method | Endpoint | Mô tả | Auth | Perf |
|--------|----------|-------|------|------|
| POST | `/api/v1/Notification/List` | Danh sách notifications | Required | 42ms |
| GET | `/api/v1/Notification/TotalNew` | Đếm chưa đọc | Required | 80ms |
| POST | `/api/v1/Notification/Read/{id}` | Đánh dấu đã đọc | Required | — |
| POST | `/api/v1/Notification/ReadAll` | Đọc tất cả | Required | — |

### Notification Sender (Internal)

| Method | Endpoint | Mô tả | Auth | Perf |
|--------|----------|-------|------|------|
| POST | `/api/v1/NotificationSender/SendMessage` | Gửi notification | Required | **0.48ms** |

### Notification Templates

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/NotificationTemplate/List` | DS mẫu | Required |
| POST | `/api/v1/NotificationTemplate/Add` | Thêm mẫu | Admin |
| POST | `/api/v1/NotificationTemplate/Update` | Sửa mẫu | Admin |

### Support Chat

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/NotificationSupportSession/List` | DS phiên chat | Required |
| POST | `/api/v1/NotificationSupportSession/Add` | Tạo phiên mới | Required |
| POST | `/api/v1/NotificationSupportMessage/List` | Tin nhắn trong phiên | Required |
| POST | `/api/v1/NotificationSupportMessage/Add` | Gửi tin nhắn | Required |
| POST | `/api/v1/NotificationSupportAction/List` | Actions | Required |

---

## Kiến trúc notification

### Gửi notification — Async Queue Pattern

```
[Caller Service]
    │ POST /NotificationSender/SendMessage
    │ { targetUserGuid, title, body, type, data }
    │
    ▼ ←── 0.48ms return (async, không chờ gửi)
[NotificationService Queue]        ← In-memory queue (INotificationService singleton)
    │
    ▼ (Background processing)
[NotificationBatchJobEngine]
    │
    ├──► [SignalR Hub]              ← Real-time push (user đang online)
    │    /NotificationHub
    │    → Client nhận event ngay lập tức
    │
    ├──► [FCM / APNs]              ← Push notification (app background)
    │    → PushNotificationEngine
    │    → Lưu PushNotificationHistory
    │
    └──► [Database]                 ← Lưu NotificationMessage
         → User có thể đọc lại sau
```

### Channels

| Channel | Khi nào | Latency |
|---------|---------|---------|
| **SignalR** | User đang mở app | < 100ms |
| **FCM/APNs Push** | App ở background | 1-5s |
| **In-app List** | User mở tab notifications | On-demand |

### SignalR Hub

```
Endpoint: /NotificationHub?access_token=<jwt>

Client ──── WSS ────► NotificationHub
                          │
                          ├── OnConnectedAsync()   → Register connection
                          ├── SendMessage()         → Push to specific user
                          └── OnDisconnectedAsync() → Cleanup
```

---

## Support Chat

Chat hỗ trợ trong app, giữa user và support staff.

### Flow

```
[User]                           [Sigo API]                    [Staff]
   │                                  │                            │
   ├─ SupportSession/Add ────────────►│ Tạo phiên mới             │
   │   { subject: "Vấn đề đơn #123" }│ Status: Open               │
   │                                  │ ─── Notify staff ──────────►│
   │                                  │                            │
   ├─ SupportMessage/Add ────────────►│ Gửi tin nhắn              │
   │   { content: "Xe bị trầy..." }  │ ─── SignalR push ──────────►│
   │                                  │                            │
   │                                  │◄── SupportMessage/Add ──────│
   │◄── SignalR push ────────────────│   Staff phản hồi            │
   │                                  │                            │
   │                                  │  Action buttons:           │
   │                                  │  [Hoàn tiền] [Liên hệ CSKH]│
   │                                  │                            │
   │◄── SupportAction ──────────────│  Staff chọn action          │
   │   { type: "refund", data: {} }  │                            │
```

### Support Templates

Staff có thể dùng mẫu phản hồi nhanh:
- FAQ answers (câu hỏi thường gặp)
- Guided troubleshooting flows
- Standard responses với action buttons

---

## Proactive Notification

Thông báo chủ động — hệ thống tự trigger dựa trên điều kiện:

| Engine | Trigger | Mục đích |
|--------|---------|---------|
| `ProactiveNotificationHasNewUser` | User mới đăng ký | Alert admin/staff |
| `NotificationRentalCarHighViewsJob` | Xe nhiều views, ít booking | Gợi ý owner giảm giá |
| `NotificationRentalCarUpcomingAvailabilityJob` | Xe sắp trống lịch | Nhắc owner cập nhật |
| `NotificationReviewOrderRemindJob` | Đơn hoàn tất chưa đánh giá | Nhắc renter rating |
| `NotificationRentalCarDiscount_InWeekJob` | Weekly | Gợi ý discount tuần |
| `NotificationRentalCarUpdateInfoRemindJob` | Xe thiếu thông tin | Nhắc owner bổ sung |
| `NotifyOrderBeforeDepartureEngine` | Gần giờ nhận xe | Reminder cho cả 2 bên |
| `NotificationOrderUnhidePhoneNumberEngine` | Thời điểm cho phép | Hiện SĐT trong đơn |

---

*Xem thêm: [background-engines.md](../01_ARCHITECTURE/background-engines.md) | [ADR-0008](../07_ADR/0008-signalr-realtime-notification.md)*
