# ADR-0014: Response chuẩn hoá với EzyResultObject

## Status

Accepted

## Context

Sigo API có 50+ controllers với hàng trăm endpoints. Nếu mỗi endpoint tự trả về response structure riêng, clients (mobile, web) sẽ phải handle nhiều format khác nhau:
- Một số endpoint trả `{ data: ... }`
- Một số trả `{ result: ..., message: ... }`
- Error format không nhất quán

Điều này gây khó khăn cho:
- Mobile developers phải viết nhiều response parser
- API testing không có convention chung
- Logging và monitoring không uniform

## Decision Drivers

* **Consistency** — mọi API response có cùng structure
* **Client simplicity** — mobile dev chỉ cần một response parser
* **Error handling** — error messages chuẩn hoá
* **Status signaling** — phân biệt rõ success vs failure
* **Pagination** — response hỗ trợ paging info

## Considered Options

### Option 1: EzyResultObject wrapper (custom)
```json
{
  "Status": 1,
  "Message": "Thành công",
  "Data": { ... }
}
```
- **Pros:** Đơn giản, quen thuộc với team, kiểm soát hoàn toàn
- **Cons:** Không theo RFC/standard nào

### Option 2: RFC 7807 Problem Details (cho errors)
```json
{
  "type": "https://example.com/errors/not-found",
  "title": "Not Found",
  "status": 404,
  "detail": "User 123 not found"
}
```
- **Pros:** RFC standard, tooling support
- **Cons:** Chỉ cho errors, không cho success responses

### Option 3: HTTP Status Codes thuần
- 200 OK với body, 404 Not Found, 400 Bad Request...
- **Pros:** RESTful chuẩn
- **Cons:** Mobile dev khó parse, cần check status code + body, không nhất quán với existing code

### Option 4: JSON:API
```json
{
  "data": { "type": "user", "id": "1", "attributes": {...} },
  "errors": [...],
  "meta": {...}
}
```
- **Pros:** Chuẩn mở, tooling support
- **Cons:** Verbose, overkill cho use case hiện tại, learning curve

## Decision

Chúng ta sẽ dùng **EzyResultObject\<T\>** wrapper cho toàn bộ API responses.

## Rationale

1. **Simplicity:** Mobile developers chỉ cần một model parse duy nhất
2. **Status field:** `Status: 1` (success) vs `Status: 0` (failure) rõ ràng, không cần parse HTTP status code
3. **Vietnamese context:** `Message` field hỗ trợ tiếng Việt cho user-facing messages
4. **Existing convention:** Team đã quen với pattern này, không cần refactor
5. **Generic type:** `EzyResultObject<T>` type-safe với generic Data

## Response Structure

### Success Response
```json
{
  "Status": 1,
  "Message": "Thành công",
  "Data": {
    "UserId": "uuid-here",
    "Name": "Nguyễn Văn A",
    "Phone": "0901234567"
  }
}
```

### Success với List + Pagination
```json
{
  "Status": 1,
  "Message": "Thành công",
  "Data": {
    "Items": [...],
    "TotalCount": 100,
    "PageIndex": 1,
    "PageSize": 20
  }
}
```

### Error Response
```json
{
  "Status": 0,
  "Message": "Số điện thoại không hợp lệ",
  "Data": null
}
```

### Validation Error
```json
{
  "Status": 0,
  "Message": "Dữ liệu không hợp lệ",
  "Data": {
    "Errors": {
      "Phone": ["Số điện thoại không được để trống"],
      "Password": ["Mật khẩu phải có ít nhất 6 ký tự"]
    }
  }
}
```

## Status Code Convention

| Status | Ý nghĩa |
|--------|---------|
| `1` | Thành công |
| `0` | Lỗi nghiệp vụ |
| `-1` | Lỗi hệ thống |
| `-2` | Unauthorized |

## Controller Pattern

```csharp
[HttpPost("Login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var result = await _accountService.LoginAsync(request);

    if (!result.IsSuccess)
        return Ok(new EzyResultObject<object>
        {
            Status = 0,
            Message = result.ErrorMessage,
            Data = null
        });

    return Ok(new EzyResultObject<LoginResponse>
    {
        Status = 1,
        Message = "Đăng nhập thành công",
        Data = result.Data
    });
}
```

## Consequences

### Tích cực
- Mobile app chỉ cần một response parser chung
- `Status` field dễ check hơn HTTP status codes trên mobile
- `Message` field hỗ trợ tiếng Việt cho user-facing messages
- HTTP 200 luôn trả về, lỗi nghiệp vụ trong body → mobile không cần handle HTTP exceptions riêng

### Tiêu cực
- Không theo REST conventions (luôn 200, kể cả lỗi)
- Khó tích hợp với tools expect standard HTTP errors (APM, API Gateway)
- Không có standard schema → documentation phải mô tả thủ công

### Rủi ro
- Status code confusion: HTTP 200 nhưng `Status: 0` → monitors không detect errors
- **Giảm thiểu:** APM monitoring check `Status` field trong response body
- Inconsistent Status values nếu không enforce
- **Giảm thiểu:** Constants/enum cho Status codes, code review enforce

## Related Decisions

- ADR-0013: API Versioning (EzyResultObject dùng cho cả v1/v2)
- ADR-0002: Layered Architecture (service return domain objects, controller wrap trong EzyResultObject)

---

*Ngày tạo: 2021-01-01*
