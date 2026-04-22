# Design: Lấy giá bảo hiểm thực từ Vifo khi booking

**Date:** 2026-04-22  
**Branch:** CARSHORT  
**Status:** Approved

## Mục tiêu

Khi xe không có bảo hiểm chủ xe (`HaveVehicleInsurance = false`), thay vì tính phí bảo hiểm theo công thức `%/ngày`, hệ thống sẽ gọi Vifo `/v2/insurance/total-price` để lấy giá thực tế. Áp dụng cho cả preview lẫn lúc đặt xe. Được bật/tắt qua cờ `IsUsingBooking` trong `SYSTEM_VIFO_SETTINGS`.

## Thay đổi

### 1. Project reference

`AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Shared.csproj` thêm:

```xml
<ProjectReference Include="..\Ezy.Module.Insurance\Ezy.Module.Insurance.Service\Ezy.Module.Insurance.Shared.csproj" />
```

### 2. `VifoConfig` — thêm flag

`Ezy.Module.Insurance/Ezy.Module.Insurance.Service/Providers/Vifo/VifoConfig.cs`:

```csharp
public bool IsUsingBooking { get; set; }
```

Default `false` → không ảnh hưởng production nếu chưa cấu hình trong `SYSTEM_VIFO_SETTINGS`.

### 3. `RentCarHelper` — logic thay đổi

File: `AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs`

**Nhánh A (xe có bảo hiểm chủ xe):** không đổi.

**Nhánh B (`needGetInsurance = true`):** thêm phân nhánh theo `IsUsingBooking`:

```
needGetInsurance = true
  └─ IsUsingBooking = true?
       ├─ Yes → TryGetVifoTotalPrice(model, config, out price, out err)
       │          ├─ Success → result = price
       │          └─ Fail   → fallback: result = totalPrice * (%) + GGChat + LogError
       └─ No  → result = totalPrice * (%)   [hành vi hiện tại]
```

### 4. `TryGetVifoTotalPrice()` — private helper trong `RentCarHelper`

```csharp
private static bool TryGetVifoTotalPrice(
    RentCarFormulaInputModel model,
    VifoConfig config,
    out decimal price,
    out string error)
```

**Lazy registration:**
```csharp
if (!InsuranceHelper.HasProvider("VIFO"))
    InsuranceHelper.RegisterVifo(config);
```
Provider và token cache (50 phút TTL) tồn tại suốt vòng đời process.

**Validation trước khi gọi API:**  
Nếu `PlateNumber`, `YearModel`, hoặc `VehicleNoOfSeatId` là null → return `false` ngay, không gọi API, không GGChat.

**Payload `VifoCarShortPayload`:**

| Field | Nguồn |
|---|---|
| `plate_no` | `vehicle.PlateNumber` |
| `year` | `vehicle.YearModel` |
| `brand` | `CachedDataManagement.ConfigVehicleMake_Get_Instance_Id(vehicle.VehicleMakeId)?.Name` |
| `model` | `CachedDataManagement.ConfigVehicleModel_Get_Instance_Id(vehicle.VehicleModelId)?.Name` |
| `seat` | `CachedDataManagement.ConfigVehicleNoOfSeat_Get_Instance_Id(vehicle.VehicleNoOfSeatId)?.NumberOfSeat` |
| `start_date` | `rentInfo.FromDate` → VN timezone, clamp về hôm nay nếu quá khứ |
| `end_date` | `rentInfo.ToDate` → VN timezone |
| `family_code` | `config.ProductFamilyCode` |
| `provider_code` | `config.ProductProviderCode` |
| `email` | `config.Email` |
| `phone`, `fullname` | `""` — chỉ required khi tạo order, không cần cho total-price |

**Error handling khi `GetTotalPrice()` thất bại:**
1. `LogLogicHelper.LogError(...)` — method `"TryGetVifoTotalPrice"`
2. `GGNotifyHelper.SendMessageGGChat_SystemReport(...)` — kèm slug xe + thông tin lỗi
3. return `false` → caller fallback về công thức `%`

## Scope không đổi

- Nhánh A (xe có bảo hiểm chủ xe) — không đổi
- `Order_Vehicle_InsuranceSubmittedService.GetVNIInsurance()` — không đổi (vẫn dùng khi INTHETRIP)
- `VIFOService.cs` cũ — không đổi, không xóa
- Database schema — không đổi

## Success criteria

1. Thêm `<ProjectReference>` → project build thành công
2. `VifoConfig` có field `IsUsingBooking`
3. Khi `IsUsingBooking = false` (default): hành vi `GetInsuranceFee()` y hệt hiện tại
4. Khi `IsUsingBooking = true` + Vifo trả thành công: `InsuranceFee = FinalAmount` từ Vifo
5. Khi `IsUsingBooking = true` + Vifo lỗi: fallback về `%`, GGChat nhận thông báo, LogError được ghi
6. Validation null (PlateNumber/Year/Seat): return false ngay, không gọi API, không GGChat
