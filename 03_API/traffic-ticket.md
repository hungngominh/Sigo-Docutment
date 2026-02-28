# API: Traffic Ticket (Phạt nguội xe)

> **Module:** `Ezy.Module.TrafficTicket`
> **Controllers:** `TrafficTicket_VehicleController`, `TrafficTicket_CHeckAttController`, `TrafficTicket_Request_QueueController`, `TrafficTicket_Request_StatusController`, `TrafficTicket_Vehicle_ResultController`, `TrafficTicket_Vehicle_Result_HistoryController`, `TrafficTicket_BatchJobController`
> **Base:** `/api/v1/Traffic*` và `/api/v1/TrafficTicket*`

## Mục lục
- [Tổng quan](#tổng-quan)
- [Tra cứu phạt nguội — Public](#post-apiv1traffic_vehiclechecktrafficticket)
- [Tra cứu phạt nguội — Nội bộ](#post-apiv1traffic_vehiclechecktrafficticketnowasyncc)
- [Danh sách xe theo dõi](#post-apiv1traffic_vehiclelist)
- [Lên lịch tra cứu](#post-apiv1traffic_vehiclescheduletrafficticketforfinished)
- [Điểm kiểm tra (Check At)](#post-apiv1traffic_checkatlist)
- [Hàng đợi yêu cầu](#post-apiv1trafficticket_request_queuelist)
- [Trạng thái yêu cầu](#post-apiv1trafficticket_request_statuslist)
- [Kết quả tra cứu](#post-apiv1trafficticket_vehicle_resultlist)
- [Lịch sử kết quả](#post-apiv1trafficticket_vehicle_result_historylisthistory)
- [Batch Jobs](#post-apiv1trafficticketbatchjobslist)

---

## Tổng quan

Module TrafficTicket cung cấp chức năng tra cứu phạt nguội xe tự động:
1. Tra cứu theo biển số xe (public + internal)
2. Lên lịch tra cứu định kỳ cho xe đang cho thuê
3. Quản lý hàng đợi và kết quả tra cứu
4. Lưu lịch sử để theo dõi biến động

---

## `POST /api/v1/Traffic_Vehicle/CheckTrafficTicket`

**Mô tả:** Tra cứu phạt nguội theo biển số xe (dùng cho user trên app).

**Phân quyền:** `[AllowAnonymous]` — Public

### Request Body — `TrafficTicket_VehicleModel`
```json
{
  "PlateNumber": "51A-12345",
  "VehicleType": "Car"
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| PlateNumber | string | Có | Biển số xe (VD: `51A-12345`) |
| VehicleType | string | Không | Loại xe: `Car`, `Motorbike` |

### Response — `EzyResultObject<TrafficTicket_Vehicle_ResultUIModel[]>`
```json
{
  "StatusCode": 1,
  "Data": [
    {
      "PlateNumber": "51A-12345",
      "ViolationDate": "2025-12-01",
      "ViolationLocation": "TP.HCM",
      "ViolationContent": "Vượt đèn đỏ",
      "FineAmount": 4000000,
      "Status": "Chưa xử phạt",
      "CheckAt": "Công an TP.HCM"
    }
  ]
}
```

---

## `POST /api/v1/Traffic_Vehicle/CheckTrafficTicketNowAsync`

**Mô tả:** Tra cứu phạt nguội nội bộ (admin/internal) — trả về thêm thông tin kỹ thuật.

**Phân quyền:** `[Authorize]`

### Request Body — `TrafficTicket_VehicleModel`
```json
{
  "PlateNumber": "51A-12345"
}
```

### Response — `EzyResultObject<(TrafficTicket_Vehicle_ResultUIModel[], string sMessage)>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Results": [ { ... } ],
    "SystemMessage": "Tra cứu thành công tại 3 điểm kiểm tra"
  }
}
```

---

## `POST /api/v1/Traffic_Vehicle/List`

**Mô tả:** Lấy danh sách xe đang được theo dõi phạt nguội.

**Phân quyền:** `[Authorize]`

### Request Body — `TrafficTicket_VehicleParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "PlateNumber": "",
  "IsActive": true
}
```

### Response — `EzyResultObject<EzyDataSourceResult<TrafficTicket_VehicleModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 50,
    "Data": [
      {
        "TrafficTicket_VehicleId": "guid",
        "PlateNumber": "51A-12345",
        "LastCheckedAt": "2026-01-01T08:00:00Z",
        "NextScheduledAt": "2026-01-08T08:00:00Z"
      }
    ]
  }
}
```

---

## `POST /api/v1/Traffic_Vehicle/ScheduleTrafficTicketForFinished`

**Mô tả:** Lên lịch tra cứu phạt nguội cho xe vừa hoàn tất chuyến thuê.

**Phân quyền:** `[Authorize]`

### Request Body — `TrafficTicket_VehicleModel`
```json
{
  "PlateNumber": "51A-12345",
  "OrderId": "order-guid"
}
```

### Response — `EzyResultObject<TrafficTicketScheduleResult>`
```json
{
  "StatusCode": 1,
  "Data": {
    "ScheduledAt": "2026-01-08T08:00:00Z",
    "JobId": "job-guid"
  }
}
```

> **Lưu ý:** Được gọi tự động khi đơn hàng thuê xe kết thúc.

---

## `POST /api/v1/Traffic_CheckAt/List`

**Mô tả:** Lấy danh sách các điểm kiểm tra (cơ quan công an) dùng để tra cứu.

**Phân quyền:** `[Authorize]`

### Request Body — `TrafficTicket_Request_CheckAtParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 50
}
```

### Response — `EzyResultObject<EzyDataSourceResult<TrafficTicket_Request_CheckAtModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 10,
    "Data": [
      {
        "CheckAtId": 1,
        "Name": "Cục CSGT Bộ Công An",
        "Code": "CSX",
        "IsActive": true
      }
    ]
  }
}
```

---

## `POST /api/v1/TrafficTicket_Request_Queue/List`

**Mô tả:** Lấy danh sách hàng đợi yêu cầu tra cứu phạt nguội.

**Phân quyền:** `[Authorize]`

### Request Body — `TrafficTicket_Request_QueueParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "Status": "Pending"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<TrafficTicket_Request_QueueModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 5,
    "Data": [
      {
        "QueueId": "queue-guid",
        "PlateNumber": "51A-12345",
        "Status": "Pending",
        "CreatedAt": "2026-01-01T10:00:00Z"
      }
    ]
  }
}
```

---

## `POST /api/v1/TrafficTicket_Request_Status/List`

**Mô tả:** Lấy danh sách trạng thái của các yêu cầu tra cứu.

**Phân quyền:** `[Authorize]`

### Request Body — `TrafficTicket_Request_StatusParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20
}
```

---

## `POST /api/v1/TrafficTicket_Vehicle_Result/List`

**Mô tả:** Lấy danh sách kết quả tra cứu phạt nguội.

**Phân quyền:** `[Authorize]`

### Request Body — `TrafficTicket_Vehicle_ResultParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "PlateNumber": "51A-12345"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<TrafficTicket_Vehicle_ResultModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 3,
    "Data": [
      {
        "ResultId": "result-guid",
        "PlateNumber": "51A-12345",
        "ViolationDate": "2025-12-01",
        "ViolationContent": "Vượt đèn đỏ",
        "FineAmount": 4000000
      }
    ]
  }
}
```

---

## `POST /api/v1/TrafficTicket_Vehicle_Result/UpdateAdditionalData`

**Mô tả:** Cập nhật thêm dữ liệu bổ sung cho kết quả tra cứu (admin job).

**Phân quyền:** `[Authorize]`

### Request Body
Không có body.

---

## `POST /api/v1/TrafficTicket_Vehicle_Result_History/List`

**Mô tả:** Lấy danh sách lịch sử kết quả tra cứu.

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/TrafficTicket_Vehicle_Result_History/ListHistory`

