# Ezy.Module.Insurance

Module tính giá & quản lý đơn bảo hiểm, hỗ trợ nhiều nhà cung cấp (provider). Hiện tại tích hợp **VIFO**.

---

## Mục lục

1. [Cấu trúc module](#1-cấu-trúc-module)
2. [Cài đặt & tham chiếu](#2-cài-đặt--tham-chiếu)
3. [Cấu hình VIFO](#3-cấu-hình-vifo)
4. [Đăng ký provider](#4-đăng-ký-provider)
5. [API danh mục](#5-api-danh-mục)
6. [API đơn bảo hiểm](#6-api-đơn-bảo-hiểm)
7. [Typed Payload VIFO](#7-typed-payload-vifo)
8. [Thêm provider mới](#8-thêm-provider-mới)
9. [Danh sách loại bảo hiểm VIFO](#9-danh-sách-loại-bảo-hiểm-vifo)

---

## 1. Cấu trúc module

```
Ezy.Module.Insurance/
└── Ezy.Module.Insurance.Service/          ← AssemblyName: Ezy.Module.Insurance.Shared
    ├── Helpers/
    │   └── InsuranceHelper.cs             ← Entry point duy nhất
    ├── Models/
    │   └── InsuranceModels.cs             ← Unified request/result DTOs
    └── Providers/
        ├── IInsurancePriceProvider.cs     ← Interface cho mọi provider
        └── Vifo/
            ├── VifoConfig.cs              ← Cấu hình kết nối VIFO
            ├── VifoModels.cs              ← VIFO API DTOs + typed payloads
            └── VifoInsuranceProvider.cs   ← Implement VIFO
```

---

## 2. Cài đặt & tham chiếu

**Bước 1:** Build project `Ezy.Module.Insurance.Service`, copy `Ezy.Module.Insurance.Shared.dll` vào thư mục `Libs/`.

**Bước 2:** Thêm reference vào `AllianceMiddlemanWebAPI.Shared.csproj`:

```xml
<Reference Include="Ezy.Module.Insurance.Shared">
  <HintPath>..\..\Libs\Ezy.Module.Insurance.Shared.dll</HintPath>
</Reference>
```

---

## 3. Cấu hình VIFO

Cấu hình được lưu trong bảng **System Config** theo cùng pattern với `MisaInvoiceHelper`, `VietQRHelper`.

| Trường | Giá trị |
|--------|---------|
| **Config Key** | `SYSTEM_VIFO_SETTINGS` |
| **Value (JSON)** | xem bên dưới |

### Sandbox vs Production

| Môi trường | BaseUrl |
|------------|---------|
| Sandbox    | `https://sapi.vifo.vn` |
| Production | `https://api.vifo.vn`  |

### Ví dụ JSON lưu vào System Config

```json
{
  "BaseUrl":                     "https://sapi.vifo.vn",
  "UserName":                    "VIFO_SIGO_demotest",
  "Password":                    "sigo@123",
  "Email":                       "SIGO_sale@email.com",
  "ProductFamilyCode":           "CARSHORT",
  "ProductProviderCode":         "VNI_SGD2",
  "IsGetVNIInsuranceInBackground": true,
  "VifoCompanyName":             "VNI",
  "VifoCompanyTaxCode":          "0102737963-034"
}
```

> **Lưu ý:** Token được tự động lấy qua UserName/Password khi khởi tạo provider và cache 50 phút.

---

## 4. Đăng ký provider

Gọi **một lần** khi khởi động ứng dụng (trong `Startup.cs` hoặc `FirstRunHostedService`).

### Cách 1 – Đọc từ System Config *(khuyến nghị)*

```csharp
using Ezy.Module.Insurance.Shared.Helpers;

// Tự động đọc key "SYSTEM_VIFO_INSURANCE" từ bảng System Config
InsuranceHelper.RegisterVifo();
```

### Cách 2 – Truyền config thủ công

```csharp
using Ezy.Module.Insurance.Shared.Helpers;
using Ezy.Module.Insurance.Shared.Providers.Vifo;

InsuranceHelper.RegisterVifo(new VifoConfig
{
    BaseUrl              = "https://sapi.vifo.vn",
    UserName             = "VIFO_SIGO_demotest",
    Password             = "sigo@123",
    Email                = "SIGO_sale@email.com",
    ProductFamilyCode    = "CARSHORT",
    ProductProviderCode  = "VNI_SGD2"
});
```

### Đọc lại config hiện tại

```csharp
VifoConfig config = InsuranceHelper.GetVifoConfig();
```

---

## 5. API danh mục

### 5.1 Lấy danh sách loại bảo hiểm (Families)

```csharp
InsuranceFamiliesResult result = InsuranceHelper.GetFamilies("VIFO");

if (result.IsSuccessful)
{
    foreach (var family in result.Families)
    {
        // family.Code         → "BHYT", "CARSHORT", "TNDS_XE_MAY", ...
        // family.Name         → tên tiếng Việt
        // family.NameEn       → tên tiếng Anh
        // family.ProductCount → số sản phẩm trong family
    }
}
```

**Endpoint VIFO:** `GET /v2/families`

---

### 5.2 Lấy danh sách sản phẩm (Products)

```csharp
// Một trang
InsuranceProductsResult result = InsuranceHelper.GetProducts("VIFO", "BHYT", page: 1, perPage: 50);

// Tất cả trang (tự động phân trang)
InsuranceProductsResult result = InsuranceHelper.GetAllProducts("VIFO", "BHYT");

if (result.IsSuccessful)
{
    foreach (var product in result.Products)
    {
        // product.ProductCode  → "BHYT0305220003"
        // product.Name         → tên sản phẩm
        // product.Price        → giá cơ bản
        // product.Options      → danh sách điều khoản bổ sung (Sku, Title, Price)
    }
}
```

**Endpoint VIFO:** `GET /v2/products?family_code=&page=&per_page=`

---

## 6. API đơn bảo hiểm

### 6.1 Tính giá đơn bảo hiểm

```csharp
var result = InsuranceHelper.GetTotalPrice(new InsurancePriceRequest
{
    ProviderCode = "VIFO",
    FamilyCode   = "BHYT",
    Payload      = new VifoBhytPayload
    {
        ProductCode     = "BHYT0305220003",
        Fullname        = "Nguyen Van A",
        Phone           = "0901234567",
        FinalAmount     = 0,              // luôn = 0 khi chỉ tính giá
        BeneficiaryList = new List<VifoBhytBeneficiary>
        {
            new VifoBhytBeneficiary
            {
                Fullname   = "Nguyen Van A",
                Birthday   = "1990-05-15",
                Nic        = "012345678901",
                Hospital   = "82-238",
                Gender     = "1",
                Renewal    = false,
                StartDate  = "2025-01-01"
            }
        }
    }
});

if (result.IsSuccessful)
    Console.WriteLine($"Phí BH: {result.FinalAmount:N0} VND");
else
    Console.WriteLine($"Lỗi: {result.ErrorMessage}");
```

**Endpoint VIFO:** `POST /v2/insurance/total-price`

---

### 6.2 Tạo đơn bảo hiểm

Payload giống Tính giá nhưng `FinalAmount` phải là giá thật (lấy từ bước tính giá).

```csharp
var createResult = InsuranceHelper.CreateOrder(new InsuranceCreateOrderRequest
{
    ProviderCode = "VIFO",
    FamilyCode   = "BHYT",
    Payload      = new VifoBhytPayload
    {
        ProductCode     = "BHYT0305220003",
        Fullname        = "Nguyen Van A",
        Phone           = "0901234567",
        FinalAmount     = 1263600,        // lấy từ GetTotalPrice
        BeneficiaryList = new List<VifoBhytBeneficiary> { ... }
    }
});

if (createResult.IsSuccessful)
{
    string orderNumber = createResult.OrderNumber;
    // Lưu orderNumber vào DB để tra cứu / hủy sau
}
```

**Endpoint VIFO:** `POST /v2/insurance`  
**HTTP Status:** 201 khi thành công

---

### 6.3 Kiểm tra trạng thái đơn

```csharp
InsuranceOrderDetailResult detail = InsuranceHelper.CheckOrder("VIFO", orderNumber);

if (detail.IsSuccessful)
{
    // detail.Status   → "pending" / "active" / "expired" / "cancelled"
    // detail.RawData  → full response từ VIFO
}
```

**Endpoint VIFO:** `GET /v2/insurance/{order_number}`

---

### 6.4 Hủy đơn bảo hiểm

> **Lưu ý:** Chỉ áp dụng cho một số sản phẩm nhất định. Liên hệ vận hành VIFO trước khi sử dụng.

```csharp
InsuranceTerminateResult terminate = InsuranceHelper.TerminateOrder("VIFO", orderNumber);

if (terminate.IsSuccessful)
    Console.WriteLine($"Đã hủy: {terminate.Message}");
else
    Console.WriteLine($"Lỗi: {terminate.ErrorMessage}");
```

**Endpoint VIFO:** `POST /v2/order/{order_number}/terminate`

---

### 6.5 Kiểm tra trạng thái đơn BHXH từ PVI

```csharp
InsurancePviStatusResult pvi = InsuranceHelper.CheckOrderPviStatus("VIFO", orderNumber);

if (pvi.IsSuccessful)
{
    // pvi.RawData → thông tin tờ khai từ phía PVI
}
```

**Endpoint VIFO:** `GET /v2/order/{order_number}/pvi`

---

### Flow hoàn chỉnh tạo đơn BHYT

```csharp
// Bước 1: Lấy danh sách sản phẩm
var products = InsuranceHelper.GetAllProducts("VIFO", "BHYT");
var product  = products.Products.First(p => p.ProductCode == "BHYT0305220003");

// Bước 2: Build payload
var payload = new VifoBhytPayload
{
    ProductCode     = product.ProductCode,
    Fullname        = "Nguyen Van A",
    Phone           = "0901234567",
    FinalAmount     = 0,
    BeneficiaryList = new List<VifoBhytBeneficiary>
    {
        new VifoBhytBeneficiary
        {
            Fullname  = "Nguyen Van A",
            Birthday  = "1990-05-15",
            Nic       = "012345678901",
            Hospital  = "82-238",
            Gender    = "1",
            Renewal   = false,
            StartDate = "2025-01-01"
        }
    }
};

// Bước 3: Tính giá
var price = InsuranceHelper.GetTotalPrice(new InsurancePriceRequest
{
    ProviderCode = "VIFO",
    FamilyCode   = "BHYT",
    Payload      = payload
});
if (!price.IsSuccessful) return;

// Bước 4: Tạo đơn với giá thật
payload.FinalAmount = price.FinalAmount;
var order = InsuranceHelper.CreateOrder(new InsuranceCreateOrderRequest
{
    ProviderCode = "VIFO",
    FamilyCode   = "BHYT",
    Payload      = payload
});
if (!order.IsSuccessful) return;

// Bước 5: Lưu order_number
string orderNumber = order.OrderNumber;

// Bước 6 (tùy chọn): Kiểm tra trạng thái
var detail = InsuranceHelper.CheckOrder("VIFO", orderNumber);
```

---

## 7. Typed Payload VIFO

Module cung cấp sẵn các class payload có kiểu dữ liệu cho những loại bảo hiểm phổ biến.

### VifoBhytPayload – BH Y tế (BHYT / BHYTHGD)

| Property | Kiểu | Bắt buộc | Mô tả |
|----------|------|----------|-------|
| `ProductCode` | string | ✅ | Mã sản phẩm |
| `Fullname` | string | ✅ | Họ tên người mua |
| `Phone` | string | ✅ | Số điện thoại |
| `Email` | string | | Email |
| `Options` | `List<string>` | ✅ | SKU thành viên thêm (VF2, VF3,…) – để `[]` nếu 1 người |
| `FinalAmount` | long | ✅ | 0 khi tính giá; giá thật khi tạo đơn |
| `BeneficiaryList` | `List<VifoBhytBeneficiary>` | ✅ | Danh sách người thụ hưởng |

**VifoBhytBeneficiary:**

| Property | Kiểu | Bắt buộc | Mô tả |
|----------|------|----------|-------|
| `Fullname` | string | ✅ | Họ tên |
| `Birthday` | string | ✅ | Ngày sinh (yyyy-MM-dd) |
| `Nic` | string | ✅ | Số CCCD |
| `MedicalId` | string | | Số BHXH – bắt buộc khi tái tục |
| `Hospital` | string | ✅ | Mã bệnh viện (full_code) |
| `Gender` | string | ✅ | `"1"` = Nam, `"2"` = Nữ |
| `Renewal` | bool | ✅ | `false` = Tăng mới, `true` = Tái tục |
| `StartDate` | string | ✅ | Ngày hiệu lực dự kiến (yyyy-MM-dd) |
| `OldCardStartDate` | string | | Ngày đầu thẻ cũ – bắt buộc khi tái tục |
| `OldCardEndDate` | string | | Ngày hết thẻ cũ – bắt buộc khi tái tục |
| `FiveYearDate` | string | | Thời điểm đủ 5 năm liên tục |
| `SocialFamilyId` | string | | Mã hộ gia đình |
| `Nation` | string | | Mã dân tộc |

---

### VifoTndsMotorbikePayload – BH TNDS xe máy

| Property | Kiểu | Bắt buộc | Mô tả |
|----------|------|----------|-------|
| `ProductCode` | string | ✅ | Mã sản phẩm |
| `Fullname` | string | ✅ | Họ tên chủ xe |
| `Phone` | string | ✅ | Số điện thoại |
| `StartDate` | string | ✅ | Ngày hiệu lực (yyyy-MM-dd) |
| `LicensePlate` | string | | Biển số xe |
| `ChassisNumber` | string | | Số khung |
| `EngineNumber` | string | | Số máy |
| `FinalAmount` | long | ✅ | 0 khi tính giá |

---

### VifoCarPayload – BH vật chất / TNDS xe ô tô

| Property | Kiểu | Bắt buộc | Mô tả |
|----------|------|----------|-------|
| `ProductCode` | string | ✅ | Mã sản phẩm |
| `Fullname` | string | ✅ | Họ tên chủ xe |
| `Phone` | string | ✅ | Số điện thoại |
| `StartDate` | string | ✅ | Ngày bắt đầu BH (yyyy-MM-dd) |
| `EndDate` | string | | Ngày kết thúc BH |
| `LicensePlate` | string | | Biển số xe |
| `ChassisNumber` | string | | Số khung |
| `EngineNumber` | string | | Số máy |
| `CarValue` | long? | | Giá trị xe (VND) |
| `FinalAmount` | long | ✅ | 0 khi tính giá |

---

### VifoCarShortPayload – BH vật chất xe ô tô cho thuê theo chuyến (CARSHORT)

> **Lưu ý:** CARSHORT là sản phẩm riêng của VIFO dành cho SIGO, không có trong docs public vifo.vn.
> Dùng `family_code` + `provider_code` thay vì `product_code`.

| Property | Kiểu | Bắt buộc | Mô tả |
|----------|------|----------|-------|
| `Phone` | string | ✅ | SĐT chủ xe |
| `Fullname` | string | ✅ | Họ tên chủ xe |
| `Email` | string | | Email — dùng `settings.Email` từ config |
| `FamilyCode` | string | ✅ | Luôn = `"CARSHORT"` (default) |
| `ProviderCode` | string | ✅ | Mã provider, ví dụ `"VNI_SGD2"` |
| `StartDate` | string | ✅ | Ngày bắt đầu BH (yyyy-MM-dd, không được là quá khứ) |
| `EndDate` | string | ✅ | Ngày kết thúc BH (yyyy-MM-dd) |
| `PlateNo` | string | ✅ | Biển số xe |
| `Year` | int? | ✅ | Năm sản xuất xe |
| `Brand` | string | ✅ | Tên hãng xe (ví dụ: `"Toyota"`) |
| `Model` | string | ✅ | Tên dòng xe (ví dụ: `"Vios"`) |
| `Seat` | int? | ✅ | Số chỗ ngồi |

**Ví dụ sử dụng:**

```csharp
var result = InsuranceHelper.CreateCarShortOrder(new VifoCarShortPayload
{
    Phone        = order.OwnerPhoneNumber,
    Fullname     = order.OwnerName,
    Email        = settings.Email,
    FamilyCode   = settings.ProductFamilyCode,   // "CARSHORT"
    ProviderCode = settings.ProductProviderCode, // "VNI_SGD2"
    StartDate    = startDate.ToString("yyyy-MM-dd"),
    EndDate      = endDate.ToString("yyyy-MM-dd"),
    PlateNo      = vehicle.PlateNumber,
    Year         = vehicle.YearModel,
    Brand        = brand?.Name,
    Model        = carModel?.Name,
    Seat         = numberOfSeats?.NumberOfSeat
});

if (result.IsSuccessful)
{
    // result.OrderNumber         → VifoOrderNumber
    // result.ExternalId          → VifoOrderId
    // result.ProviderOrderNumber → VifoProviderOrderNumber
    // result.ContractUrl         → VifoContractFileUrl
    // result.CreatedAt           → VifoOrderCreatedDate (normalize to UTC before storing)
}
```

---

### Payload cho loại BH khác

Với các loại bảo hiểm chưa có typed class (du lịch, sức khỏe toàn diện, tai nạn cá nhân,…), truyền trực tiếp bằng `Dictionary<string, object>` hoặc object ẩn danh:

```csharp
Payload = new
{
    product_code     = "PA3...",
    fullname         = "Nguyen Van A",
    phone            = "0901234567",
    final_amount     = 0,
    options          = new string[] { },
    start_date       = "2025-01-01",
    // ... các trường theo tài liệu VIFO
}
```

> Tham khảo schema đầy đủ tại: [docs.vifo.vn/docs/api](https://docs.vifo.vn/docs/api/total-price-order)

---

## 8. Thêm provider mới

Implement interface `IInsurancePriceProvider`:

```csharp
public class BshInsuranceProvider : IInsurancePriceProvider
{
    public string ProviderCode => "BSH";

    public InsurancePriceResult   GetTotalPrice(InsurancePriceRequest request)     { /* ... */ }
    public InsuranceCreateOrderResult CreateOrder(InsuranceCreateOrderRequest req) { /* ... */ }
    public InsuranceOrderDetailResult CheckOrder(string orderNumber)               { /* ... */ }
    public InsuranceTerminateResult   TerminateOrder(string orderNumber)           { /* ... */ }
    public InsurancePviStatusResult   CheckOrderPviStatus(string orderNumber)      { /* ... */ }
    public InsuranceFamiliesResult    GetFamilies()                                { /* ... */ }
    public InsuranceProductsResult    GetProducts(string familyCode, int page, int perPage) { /* ... */ }
}
```

Đăng ký:

```csharp
InsuranceHelper.RegisterProvider(new BshInsuranceProvider(...));

// Sau đó gọi bình thường với providerCode = "BSH"
InsuranceHelper.GetTotalPrice(new InsurancePriceRequest { ProviderCode = "BSH", ... });
```

---

## 9. Danh sách loại bảo hiểm VIFO

| Family Code | Tên |
|-------------|-----|
| `BHYT` | BH Y tế cá nhân |
| `BHYTHGD` | BH Y tế hộ gia đình |
| `BHXHTN` | BH Xã hội tự nguyện |
| `TNDS` | BH TNDS xe máy |
| `TNCAR` | BH TNDS xe ô tô |
| `CARSHORT` | BH vật chất xe ô tô (cho thuê theo chuyến) |
| `FLCA` | BH hủy chuyến bay |
| `IN-10` | BH trễ / đổi chuyến bay |
| `PA-3` | BH tai nạn cá nhân |
| `FA` | BH tai nạn hộ gia đình |
| `CANCER` | BH ung thư & bệnh hiểm nghèo |
| `HS` | BH trợ cấp viện phí |
| `INBO` | BH người nước ngoài du lịch tại VN |
| `VNT` | BH du lịch trong nước |
| `05` | BH du lịch quốc tế |
| `08` | BH sức khỏe toàn diện |
| `08-G` | BH sức khỏe toàn diện nhóm |
| `BANCA` | BH bảo an tín dụng |
| `03` | BH nhà tư nhân |
