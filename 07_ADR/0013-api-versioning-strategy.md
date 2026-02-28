# ADR-0013: Chiến lược Versioning API (v1 / v2)

## Status

Accepted

## Context

Sigo API phục vụ nhiều clients (iOS app, Android app, web admin, third-party partners). Khi API cần thay đổi breaking (response structure, field rename, behavior change), cần tránh break existing clients đang dùng phiên bản cũ.

Ví dụ thực tế: `User/HomePage_App` có cả `/api/v1/` và `/api/v2/` — phiên bản v2 trả về data structure khác.

## Decision Drivers

* **Backward compatibility** — clients cũ không bị break khi API cập nhật
* **Progressive migration** — clients có thể migrate sang v2 theo thời gian
* **Clear signaling** — developer biết rõ khi nào có breaking change
* **Simple implementation** — không cần framework phức tạp
* **Minimal duplication** — tránh copy/paste code giữa các versions

## Considered Options

### Option 1: URL Path Versioning (`/api/v1/`, `/api/v2/`)
```
GET /api/v1/User/HomePage_App
GET /api/v2/User/HomePage_App
```
- **Pros:**
  - Rõ ràng, dễ nhận biết trong URL
  - Dễ log, debug, proxy
  - Bookmark và cache dễ
  - Phổ biến nhất
- **Cons:**
  - URL "không clean" theo REST purists
  - Nếu nhiều resources thì nhiều routes

### Option 2: Query String Versioning
```
GET /api/User/HomePage_App?version=1
GET /api/User/HomePage_App?version=2
```
- **Pros:** URL sạch hơn
- **Cons:** Dễ quên, cache phức tạp hơn, ít visible

### Option 3: Header Versioning
```
GET /api/User/HomePage_App
API-Version: 2
```
- **Pros:** URL clean, RESTful
- **Cons:** Invisible trong URL, khó test trong browser, phức tạp cho proxy/CDN

### Option 4: Accept Header / Content Negotiation
```
Accept: application/vnd.sigo.v2+json
```
- **Pros:** RESTful chuẩn
- **Cons:** Phức tạp cho developers, khó debug

## Decision

Chúng ta sẽ dùng **URL Path Versioning** với prefix `/api/v1/` và `/api/v2/`. Version mặc định là v1. Chỉ tạo v2 khi có breaking change thực sự.

## Rationale

1. **Visibility:** Developers nhìn URL biết ngay đang dùng version nào
2. **Caching:** CDN và proxy có thể cache theo URL path
3. **Logging:** Access logs tự động phân biệt v1/v2 requests
4. **Simplicity:** Không cần middleware parse header, chỉ cần route prefix
5. **Mobile compatibility:** Mobile apps dễ hardcode v1/v2 URL

## Quy tắc khi tạo v2

Chỉ tạo v2 endpoint khi:
- Thay đổi cấu trúc response (field bị xóa, đổi tên, đổi type)
- Thay đổi request format
- Thay đổi behavior (logic thay đổi gây kết quả khác)

Không tạo v2 khi:
- Chỉ thêm field mới vào response (backward compatible)
- Fix bug giữ nguyên contract
- Performance optimization

## Ví dụ thực tế

```csharp
// v1 - response cũ
[Route("api/v1/[controller]")]
public class UserController : ControllerBase
{
    [HttpPost("HomePage_App")]
    public async Task<IActionResult> HomePage_App_v1([FromBody] HomePageRequest request)
    {
        // Returns legacy structure
    }
}

// v2 - response mới, structure khác
[Route("api/v2/[controller]")]
public class UserController_v2 : ControllerBase
{
    [HttpPost("HomePage_App")]
    public async Task<IActionResult> HomePage_App_v2([FromBody] HomePageRequest request)
    {
        // Returns new structure
    }
}
```

## Deprecation Process

1. Announce deprecation của v1 endpoint
2. Cho migration period (minimum 3 tháng)
3. Log warnings khi v1 được gọi
4. Remove v1 chỉ khi không còn client nào dùng

## Consequences

### Tích cực
- Clients cũ không bị break khi API thay đổi
- Mobile apps cần update chậm (store review) vẫn hoạt động
- API changelog rõ ràng theo version
- Dễ A/B test giữa v1 và v2

### Tiêu cực
- Maintain nhiều versions cùng lúc tốn effort
- Code duplication nếu v1 và v2 có nhiều điểm chung
- Phải document rõ deprecated endpoints

### Rủi ro
- Version proliferation (v1, v2, v3... vô tận)
- **Giảm thiểu:** Policy: max 2 active versions, aggressively deprecate
- Logic drift giữa v1 và v2
- **Giảm thiểu:** Chia sẻ service layer, chỉ controller và DTO khác nhau

## Related Decisions

- ADR-0014: Standardized Response Wrapper (EzyResultObject áp dụng cho cả v1/v2)
- ADR-0002: Layered Architecture (service layer share giữa v1/v2)

---

*Ngày tạo: 2022-01-01*
