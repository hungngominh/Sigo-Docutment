# Notification System — Architecture chi tiết

## Mục lục
- [Tổng quan Architecture](#tổng-quan-architecture)
- [NotificationService — Hosted Service](#notificationservice--hosted-service)
- [FCM — Firebase Cloud Messaging](#fcm--firebase-cloud-messaging)
- [SignalR — Real-time Hub](#signalr--real-time-hub)
- [Notification Templates](#notification-templates)
- [Notification Scheduling — ProactiveNotification](#notification-scheduling--proactivenotification)
- [In-App Notification Storage](#in-app-notification-storage)
- [Processing Pipeline](#processing-pipeline)

---

## Tổng quan Architecture

```
[Business Logic]
     │
     ▼
[PushNotificationHelper]
     │ Enqueue
     ▼
[System.Threading.Channels]  ←── Bounded, capacity 1000, DropOldest
     │
     ▼
[Worker Pool] ×3 workers
     │
     ├──► [FCM] Firebase Cloud Messaging (Android/iOS push)
     ├──► [SignalR] NotificationHub (Web real-time)
     └──► [DB] NotificationMessage + PushNotificationHistory
```

### Channels cần gửi notification

| Channel | Technology | Use case |
|---------|-----------|----------|
| **FCM Push** | Firebase Cloud Messaging | Mobile app (background/foreground) |
| **SignalR** | WebSocket via ASP.NET Core SignalR | Web app (real-time) |
| **Google Chat** | Webhook | System alerts cho admin/dev team |
| **Email** | Ezy.APIService.EmailMessage | Email templates |
| **SMS** | Via AutoMappingSMS engine | Bank SMS matching |

---

## NotificationService — Hosted Service

**File:** `AllianceMiddlemanWebAPI.Service/Services/AppSystem/HostedService/NotificationService.cs`

### Configuration

```csharp
public class NotificationOptions
{
    public int ChannelCapacity { get; set; } = 1000;     // Max queued items
    public bool DropOldestOnFull { get; set; } = true;   // Drop policy
    public int WorkerCount { get; set; }                  // min(3, CPU cores)
}
```

### Implementation Pattern

```csharp
public class NotificationService : IHostedService, INotificationService
{
    private readonly Channel<NotificationItem> _channel;
    private readonly List<Task> _workers;

    public NotificationService(IOptions<NotificationOptions> options)
    {
        var opts = new BoundedChannelOptions(options.Value.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        };
        _channel = Channel.CreateBounded<NotificationItem>(opts);
    }

    // Non-blocking enqueue
    public async Task EnqueueAsync(NotificationItem item)
    {
        await _channel.Writer.WriteAsync(item);
    }

    // Start workers
    public Task StartAsync(CancellationToken ct)
    {
        int workerCount = Math.Min(3, Environment.ProcessorCount);
        for (int i = 0; i < workerCount; i++)
        {
            _workers.Add(Task.Run(() => ProcessAsync(ct)));
        }
        return Task.CompletedTask;
    }

    // Worker loop
    private async Task ProcessAsync(CancellationToken ct)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(ct))
        {
            try { await SendNotificationAsync(item); }
            catch (Exception ex) { LogError(ex); }
        }
    }

    // Graceful shutdown — drain queue
    public async Task StopAsync(CancellationToken ct)
    {
        _channel.Writer.Complete();
        await Task.WhenAll(_workers);
    }
}
```

### DI Registration

```csharp
// Startup.ConfigureServices()
services.Configure<NotificationOptions>(config);
services.AddSingleton<INotificationService, NotificationService>();
services.AddHostedService(sp => sp.GetRequiredService<INotificationService>());
```

---

## FCM — Firebase Cloud Messaging

**File:** `AllianceMiddlemanWebAPI.Service/Helper/PushNotificationHelper.cs`

### Configuration (PUSH_NOTIFICATION_FCM_SETTING)

```json
{
  "ServerAPIKey": "AAAA94Cn4FE:APA91bHHlPr-...",
  "ServerSenderId": "1063015407697",
  "AppTitle": "SIGO",
  "PathFirebaseJson": "/path/to/firebase-service-account.json",
  "PushNotificationMessageUrl": "https://fcm.googleapis.com/v1/projects/{project}/messages:send",
  "TTL": 4500,
  "Priority": "high"
}
```

### Access Token Management

```csharp
// Google OAuth2 credential for FCM v1 API
private static GoogleCredential _credential;
private static string _accessToken;
private static DateTime _tokenExpiry;

public static async Task<string> GetAccessTokenAsync()
{
    if (_accessToken == null || DateTime.UtcNow >= _tokenExpiry)
    {
        _credential = GoogleCredential.FromFile(PathFirebaseJson)
            .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
        _accessToken = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _tokenExpiry = DateTime.UtcNow.AddMinutes(50); // Refresh trước khi hết hạn
    }
    return _accessToken;
}
```

### Message Structure

```json
{
  "message": {
    "token": "{FCM_DEVICE_TOKEN}",
    "notification": {
      "title": "SIGO",
      "body": "Đơn hàng ORD-001 đã được xác nhận"
    },
    "data": {
      "action": "ORDER_DETAIL",
      "entityId": "12345",
      "entityName": "Order",
      "requestParam": "{\"OrderId\": 12345}"
    },
    "android": {
      "ttl": "4500s",
      "priority": "high",
      "notification": {
        "channel_id": "sigo_notifications",
        "sound": "default"
      }
    },
    "apns": {
      "headers": {
        "apns-priority": "10"
      },
      "payload": {
        "aps": {
          "alert": { "title": "SIGO", "body": "..." },
          "sound": "default",
          "category": "ORDER"
        }
      }
    }
  }
}
```

### FCM Config Details

| Parameter | Value | Mô tả |
|-----------|-------|-------|
| TTL | 4500 seconds (75 min) | Time-to-live cho message |
| Priority | `high` | Delivery priority |
| Android Channel | `sigo_notifications` | Android notification channel |
| Sound | `default` | Notification sound |
| Category (iOS) | Dynamic per template | APNs category |

### History Tracking

Mỗi push notification được log vào `PushNotificationHistory`:

| Field | Mô tả |
|-------|-------|
| FCMToken | Device token |
| UserId | Target user |
| Timestamp | Send time |
| IsSuccess | true/false |
| FCMResponse | Full response JSON |
| InputPayload | Request JSON |

---

## SignalR — Real-time Hub

**File:** `AllianceMiddlemanWebAPI.Service/Services/Hubs/Notification.cs`

### Hub Setup

```csharp
// Startup.Configure()
app.UseEndpoints(endpoints => {
    endpoints.MapHub<NotificationHub>("/NotificationHub");
});
```

### Authentication

```csharp
// JWT từ query string cho WebSocket
OnMessageReceived = context => {
    var accessToken = context.Request.Query["access_token"];
    var path = context.HttpContext.Request.Path;
    if (!string.IsNullOrEmpty(accessToken)
        && path.StartsWithSegments("/NotificationHub"))
    {
        context.Token = accessToken;
    }
    return Task.CompletedTask;
};
```

**Client connection:** `wss://api.sigo.vn/NotificationHub?access_token={JWT_TOKEN}`

### Security

- **SignalR Security Key:** Config `System_Security_Key_SignalR` — validate connection
- **User identification:** JWT claim `user_uniqueid`
- **App identification:** Query parameter `appid` (optional)

### Hub Methods

```csharp
public class NotificationHub : Hub
{
    // Connection lifecycle
    public override async Task OnConnectedAsync()
    {
        // Track connection: userId → connectionId mapping
        // Store in NotificationHubHelper.NotificationHub_Dic
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        // Remove connection tracking
    }

    // Server → Client methods
    public async Task SendToUser(string userId, object message)
    {
        // Send to specific user's all connections
    }

    public async Task SendToGroup(string groupName, object message);
    public async Task SendToOthersInGroup(string groupName, object message);

    // Group management
    public async Task JoinGroup(string groupName);
    public async Task LeaveGroup(string groupName);
}
```

### Connection Tracking

```csharp
public static class NotificationHubHelper
{
    // Dictionary<userId, List<NotificationConnection>>
    public static Dictionary<string, List<NotificationConnection>> NotificationHub_Dic;

    // Dictionary<appId, List<NotificationConnection>>
    public static Dictionary<string, List<NotificationConnection>> NotificationHub_App_Dic;

    // Thread-safe via lock objects
}

public class NotificationConnection
{
    public string ConnectionId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public DateTime ConnectedAt { get; set; }
}
```

### Client-side Events

| Event Name | Payload | Mô tả |
|------------|---------|-------|
| `ReceiveMessage` | `NotificationSignalRBase` | Primary notification handler |

```csharp
public class NotificationSignalRBase
{
    public string Type { get; set; }       // Notification type
    public string Msg { get; set; }        // Message content
    public string StatusCode { get; set; } // Status indicator
}

public class NotificationSignalR_Count : NotificationSignalRBase
{
    public int NotifyCount { get; set; }   // Unread count
}
```

---

## Notification Templates

**File:** `AllianceMiddlemanWebAPI.DataShared/Common/NotificationTemplateCodes.cs`

### Template Codes — Đầy đủ

#### Order Lifecycle — User (Renter)

| Code | Trigger | Mô tả |
|------|---------|-------|
| `RENT_CAR_USER_REQUEST_RENT_SUCCESSFUL_NOTIFY_TO_USER` | Booking created | Yêu cầu thuê thành công |
| `RENT_CAR_OWNER_ACCEPTED_REQUEST_NOTIFY_TO_USER` | Owner confirm | Chủ xe đã chấp nhận |
| `RENT_CAR_DEPOSIT_SUCCESSFUL` | Deposit done | Cọc thành công |
| `RENT_CAR_ABOUT_TO_DEPART_TO_USER` | 180 min before FromDate | Sắp đến giờ nhận xe |
| `RENT_CAR_BEGIN_TRIP_NOTIFY_TO_USER` | Trip begin | Bắt đầu chuyến |
| `RENT_CAR_ORDER_COMPLETED_NOTIFY_TO_USER` | Trip end | Hoàn thành chuyến |
| `RENT_CAR_ORDER_REVIEW_NOTIFY_TO_USER` | After completion | Nhắc đánh giá |
| `RENT_CAR_ORDER_UNHIDE_PHONENUMBER_TO_USER` | Before pickup | Hiện SĐT chủ xe |
| `RENT_CAR_USER_CANCEL_REQUEST_NOTIFY_TO_USER` | Renter cancel | Xác nhận huỷ đơn |

#### Order Lifecycle — Owner

| Code | Trigger | Mô tả |
|------|---------|-------|
| `RENT_CAR_USER_REQUEST_RENT_SUCCESSFUL_NOTIFY_TO_OWNER` | Booking created | Có yêu cầu thuê mới |
| `RENT_CAR_OWNER_CANCEL_REQUEST_NOTIFY_TO_OWNER` | Owner cancel | Xác nhận owner huỷ |
| `RENT_CAR_DEPOSIT_RECEIVED` | Deposit done | Đã nhận tiền cọc |
| `RENT_CAR_AUTO_ORDER_COMPLETED_NOTIFY_TO_OWNER` | Auto-complete | Đơn tự động hoàn thành |
| `RENT_CAR_ORDER_UNHIDE_PHONENUMBER_TO_OWNER` | Before pickup | Hiện SĐT người thuê |

#### System Cancel

| Code | Trigger | Mô tả |
|------|---------|-------|
| `RENT_CAR_SYSTEM_CANCEL_OWNER_CONFIRM_TIMEOUT_NOTIFY_TO_USER` | Owner timeout | Chủ xe không confirm kịp |
| `RENT_CAR_SYSTEM_CANCEL_DEPOSIT_TIMEOUT_NOTIFY_TO_USER` | Deposit timeout | Hết hạn cọc |

#### Admin

| Code | Trigger | Mô tả |
|------|---------|-------|
| `RENT_CAR_WAITING_APPROVE_ADD_NEW_ADMIN` | Vehicle submitted | Xe chờ duyệt |
| `RENT_CAR_AUTO_ORDER_COMPLETED_NOTIFY_TO_ADMIN` | Auto-complete | Thông báo admin |

#### Promotional / Proactive

| Code | Trigger | Mô tả |
|------|---------|-------|
| `RENT_CAR_DISCOUNT_INWEEEK` | Weekly job | Gợi ý giảm giá tuần |
| `RENT_CAR_HIGH_VIEW` | High views, no booking | Xe nhiều view nhưng ít booking |
| `RENT_CAR_UPDATE_INFO_REMIND` | Periodic | Nhắc cập nhật thông tin xe |
| `RENT_CAR_ORDER_REVIEW_REMIND` | Daily | Nhắc đánh giá đơn |

#### Cancel Suggestions

| Code | Trigger | Mô tả |
|------|---------|-------|
| `RENT_CAR_SUGGESTION_AFTER_OWNER_CANCEL` | Owner cancel | Gợi ý xe mới cho renter |

### Template Entity

```csharp
public partial class NotificationTemplate
{
    public long Id { get; set; }
    public string Code { get; set; }         // Template code (unique)
    public string Title { get; set; }        // Notification title
    public string Body { get; set; }         // Body with {placeholders}
    public string Type { get; set; }         // Notification type
    public string ActionCode { get; set; }   // Client action mapping
    public string EntityName { get; set; }   // Related entity
    public bool IsPushNotification { get; set; }  // Send via FCM
    public bool IsSignalR { get; set; }      // Send via SignalR
    // + standard audit fields
}
```

**Caching:** `CachedDataManagement_NotificationTemplate` — load tất cả templates vào memory.

---

## Notification Scheduling — ProactiveNotification

### Schedule Types

| Type | Code | Mô tả |
|------|------|-------|
| 0 | Exactly (One-time) | Gửi 1 lần tại thời điểm cụ thể |
| 1 | Daily | Gửi hàng ngày |
| 2 | Weekly | Gửi hàng tuần |
| 3 | Monthly | Gửi hàng tháng |
| 4 | Yearly | Gửi hàng năm |

### ProactiveNotification Entity

| Field | Type | Mô tả |
|-------|------|-------|
| `ScheduleType` | int | 0-4 (xem bảng trên) |
| `FromDate` | DateTime? | Ngày bắt đầu lịch |
| `ToDate` | DateTime? | Ngày kết thúc lịch |
| `Timezone` | string | IANA timezone (e.g., "Asia/Ho_Chi_Minh") |
| `SendTimes` | string | CSV of HH:MM (e.g., "08:00,12:00,18:00") |
| `Weekdays` | string | JSON array [0-6] cho Weekly (0=Sun) |
| `CustomDates` | string | JSON array cho Monthly (e.g., [1, 15, -1] where -1=last day) |
| `LastSentAt` | DateTime? | Thời điểm gửi lần cuối |
| `TotalSentCount` | int | Tổng số lần đã gửi |
| `NextSendAt` | DateTime? | Thời điểm gửi tiếp theo (calculated) |
| `Status` | string | "SENT", "ERROR" |
| `Subject` | string | Display name |
| `IsDisable` | bool | Tạm dừng |
| `IsDeleted` | bool | Soft delete |

### Duplicate Prevention Cache

```csharp
// Tránh gửi trùng trong cùng period
ThreadSafeList<long> Notification_DontNeedRun_Today;
ThreadSafeList<long> Notification_DontNeedRun_ThisMonth;
ThreadSafeList<long> Notification_DontNeedRun_ThisYear;
```

### NotificationBatchJobEngine Logic

```
FOR EACH proactiveNotification WHERE IsDisable = false:
    1. Calculate NextSendAt based on ScheduleType
    2. Check if already sent (via DontNeedRun caches)
    3. Check timezone-aware SendTimes
    4. IF shouldSend:
       a. Get target users from ProactiveNotificationDetail
       b. Send notification via FCM + SignalR
       c. Update LastSentAt, TotalSentCount
       d. Add to DontNeedRun cache
```

---

## In-App Notification Storage

### NotificationMessage Entity

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | long | PK |
| `UserId` | long | Target user |
| `Title` | string | Notification title |
| `Body` | string | Body text |
| `Type` | string | Notification type |
| `EntityId` | string | Related entity ID |
| `EntityName` | string | Related entity type |
| `IsRead` | bool | Read status |
| `ReadAt` | DateTime? | Read timestamp |
| `ActionCode` | string | Client action |
| `RequestParam` | string | JSON parameters |
| `TemplateCode` | string | Source template |

**Index:** `idx_notification_entity_type ON (EntityId, Type)`

### SP: `sp_GetUnreadLatestNotify_Json`

```sql
-- Lấy notifications chưa đọc mới nhất
-- Parameters: {"UserId": long, "Top": int}
-- Returns: NotificationInfo[] sorted by Log_CreatedDate DESC
```

---

## Processing Pipeline

```
1. Trigger: Business event (order confirm, deposit done, etc.)
     │
2. Service gọi PushNotificationHelper
     │
3. PushNotificationHelper.PushNotify_SQLSP()
     │ Execute: sp_GetNotifyMessage4Push_Json
     ▼
4. Parse JSON → PushNotifyItem[]
     │
5. FOR EACH item:
     │
     ├── 5a. FCM Push: SendPushNotification_REST_HTTP_V1Async()
     │   → POST to FCM v1 API with Bearer token
     │   → Log result to PushNotificationHistory
     │
     ├── 5b. SignalR: NotificationHub.SendToUser()
     │   → Lookup connections in NotificationHubHelper.NotificationHub_Dic
     │   → Send to all active connections
     │
     └── 5c. DB: Insert NotificationMessage
         → In-app notification storage
```

---

*Xem thêm: [background-engines.md](./background-engines.md) | [core-notification.md](../02_MODULES/core-notification.md)*
