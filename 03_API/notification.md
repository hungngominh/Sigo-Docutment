# API: Notification

> **Controllers:** `NotificationController`, `NotificationSenderController`, `NotificationHubConnectionController`, `NotificationSupportSessionController`, `NotificationSupportMessageController`, `NotificationSupportTemplateController`, `NotificationSupportActionController`, `ProactiveNotificationController`, `ProactiveNotificationDetailController`
> **Base:** `/api/v1/Notification*`, `/api/v1/NotificationSender`, `/api/v1/ProactiveNotification*`

## Mục lục
- [Tổng quan](#tổng-quan)
- [Luồng gọi API](#luồng-gọi-api)
- [Danh sách thông báo](#post-apiv1notificationlist)
- [Đếm thông báo mới](#post-apiv1notificationtotalnew)
- [Đánh dấu đã đọc](#post-apiv1notificationread)
- [Đánh dấu tất cả đã đọc](#post-apiv1notificationreadall)
- [Options thông báo](#post-apiv1notificationoptions)
- [Gửi thông báo (nội bộ)](#post-apiv1notificationsendersendmessage)
- [Support Chat — Sessions](#post-apiv1notificationsupportsessionlist)
- [Support Chat — Messages](#post-apiv1notificationsupportmessagelist)
- [Support Chat — Templates](#post-apiv1notificationsupporttemplatelist)
- [Proactive Notification](#post-apiv1proactivenotificationlist)
- [SignalR Hub](#signalr-hub-notificationhub)
- [Admin — Hub Connections](#post-apiv1notificationhubconnectionlist)

---

## Tổng quan

Module Notification gồm 3 phần chính:

| Phần | Mô tả | Controller |
|------|-------|-----------|
| **Push Notification** | Thông báo đẩy cho user (đặt xe, xác nhận, thanh toán...) | `NotificationController` |
| **Support Chat** | Chat hỗ trợ giữa user và CS/admin | `NotificationSupportSession/Message` |
| **Proactive Notification** | Gửi thông báo hàng loạt có lịch (marketing, nhắc nhở) | `ProactiveNotificationController` |

Tất cả notification được đẩy real-time qua **SignalR Hub** (`/notificationHub`).

---

## Luồng gọi API

### Luồng 1: Hiển thị bell icon + danh sách thông báo (Mobile App)

```
Bước 1: POST /api/v1/Notification/TotalNew
        → Mục đích: Lấy số thông báo chưa đọc để hiển thị badge trên bell icon
        → Output: NotifyCount (số nguyên)

Bước 2: POST /api/v1/Notification/List
        → Mục đích: Lấy danh sách thông báo khi user mở màn hình
        → Input: PageIndex=1, PageSize=50

Bước 3: POST /api/v1/Notification/Read/{id}   [khi user nhấn vào 1 thông báo]
        → Mục đích: Đánh dấu đã đọc + lấy data để điều hướng

Bước 4 (tuỳ chọn): POST /api/v1/Notification/ReadAll
        → Mục đích: Đánh dấu tất cả đã đọc
```

### Luồng 2: Kết nối SignalR để nhận thông báo real-time

```
Bước 1: Kết nối WebSocket
        ws://{host}/notificationHub?access_token={jwt}
        → Server gửi event "ReceiveMessage" với type "SignalR_Connected" khi thành công

Bước 2: Lắng nghe event "ReceiveMessage"
        → Khi nhận: kiểm tra type
          - type = "SignalR_NotifyCount" → cập nhật badge số thông báo
          - type khác → hiển thị popup/toast thông báo

Bước 3: Khi app vào background/logout → ngắt kết nối
        → Server tự dọn connection khỏi dictionary
```

**Side effects khi nhận thông báo mới qua SignalR:**
- Badge trên bell icon tự cập nhật (không cần gọi lại `TotalNew`)
- App điều hướng khi user nhấn vào notification dựa theo `ActionCode` trong data

### Luồng 3: User tạo support chat session

```
Bước 1: POST /api/v1/NotificationSupportSession/CreateSupportCenter
        → Mục đích: Tạo session hỗ trợ từ một đơn hàng
        → Input: OrderNumber + thông tin đơn hàng
        → Output: SupportSessionId

Bước 2: POST /api/v1/NotificationSupportMessage/List
        → Mục đích: Lấy lịch sử tin nhắn của session
        → Input: SupportSessionId

Bước 3: [Gửi tin nhắn mới qua API hoặc SignalR]
        → Tin nhắn mới được push real-time qua SignalR đến CS

Bước 4: POST /api/v1/NotificationSupportSession/GetDetail
        → Mục đích: Refresh trạng thái session (đã xử lý / đang chờ)
```

### Luồng 4: Admin gửi proactive notification hàng loạt

```
Bước 1: POST /api/v1/ProactiveNotification/List
        → Mục đích: Xem danh sách notifications đã tạo

Bước 2: POST /api/v1/ProactiveNotificationDetail/AddMutiFromRole  [hoặc AddMutiOwnerHasCarActive]
        → Mục đích: Thêm danh sách người nhận theo role/điều kiện
        → Output: Danh sách user được thêm vào

Bước 3: POST /api/v1/ProactiveNotification/PreviewPushNotification
        → Mục đích: Xem trước nội dung + đếm số người nhận
        → Input: ProactiveNotificationId

Bước 4: POST /api/v1/ProactiveNotification/PushNotification
        → Mục đích: Gửi thật đến tất cả người nhận
        → Output: Trạng thái gửi

Bước 5: POST /api/v1/ProactiveNotification/CountSentNotify
        → Mục đích: Kiểm tra kết quả — bao nhiêu đã nhận
```

---

## `POST /api/v1/Notification/List`

**Mô tả:** Lấy danh sách thông báo của user hiện tại, có phân trang.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 2 trong [Luồng 1](#luồng-1-hiển-thị-bell-icon--danh-sách-thông-báo-mobile-app)

**Thống kê:** 2,803 lượt/30 ngày | avg 42ms ✅

### Request Body — `NotifyFlyParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 50,
  "IsRead": null
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| PageIndex | int | Không | Trang (default: 1) |
| PageSize | int | Không | Số item/trang (default: 50) |
| IsRead | bool? | Không | `null` = tất cả, `false` = chưa đọc, `true` = đã đọc |

### Response — `EzyResultObject<EzyDataSourceResult<NotificationFlyModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 25,
    "Data": [
      {
        "NotificationId": "notif-guid",
        "Title": "Chủ xe đã xác nhận đơn của bạn",
        "Content": "Đơn ORD-20260301-001 đã được xác nhận",
        "ActionCode": "ORDER_CONFIRMED",
        "ActionData": "ORD-20260301-001",
        "IsRead": false,
        "CreatedAt": "2026-03-01T10:00:00Z",
        "IconUrl": "https://..."
      }
    ]
  }
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| ActionCode | string | Mã hành động để điều hướng (VD: `ORDER_CONFIRMED`, `ORDER_BEGIN`, `PAYMENT_SUCCESS`) |
| ActionData | string | Dữ liệu kèm theo (thường là `OrderNumber` hoặc ID) |
| IsRead | bool | Trạng thái đọc |

---

## `POST /api/v1/Notification/TotalNew`

**Mô tả:** Đếm số thông báo chưa đọc — dùng để hiển thị badge trên icon.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 1 trong [Luồng 1](#luồng-1-hiển-thị-bell-icon--danh-sách-thông-báo-mobile-app)

**Thống kê:** 1,472 lượt/30 ngày | avg 80ms ✅

### Request Body — `NotifyTotalParamModel`
```json
{
  "Actions": []
}
```

### Response — `EzyResultObject<NotificationTotalModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "NotifyCount": 5
  }
}
```

---

## `POST /api/v1/Notification/Read/{id}`

**Mô tả:** Đánh dấu một thông báo đã đọc theo ID.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 3 trong [Luồng 1](#luồng-1-hiển-thị-bell-icon--danh-sách-thông-báo-mobile-app)

**Route param:** `{id}` — NotificationId

### Response — `EzyResultObject<NotificationFlyModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "NotificationId": "notif-guid",
    "IsRead": true,
    "ActionCode": "ORDER_CONFIRMED",
    "ActionData": "ORD-20260301-001"
  }
}
```

> **Lưu ý:** Response trả về `ActionCode` + `ActionData` để client điều hướng ngay sau khi đánh dấu đã đọc.

---

## `POST /api/v1/Notification/Read`

**Mô tả:** Đánh dấu đã đọc qua model (thay thế cho `/Read/{id}`).

**Phân quyền:** `[Authorize]`

### Request Body — `NotificationActionBaseModel`
```json
{
  "NotificationId": "notif-guid"
}
```

---

## `POST /api/v1/Notification/ReadAll`

**Mô tả:** Đánh dấu tất cả thông báo đã đọc (async).

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 4 (tuỳ chọn) trong [Luồng 1](#luồng-1-hiển-thị-bell-icon--danh-sách-thông-báo-mobile-app)

### Request Body — `NotifyFlyParamModel`
```json
{}
```

### Response — `EzyResultObject<object>`
```json
{
  "StatusCode": 1,
  "Msg": "Đã đánh dấu tất cả là đã đọc"
}
```

> **Alias:** `POST /api/v1/Notification/SetAllRead` — tương tự, dùng cùng logic.

---

## `POST /api/v1/Notification/Options`

**Mô tả:** Lấy các tùy chọn UI cho màn hình thông báo (filter types, action codes...).

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<NotifcationFlyOptionModel>`

---

## `POST /api/v1/Notification/SendTestNotify`

**Mô tả:** Gửi thông báo test (dùng khi debug/dev).

**Phân quyền:** `[Authorize]`

### Request Body — `NotificationMessageParamModel`
```json
{
  "UserId": "user-guid",
  "Title": "Test notification",
  "Content": "This is a test"
}
```

---

## `POST /api/v1/NotificationSender/SendMessage`

**Mô tả:** Gửi thông báo đến user qua SignalR (internal — chỉ gọi từ server-side hoặc admin).

**Phân quyền:** Không bắt buộc (nên dùng từ internal only)

**Thống kê:** 36,307 lượt/30 ngày | avg **0.48ms** ✅ (async queue — không block)

### Request Body — `NotificationSenderParamModel`
```json
{
  "id": "user-guid-hoặc-connection-id",
  "type": "SignalR_NotifyCount",
  "message": "5"
}
```

| Field | Type | Mô tả |
|-------|------|-------|
| id | string | UserId hoặc ConnectionId nhận thông báo |
| type | string | Loại message: `SignalR_NotifyCount`, `SignalR_Connected`, hoặc custom |
| message | string | Nội dung gửi (JSON string hoặc plain text) |

### Response — `EzyResultObject<NotificationSenderParamModel>`

---

## `POST /api/v1/NotificationSupportSession/List`

**Mô tả:** Lấy danh sách support chat sessions (admin/CS view).

**Phân quyền:** `[Authorize]`

### Request Body — `NotificationSupportSessionParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "Status": "Open"
}
```

---

## `POST /api/v1/NotificationSupportSession/GetDetail`

**Mô tả:** Lấy chi tiết một support session.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 4 trong [Luồng 3](#luồng-3-user-tạo-support-chat-session)

### Response — `EzyResultObject<NotificationSupportSessionModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "SessionId": "session-guid",
    "OrderNumber": "ORD-20260301-001",
    "Status": "Open",
    "CreatedAt": "2026-03-01T10:00:00Z",
    "LastMessageAt": "2026-03-01T11:00:00Z"
  }
}
```

---

## `POST /api/v1/NotificationSupportSession/CreateSupportCenter`

**Mô tả:** Tạo support session từ một đơn hàng — dùng khi user cần hỗ trợ liên quan đến đơn.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 1 trong [Luồng 3](#luồng-3-user-tạo-support-chat-session)

### Request Body — `OrderNotificationSupportSessionInfo`
```json
{
  "OrderNumber": "ORD-20260301-001"
}
```

### Response — `EzyResultObject<NotificationSupportSessionModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "SessionId": "session-guid",
    "Status": "Open"
  }
}
```

---

## `POST /api/v1/NotificationSupportMessage/List`

**Mô tả:** Lấy lịch sử tin nhắn trong một support session.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 2 trong [Luồng 3](#luồng-3-user-tạo-support-chat-session)

### Request Body — `NotificationSupportMessageParamModel`
```json
{
  "SessionId": "session-guid",
  "PageIndex": 1,
  "PageSize": 50
}
```

### Response — `EzyResultObject<EzyDataSourceResult<NotificationSupportMessageModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 10,
    "Data": [
      {
        "MessageId": "msg-guid",
        "SessionId": "session-guid",
        "SenderName": "Nguyễn Văn A",
        "Content": "Tôi cần hỗ trợ về đơn hàng",
        "SentAt": "2026-03-01T10:30:00Z",
        "IsFromCS": false
      }
    ]
  }
}
```

---

## `POST /api/v1/NotificationSupportTemplate/List`

**Mô tả:** Lấy danh sách template tin nhắn cho CS (câu trả lời mẫu nhanh).

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/NotificationSupportAction/List`

**Mô tả:** Lấy danh sách action buttons trong support chat (VD: "Chuyển đơn", "Hoàn tiền"...).

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/NotificationSupportTemplateAction/List`

**Mô tả:** Lấy danh sách mapping giữa template và action.

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/NotificationMessage4Admin/List`

**Mô tả:** Lấy danh sách tất cả notification messages (admin audit view).

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/ProactiveNotification/List`

**Mô tả:** Lấy danh sách proactive notification campaigns đã tạo.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 1 trong [Luồng 4](#luồng-4-admin-gửi-proactive-notification-hàng-loạt)

---

## `POST /api/v1/ProactiveNotification/PreviewPushNotification`

**Mô tả:** Xem trước thông báo sẽ gửi — đếm số người nhận và preview nội dung.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 3 trong [Luồng 4](#luồng-4-admin-gửi-proactive-notification-hàng-loạt)

### Request Body — `ProactiveNotificationModel`
```json
{
  "ProactiveNotificationId": "campaign-guid"
}
```

### Response — `EzyResultObject<string>`
```json
{
  "StatusCode": 1,
  "Msg": "Sẽ gửi đến 1,250 người dùng"
}
```

---

## `POST /api/v1/ProactiveNotification/PushNotification`

**Mô tả:** Gửi thật proactive notification đến toàn bộ danh sách người nhận đã cấu hình.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 4 trong [Luồng 4](#luồng-4-admin-gửi-proactive-notification-hàng-loạt)

> ⚠️ **Không thể hoàn tác** — Gọi `PreviewPushNotification` trước để xác nhận.

---

## `POST /api/v1/ProactiveNotification/CountSentNotify`

**Mô tả:** Kiểm tra số lượng notification đã gửi thành công.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 5 trong [Luồng 4](#luồng-4-admin-gửi-proactive-notification-hàng-loạt)

### Response — `EzyResultObject<ProactiveNotificationModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "TotalTarget": 1250,
    "TotalSent": 1248,
    "TotalFailed": 2
  }
}
```

---

## `POST /api/v1/ProactiveNotificationDetail/AddMutiFromRole`

**Mô tả:** Thêm người nhận vào campaign dựa theo role người dùng.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 2 trong [Luồng 4](#luồng-4-admin-gửi-proactive-notification-hàng-loạt)

### Request Body — `ProactiveNotificationDetailModel`
```json
{
  "ProactiveNotificationId": "campaign-guid",
  "RoleCode": "OWNER"
}
```

---

## `POST /api/v1/ProactiveNotificationDetail/AddMutiOwnerHasCarActive`

**Mô tả:** Thêm tất cả chủ xe đang có xe hoạt động vào campaign.

**Phân quyền:** `[Authorize]`

**Vai trò trong luồng:** Bước 2 (thay thế) trong [Luồng 4](#luồng-4-admin-gửi-proactive-notification-hàng-loạt)

---

## `POST /api/v1/ProactiveNotificationDetail/GetListStaff`

**Mô tả:** Lấy danh sách staff để thêm vào danh sách nhận notification.

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/NotificationHubConnection/List`

**Mô tả:** Lấy danh sách kết nối SignalR đang active (admin monitoring).

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<EzyDataSourceResult<NotificationHubConnectionModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 150,
    "Data": [
      {
        "ConnectionId": "conn-guid",
        "UserId": "user-guid",
        "ConnectedAt": "2026-03-01T08:00:00Z"
      }
    ]
  }
}
```

---

## SignalR Hub: NotificationHub

**Endpoint kết nối:** `/notificationHub`

**Kết nối với JWT:**
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/notificationHub?access_token=" + jwtToken)
  .withAutomaticReconnect()
  .build();
```

