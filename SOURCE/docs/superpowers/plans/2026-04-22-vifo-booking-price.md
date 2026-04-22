# Vifo Booking Price Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Khi xe không có bảo hiểm chủ xe và `IsUsingBooking = true` trong config, gọi Vifo `/v2/insurance/total-price` để lấy phí bảo hiểm thực thay vì tính theo `%/ngày`.

**Architecture:** Thêm ProjectReference `Ezy.Module.Insurance.Shared` vào `AllianceMiddlemanWebAPI.Service`. Thêm flag `IsUsingBooking` vào `VifoConfig`. Thêm private helper `TryGetVifoTotalPrice()` vào `RentCarHelper` — được gọi sau khi đã tính `%` (fallback tự nhiên), override `result` nếu Vifo thành công.

**Tech Stack:** C# .NET 5, RestSharp (qua `Ezy.Module.Insurance.Shared`), xUnit (test project)

---

## Files thay đổi

| File | Thao tác |
|---|---|
| `Ezy.Module.Insurance/Ezy.Module.Insurance.Service/Providers/Vifo/VifoConfig.cs` | Modify — thêm `IsUsingBooking` |
| `AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Shared.csproj` | Modify — thêm ProjectReference |
| `AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs` | Modify — thêm `TryGetVifoTotalPrice()` + sửa `GetInsuranceFee()` |

---

## Task 1: Thêm `IsUsingBooking` vào `VifoConfig`

**Files:**
- Modify: `Ezy.Module.Insurance/Ezy.Module.Insurance.Service/Providers/Vifo/VifoConfig.cs`

- [ ] **Step 1: Thêm property**

Mở [VifoConfig.cs](Ezy.Module.Insurance/Ezy.Module.Insurance.Service/Providers/Vifo/VifoConfig.cs). Thêm property sau `IsGetVNIInsuranceInBackground`:

```csharp
public bool IsGetVNIInsuranceInBackground { get; set; }
/// <summary>
/// Khi true, gọi Vifo /v2/insurance/total-price để lấy phí bảo hiểm thực
/// thay vì tính theo % cấu hình. Fallback về % nếu Vifo lỗi.
/// </summary>
public bool IsUsingBooking { get; set; }
public string VifoCompanyName { get; set; }
```

- [ ] **Step 2: Build để xác nhận không lỗi syntax**

```bash
cd AllianceMiddlemanWebAPI.Service
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add Ezy.Module.Insurance/Ezy.Module.Insurance.Service/Providers/Vifo/VifoConfig.cs
git commit -m "feat: add IsUsingBooking flag to VifoConfig"
```

---

## Task 2: Thêm ProjectReference vào `AllianceMiddlemanWebAPI.Service`

**Files:**
- Modify: `AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Shared.csproj`

- [ ] **Step 1: Thêm ProjectReference**

Mở [AllianceMiddlemanWebAPI.Shared.csproj](AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Shared.csproj). Thêm vào `<ItemGroup>` chứa các `<ProjectReference>` hiện có (sau dòng `AllianceMiddlemanWebAPI.DataShared`):

```xml
<ItemGroup>
  <ProjectReference Include="..\AllianceMiddlemanWebAPI.Core\AllianceMiddlemanWebAPI.Core.csproj" />
  <ProjectReference Include="..\AllianceMiddlemanWebAPI.DataShared\AllianceMiddlemanWebAPI.DataShared.csproj" />
  <ProjectReference Include="..\Ezy.Module.Insurance\Ezy.Module.Insurance.Service\Ezy.Module.Insurance.Shared.csproj" />
</ItemGroup>
```

- [ ] **Step 2: Build `AllianceMiddlemanWebAPI.Service`**

```bash
cd AllianceMiddlemanWebAPI.Service
dotnet build
```

Expected: `Build succeeded.` Nếu lỗi `RestSharp` duplicate (vì cả 2 project đều reference `RestSharp.dll`), xem Step 2b.

- [ ] **Step 2b (nếu cần): Xử lý RestSharp conflict**

Nếu build báo lỗi `Duplicate 'Reference' items` hoặc conflict RestSharp, thêm `ExcludeAssets` vào ProjectReference:

```xml
<ProjectReference Include="..\Ezy.Module.Insurance\Ezy.Module.Insurance.Service\Ezy.Module.Insurance.Shared.csproj">
  <Private>true</Private>
</ProjectReference>
```

Nếu vẫn lỗi, kiểm tra version RestSharp trong cả 2 `HintPath` — dùng cùng một file `.dll`.

- [ ] **Step 3: Build toàn bộ solution**

