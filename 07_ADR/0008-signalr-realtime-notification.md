# ADR-0008: Dùng SignalR cho Real-time Notification

## Status

Accepted

## Context

Sigo API cần push notifications tới mobile và web clients trong thời gian thực cho các sự kiện:
- Chủ xe nhận yêu cầu đặt xe mới
- Người thuê nhận xác nhận đặt xe
- Cập nhật trạng thái order
- Nhận tin nhắn từ support/hotline
- Thông báo thanh toán thành công

Nếu dùng polling, client phải gọi API định kỳ để check notification mới → tốn bandwidth và battery trên mobile.

## Decision Drivers

* **Real-time delivery** — user nhận notification ngay lập tức
* **Mobile battery efficiency** — không polling liên tục
* **Bidirectional** — server push tới client (không cần client request)
* **ASP.NET Core native** — tích hợp sẵn, không cần thêm server
* **JWT integration** — dùng cùng auth token
* **Fallback support** — graceful degradation nếu WebSocket không available

## Considered Options

### Option 1: SignalR (ASP.NET Core)
- **Pros:**
  - Tích hợp native với ASP.NET Core
  - Hỗ trợ WebSocket, Server-Sent Events, Long Polling (auto fallback)
  - JWT authentication tích hợp sẵn
  - Strongly-typed Hub
  - Azure SignalR Service scale-out option
  - .NET client library cho mobile (Xamarin/MAUI)
- **Cons:**
  - Cần persistent connection → resource consumption
  - Cần sticky session hoặc backplane khi scale nhiều instances

### Option 2: Firebase Cloud Messaging (FCM)
- **Pros:**
  - Không cần persistent connection
  - Push notification khi app background/closed
  - Google managed infrastructure
  - iOS (APNs) và Android đều support
- **Cons:**
  - Vendor lock-in Google
  - Không real-time khi app foreground (có delay)
  - Không bidirectional
  - Phụ thuộc internet và Google services

### Option 3: Server-Sent Events (SSE)
- **Pros:**
  - HTTP/1.1 native
  - Đơn giản hơn WebSocket
  - Auto-reconnect
- **Cons:**
  - Unidirectional (server → client only)
  - Không hỗ trợ tốt trên một số browsers
  - Không có .NET library tốt

### Option 4: WebSocket thuần
- **Pros:**
  - Full control
  - Thấp nhất về overhead
- **Cons:**
  - Phải implement từ đầu: reconnection, auth, message protocol
  - Không có fallback
  - Nhiều edge cases cần handle

## Decision

Chúng ta sẽ dùng **ASP.NET Core SignalR** cho real-time notification với Hub `/NotificationHub`.

## Rationale

1. **Native integration:** Không cần thêm server hay service, chạy cùng ASP.NET Core app
2. **Auto-fallback:** SignalR tự động chọn WebSocket → SSE → Long Polling tùy client support
3. **JWT shared:** Dùng cùng JWT token với REST API, không cần auth riêng cho WebSocket
4. **Mobile support:** SignalR client library cho iOS/Android, không cần FCM cho in-app notifications
5. **Async notification:** Kết hợp với `NotificationBatchJobEngine`, push notification đến đúng client user

## Cấu hình thực tế

```csharp
// Startup.cs
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<NotificationHub>("/NotificationHub");
});

// JWT cho SignalR — đọc token từ query string
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/NotificationHub"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

**Client connection:**
```
ws://api.sigo.vn/NotificationHub?access_token=<jwt_token>
```

## Notification flow

```
Event xảy ra (order confirmed, payment received...)
    │
    ▼
[Service Layer] → Tạo Notification record trong DB
    │
    ▼
[NotificationBatchJobEngine] → Queue notification
    │
    ▼
[PushNotificationHelper] → Gọi SignalR Hub
    │
    ▼
[NotificationHub] → Push tới client connection
    │
    ▼
[Mobile/Web Client] → Hiện notification
```

## Consequences

### Tích cực
- User nhận notification realtime không cần refresh
- `NotificationSender/SendMessage` chỉ 0.48ms (async push)
- Battery efficient trên mobile (không polling)
- Cùng authentication với REST API
- Không cần Google/Apple push notification service cho in-app

### Tiêu cực
- Persistent connections chiếm resource server
- Cần Redis backplane hoặc Azure SignalR nếu nhiều server instances
- Mobile app bị kill thì mất connection → cần FCM bổ sung cho offline notifications

### Rủi ro
- Connection drops không được detect
- **Giảm thiểu:** Client implement reconnection logic, heartbeat
- Scale-out cần backplane
- **Giảm thiểu:** Cấu hình Redis backplane: `services.AddSignalR().AddStackExchangeRedis()`
- Memory leak từ zombie connections
- **Giảm thiểu:** Implement `OnDisconnectedAsync` cleanup, connection timeout

## Related Decisions

- ADR-0001: ASP.NET Core 5.0 (SignalR tích hợp native)
- ADR-0004: JWT Auth (token qua query string cho WebSocket)
- ADR-0006: Redis (SignalR backplane cho scale-out)
- ADR-0009: Background Engines (notification delivery engine)

---

*Ngày tạo: 2021-06-01*
