# Module: TrafficTicket — Phạt nguội

> **Solution:** `Ezy.Module.TrafficTicket` | **Trạng thái:** Active

## Mục lục
- [Tổng quan](#tổng-quan)
- [Cấu trúc](#cấu-trúc)
- [Entities](#entities)
- [Services](#services)
- [API Endpoints](#api-endpoints)
- [Tích hợp bên ngoài](#tích-hợp-bên-ngoài)
- [Vấn đề hiệu năng](#vấn-đề-hiệu-năng)
- [Luồng kiểm tra phạt nguội](#luồng-kiểm-tra-phạt-nguội)

---

## Tổng quan

Module TrafficTicket cung cấp tính năng **kiểm tra phạt nguội** cho xe đăng ký trên Sigo platform. Sigo tự động kiểm tra xem xe có bị phạt nguội không trước/sau khi cho thuê để bảo vệ cả chủ xe và người thuê.

**Mục đích:**
- Kiểm tra vi phạm giao thông theo biển số xe
- Lưu lịch sử kết quả kiểm tra
- Tự động check theo lịch (batch job)
- Cảnh báo chủ xe khi phát hiện phạt nguội mới

**Đối tượng sử dụng:**
- **Chủ xe (Owner):** Kiểm tra xe của mình
- **Hệ thống:** Tự động check trước khi duyệt đơn (scheduling)
- **Admin:** Theo dõi xe có phạt nguội trong fleet

**Tích hợp bên ngoài:**
- API tra cứu phạt nguội (Cục CSGT / service bên thứ 3)
- Response time trung bình: **5,432ms** (phụ thuộc hoàn toàn vào external API)

---

## Cấu trúc

```
Ezy.Module.TrafficTicket/
├── Ezy.Module.TrafficTicket.API/
│   └── Controllers/
│       ├── TrafficTicket_VehicleController.cs          # Core check API
│       ├── TrafficTicket_Vehicle_ResultController.cs   # Kết quả kiểm tra
│       ├── TrafficTicket_Vehicle_Result_HistoryController.cs  # Lịch sử
│       ├── TrafficTicket_Request_StatusController.cs   # Trạng thái request
│       ├── TrafficTicket_Request_QueueController.cs    # Hàng chờ batch
│       ├── TrafficTicket_CHeckAttController.cs         # Check attendance/attachment
│       └── TrafficTicket_BatchJobController.cs         # Batch job management
│
├── Ezy.Module.TrafficTicket.Core/
│   └── Data/
│       ├── BusinessData/    # Entities chính
│       ├── Categories/      # Lookup tables
│       └── Stores/          # Stored procedures
│
├── Ezy.Module.TrafficTicket.Service/
│   ├── Services/            # Business logic
│   ├── Engines/             # Background workers
│   └── Helper/              # Utilities
│
├── Ezy.Module.TrafficTicket.DataShared/  # DTOs
└── Script/                               # DB migrations
```

**Tổng số C# files:** ~212

---

## Entities

### `TrafficTicket_Vehicle` — Xe cần kiểm tra

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | int | PK |
| `ID_GUID` | Guid | Business key |
| `VehicleId` | Guid | FK → Vehicle (main business) |
| `PlateNumber` | string | Biển số xe |
| `PlateType` | string | Loại biển (trắng/vàng/xanh) |
| `LastCheckDateTime` | DateTime? | Lần check gần nhất |
| `NextScheduledCheck` | DateTime? | Lịch check tiếp theo |
| `Status` | string | `Active` / `Paused` / `Removed` |
| `AutoCheckEnabled` | bool | Tự động check theo lịch |
| `IsDeleted` | bool | Soft delete |
| `Log_CreatedDate` | DateTime | |
| `Log_UpdatedDate` | DateTime | |

### `TrafficTicket_Vehicle_Result` — Kết quả kiểm tra

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | int | PK |
| `ID_GUID` | Guid | Business key |
| `VehicleGuid` | Guid | FK → TrafficTicket_Vehicle |
| `CheckDateTime` | DateTime | Thời điểm kiểm tra |
| `HasViolations` | bool | Có phạt nguội không |
| `ViolationCount` | int | Số lần vi phạm |
| `ViolationDetails` | string (JSON) | Chi tiết vi phạm (ngày, địa điểm, lỗi) |
| `RawResponse` | string | Response thô từ external API |
| `ExternalRefId` | string | Reference ID từ external system |
| `CheckedBy` | string | `System` hoặc UserGuid |
| `Source` | string | `Manual` / `Scheduled` / `PreOrder` |

### `TrafficTicket_Vehicle_Result_History` — Lịch sử thay đổi kết quả

Lưu lịch sử mỗi lần kết quả thay đổi (phạt nguội mới xuất hiện hoặc được xử lý).

### `TrafficTicket_Request_Queue` — Hàng chờ batch check

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | int | PK |
| `VehicleGuid` | Guid | FK → Vehicle |
| `Priority` | int | Ưu tiên xử lý (thấp = ưu tiên cao hơn) |
| `Status` | string | `Pending` / `Processing` / `Done` / `Failed` |
| `RetryCount` | int | Số lần thử lại |
| `CreatedAt` | DateTime | |
| `ProcessedAt` | DateTime? | |

### `TrafficTicket_Request_Status` — Trạng thái request

Tracking trạng thái của các request kiểm tra đang in-flight.

---

## Services

| Service | Mô tả |
|---------|-------|
| `ITrafficTicket_VehicleService` | Quản lý xe cần check, trigger check manual/auto |
| `ITrafficTicket_ResultService` | Lưu và truy vấn kết quả kiểm tra |
| `ITrafficTicket_QueueService` | Quản lý hàng chờ batch processing |
| `ITrafficTicket_ExternalAPIService` | Wrapper gọi external traffic police API |
| `ITrafficTicket_BatchJob` | Interface batch job (từ Identity module) |

---

## API Endpoints

Base URL: `/api/v1/`

### Kiểm tra phạt nguội

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Traffic_Vehicle/List` | Danh sách xe đã đăng ký kiểm tra | Required |
| POST | `/Traffic_Vehicle/CheckTrafficTicketNowAsync` | Kiểm tra ngay lập tức (async) | Required |
| POST | `/Traffic_Vehicle/CheckTrafficTicket` | Kiểm tra phạt nguội theo biển số | **Public** |
| POST | `/Traffic_Vehicle/ScheduleTrafficTicketForFinished` | Đặt lịch check khi hoàn tất | Required |

> **Lưu ý:** `CheckTrafficTicket` là `[AllowAnonymous]` cho phép user kiểm tra biển số bất kỳ.
> **Performance:** Avg 5,432ms — xem mục [Vấn đề hiệu năng](#vấn-đề-hiệu-năng).

### Kết quả & Lịch sử

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Traffic_Vehicle_Result/List` | Danh sách kết quả kiểm tra | Required |
| POST | `/Traffic_Vehicle_Result_History/List` | Lịch sử thay đổi kết quả | Required |

### Quản lý Queue & Status

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Traffic_Request_Queue/List` | Hàng chờ batch check | Required |
| POST | `/Traffic_Request_Status/List` | Trạng thái các request | Required |

### Batch Jobs

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Traffic_BatchJob/Run` | Chạy batch check ngay | Admin |
| POST | `/Traffic_BatchJob/List` | Lịch sử batch runs | Admin |

---

## Tích hợp bên ngoài

| Hệ thống | Mục đích | Timeout | Ghi chú |
|----------|---------|---------|---------|
| **Traffic Police API** (Cục CSGT / 3rd party) | Tra cứu vi phạm theo biển số | ~30s | Không ổn định, response time cao |

### External API Call Pattern

```csharp
// TrafficTicket_ExternalAPIService
public async Task<TrafficTicketResponse> CheckAsync(string plateNumber)
{
    var response = await _httpClient.PostAsync(
        _options.ExternalApiUrl,
        content,
        cancellationToken: cts.Token   // ← cần timeout
    );
    // Parse và map về TrafficTicket_Vehicle_Result
}
```

**Cấu hình trong appsettings:**
```json
{
  "TrafficTicket": {
    "ExternalApiUrl": "https://...",
    "ApiKey": "...",
    "TimeoutSeconds": 30
  }
}
```

---

## Vấn đề hiệu năng

> **Trạng thái: CRITICAL** — Cần xử lý trước khi scale

| Chỉ số | Giá trị |
|--------|---------|
| Avg response time | **5,432 ms** |
| Max response time | **34,447 ms** |
| Root cause | External traffic police API |

### Nguyên nhân

API phụ thuộc hoàn toàn vào external service của Cục CSGT hoặc bên thứ 3. Khi external API chậm hoặc không ổn định:
- Request bị block thread của ASP.NET Core
- Timeout không được handle → request treo
- Không có circuit breaker → cascade failure

### Giải pháp đề xuất

```
[ ] 1. Thêm timeout cứng (5-10 giây) cho external API call
[ ] 2. Implement Circuit Breaker (Polly)
       → Sau N failures liên tiếp → open circuit 30s
[ ] 3. Cache kết quả 24 giờ
       → Cùng biển số không check lại trong 24h
[ ] 4. Async queue pattern
       → User request → enqueue → return task_id
       → Background engine check và notify khi có kết quả
[ ] 5. Thêm index trên (PlateNumber, CheckDateTime)
[ ] 6. Giám sát external API SLA, có fallback nếu down
```

**Priority:** Implement item 1 và 3 trước (quick wins), sau đó item 4 (async queue).

Xem chi tiết tại: [../05_PERFORMANCE/db-optimization-notes.md](../05_PERFORMANCE/db-optimization-notes.md)

---

## Luồng kiểm tra phạt nguội

### Manual Check (User request)

```
[User / Admin]                    [TrafficTicket API]           [External API]
      │                                   │                          │
      ├─ POST /CheckTrafficTicket ────────►│                          │
      │   { plateNumber: "51A-12345" }     │                          │
      │                                   │ Check cache (24h)         │
      │                                   │                          │
      │                                   ├─ Call External API ──────►│
      │                                   │                          │ ~5s
      │                                   │◄─ Violation data ─────────│
      │                                   │ Save TrafficTicket_Result │
      │◄─ Result ─────────────────────────│                          │
```

### Auto Check (Scheduled / Pre-order)

```
[Batch Job Engine]          [Queue]           [External API]        [DB]
      │                        │                    │                 │
      ├─ Get pending queue ────►│                   │                 │
      │◄─ [VehicleGuid list] ───│                   │                 │
      │                                             │                 │
      │──── For each vehicle ──────────────────────►│                 │
      │◄─── Result ────────────────────────────────│                 │
      │                                                               │
      ├──── Save Result ──────────────────────────────────────────────►│
      │                                                               │
      │  If HasViolations == true:
      │──── Push Notification → Owner
```

---

*Xem thêm: [db-optimization-notes.md](../05_PERFORMANCE/db-optimization-notes.md) | [background-engines.md](../01_ARCHITECTURE/background-engines.md)*