```bash
cd ..
dotnet build AllianceMiddlemanWebAPI.sln
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add AllianceMiddlemanWebAPI.Service/AllianceMiddlemanWebAPI.Shared.csproj
git commit -m "feat: reference Ezy.Module.Insurance.Shared from AllianceMiddlemanWebAPI.Service"
```

---

## Task 3: Thêm `TryGetVifoTotalPrice()` vào `RentCarHelper`

**Files:**
- Modify: `AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs`

- [ ] **Step 1: Thêm usings**

Mở [RentCarHelper.cs](AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs). Thêm 3 dòng sau vào phần usings (sau các using hiện có):

```csharp
using Ezy.Module.Insurance.Shared.Helpers;
using Ezy.Module.Insurance.Shared.Models;
using Ezy.Module.Insurance.Shared.Providers.Vifo;
```

- [ ] **Step 2: Thêm method `TryGetVifoTotalPrice` ngay sau `GetInsuranceFee()`**

Vị trí: sau dấu `}` đóng của `GetInsuranceFee()` tại [RentCarHelper.cs:867](AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs#L867), trước `GetDiscountMoney()`.

Thêm method sau:

```csharp
/// <summary>
/// Gọi Vifo /v2/insurance/total-price để lấy phí bảo hiểm thực.
/// Trả false nếu xe thiếu thông tin bắt buộc hoặc Vifo lỗi.
/// Không gọi API và không gửi GGChat nếu xe thiếu thông tin.
/// </summary>
private static bool TryGetVifoTotalPrice(RentCarFormulaInputModel model, VifoConfig vifoConfig, out decimal price, out string error)
{
    price = 0;
    error = null;
    var rentalServiceItem = model.ServiceInfo.RentalServiceItem;
    var vehicle = rentalServiceItem.Vehicle;
    var rentInfo = model.RentInfo;

    if (string.IsNullOrEmpty(vehicle.PlateNumber) || vehicle.YearModel == null || vehicle.VehicleNoOfSeatId == null)
        return false;

    if (!InsuranceHelper.HasProvider("VIFO"))
        InsuranceHelper.RegisterVifo(vifoConfig);

    var ui_TimezoneOffset = rentInfo.UI_TimezoneOffset.Value;
    var now = DateTime.UtcNow.AddMinutes(-ui_TimezoneOffset).Date;
    DateTime fromDate = rentInfo.FromDate.Value.AddMinutes(ui_TimezoneOffset).Date,
             toDate   = rentInfo.ToDate.Value.AddMinutes(ui_TimezoneOffset).Date;
    if (fromDate < now) fromDate = now;

    var brand         = CachedDataManagement.ConfigVehicleMake_Get_Instance_Id(vehicle.VehicleMakeId);
    var carModel      = CachedDataManagement.ConfigVehicleModel_Get_Instance_Id(vehicle.VehicleModelId);
    var numberOfSeats = CachedDataManagement.ConfigVehicleNoOfSeat_Get_Instance_Id(vehicle.VehicleNoOfSeatId);

    var priceResult = InsuranceHelper.GetTotalPrice(new InsurancePriceRequest
    {
        ProviderCode = "VIFO",
        FamilyCode   = vifoConfig.ProductFamilyCode,
        Payload      = new VifoCarShortPayload
        {
            Phone        = "",
            Fullname     = "",
            Email        = vifoConfig.Email,
            FamilyCode   = vifoConfig.ProductFamilyCode,
            ProviderCode = vifoConfig.ProductProviderCode,
            StartDate    = fromDate.ToString("yyyy-MM-dd"),
            EndDate      = toDate.ToString("yyyy-MM-dd"),
            PlateNo      = vehicle.PlateNumber,
            Year         = vehicle.YearModel,
            Brand        = brand?.Name,
            Model        = carModel?.Name,
            Seat         = numberOfSeats?.NumberOfSeat
        }
    });

    if (priceResult.IsSuccessful)
    {
        price = priceResult.FinalAmount;
        return true;
    }

    error = priceResult.ErrorMessage;
    return false;
}
```

- [ ] **Step 3: Build để verify**

```bash
cd AllianceMiddlemanWebAPI.Service
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs
git commit -m "feat: add TryGetVifoTotalPrice private helper to RentCarHelper"
```

---

## Task 4: Sửa `GetInsuranceFee()` để gọi helper

**Files:**
- Modify: `AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs:831-847`

- [ ] **Step 1: Thay thế nhánh `if (needGetInsurance)`**

Tại [RentCarHelper.cs:831](AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs#L831), thay **toàn bộ** block `if (needGetInsurance) { ... }` (dòng 831–847) bằng:

```csharp
if (needGetInsurance)
{
    decimal? insurancePercentPerDay = null;
    var vehicle = rentalServiceItem.Vehicle;
    var cfVehicle = CachedDataManagement.ConfigVehicleType_Get_Instance_Id(vehicle.VehicleTypeId);
    if (cfVehicle != null)
    {
        var cfCompany = CachedDataManagement.ConfigVehicleInsuranceCompanys.FirstOrDefault(c => c.VehicleTypeId == cfVehicle.Id && c.IsDefault);
        if (cfCompany != null && cfCompany.InsurancePercentPerDay > 0)
        {
            insurancePercentPerDay = cfCompany.InsurancePercentPerDay;
            insuranceCompanyId = cfCompany.Id;
        }
    }
    insurancePercentPerDay ??= (model.ServiceInfo.SettingJson.InsurancePercentPerDay ?? 0);
    result = totalPrice * (insurancePercentPerDay.Value / 100m);

    var vifoConfig = InsuranceHelper.GetVifoConfig();
    if (vifoConfig.IsUsingBooking)
    {
        if (TryGetVifoTotalPrice(model, vifoConfig, out decimal vifoPrice, out string vifoError))
        {
            result = vifoPrice;
        }
        else if (!string.IsNullOrEmpty(vifoError))
        {
            GGNotifyHelper.SendMessageGGChat_SystemReport($"Lấy giá bảo hiểm Vifo thất bại, dùng giá ước tính. Lỗi: {vifoError}. Xe có slug {rentalServiceItem.Slug}");
            LogLogicHelper.LogError(new LogLogicErrorModel
            {
                ClientData     = JsonHelper.SerializeObject(new { rentalServiceItem.Slug, vifoError }),
                MainEntityId   = rentalServiceItem.Id.ToString(),
                MainEntityType = "RentalServiceItem",
                Message        = vifoError,
                Method         = "TryGetVifoTotalPrice",
                StartAt        = DateTime.UtcNow
            }, "TryGetVifoTotalPrice", "System", null, null);
        }
    }
}
```

Logic: `%` luôn được tính trước → đây là fallback tự nhiên. Vifo chỉ override `result` nếu `IsSuccessful`. Nếu Vifo lỗi có `vifoError` (không phải null guard) → GGChat + LogError.

- [ ] **Step 2: Build toàn bộ solution**

```bash
dotnet build AllianceMiddlemanWebAPI.sln
```

Expected: `Build succeeded.`

- [ ] **Step 3: Chạy test suite hiện có**

```bash
cd AllianceMiddlemanWebAPI.Tests
dotnet test
```

Expected: Tất cả test pass (không regression). Nếu test cần DB connection string, xem `appsettings.json` của test project.

- [ ] **Step 4: Commit**

```bash
git add AllianceMiddlemanWebAPI.Service/Helper/RentCarHelper.cs
git commit -m "feat: call Vifo total-price when IsUsingBooking=true in GetInsuranceFee"
```

---

## Task 5: Verify thủ công

- [ ] **Step 1: Cấu hình `IsUsingBooking = false` (default)**

Trong bảng SystemConfig, key `SYSTEM_VIFO_SETTINGS`, đảm bảo `IsUsingBooking` không có hoặc `false`. Gọi API tính giá thuê xe có `HaveVehicleInsurance = false`. Kiểm tra `InsuranceFee` = giá theo `%` như trước → **không thay đổi behavior**.

- [ ] **Step 2: Cấu hình `IsUsingBooking = true`**

Sửa `SYSTEM_VIFO_SETTINGS`:
```json
{
  "BaseUrl": "https://sapi.vifo.vn",
  "UserName": "VIFO_SIGO_demotest",
  "Password": "sigo@123",
  "Email": "SIGO_sale@email.com",
  "ProductFamilyCode": "CARSHORT",
  "ProductProviderCode": "VNI_SGD2",
  "IsUsingBooking": true,
  "IsGetVNIInsuranceInBackground": true,
  "VifoCompanyName": "VNI",
  "VifoCompanyTaxCode": "0102737963-034"
}
```

Gọi lại API tính giá → `InsuranceFee` phải khác với giá `%` và khớp với `final_amount` từ Vifo sandbox.

- [ ] **Step 3: Kiểm tra fallback**

Tạm thời sửa `Password` thành sai để Vifo auth fail. Gọi API → `InsuranceFee` phải fallback về giá `%`, GGChat nhận thông báo lỗi, `LogLogicError` được ghi. Khôi phục `Password` sau khi kiểm tra.

- [ ] **Step 4: Kiểm tra null guard**

Chọn xe không có `PlateNumber` (hoặc set null tạm trong DB test). Gọi API → `InsuranceFee` = giá `%`, không có GGChat nào được gửi.