**Kết nối với App Key (internal services):**
```
/notificationHub?key={System_Security_Key_SignalR}&appid={appId}
```

### Server → Client Events

| Event | Payload | Mô tả |
|-------|---------|-------|
| `ReceiveMessage` | `NotificationSignalRBase` | Event chính — nhận tất cả thông báo |

**Payload types của `ReceiveMessage`:**

```json
// type = "SignalR_Connected" — xác nhận kết nối thành công
{
  "Type": "SignalR_Connected",
  "Msg": "Connected"
}

// type = "SignalR_NotifyCount" — cập nhật số thông báo chưa đọc
{
  "Type": "SignalR_NotifyCount",
  "Msg": "",
  "NotifyCount": 5
}
```

### Client → Server Methods

| Method | Params | Mô tả |
|--------|--------|-------|
| `Send` | `(name, message)` | Gửi message đến tất cả client |
| `SendMessage` | `(message)` | Gửi message đến tất cả |
| `SendToOthers` | `(name, message)` | Gửi đến tất cả trừ caller |
| `SendToConnection` | `(connectionId, name, message)` | Gửi đến connection cụ thể |
| `SendToUser` | `(userId, message)` | Gửi đến user cụ thể (tất cả connections) |
| `SendToGroup` | `(groupName, name, message)` | Gửi đến group |
| `JoinGroup` | `(groupName, name)` | Tham gia group |
| `LeaveGroup` | `(groupName, name)` | Rời group |
| `Echo` | `(name, message)` | Gửi lại chính caller |

### Lifecycle

| Event | Mô tả |
|-------|-------|
| `OnConnectedAsync` | Đăng ký connection vào dictionary theo UserId, gửi `SignalR_Connected` |
| `OnDisconnectedAsync` | Xoá connection khỏi dictionary |

---

## Ghi chú kỹ thuật

- **`NotificationSender`** được gọi từ server-side (các service khác) khi cần push notification, không phải từ mobile client.
- **Batch notification** qua `ProactiveNotification` được gửi async — không block request.
- **Support chat** dùng cả polling (`/List`) lẫn real-time (SignalR) — client nên kết hợp cả 2.
- `NotificationHub_Dic`: dictionary lưu `UserId → [ConnectionId]` — một user có thể có nhiều connection (nhiều thiết bị).
