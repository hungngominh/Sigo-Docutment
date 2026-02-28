# Base Service Pattern — CRUD Framework

## Mục lục
- [Tổng quan](#tổng-quan)
- [Response Wrapper — EzyResultObject](#response-wrapper--ezyresultobject)
- [Pagination — EzyDataSourceResult](#pagination--ezydatasourceresult)
- [Parameter Models](#parameter-models)
- [Interface Hierarchy](#interface-hierarchy)
- [EFBaseCategoryService — Base Class](#efbasecategoryservice--base-class)
- [CRUD Pipeline](#crud-pipeline)
- [Entity Conversion](#entity-conversion)
- [Error Handling Patterns](#error-handling-patterns)
- [Repository Pattern](#repository-pattern)
- [Controller Pattern](#controller-pattern)
- [Tạo Service Mới — Step by Step](#tạo-service-mới--step-by-step)

---

## Tổng quan

Toàn bộ business services kế thừa từ `EFBaseCategoryService<T, TModel, TModelList, TParam, TOption>` — một generic base class cung cấp sẵn CRUD operations, pagination, query building, và entity conversion.

**Pipeline tổng quát:**

```
[Controller] → ParamModel → [Service.GetListAsync] → IQueryable<Entity> → Entity[] → Model[] → [Response]
                                                        ↑ BuildQuery        ↑ ConvertToModel    ↑ EzyResultObject
```

---

## Response Wrapper — EzyResultObject<T>

Mọi API response đều wrap trong `EzyResultObject<T>`:

```csharp
public class EzyResultObject<T>
{
    public int Status { get; set; }      // 1=Success, 0=Error, -1=ServerError, -2=Unauthorized
    public string Message { get; set; }  // Human-readable message
    public T Data { get; set; }          // Generic payload
}
```

### Result Codes

```csharp
public enum ResultCode
{
    Success = 1,           // Thành công
    Error = 0,             // Lỗi nghiệp vụ
    Ok = 200,              // HTTP OK
    BadRequest = 400,      // Input không hợp lệ
    Unauthorized = 401,    // Chưa đăng nhập
    Unauthorized_v2 = 402, // Hết quyền truy cập
    NotFound = 404,        // Không tìm thấy
    ServerError = 500,     // Lỗi server
}
```

### Response format JSON

**Thành công:**
```json
{
  "Status": 1,
  "Message": "Thành công",
  "Data": { ... }
}
```

**Lỗi nghiệp vụ:**
```json
{
  "Status": 0,
  "Message": "[MsgCode:0] Dữ liệu không hợp lệ",
  "Data": null
}
```

**Lỗi server:**
```json
{
  "Status": -1,
  "Message": "[MsgCode:500] Lỗi xảy ra trong quá trình xử lý",
  "Data": null
}
```

**Message format:** `[MsgCode:{ResultCode}] {message text}`

---

## Pagination — EzyDataSourceResult<T>

Container cho paginated list data:

```csharp
public class EzyDataSourceResult<T>
{
    public T[] Data { get; set; }           // Mảng items trong trang hiện tại
    public int TotalCount { get; set; }     // Tổng records (bỏ qua pagination)
    public object Errors { get; set; }      // Error message nếu có
    // Metadata bổ sung cho UI:
    // Columns config, chart data, totals...
}
```

**JSON response:**
```json
{
  "Status": 1,
  "Data": {
    "Data": [
      { "Id": "1", "Name": "Item 1" },
      { "Id": "2", "Name": "Item 2" }
    ],
    "TotalCount": 50,
    "Errors": null
  }
}
```

---

## Parameter Models

### Base Classes

```csharp
// Level 1: Base param
public class EzyBaseParamModel
{
    public int? UI_TimezoneOffset { get; set; }  // Timezone offset (phút)
}

// Level 2: Paging param (phổ biến nhất)
public class EzyBasePagingParamModel : EzyBaseParamModel, IEzyBasePagingParam
{
    public string AppName { get; set; }          // Application identifier
    public int Page { get; set; }                // 1-based page number
    public int PageSize { get; set; }            // Items per page
    public string TextSearch { get; set; }       // Global search term
    public EzySort[] Sorts { get; set; }         // Sort configuration

    // Default timezone = Vietnam (+7)
    public override int? UI_TimezoneOffset
    {
        get { return base.UI_TimezoneOffset ?? UI_Timezone_OffsetKeys.VN; }
        set => base.UI_TimezoneOffset = value;
    }
}
```

### ListParamModel<TParam> (Internal wrapper)

```csharp
public class ListParamModel<TParam>
{
    public TParam QueryStringModel { get; set; }              // Actual query parameters
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string TextSearch { get; set; }
    public EzySort[] Sorts { get; set; }
    public Func<TParam, TParam> Func_ModifyValueQueryModel;  // Callback to modify query params
}
```

### Concrete Example

```csharp
public class ConfigAddressParamModel : EzyBasePagingParamModel
{
    public string Type { get; set; }        // Domain filter
    public string ParentId { get; set; }    // Hierarchy filter
    public string UpdateType { get; set; }  // Operation type
}
```

---

## Interface Hierarchy

```
IEzyCategoryService<TModel, TModelList, TParam, TOption>     (Ezy.Module.BaseService DLL)
  └── IBaseCategoryService<TModel, TModelList, TParam, TOption>  (Project-specific)
        └── IConcrete Service (e.g., IEmailTemplateService)
```

### IBaseCategoryService Definition

```csharp
public interface IBaseCategoryService<TModel, TModelList, TParam, TOption>
    : IEzyCategoryService<TModel, TModelList, TParam, TOption>
    where TParam : class
{
    string UIAppName { get; set; }

    // Async methods (preferred)
    Task<EzyDataSourceResult<TModelList>> GetListAsync(ListParamModel<TParam> param);
    Task<(TModelList[] data, string error)> GetListSimpleAsync(TParam param);

    // Sync wrapper
    TModelList[] GetListSimple(TParam param, out string sMessage);
}
```

---

## EFBaseCategoryService — Base Class

### Generic Parameters

```csharp
public partial class EFBaseCategoryService<T, TModel, TModelList, TParam, TOption>
    : EFEzyBaseCategoryService<T, TModel, TModelList, TParam, TOption>
    where T : class, IEFBaseEntity, new()            // Database entity
    where TModel : EzyBaseModel, new()               // Detail/edit model
    where TParam : EzyBaseParamModel, new()          // Filter parameters
    where TModelList : EzyBaseModel, new()           // List row model
    where TOption : BaseCategoryOptionModel, new()   // UI options
```

### Key Properties

```csharp
public EFBaseCategoryService()
{
    CanExportToExcel = true;           // Excel export enabled by default
    CanExportExcelToGS = false;        // Google Sheets disabled
    ProjectIdIsNotRequired = true;     // Multi-tenant handling
}

// Properties set trong InitService():
public string ScreenCode { get; set; }     // Permission check key
public string UIAppName { get; set; }      // Source application
public string LogBy { get; set; }          // Audit logging user

// Delegates cho query building:
public Func<TParam, IQueryable<T>, Task<IQueryable<T>>> func_BuildQueryAsync;
public Func<TParam, IQueryable<T>, Task<IQueryable<T>>> func_BuildQuery_RequiredAsync;
public Func<TParam, IQueryable<T>, IQueryable<T>> func_BuildQuery;
public Func<TParam, IQueryable<T>, IQueryable<T>> func_BuildQuery_Required;
public Func<IQueryable<T>, IQueryable<T>> func_BuildQueryOrder;

// Delegate cho model conversion:
public Func<T, TModelList> Func_ConvertToListModel;
```

### Partial File Structure

`EFBaseCategoryService` split thành nhiều partial files:

| File | Vai trò |
|------|---------|
| `EFBaseCategoryService.cs` | Core properties, entity conversion, base methods |
| `EFBaseCategoryService_GetList.cs` | Sync GetList operations |
| `EFBaseCategoryService_GetListAsync.cs` | Async GetList (primary) |
| `EFBaseCategoryService_DateTime.cs` | DateTime/timezone utilities |

---

## CRUD Pipeline

### GetListAsync — Chi tiết

```csharp
public virtual async Task<EzyDataSourceResult<TModelList>> GetListAsync(ListParamModel<TParam> param)
{
    var result = new EzyDataSourceResult<TModelList>() { Data = Array.Empty<TModelList>() };
    string sError = "";

    try
    {
        // 1. Extract & validate parameters
        var queryParam = GetParamModel(param);
        InitServiceByParam_ScreenGUID(queryParam);

        // 2. Set default values + permission check
        queryParam = GetList_SetDefautValueForParam_WithCheck(queryParam, out sDefaultMsg);
        InitServiceByParam_Base(queryParam);
        long? sProject = GetProjectIdFromParam(queryParam);
        sError = CheckAccessPermissionAndLog_Role(sProject, "LIST");

        if (string.IsNullOrEmpty(sError))
        {
            // 3. Pre-processing hook (override point)
            await ActionRunBeforeList_BaseAsync(queryParam);

            // 4. Get paginated data from database
            //    → BuildQuery → Apply pagination → Execute query → ConvertToListModel
            result = await GetList_GetDataAsync(param);

            // 5. Post-process results (override point)
            if (result.Data?.Any() == true)
                await RefineListData_CoreAsync(result);

            // 6. Custom transformations (override point)
            result.Data = await GetList_FuncCustomListBaseAsync(result.Data, queryParam);

            // 7. UI sort/order
            if (result.Data?.Any() == true)
                result.Data = OrderbyUIJson(result.Data);

            // 8. Attach metadata
            GetList_GetExtraColumns(result, queryParam);     // Column config
            GetList_GetExtraChartData(result, queryParam);   // Chart data
            GetList_GetDataTotal(result, queryParam);        // Totals row
        }
    }
    catch (Exception ex)
    {
        SaveLogException(ex, "GetList", param);
        sError = Exception_GetMessage(ex);
    }

    if (!string.IsNullOrEmpty(sError))
        result.Errors = sError;

    return result;
}
```

### Override Points cho GetList

| Method | Mục đích | Default |
|--------|----------|---------|
| `ActionRunBeforeListAsync(param)` | Pre-processing trước query | No-op |
| `GetList_BuildQueryBaseAsync(query, param, paramQuery)` | Custom query filter | Apply func_BuildQuery delegates |
| `RefineListDataAsync(data)` | Post-process sau convert | No-op |
| `GetList_FuncCustomListAsync(data, param)` | Transform list data | Return unchanged |
| `SetOptionsValue(option, param)` | Populate UI options/dropdowns | No-op |

### GetListSimple — Simplified Wrapper

```csharp
// Sync version
public TModelList[] GetListSimple(TParam param, out string sMessage)
{
    var (data, error) = GetListSimpleAsync(param).GetAwaiter().GetResult();
    sMessage = error;
    return data;
}

// Async version — skip metadata, get data only
public virtual async Task<(TModelList[] data, string error)> GetListSimpleAsync(TParam param)
{
    GetList_DontGetColumnsConfig = true;  // Skip column metadata
    var listParam = GetParamListModel(param);
    var listResult = await GetListAsync(listParam);
    return (listResult?.Data?.ToArray(), listResult?.Errors?.ToString());
}
```

### Insert / Update / Delete

Built-in từ base class `EFEzyBaseCategoryService`:

```csharp
// Insert
public virtual TModel Insert(TModel model, out string sMessage)
{
    sMessage = "";
    using (var repo = InitRepository())
    {
        var entity = new T();
        entity = ConvertToEntity(entity, model, out bool bHasChange);
        sMessage = repo.Insert(entity);
        return ConvertToModel(null, entity);
    }
}

// Update
public virtual TModel Update(TModel model, out string sMessage)
{
    sMessage = "";
    using (var repo = InitRepository())
    {
        var entity = repo.GetQueryable().FirstOrDefault(c => c.Id == id);
        entity = ConvertToEntity(entity, model, out bool bHasChange);
        if (bHasChange)
            sMessage = repo.Update(entity);
        return ConvertToModel(null, entity);
    }
}

// UpdateFields — partial update
public virtual TModel UpdateFields(EzyObjectFieldValues model, out string sMessage);

// UpdateMulti — bulk update
public TModel[] UpdateMulti(TModel[] models, out string sMessage);

// Delete — soft delete (IsDeleted = true)
public virtual bool Delete(string id, out string sMessage);
```

---

## Entity Conversion

### ConvertToEntity (Model → Entity)

```csharp
public override T ConvertToEntity(T e, TModel m, out bool bHaschange)
{
    bHaschange = false;
    if (m == null) return e;

    e ??= new T();
    e.Id = ID_ConvertStringToId(m.Id);

    if (e.Id == 0)
    {
        // INSERT: copy all properties
        bHaschange = true;
        ObjectHelper.CopyPropertiesIgnoreNull(m, e, false, null, null);
    }
    else
    {
        // UPDATE: only changed properties
        e = ConvertToEntity4Update(e, m, out bHaschange);
    }

    // Domain-specific conversions (override point)
    var bMoreChange = ConvertToEntity_SetMore(e, m);
    if (bMoreChange) bHaschange = true;

    // Optimistic locking check
    if (e.Id > 0 && bHaschange)
    {
        var rhlModel = m as ProjectNoLogBaseModel;
        if (rhlModel?.IM_ReturnTime > 0)
        {
            var bExpired = EntityCheckExpired_DB_UI(e, rhlModel);
            if (bExpired)
                throw new Exception("entity_expired");
        }
    }

    return e;
}
```

**Override points:**

| Method | Khi nào override |
|--------|-----------------|
| `ConvertToEntity4Update(e, m, out bHasChange)` | Custom update logic |
| `ConvertToEntity_SetMore(e, m)` | Thêm field mapping ngoài auto-copy |

### ConvertToModel (Entity → Model)

```csharp
public override TModel ConvertToModel(TModel m, T e)
{
    m = base.ConvertToModel(m, e);  // Auto-copy matching properties

    // Set form state tracking ID
    if (m is EzyBaseConfigModel baseModel)
        baseModel.IM_Id = m.Id;

    return m;
}
```

---

## Error Handling Patterns

### Pattern 1: `out string sMessage` (Sync)

```csharp
public TResult OperationName(TParam param, out string sMessage)
{
    TResult result = null;
    sMessage = string.Empty;

    try
    {
        // Business logic...
        if (hasError)
            sMessage = "Mô tả lỗi";
        else
            result = successValue;
    }
    catch (Exception ex)
    {
        SaveLogException(ex, "OperationName", param);
        sMessage = Exception_GetMessage(ex);
    }

    return result;
}
```

### Pattern 2: `Task<(T, string)>` (Async)

```csharp
public async Task<(TModel result, string error)> OperationAsync(TParam param)
{
    TModel result = null;
    string sMessage = string.Empty;

    try
    {
        // Async business logic...
        result = await DoWorkAsync(param);
    }
    catch (Exception ex)
    {
        SaveLogException(ex, "OperationAsync", param);
        sMessage = Exception_GetMessage(ex);
    }

    return (result, sMessage);
}
```

### Controller DoJob Pattern

```csharp
// BaseCategoryController wraps service calls:
public EzyResultObject<T> DoJob<T>(
    object oParam,
    Func<string> fnc_CheckInValid,      // Input validation
    Func<EzyResultObject<T>, T> func_Run, // Business logic
    string sSuccessfulMsg)
{
    // 1. Validate input via fnc_CheckInValid
    // 2. Execute func_Run with try/catch
    // 3. Wrap in EzyResultObject<T>
    // 4. Set Status + Message
}
```

---

## Repository Pattern

```csharp
// Khởi tạo repository
using (var repo = InitRepository())
{
    // Query
    IQueryable<T> query = repo.GetQueryable();
    IQueryable<T> queryAll = repo.GetQueryableIncludeDeleted();

    // CRUD
    string msg = repo.Insert(entity);
    string msg = repo.Update(entity, bulkMode: true);
    string msg = await repo.UpdateAsync(entity);
    string msg = repo.Delete(entity);

    // Advanced
    var entity = await repo.GetQueryable()
        .Where(c => c.Id == id)
        .FirstOrDefaultAsync();
}
```

---

## Controller Pattern

### Standard CRUD Controller

```csharp
[Route("api/v1/ConfigAddress")]
[Authorize]
public class ConfigAddressController : BaseCategoryController<
    ConfigAddressModel,
    ConfigAddressModel,           // List model = Detail model
    ConfigAddressParamModel,
    ConfigAddressOptionModel>
{
    [Route("List")]
    [HttpPost]
    public virtual EzyResultObject<EzyDataSourceResult<ConfigAddressModel>> List(ConfigAddressParamModel model)
    {
        return base.List_Simple(model);
    }

    [Route("Update")]
    [HttpPost]
    public EzyResultObject<ConfigAddressModel> Update(ConfigAddressModel model)
    {
        return base.Update_Simple(model);
    }

    [Route("Detail")]
    [HttpPost]
    public EzyResultObject<ConfigAddressModel> Detail(ConfigAddressParamModel model)
    {
        return base.Detail_Simple(model);
    }

    [Route("Delete")]
    [HttpPost]
    public EzyResultObject<bool> Delete(ConfigAddressParamModel model)
    {
        return base.Delete_Simple(model);
    }
}
```

---

## Base Model Classes

### EzyBaseModel

```csharp
public class EzyBaseModel
{
    public string Id { get; set; }  // String representation of entity ID (long → string)
}
```

### EzyBaseConfigModel

```csharp
public class EzyBaseConfigModel : EzyBaseModel
{
    public string IM_Id { get; set; }         // Internal model ID (form state tracking)
    public string Name { get; set; }
    public string Code { get; set; }
    public string Note { get; set; }
    public decimal OrderNo { get; set; }      // Sort order
    public bool IsDisable { get; set; }       // Soft disable flag
    public string IconUrl { get; set; }
    public string ColorCode { get; set; }
    public string ColorCodeBG { get; set; }
    public string Description { get; set; }
}
```

### IEFBaseEntity (Entity Interface)

```csharp
public interface IEFBaseEntity
{
    long Id { get; set; }                     // PK auto-increment
    DateTime? Log_CreatedDate { get; set; }   // Auto-set on insert
    string Log_CreatedBy { get; set; }        // Auto-set on insert
    DateTime? Log_UpdatedDate { get; set; }   // Auto-set on update
    string Log_UpdatedBy { get; set; }        // Auto-set on update
    bool IsDeleted { get; set; }              // Soft delete flag
    object GetId();
}
```

---

## Timezone Handling

```csharp
// Default timezone: Vietnam (+7)
public static int VN = -420;  // -7 * 60 minutes

// Get current time in client timezone
public DateTime DateTime_Now_Client()
{
    return DateTime.UtcNow.AddMinutes(-(UI_TimezoneOffset ?? UI_Timezone_OffsetKeys.VN));
}

// Convert UTC to client datetime
public DateTime? DateTime_To_ClientDateTime(double? epochMilli)
{
    if (epochMilli != null)
        return DateTimeUTC_ToServer(epochMilli).Value
            .AddMinutes(-(UI_TimezoneOffset ?? UI_Timezone_OffsetKeys.VN));
    return null;
}

// Display format: dd/MM/yyyy HH:mm
public string ToStringClientDateTime(DateTime? dateUTC)
{
    return dateUTC?.AddMinutes(-(UI_TimezoneOffset ?? UI_Timezone_OffsetKeys.VN))
        .ToString("dd/MM/yyyy HH:mm");
}
```

---

## Tạo Service Mới — Step by Step

Để tạo một API CRUD mới, cần tạo các file sau:

### 1. Entity (`Core/Data/BusinessData/`)

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

    // Domain-specific properties
    public string Name { get; set; }
    public string Code { get; set; }
}
```

### 2. Register DbSet (`Core/Data/*/BusinessEntities.cs`)

```csharp
public DbSet<MyEntity> MyEntity { get; set; }
```

### 3. Model (`DataShared/Models/`)

```csharp
public class MyEntityModel : EzyBaseConfigModel
{
    // Domain-specific fields
}

public class MyEntityParamModel : EzyBasePagingParamModel
{
    // Filter fields
}

public class MyEntityOptionModel : BaseCategoryOptionModel
{
    // Dropdown/option data
}
```

### 4. Service Interface (`Service/Services/`)

```csharp
public interface IMyEntityService
    : IBaseCategoryService<MyEntityModel, MyEntityModel, MyEntityParamModel, MyEntityOptionModel>
{
    // Custom methods beyond CRUD
}
```

### 5. Service Implementation (`Service/Services/`)

```csharp
public class MyEntityService
    : EFBaseCategoryService<MyEntity, MyEntityModel, MyEntityModel, MyEntityParamModel, MyEntityOptionModel>
    , IMyEntityService, ISQLFrameWork
{
    public MyEntityService()
    {
        InitService();
    }

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

### 6. Controller (`WebAPI/Controllers/`)

```csharp
[Route("api/v1/MyEntity")]
[Authorize]
public class MyEntityController : BaseCategoryController<
    MyEntityModel, MyEntityModel, MyEntityParamModel, MyEntityOptionModel>
{
    [Route("List")]
    [HttpPost]
    public EzyResultObject<EzyDataSourceResult<MyEntityModel>> List(MyEntityParamModel model)
        => base.List_Simple(model);

    [Route("Detail")]
    [HttpPost]
    public EzyResultObject<MyEntityModel> Detail(MyEntityParamModel model)
        => base.Detail_Simple(model);

    [Route("Update")]
    [HttpPost]
    public EzyResultObject<MyEntityModel> Update(MyEntityModel model)
        => base.Update_Simple(model);

    [Route("Delete")]
    [HttpPost]
    public EzyResultObject<bool> Delete(MyEntityParamModel model)
        => base.Delete_Simple(model);
}
```

### 7. Cache (Optional) (`Core/DataInfo/Cached/`)

```csharp
public partial class CachedDataManagement
{
    public static void MyEntities_Refresh() { /* reload from DB */ }
    public static MyEntityInfo[] MyEntities { get; }
    public static Dictionary<string, MyEntityInfo> MyEntity_Dic { get; }
    public static MyEntityInfo MyEntity_Get_Instance_Id(object sId) { /* lookup */ }
}
```

> **ISQLFrameWork marker interface:** Service implement `ISQLFrameWork` sẽ được framework tự động phát hiện và register vào service container khi startup.

---

## AI Code Gen — Templates

> Các template dưới đây dùng cho AI code generation. Copy prompt + context vào AI tool để scaffold code mới.

### Template 1: Tạo CRUD Service hoàn chỉnh

**Prompt:**
```
Tạo CRUD service cho entity "{EntityName}" trong AllianceMiddleman.
- Entity fields: {liệt kê fields}
- Custom filters: {liệt kê filters}
- Có cache: {yes/no}
```

**Output expected (6 files):**

```
1. Entity:     Core/Data/BusinessData/{EntityName}.cs
   Pattern:    IEFBaseEntity (Id, IsDeleted, Log_*)

2. DbSet:     Core/Data/BusinessData/BusinessEntities.cs
   Add:        public DbSet<{EntityName}> {EntityName} { get; set; }

3. Models:    DataShared/Models/{EntityName}Model.cs
   Classes:    {EntityName}Model : EzyBaseConfigModel
               {EntityName}ParamModel : EzyBasePagingParamModel
               {EntityName}OptionModel : BaseCategoryOptionModel

4. Interface: Service/Services/I{EntityName}Service.cs
   Extends:    IBaseCategoryService<{EntityName}Model, {EntityName}Model,
               {EntityName}ParamModel, {EntityName}OptionModel>

5. Service:   Service/Services/{EntityName}Service.cs
   Extends:    EFBaseCategoryService<{EntityName}, {EntityName}Model,
               {EntityName}Model, {EntityName}ParamModel, {EntityName}OptionModel>
   Implements: I{EntityName}Service, ISQLFrameWork
   Must have:  InitService() with ScreenCode, func_BuildQuery, func_BuildQueryOrder

6. Controller: WebAPI/Controllers/{EntityName}Controller.cs
   Extends:    BaseCategoryController<...>
   Endpoints:  List, Detail, Update, Delete
   Route:      api/v1/{EntityName}
```

### Template 2: Tạo Background Engine

**Prompt:**
```
Tạo background engine "{EngineName}" cho AllianceMiddleman.
- Trigger: {timer/queue/event}
- Business logic: {mô tả}
- Service cần dùng: {service names}
```

**Output expected (2 files):**

```
1. Engine:    Service/Engines/{EngineName}Engine.cs
   Pattern:
   ─────────────────────────────────────────────
   public class {EngineName}Engine : ProjectEngineEntityAsync
   {
       protected override async Task DoJob()
       {
           var service = CreateServiceInstance<{ServiceName}>();
           // Business logic here
       }
   }
   ─────────────────────────────────────────────

2. Registration: Service/Engines/ProjectEngineAsyncHelper.cs
   Add method:
   ─────────────────────────────────────────────
   private void {EngineName}Engine_Register()
   {
       var engine = new {EngineName}Engine();
       RegisterEngine(engine, isAutoStartEngine);
   }
   ─────────────────────────────────────────────
   Call in StartAllEngines().
```

### Template 3: Custom Query Override

**Prompt:**
```
Service "{ServiceName}" cần custom query:
- Join thêm table: {table}
- Filter theo: {conditions}
- Sort theo: {fields}
```

**Output pattern:**

```csharp
// Override in InitService():
func_BuildQueryAsync = async (p, q) =>
{
    // Custom joins via .Include() or manual
    if (!string.IsNullOrEmpty(p.CustomFilter))
        q = q.Where(c => c.Field == p.CustomFilter);

    // Date range filter (common pattern)
    if (p.FromDate.HasValue)
        q = q.Where(c => c.Log_CreatedDate >= p.FromDate.Value);

    return q;
};

func_BuildQueryOrder = (q) => q.OrderByDescending(t => t.Log_CreatedDate);

// Override RefineListDataAsync for post-processing:
protected override async Task RefineListDataAsync(EzyDataSourceResult<TModelList> result)
{
    foreach (var item in result.Data)
    {
        // Enrich with cached data
        item.CategoryName = CachedDataManagement
            .ConfigCategory_Get_Instance_Id(item.CategoryId)?.Name;
    }
}
```

---

*Xem thêm: [project-structure.md](./project-structure.md) | [database-design.md](./database-design.md)*
