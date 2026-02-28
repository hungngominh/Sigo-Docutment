# Deployment — AllianceMiddleman

## Mục lục
- [Tổng quan](#tổng-quan)
- [Docker](#docker)
- [Jenkins CI/CD](#jenkins-cicd)
- [Môi trường](#môi-trường)
- [Cấu hình (appsettings)](#cấu-hình-appsettings)
- [Health checks & monitoring](#health-checks--monitoring)
- [Rollback](#rollback)

---

## Tổng quan

```
Developer → SVN commit
    │
    ▼
Jenkins (trigger manual hoặc webhook)
    │
    ▼
[Build Docker Image]  ─── mcr.microsoft.com/dotnet/sdk:5.0
    │
    ▼
[Push to Docker Hub]  ─── thanghatien/sigo-api-dev:{tag}
    │
    ▼
[Trigger CD Pipeline] ─── SIGO_DEV_DEPLOY job
    │
    ▼
Deploy to Server       ─── docker pull + docker run
```

---

## Docker

### Multi-stage Dockerfile

**File:** `SOURCE/Dockerfile`

```dockerfile
# ─── Stage 1: Build ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

# Restore NuGet packages (layer cache optimization)
COPY ["AllianceMiddleman_NetCore.sln", "./"]
COPY ["AllianceMiddlemanWebAPI/AllianceMiddlemanWebAPI.csproj",
      "AllianceMiddlemanWebAPI/"]
# ... các project khác
RUN dotnet restore

# Build & publish Release
COPY . .
RUN dotnet publish "AllianceMiddlemanWebAPI/AllianceMiddlemanWebAPI.csproj" \
    -c Release -o /app/publish

# ─── Stage 2: Runtime ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app

EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AllianceMiddlemanWebAPI.dll"]
```

### Lợi ích multi-stage
| | Stage 1 (sdk:5.0) | Stage 2 (aspnet:5.0) |
|--|------------------|--------------------|
| **Kích thước** | ~700 MB | ~200 MB |
| **Chứa SDK** | Có | Không |
| **Production image** | Không | Có |

### Build & run thủ công

```bash
# Build image
docker build -t alliance-middleman .

# Run local
docker run -p 8080:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e "ConnectionStrings__DefaultConnection=Server=...;Database=...;" \
  alliance-middleman

# Với volume cho static files
docker run -p 8080:80 \
  -v /data/uploads:/app/home \
  alliance-middleman
```

---

## Jenkins CI/CD

**File:** `SOURCE/Jenkinsfile`

### Environment variables

| Biến | Giá trị | Mô tả |
|------|---------|-------|
| `APP_NAME` | `sigo-api-dev` | Tên ứng dụng |
| `RELEASE` | `1.0.0` | Phiên bản release |
| `DOCKER_USER` | `thanghatien` | Docker Hub username |
| `IMAGE_NAME` | `thanghatien/sigo-api-dev` | Full image name |
| `IMAGE_TAG` | `${RELEASE}-${SVN_REVISION}` | Tag kết hợp version + revision |
| `DOTNET_CLI_TELEMETRY_OPTOUT` | `1` | Tắt telemetry .NET CLI |

### Pipeline stages

#### Stage 1: Prepare NuGet Volume
```groovy
stage('Prepare NuGet Volume') {
    steps {
        // Tạo Docker volume để cache NuGet packages
        // Volume name: dotnet5-nuget-cache
        // Mount khi build để tái dùng packages đã download
    }
}
```
**Mục đích:** Giảm thời gian build bằng cách cache NuGet packages giữa các lần build.

---

#### Stage 2: Get SVN Revision
```groovy
stage('Get SVN Revision') {
    steps {
        script {
            // Upgrade SVN working copy format nếu cần
            sh 'svn upgrade'

            // Lấy SVN revision number
            env.SVN_REVISION = sh(
                script: "svn info --show-item revision .",
                returnStdout: true
            ).trim()
        }
    }
}
```
**Mục đích:** SVN revision dùng làm build identifier, đảm bảo mỗi image tag unique và traceable.

---

#### Stage 3: Build & Push Docker Image
```groovy
stage('Build & Push Docker Image') {
    steps {
        // Build Docker image
        // docker build -t ${IMAGE_NAME} .

        // Tag với version cụ thể
        // docker tag ${IMAGE_NAME} ${IMAGE_NAME}:${IMAGE_TAG}

        // Tag latest
        // docker tag ${IMAGE_NAME} ${IMAGE_NAME}:latest

        // Push cả hai tags lên Docker Hub
        // docker push ${IMAGE_NAME}:${IMAGE_TAG}
        // docker push ${IMAGE_NAME}:latest
    }
}
```

**Image naming:**
```
thanghatien/sigo-api-dev:1.0.0-1234   ← specific version
thanghatien/sigo-api-dev:latest        ← luôn latest build
```

---

#### Stage 4: Cleanup Artifacts
```groovy
stage('Cleanup Artifacts') {
    steps {
        // Xoá image khỏi local Docker engine sau khi push
        // docker rmi ${IMAGE_NAME}:${IMAGE_TAG}
        // docker rmi ${IMAGE_NAME}:latest
        // Giải phóng disk space trên Jenkins agent
    }
}
```

---

#### Stage 5: Trigger CD Pipeline
```groovy
stage('Trigger CD Pipeline') {
    steps {
        // HTTP POST tới Jenkins CD job
        // URL: https://jenkins-dev.allianceitsc.com:442/job/SIGO_DEV_DEPLOY/buildWithParameters
        // Params: IMAGE_TAG=${IMAGE_TAG}
        // Auth: JENKINS_API_TOKEN
    }
}
```
**Mục đích:** Trigger deployment job riêng biệt (`SIGO_DEV_DEPLOY`) với image tag cụ thể.

### Quy trình tổng thể

```
SVN Commit
    │
    ▼ (manual trigger hoặc webhook)
Jenkins: AllianceMiddleman Build Job
    ├── Stage 1: Prepare NuGet volume cache
    ├── Stage 2: Get SVN revision → SVN_REVISION=1234
    ├── Stage 3: Build image → push thanghatien/sigo-api-dev:1.0.0-1234
    ├── Stage 4: Cleanup local images
    └── Stage 5: POST → SIGO_DEV_DEPLOY?IMAGE_TAG=1.0.0-1234
                            │
                            ▼
                    Jenkins: SIGO_DEV_DEPLOY Job
                        └── docker pull + restart container
```

---

## Môi trường

| Môi trường | URL | `ASPNETCORE_ENVIRONMENT` | Ghi chú |
|-----------|-----|--------------------------|---------|
| **Production** | `https://api.sigo.vn` | `Production` | Live traffic |
| **Staging** | *(internal)* | `Staging` | Pre-release testing |
| **Development** | `http://localhost:5000` / `https://localhost:5001` | `Development` | Local dev |

### Cấu hình theo môi trường
ASP.NET Core merge config theo thứ tự ưu tiên (cao hơn ghi đè thấp hơn):

```
appsettings.json                    ← Base config
    +
appsettings.{Environment}.json     ← Environment override
    +
Environment Variables              ← Runtime override (ưu tiên cao nhất)
```

---

## Cấu hình (appsettings)

### Cấu trúc `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "EPPlus": {
    "ExcelPackage": {
      "LicenseContext": "NonCommercial"
    }
  },
  "AllowedHosts": "*"
}
```

### Các section cần cấu hình cho production

Các key sau cần được set qua environment variables hoặc `appsettings.Production.json`:

| Section / Key | Mô tả | Bắt buộc |
|--------------|-------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string | Có |
| `ConnectionStrings:Redis` | Redis connection string | Có |
| `ConnectionStrings:PostgreSQL` | PostgreSQL (nếu dùng) | Tuỳ chọn |
| `JwtSettings:SecretKey` | JWT signing key (≥32 ký tự) | Có |
| `JwtSettings:Issuer` | JWT issuer | Có |
| `JwtSettings:Audience` | JWT audience | Có |
| `JwtSettings:ExpiresInMinutes` | Token expiry (mặc định 60) | Có |
| `USER_AUTO_LOGIN_URL` | OAuth2 server URL | Có |
| `MBBank:ApiUrl` | MB Bank API endpoint | Có |
| `MBBank:ClientId` | MB Bank client ID | Có |
| `MBBank:ClientSecret` | MB Bank client secret | Có |
| `Google:ClientId` | Google OAuth2 client ID | Có |
| `Google:ClientSecret` | Google OAuth2 client secret | Có |
| `Kafka:BootstrapServers` | Kafka broker URL | Có |
| `SFTP:Host` | SFTP server host | Có |
| `SFTP:Username` | SFTP username | Có |
| `SFTP:Password` | SFTP password | Có |
| `Notification:*` | Notification service config | Có |

> Xem chi tiết từng key tại: [../06_OPERATIONS/config-keys.md](../06_OPERATIONS/config-keys.md)

### Environment Variables override

```bash
# Ví dụ: override connection string qua env var
export ConnectionStrings__DefaultConnection="Server=prod-db;Database=Sigo;..."
export JwtSettings__SecretKey="your-32-char-secret-key-here"
```

> **Lưu ý:** ASP.NET Core dùng `__` (double underscore) thay cho `:` khi set qua environment variables.

---

## Health checks & monitoring

### Endpoints

| Endpoint | Method | Response | Dùng cho |
|---------|--------|---------|---------|
| `GET /health/ready` | GET | `200 OK` hoặc `503` | Kubernetes readiness probe |
| `GET /health/live` | GET | `200 ALIVE` | Kubernetes liveness probe |
| `GET /deployment` | GET | JSON với version info | Verify deployment version |

### Deployment verification

```bash
# Kiểm tra version đang chạy
curl https://api.sigo.vn/deployment
# → {"version":"1.0.0","commitHash":"abc123","buildDate":"..."}

# Kiểm tra readiness
curl https://api.sigo.vn/health/ready
# → 200 nếu app sẵn sàng, 503 nếu đang khởi tạo

# Kiểm tra liveness
curl https://api.sigo.vn/health/live
# → 200 ALIVE
```

---

## Rollback

### Rollback về version cũ

```bash
# 1. Xác định IMAGE_TAG cần rollback về
#    Ví dụ: 1.0.0-1200 (SVN revision 1200)

# 2. Pull image cụ thể
docker pull thanghatien/sigo-api-dev:1.0.0-1200

# 3. Stop container hiện tại
docker stop sigo-api
docker rm sigo-api

# 4. Start với image cũ
docker run -d \
  --name sigo-api \
  -p 80:80 \
  -p 443:443 \
  --env-file /etc/sigo/production.env \
  thanghatien/sigo-api-dev:1.0.0-1200
```

### Xác định revision cần rollback

```bash
# Xem SVN log để tìm revision tương ứng
svn log --limit 20 https://svn-repo/AllianceMiddleman

# Hoặc check Docker Hub tags
# thanghatien/sigo-api-dev → xem danh sách tags
```

---

*Xem thêm: [overview.md](./overview.md) | [../06_OPERATIONS/setup-dev.md](../06_OPERATIONS/setup-dev.md) | [../06_OPERATIONS/config-keys.md](../06_OPERATIONS/config-keys.md) | [../07_ADR/0011-docker-multistage-deployment.md](../07_ADR/0011-docker-multistage-deployment.md)*
