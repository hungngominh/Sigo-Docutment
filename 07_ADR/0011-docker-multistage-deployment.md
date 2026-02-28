# ADR-0011: Triển khai bằng Docker Multi-stage Build

## Status

Accepted

## Context

Sigo API cần triển khai nhất quán trên nhiều môi trường:
- Development: Windows (local Visual Studio)
- Staging: Linux server
- Production: Linux server

Team gặp vấn đề "works on my machine" khi deploy lên server. Cần giải pháp đóng gói application cùng với dependencies của nó.

Yêu cầu:
- Image size nhỏ để deploy nhanh
- Build reproducible, không phụ thuộc môi trường máy build
- Hỗ trợ CI/CD pipeline (Jenkins)
- ASP.NET Core app chạy được trên Linux container

## Decision Drivers

* **Reproducibility** — build trên mọi môi trường cho cùng kết quả
* **Image size** — image nhỏ = deploy nhanh hơn
* **Security** — không include SDK tools trong production image
* **CI/CD integration** — Jenkins build pipeline
* **Cross-platform** — Windows dev → Linux production

## Considered Options

### Option 1: Docker Multi-stage Build
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
COPY . .
RUN dotnet publish -c Release

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:5.0
COPY --from=build /app/publish .
```
- **Pros:**
  - Production image không chứa SDK (nhỏ hơn nhiều)
  - Build reproducible
  - Security tốt hơn (attack surface nhỏ)
- **Cons:**
  - Dockerfile phức tạp hơn single-stage

### Option 2: Single-stage Docker Build
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY . .
RUN dotnet publish
ENTRYPOINT ["dotnet", "app.dll"]
```
- **Pros:** Đơn giản
- **Cons:** Image rất lớn (SDK ~700MB vs Runtime ~200MB), security risk

### Option 3: Publish locally rồi copy binary
- Build `dotnet publish` trên máy
- Copy output vào server thủ công hoặc qua CI
- **Pros:** Không cần Docker cho build
- **Cons:** Phụ thuộc môi trường máy build, không reproducible

### Option 4: Kubernetes + Helm
- **Pros:** Enterprise-grade orchestration
- **Cons:** Overkill cho team size và infrastructure hiện tại

## Decision

Chúng ta sẽ dùng **Docker multi-stage build** với base images chính thức từ Microsoft.

## Rationale

1. **Image size:** aspnet:5.0 runtime (~200MB) nhỏ hơn nhiều so với sdk:5.0 (~700MB) → deploy nhanh hơn
2. **Security:** Production container không có SDK, compiler, debugger tools → attack surface nhỏ hơn
3. **CI/CD friendly:** Jenkins `docker build` + `docker push` đơn giản
4. **Cross-platform:** Linux container chạy được trên mọi Linux host, không phụ thuộc Windows

## Dockerfile thực tế

```dockerfile
# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

# Copy solution và restore NuGet packages (layer caching)
COPY ["AllianceMiddleman_NetCore.sln", "./"]
COPY ["AllianceMiddlemanWebAPI/AllianceMiddlemanWebAPI.csproj", "AllianceMiddlemanWebAPI/"]
# ... các project khác
RUN dotnet restore

# Build và publish
COPY . .
RUN dotnet publish "AllianceMiddlemanWebAPI/AllianceMiddlemanWebAPI.csproj" \
    -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AllianceMiddlemanWebAPI.dll"]
```

## Image Tagging Strategy

```
thanghatien/sigo-api-dev:{release}-{svn_revision}
thanghatien/sigo-api-dev:latest
```

Ví dụ: `thanghatien/sigo-api-dev:v1.2.0-1234`

## Consequences

### Tích cực
- Production image ~3x nhỏ hơn single-stage
- Build 100% reproducible, không phụ thuộc môi trường máy developer
- `latest` tag cho quick deploy, revision tag cho rollback
- Layer caching cho NuGet restore → build sau nhanh hơn

### Tiêu cực
- Dockerfile cần maintain khi thêm project mới
- Multi-stage có thể confusing cho developer mới
- NuGet cache không persist giữa các Jenkins builds nếu không cấu hình volume

### Rủi ro
- Base image outdated (security patches)
- **Giảm thiểu:** Pin minor version, có policy update base image định kỳ
- Layer cache miss nếu Dockerfile thay đổi
- **Giảm thiểu:** Tối ưu thứ tự COPY để tận dụng cache tốt nhất

## Related Decisions

- ADR-0012: Jenkins CI/CD (build và push Docker image)
- ADR-0001: ASP.NET Core 5.0 (dotnet aspnet:5.0 base image)

---

*Ngày tạo: 2021-09-01*
