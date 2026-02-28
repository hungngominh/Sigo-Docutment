# ADR-0006: Dùng Redis làm Caching Layer

## Status

Accepted

## Context

Sigo API có các endpoints có tải cao và thường xuyên trả về dữ liệu ít thay đổi:
- Danh sách cấu hình (banks, order statuses, delivery fees...)
- Homepage data (web và mobile)
- Search results cho rental service
- Notification count (poll thường xuyên từ mobile)

Theo performance report:
- `SearchingRentalService/List` trung bình 1,781ms → cần cache
- `Notification/TotalNew` 80ms (đã tối ưu, cần duy trì)
- `Notification/List` 42ms (đã tối ưu với cache)

Cần giải pháp caching để giảm tải database và cải thiện response time.

## Decision Drivers

* **Giảm database load** — tránh query lặp lại cho dữ liệu tĩnh
* **Cải thiện response time** — đặc biệt cho homepage và search
* **Distributed cache** — hỗ trợ nhiều instance API (horizontal scaling)
* **TTL support** — tự động expire cache sau thời gian định trước
* **.NET integration** — dễ tích hợp với ASP.NET Core
* **High availability** — cache có thể recover nếu Redis restart

## Considered Options

### Option 1: Redis (StackExchange.Redis)
- **Pros:**
  - In-memory, cực nhanh (sub-millisecond)
  - Distributed → nhiều API instance cùng dùng chung cache
  - TTL native
  - Data structures phong phú (String, Hash, List, Set, Sorted Set)
  - Pub/Sub cho invalidation
  - StackExchange.Redis library mature và widely used
- **Cons:**
  - Thêm infrastructure component
  - Cần Redis server running
  - Cache invalidation phức tạp

### Option 2: In-Memory Cache (IMemoryCache)
- **Pros:**
  - Không cần external service
  - Đơn giản nhất
  - Zero network latency
- **Cons:**
  - Không distributed → nhiều instance sẽ có inconsistent cache
  - Mất cache khi restart app
  - Memory pressure trên app server

### Option 3: SQL Server Query Cache
- **Pros:**
  - Không cần thêm component
- **Cons:**
  - Không phải caching solution thực sự
  - Vẫn hit database
  - Không linh hoạt

### Option 4: Memcached
- **Pros:**
  - Đơn giản, nhanh
- **Cons:**
  - Chỉ hỗ trợ string values
  - Không có persistence
  - Ít features hơn Redis
  - .NET library kém active hơn

## Decision

Chúng ta sẽ dùng **Redis** với **StackExchange.Redis** client làm distributed caching layer chính.

## Rationale

1. **Distributed:** Nhiều instance AllianceMiddlemanWebAPI có thể chạy parallel và share cùng cache — quan trọng khi deploy nhiều container
2. **Performance:** Sub-millisecond latency giúp `Notification/List` đạt 42ms, `Notification/TotalNew` 80ms
3. **Flexibility:** Dùng Hash, String, Sorted Set tùy use case
4. **TTL:** Tự động invalidate sau thời gian định trước, không cần manual cleanup
5. **Session storage:** Ngoài cache, Redis có thể dùng cho distributed session nếu cần

## Cấu hình thực tế

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

**Cache-aside pattern:**
```csharp
// Đọc từ cache trước, nếu miss thì query DB và set cache
var cached = await _redis.StringGetAsync(key);
if (!cached.HasValue)
{
    var data = await _service.GetDataAsync();
    await _redis.StringSetAsync(key, Serialize(data), TimeSpan.FromMinutes(5));
    return data;
}
return Deserialize(cached);
```

## Consequences

### Tích cực
- Response time giảm đáng kể cho read-heavy endpoints
- Database load giảm, tránh N+1 queries cho config data
- Horizontal scaling dễ dàng vì cache distributed
- `Notification/List` 42ms, `Notification/TotalNew` 80ms — đạt target

### Tiêu cực
- Thêm infrastructure component cần monitor và maintain
- Cache invalidation phải cẩn thận, stale data là rủi ro
- Memory cost cho Redis server
- Thêm network hop cho mỗi cache read

### Rủi ro
- Cache stampede khi nhiều requests cùng miss cache
- **Giảm thiểu:** Singleflight pattern hoặc short TTL
- Stale data khi cache chưa expire nhưng DB đã update
- **Giảm thiểu:** Invalidate cache ngay khi write, TTL ngắn cho critical data
- Redis OOM (Out of Memory)
- **Giảm thiểu:** Cấu hình `maxmemory-policy allkeys-lru`

## Related Decisions

- ADR-0003: SQL Server (Redis bổ sung cho database)
- ADR-0005: EF Core (cache-aside kết hợp với EF queries)
- ADR-0009: Background Engines (một số engine đọc/ghi Redis)

---

*Ngày tạo: 2021-06-01*
