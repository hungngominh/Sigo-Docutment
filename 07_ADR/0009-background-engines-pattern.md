# ADR-0009: Pattern Background Engines (IHostedService) cho xử lý bất đồng bộ

## Status

Accepted

## Context

Sigo API có nhiều tác vụ cần chạy nền, không nên block API request:
- Auto-approve payment sau thời gian chờ
- Nén ảnh sau khi upload
- Gửi notification theo batch
- Đồng bộ dữ liệu với MB Bank
- Xử lý withdrawal requests
- Auto-complete orders hết hạn
- Gửi nhắc nhở định kỳ
- Tính toán cancel ratio
- Cleanup dữ liệu hết hạn

Nếu thực hiện synchronous trong request cycle sẽ làm tăng response time và rủi ro timeout.

## Decision Drivers

* **Non-blocking API** — request trả về nhanh, processing xảy ra sau
* **Reliability** — tác vụ quan trọng (payment, withdrawal) không được bỏ sót
* **Scheduling** — một số task chạy theo lịch (daily, weekly)
* **ASP.NET Core native** — không cần external task scheduler
* **Resource isolation** — background processing không ảnh hưởng request handling

## Considered Options

### Option 1: IHostedService / BackgroundService (ASP.NET Core)
- **Pros:**
  - Native với ASP.NET Core, không cần library thêm
  - Chạy trong cùng process, share DI container
  - Lifecycle managed bởi host (start/stop với app)
  - Có thể dùng scoped services với `IServiceScopeFactory`
  - 27 engines hoạt động song song
- **Cons:**
  - Chạy trong cùng process → lỗi engine có thể ảnh hưởng app
  - Không có built-in retry với backoff
  - Khó monitor từng engine riêng lẻ

### Option 2: Hangfire
- **Pros:**
  - Dashboard UI cho monitoring
  - Retry tự động
  - Cron scheduling
  - Job persistence
- **Cons:**
  - Thêm dependency và database cho job storage
  - Overhead cho simple background tasks
  - License phí cho một số features

### Option 3: Quartz.NET
- **Pros:**
  - Cron scheduling mạnh mẽ
  - Clustered scheduling
- **Cons:**
  - Phức tạp hơn IHostedService cho tasks đơn giản
  - Thêm configuration overhead
  - Overkill cho use case hiện tại

### Option 4: Separate Worker Service / Microservice
- **Pros:**
  - Hoàn toàn tách biệt, không ảnh hưởng API
  - Có thể scale riêng
- **Cons:**
  - Phức tạp deployment
  - Cần inter-service communication
  - Overhead cho use case hiện tại

## Decision

Chúng ta sẽ dùng **ASP.NET Core IHostedService / BackgroundService** pattern, implement mỗi background task là một Engine class riêng biệt.

## Rationale

1. **Simplicity:** Native với ASP.NET Core, không thêm dependency
2. **DI integration:** Engines có thể inject các services thông qua `IServiceScopeFactory`
3. **Co-location:** Chạy cùng process với API, share configuration và logging
4. **27 engines hiện tại:** Pattern này scale tốt với nhiều engines song song
5. **Flexible scheduling:** Mỗi engine có thể implement scheduling riêng (Timer, CancellationToken)

## Danh sách Engines hiện tại

| Engine | Tần suất | Mô tả |
|--------|---------|-------|
| `AutoApprovedOrder_PaymentEngine` | Liên tục | Tự động duyệt thanh toán |
| `AutoCompleteOrderEngine` | Liên tục | Tự động hoàn tất order quá hạn |
| `AutoWithdrawEngine` | Liên tục | Xử lý yêu cầu rút tiền |
| `AutoUpdateWithdrawStatus_MBBankEngine` | Periodic | Cập nhật trạng thái rút từ MB Bank |
| `AutoCompressServiceItemImageEngine` | Liên tục | Nén ảnh sau upload |
| `DeleteDraftFileEngine` | Daily | Xóa file draft |
| `NotificationBatchJobEngine` | Liên tục | Gửi notification theo batch |
| `ProactiveNotificationHasNewUser` | Daily | Alert user mới |
| `NotificationRentalCarDiscount_InWeekJob` | Weekly | Thông báo discount |
| `NotificationRentalCarHighViewsJob` | Periodic | Alert xe có nhiều view |
| `NotificationRentalCarUpcomingAvailabilityJob` | Periodic | Alert lịch trống |
| `NotificationRentalCarUpdateInfoRemindJob` | Periodic | Nhắc cập nhật thông tin |
| `NotificationReviewOrderRemindJob` | Periodic | Nhắc đánh giá |
| `AutoCreateQuickOrderEngine` | Liên tục | Tạo quick orders tự động |
| `AutoExportBillEngine` | Periodic | Export hóa đơn |
| `CalcCancelOrderRatioJob` | Daily | Tính tỷ lệ hủy |
| `AutoMappingSMS_CompanyBankAccountActivityEngine` | Liên tục | Mapping SMS ngân hàng |
| `MBBank_CompareReportDataJob` | Daily | Đối soát MB Bank |
| `AutoUpdateUser_Calculating_OrderStatusEngine` | Liên tục | Cập nhật user metrics |
| `DeleteDataAfterAWeekJob` | Weekly | Retention cleanup |
| `CancelBookingEngine` | Liên tục | Xử lý hủy booking |
| `AutoRatingOrderJob` | Periodic | Tự động rating order |
| `FirstRunHostedService` | Startup only | Khởi tạo dữ liệu lần đầu |

## Pattern thực tế

```csharp
public class AutoApprovedOrder_PaymentEngine : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoApprovedOrder_PaymentEngine> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<IOrderPaymentService>();

                await service.ProcessPendingPaymentsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Engine error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

## Consequences

### Tích cực
- API response time không bị ảnh hưởng bởi background processing
- 27 engines chạy song song, fully async
- `NotificationSender/SendMessage` chỉ 0.48ms — queue và return, engine xử lý sau
- Startup/shutdown lifecycle managed tự động bởi ASP.NET Core host
- Mỗi engine isolated trong scope → không leak DbContext

### Tiêu cực
- Chạy trong cùng process — crash engine có thể ảnh hưởng stability
- Khó monitor từng engine mà không có APM tool
- Không có retry với backoff tự động
- Engine lỗi âm thầm nếu không có alerting

### Rủi ro
- Engine hung (deadlock, infinite loop) chiếm thread pool
- **Giảm thiểu:** Timeout per operation, CancellationToken, health checks
- Quá nhiều engines → resource contention
- **Giảm thiểu:** Rate limiting, delay giữa iterations, monitor CPU/memory
- DbContext scope leak
- **Giảm thiểu:** Luôn dùng `IServiceScopeFactory.CreateScope()` trong engine

## Related Decisions

- ADR-0002: Layered Architecture (engines nằm trong Service layer)
- ADR-0007: Kafka (một số engines consume Kafka messages)
- ADR-0008: SignalR (NotificationBatchJobEngine push tới SignalR)

---

*Ngày tạo: 2021-06-01*