**Mô tả:** Lấy lịch sử phạt nguội theo xe — dùng để so sánh biến động qua các lần tra cứu.

**Phân quyền:** `[Authorize]`

### Request Body — `TrafficTicket_Vehicle_Result_HistoryParamModel`
```json
{
  "PlateNumber": "51A-12345"
}
```

### Response — `EzyResultObject<TrafficTicket_Vehicle_Result_HistoryUIModel[]>`
```json
{
  "StatusCode": 1,
  "Data": [
    {
      "CheckedAt": "2026-01-01T08:00:00Z",
      "ViolationCount": 2,
      "TotalFineAmount": 8000000
    }
  ]
}
```

---

## `POST /api/v1/TrafficTicket_Vehicle_Result_History/DeletedHistory`

**Mô tả:** Xoá lịch sử tra cứu cũ (cleanup job).

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/TrafficTicketBatchJobs/List`

**Mô tả:** Lấy danh sách batch jobs tra cứu phạt nguội.

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<EzyDataSourceResult<TrafficTicket_BatchJobModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 10,
    "Data": [
      {
        "JobId": "job-guid",
        "StartedAt": "2026-01-01T08:00:00Z",
        "CompletedAt": "2026-01-01T08:05:00Z",
        "TotalVehicles": 50,
        "SuccessCount": 48,
        "FailedCount": 2
      }
    ]
  }
}
```

---

## Ghi chú

- `CheckTrafficTicket` là endpoint duy nhất không cần auth — dùng cho user tra cứu trực tiếp trên app/web.
- Sau khi đơn hàng thuê xe kết thúc, hệ thống tự động lên lịch tra cứu qua `ScheduleTrafficTicketForFinished`.
- Kết quả được so sánh với lần tra cứu trước để phát hiện vi phạm mới.

---

## Error Responses

| Trường hợp | StatusCode | Message |
|-------------|-----------|---------|
| Biển số không hợp lệ | 0 | Biển số xe không đúng định dạng |
| Không tìm thấy kết quả | 1 | (trả Data rỗng — không phải lỗi) |
| API CSGT timeout | 0 | Không thể kết nối tới hệ thống tra cứu, vui lòng thử lại sau |
| Vehicle không tồn tại | 0 | Xe không tồn tại trong hệ thống |
| Batch job đang chạy | 0 | Đang có tiến trình tra cứu, vui lòng chờ hoàn tất |
| Schedule cho đơn không hợp lệ | 0 | Đơn hàng chưa hoàn tất, không thể lên lịch tra cứu |

### Controllers chưa document chi tiết

| Controller | Route | Endpoints | Mô tả |
|-----------|-------|-----------|-------|
| `TrafficTicket_Request_QueueController` | `/api/v1/TrafficTicket_Request_Queue` | POST `/List` | Queue các yêu cầu tra cứu đang chờ xử lý |
| `TrafficTicket_Request_StatusController` | `/api/v1/TrafficTicket_Request_Status` | POST `/List` | Trạng thái request (Pending, Processing, Done, Failed) |
| `TrafficTicket_Vehicle_ResultController` | `/api/v1/TrafficTicket_Vehicle_Result` | POST `/List`, `/UpdateAdditionalData` | Kết quả tra cứu + cập nhật thông tin bổ sung |
