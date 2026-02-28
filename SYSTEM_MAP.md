# SYSTEM_MAP — Code Generation Companion

> This file helps AI agents generate code that follows AllianceMiddleman conventions.
> Read `llms.txt` first for system overview.

---

## Table of Contents
- [Feature Implementation Recipes](#feature-implementation-recipes)
- [Method Contract Reference](#method-contract-reference)
- [Entity → Service → Controller → API Mapping](#entity--service--controller--api-mapping)
- [Configuration Cascade](#configuration-cascade)

---

## Feature Implementation Recipes

### Recipe: Add New CRUD Endpoint

**Files to create/modify (6 files):**

```
1. Entity:      Core/Data/BusinessData/MyEntity.cs
2. DbSet:       Core/Data/BusinessData/BusinessEntities.cs  (add DbSet<MyEntity>)
3. Model:       DataShared/Models/MyEntityModel.cs
4. Interface:   Service/Services/MainBusiness/IMyEntityService.cs
5. Service:     Service/Services/MainBusiness/MyEntityService.cs
6. Controller:  WebAPI/Controllers/MainBusiness/MyEntityController.cs
```

**Step 1 — Entity** (inherits IEFBaseEntity):
```csharp
public partial class MyEntity
{
    public long Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? Log_CreatedDate { get; set; }
    public string Log_CreatedBy { get; set; }
    public DateTime? Log_UpdatedDate { get; set; }
    public string Log_UpdatedBy { get; set; }
    public string Note { get; set; }
    public decimal OrderNo { get; set; }
    public bool IsDisable { get; set; }
    public Guid? ID_GUID { get; set; }
    // Add domain-specific properties here
}
```

**Step 2 — Model + Param + Option:**
```csharp
public class MyEntityModel : EzyBaseConfigModel { /* domain fields */ }
public class MyEntityParamModel : EzyBasePagingParamModel { /* filter fields */ }
public class MyEntityOptionModel : BaseCategoryOptionModel { /* dropdown data */ }
```

**Step 3 — Service Interface + Implementation:**
```csharp
// Interface
public interface IMyEntityService
    : IBaseCategoryService<MyEntityModel, MyEntityModel, MyEntityParamModel, MyEntityOptionModel>
{ }

// Implementation — MUST implement ISQLFrameWork for auto-registration
public class MyEntityService
    : EFBaseCategoryService<MyEntity, MyEntityModel, MyEntityModel, MyEntityParamModel, MyEntityOptionModel>
    , IMyEntityService, ISQLFrameWork
{
    public MyEntityService() { InitService(); }

    private void InitService()
    {
        ScreenCode = "MY_ENTITY_SCREEN";
        Func_ConvertToListModel = (e) => ConvertToModel(null, e);
        func_BuildQuery = (p, q) => {
            if (!string.IsNullOrEmpty(p.TextSearch))
                q = q.Where(c => c.Name.Contains(p.TextSearch));
            return q;
        };
        func_BuildQueryOrder = (q) => q.OrderByDescending(t => t.Id);
    }
}
```

**Step 4 — Controller:**
```csharp
[Route("api/v1/MyEntity")]
[Authorize]
public class MyEntityController : BaseCategoryController<
    MyEntityModel, MyEntityModel, MyEntityParamModel, MyEntityOptionModel>
{
    [Route("List")][HttpPost]
    public EzyResultObject<EzyDataSourceResult<MyEntityModel>> List(MyEntityParamModel model)
        => base.List_Simple(model);

    [Route("Detail")][HttpPost]
    public EzyResultObject<MyEntityModel> Detail(MyEntityParamModel model)
        => base.Detail_Simple(model);

    [Route("Update")][HttpPost]
    public EzyResultObject<MyEntityModel> Update(MyEntityModel model)
        => base.Update_Simple(model);

    [Route("Delete")][HttpPost]
    public EzyResultObject<bool> Delete(MyEntityParamModel model)
        => base.Delete_Simple(model);
}
```

**Step 5 — Cache (optional):**
```csharp
// In Core/DataInfo/Cached/CachedDataManagement.cs:
public static void MyEntities_Refresh() { /* reload from DB */ }
public static MyEntityInfo[] MyEntities { get; }
```

---

### Recipe: Add New Background Engine

**Files to create/modify (2 files):**

```
1. Engine:   Service/Engines/MyNewEngine.cs
2. Register: Service/Engines/ProjectEngineAsyncHelper.cs  (add registration)
```

**Step 1 — Engine class:**
```csharp
public class MyNewEngine : ProjectEngineEntityAsync
{
    protected override async Task DoJob()
    {
        var service = CreateServiceInstance<MyService>();
        // Business logic here — called by framework timer
        await service.ProcessPendingItems();
    }
}
```

**Step 2 — Register in ProjectEngineAsyncHelper:**
```csharp
// In StartAllEngines():
private void MyNewEngine_Register()
{
    var engine = new MyNewEngine();
    RegisterEngine(engine, isAutoStartEngine);
}
```

**Key points:**
- Engines use Ezy.Module.Engine.dll timer framework (NOT Hangfire)
- Use `CreateServiceInstance<T>()` — NOT DI injection
- `DoJob()` is called periodically by the framework
- Errors are caught and logged by the base framework

---

### Recipe: Add New Validation Rule

**Files to modify:**

```
1. Service/Services/MainBusiness/RentalServiceItem/RentalServiceItemService_Booking.cs
   → Add check in CheckCanBookRentalService()
2. 01_ARCHITECTURE/validation-error-catalog.md → Document the rule
3. Core/Services/TextDisplayKeys.cs → Add error key constant
```

**Pattern:**
```csharp
// In CheckCanBookRentalService():
if (myConditionFails)
{
    sMessage = TextDisplayHelper.GetValue(
        TextDisplayKeys.msg_error_my_new_rule,
        "Default error message in Vietnamese");
    return;
}
```

**Error key convention:** `msg_error_{context}_{description}`
- Messages are managed via `TextDisplayHelper.GetValue(key, defaultMessage)`
- Admin can override messages via CMS without redeploying

---

### Recipe: Add New Notification Template

**Files to modify:**

```
1. DB: Insert into NotificationTemplate table
2. Service: Call NotificationHelper.InsertNotification() with template code
```

**Pattern:**
```csharp
// Template codes follow: {ACTION}_{CONTEXT}_NOTIFY_TO_{RECIPIENT}
// Example: RENT_CAR_OWNER_CANCEL_REQUEST_NOTIFY_TO_USER
NotificationHelper.InsertNotification(
    templateCode: "MY_ACTION_NOTIFY_TO_USER",
    entityId: order.Id,
    entityName: "Order",
    notifyToId: renter.Id,
    replacements: new Dictionary<string, string> {
        { "#OrderNumber#", order.OrderNumber },
        { "#OwnerName#", owner.DisplayName }
    });
```

**Delivery pipeline:** Insert → NotificationBatchJobEngine → SignalR + FCM

---

### Recipe: Add New Payment Integration

**Files to create:**

```
1. Controller: WebAPI/Controllers/MainBusiness/{Provider}/
2. Service:    Service/Services/MainBusiness/{Provider}/
3. Engine:     Service/Engines/Auto{Provider}Engine.cs (if async processing needed)
4. Logs:       Core entity for {Provider}_APICall_Log
```

**Follow MB Bank pattern:**
- OAuth2 authentication with token refresh
- Request/Response logging to `{Provider}_APICall_Log`
- Async processing via background engine
- Status polling (if no webhook support)
- Google Chat alerts on failures

---

### Recipe: Add New Stored Procedure

**Files to modify:**

```
1. Database: Create SP in PostgreSQL (returns JSON)
2. Core/Data/Stores/EEzyStoredProcedureNames.cs — Add enum entry
3. Core/Data/Stores/SPEntities/ — Add param + result classes
```

**SP Convention:**
```sql
-- Naming: sp_{Action}_{Entity}_Json
-- Returns: JSON string via SELECT to_json(array_agg(row_to_json(t)))
-- Called via: StoreRepository_JsonAsync.Exec_JsonStoredProceduceAsync<TResult, TParam>()
```

**Param class must extend sp_BaseParam:**
```csharp
public class sp_MyNewSP_Param : sp_BaseParam
{
    public long? MyFilterId { get; set; }
}
```

---

## Method Contract Reference

### RentCarHelper (8 methods)

| Method | Signature | Input | Output | Doc File |
|--------|-----------|-------|--------|----------|
| GetRentalHourStartEnd | `static (double, double) GetRentalHourStartEnd(Vehicle_RentalSetting setting, DateTime fromDate)` | Vehicle rental setting + pickup date | (rentalHourStart, rentalHourEnd) tuple | helper-algorithms.md §1.2 |
| GetRentalDaySegments | `static RentCarSegmentModel[] GetRentalDaySegments(RentCarFormulaInputModel model)` | Formula input (service info + rent dates) | Array of Early/FullDay/Late segments | helper-algorithms.md §1.3 |
| GetEarlyHourDeliveryFeeV2 | `static async Task<RentCarSegment_PriceModel> GetEarlyHourDeliveryFeeV2(RentCarFormulaInputModel model, DateTime fromDate, DateTime toDate)` | Formula input + segment dates | Segment price with IsFullDay/IsHalfDay flags | helper-algorithms.md §1.4 |
| GetLateHourReturnFeeV2 | `static async Task<RentCarSegment_PriceModel> GetLateHourReturnFeeV2(RentCarFormulaInputModel model, DateTime fromDate, DateTime toDate)` | Formula input + segment dates | Segment price with IsFullDay/IsHalfDay flags | helper-algorithms.md §1.5 |
| GetOriginalPrice | `static async Task<RentCarOriginalPriceModel> GetOriginalPrice(RentCarFormulaInputModel model)` | Formula input | TotalOriginalPrice, RentalDayCount, Segments[] | helper-algorithms.md §1.6 |
| GetPriceByDay | `static async Task<RentCarPriceByDayModel> GetPriceByDay(RentCarFormulaInputModel model)` | Formula input | SubTotal, PriceByDay, PromotionMoney | helper-algorithms.md §1.7 |
| BuildPriceModel | `static void BuildPriceModel(Dict<int, WeekdayPrice> dictWeekday, Dict<string, decimal?> dictDate, RentCarSegment_PriceModel m, DateTime? date, decimal days)` | Price dictionaries + segment + date | Mutates segment with price info | helper-algorithms.md §1.8 |
| CalcRerturnDepositAmount | `(method in RentCarHelper)` | Order + cancel settings | RefundAmount_Renter, RefundAmount_Owner, RefundAmount_Service | pricing-calculation.md §14 |

### SearchingVehicleHelper (pipeline)

| Method | Signature | Input | Output | Doc File |
|--------|-----------|-------|--------|----------|
| GetRentalServices_SelfdriveCar_Version2Async | `static async Task<(RentalService_SelfdriveCarRental[] data, Dict<string, DistanceItemResultModel> dictDistance, string error)>` | Search params + category settings | Filtered vehicles + distances | helper-algorithms.md §2.1 |
| GetDictHostAddressAsync | `(internal)` | Lat/lng or province/district | Dict<long?, HostAddress> | helper-algorithms.md §2 Phase 1 |
| SearchRentalService_Filter | `(internal)` | 14 filter types | Filtered IQueryable | helper-algorithms.md §2 Phase 3 |
| CheckRentalServiceNotBusy | `(internal)` | Date range | Available vehicles only | helper-algorithms.md §2 Phase 4 |

### ConfigDeliveryFeeHelper (3 methods)

| Method | Signature | Input | Output | Doc File |
|--------|-----------|-------|--------|----------|
| CalculateDeliveryFee | `static async Task<RentalFeeResult> CalculateDeliveryFee(ConfigDeliveryFeeArrJsonModel[] configArr, decimal hours, DateTime fromDate, DateTime toDate, Vehicle_RentalSetting setting, RentCarServiceInfoModel serviceInfo, bool isEarlyDelivery)` | Config array + hours + setting | RentalFeeResult (Price, IsFullDay, IsHalfDay) | helper-algorithms.md §3.2 |
| BuildByHourResult | `(internal)` | Hours + hourly rate | Price = hours × rate × 1000 | helper-algorithms.md §3.3 |
| BuildByDayResult | `(internal)` | Day fraction + price lookup | Price from BuildPriceModel × fraction | helper-algorithms.md §3.4 |

### RentalServiceHelper (5 methods)

| Method | Signature | Input | Output | Doc File |
|--------|-----------|-------|--------|----------|
| GetEndTime | `static DateTime GetEndTime(DateTime? now, int? businessHourStart, int? businessHourEnd, int? UI_TimezoneOffset, int? hour)` | Current time + business hours + timeout hours | Deadline DateTime (UTC) | helper-algorithms.md §4.1 |
| BuildDictPriceByDate | `static Dict<string, decimal?> BuildDictPriceByDate(DateTime? fromDate, DateTime? toDate, ServiceItem_DateRentalPriceInfo[] priceByDates)` | Date range + price configs | Dict<"dd/MM/yyyy", price> | helper-algorithms.md §4.2 |
| GetCompletionFee | `(method)` | Service item / owner / category | CompletionFeePercentage (priority cascade) | helper-algorithms.md §4.4 |
| GetPlanProfitAndOwnerRemain | `(method)` | SubTotal, fees, deposit | PlanProfitAmount, OwnerRemainAmount | helper-algorithms.md §4.4 |
| GetSettingJson | `(method)` | Category ID | RentalServiceCategorySettingJsonModel | helper-algorithms.md §4.3 |

### WalletHelper (6 methods)

| Method | Signature | Input | Output | Doc File |
|--------|-----------|-------|--------|----------|
| GetUserWalletPayment | `static Wallet GetUserWalletPayment(Guid? userGUID)` | User GUID | Wallet entity (PAYMENT type) | helper-algorithms.md §6.2 |
| GetUserWallets | `static Dict<Guid?, Wallet> GetUserWallets(Guid?[] userGUIDs, string walletTypeCode)` | User GUIDs + wallet type | Dict of wallets (auto-creates missing) | helper-algorithms.md §6.3 |
| FundingWallet | `static string FundingWallet(WalletFundingModel[] models, string walletTypeCode)` | Funding models | Error string (empty = success) | helper-algorithms.md §6.5 |
| TransferWallet | `static string TransferWallet(WalletTransferModel[] models)` | Transfer models | Error string | helper-algorithms.md §6.6 |
| CreateWallet | `static WalletModel CreateWallet(Guid? userGUID, long? walletId, out string sMessage)` | User GUID | WalletModel + error | helper-algorithms.md §6.7 |
| GetUserWalletSetting | `(method)` | — | TopUpBankCode, WithdrawFee, MinMoneyCanWithdraw | helper-algorithms.md §6.4 |

### PushNotificationHelper (3 methods)

| Method | Signature | Input | Output | Doc File |
|--------|-----------|-------|--------|----------|
| PushNotify_SQLSP | `static async Task PushNotify_SQLSP()` | — (reads from SP) | Sends FCM notifications, writes history | helper-algorithms.md §5.2 |
| GetPushNotifyItem | `(internal)` | Notification data | PushNotifyItem with TTL check | helper-algorithms.md §5.3 |
| SendPushNotification_REST_HTTP_V1Async | `(internal)` | FCM payload | HTTP response from FCM | helper-algorithms.md §5.5 |

---

## Entity → Service → Controller → API Mapping

### Core Business Entities

| Entity | Service | Controller | API Route |
|--------|---------|------------|-----------|
| `RentalService_SelfdriveCarRental` | `RentalService_SelfdriveCarRentalBaseService` | `RentalServiceController` | `/api/v1/RentalService/*` |
| `RentalService_SelfdriveCarRental` | `SearchingRentalServiceItemService` | `SearchingRentalServiceController` | `/api/v1/SearchingRentalService/*` |
| `Order` | `OrderService` / `OrderService_UserAction` | `Order_ListView_RentCarController` | `/api/v1/Order_ListView_RentCar/*` |
| `UserLogin` | `UserLoginService` | `UserController` | `/api/v1/User/*` |
| `Vehicle` | `VehicleService` | `VehicleController` | `/api/v1/Vehicle/*` |
| `Wallet` | `WalletActionService` (EWallet module) | `WalletActionController` | `/api/v1/WalletAction/*` |
| `NotificationMessage` | `NotificationService` | `NotificationController` | `/api/v1/Notification/*` |
| `EzyWeb_BlogPost` | `SolidEzyWeb_BlogPostService` (CMS) | `BlogPostController` | `/api/v1/BlogPost/*` |

### Config Entities (50+ controllers)

| Entity Pattern | Controller Pattern | API Route Pattern |
|---------------|-------------------|-------------------|
| `Config{Name}` | `Config{Name}Controller` | `/api/v1/Config{Name}/*` |
| `ServiceItem_{Type}` | `ServiceItem_{Type}Controller` | `/api/v1/ServiceItem_{Type}/*` |
| `Vehicle_{Type}` | `Vehicle_{Type}Controller` | `/api/v1/Vehicle_{Type}/*` |

### Key API Endpoints

| Operation | Method | Endpoint | Service Method |
|-----------|--------|----------|---------------|
| Search vehicles | POST | `/api/v1/SearchingRentalService/List` | `GetRentalServices_SelfdriveCar_Version2Async` |
| Vehicle detail | POST | `/api/v1/SearchingRentalService/Detail` | `GetDetailAsync` |
| Check booking | POST | `/api/v1/SearchingRentalService/CheckBeforeUpdateBookingInfo` | `CheckCanBookRentalService` |
| Create booking | POST | `/api/v1/RentalService/Booking` | `BookRentalService` |
| Owner confirm | POST | `/api/v1/RentalService/OrderConfirm` | `OwnerConfirm` |
| Renter pay | POST | `/api/v1/RentalService/OrderPay` | `RenterConfirmHasPay` |
| Begin trip | POST | `/api/v1/Order_ListView_RentCar/Begin` | `Begin` |
| End trip | POST | `/api/v1/Order_ListView_RentCar/End` | `End` |
| Renter cancel | POST | `/api/v1/Order_ListView_RentCar/RenterCancel` | `RenterCancel` |
| Owner cancel | POST | `/api/v1/Order_ListView_RentCar/OwnerCancel` | `OwnerCancel` |
| Wallet withdraw | POST | `/api/v1/WalletAction/Withdraw` | `WithdrawRequest` |

---

## Configuration Cascade

Settings are resolved with priority (first non-null wins):

```
Level 1: SystemConfig table (global defaults)
    ├── RENTAL_SERVICE_SETTING → JSON containing defaults
    ├── USER_WALLET_SETTING → wallet defaults
    └── Other system-wide configs

Level 2: RentalServiceCategorySettingJsonModel (per category)
    ├── DepositPercent, PenaltyDepositPercent
    ├── CompletionFeePercentage, ServiceFee
    ├── InsurancePercentPerDay
    ├── CancelOrderSetting { FullRefundWithinMinutes, NoRefundGreaterThanDays }
    ├── BusinessHourStart, BusinessHourEnd
    └── HourOwner2Confirm, HourCustomer2Deposit

Level 3: Vehicle_RentalSetting (per vehicle)
    ├── RentalHourStart, RentalHourEnd, RentalHour_Using24Hours
    ├── EarlyHourDeliveryFee, LateHourReturnFee
    ├── HaveMultidayRentalDiscount
    ├── HaveDeliverySurcharge, MaximumDeliveryMileage
    ├── HaveInsurance, HaveSecurity
    └── DepositPercent, DepositAmount (overrides)

Level 4: ServiceItem-level (per rental listing)
    ├── ServiceItem_DateRentalPrice → price overrides by date range
    ├── ServiceItem_WeekdayRentalPrice → price by day of week
    ├── ServiceItem_DateBusyRentalSchedule → busy dates
    ├── ServiceItem_WeekdaysBusyRentalSchedule → busy weekdays
    └── VehicleMultidayRentalDiscount_Detail → discount tiers
```

**Resolution example for CompletionFeePercentage:**
```
1. Check ServiceItem.CompletionFeePercentage → if not null, use it
2. Check Owner.CompletionFeePercentage → if not null, use it
3. Check RentalServiceCategorySettingJsonModel.CompletionFeePercentage → if not null, use it
4. Use ConfigCompletionFee system default
```

---

*See also: [llms.txt](./llms.txt) | [base-service-pattern.md](./01_ARCHITECTURE/base-service-pattern.md) | [helper-algorithms.md](./01_ARCHITECTURE/helper-algorithms.md)*
