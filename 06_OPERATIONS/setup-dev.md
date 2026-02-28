# Setup môi trường Development

## Yêu cầu

| Tool | Phiên bản | Download |
|------|-----------|---------|
| .NET SDK | 5.0 | https://dotnet.microsoft.com/download/dotnet/5.0 |
| SQL Server | 2019+ | |
| Redis | 6+ | |
| Docker | 20+ | |
| Visual Studio | 2019/2022 | |

---

## Clone & build

```bash
git clone {repo_url}
cd AllianceMiddleman/SOURCE

# Restore packages
dotnet restore AllianceMiddleman_NetCore.sln

# Build
dotnet build AllianceMiddleman_NetCore.sln
```

---

## Cấu hình database

1. Tạo database SQL Server
2. Chạy migration script trong `SOURCE/Script/`

```bash
# Thứ tự chạy script (trong SOURCE/Script/)
# 1. Schema — CreateTable scripts (theo thứ tự ngày)
#    Ví dụ: 20240610-1007-[QuickOrderRequest]-CreateTable.txt
#           20240610-1007-[QuickOrderRequestDetail]-CreateTable.txt
#           20241021-1226-[ConfigLandingPage]-CreateTable.txt
#           20241230-0726-[ConfigMISAInvoice]-CreateTable.txt
#           20241230-0727-[Log_MISAInvoice_PublishHSM]-CreateTable.txt
#
# 2. Alter/AddColumn scripts (theo thứ tự ngày)
#    Ví dụ: 20250226-1255-[Order_Vehicle]-AddColumn_CompanyTaxCode.txt
#
# 3. Stored Procedures
#    Ví dụ: 20240918-1308-[sp_GetNotifyMessage4Push_Json]-AlterProc.txt
#           20241202-1012-[sp_GetReport_CarRentalPriceByByLocation_Json]-CreateStore.txt
#           20250310-0938-[sp_GetConfigLandingPage_Json]-CreateStore.txt
#
# 4. Functions & Triggers
#    Ví dụ: 20240516-0813[f_GetTextUnsign]-CreateFunc.txt
#           20240704-1057-[sp_update_quickorder_responseinfo]-CreateFuncTrigger.txt

# Hoặc chạy batch qua PowerShell helpers
# pg_query.ps1 — Chạy single SQL file
# pg_query_v2.ps1 — Chạy nhiều SQL files
# run_pg_queries.bat — Batch runner
```

---

## Cấu hình appsettings

Copy file mẫu và điền thông tin:

```bash
cp SOURCE/AllianceMiddlemanWebAPI/appsettings.json \
   SOURCE/AllianceMiddlemanWebAPI/appsettings.Development.json
```

Các key cần điền:
- `ConnectionStrings:DefaultConnection` — SQL Server connection string
- `ConnectionStrings:Redis` — Redis connection string
- `JwtSettings:SecretKey` — JWT secret
- Xem chi tiết: [config-keys.md](./config-keys.md)

---

## Chạy local

```bash
cd SOURCE/AllianceMiddlemanWebAPI
dotnet run --environment Development
```

App chạy tại: `https://localhost:5001` / `http://localhost:5000`

---

## Chạy với Docker

```bash
cd SOURCE
docker build -t alliance-middleman .
docker run -p 5000:80 \
  -e ConnectionStrings__DefaultConnection="{connection_string}" \
  alliance-middleman
```

---

## Chạy module độc lập

Mỗi module có solution file riêng:
```bash
cd SOURCE/Ezy.Module.EWallet
dotnet run
```

---

## Kiểm tra

Swagger UI: `https://localhost:5001/swagger`

### Test accounts

Hệ thống sử dụng OAuth2 server tại `http://localhost:12391` cho authentication.

**Cách tạo test account:**
1. Đăng ký qua API: `POST /api/v1/Account/Register` (cần OTP → `POST /api/v1/Account/GetOTP`)
2. Hoặc insert trực tiếp vào DB tables: `UserLogin` (AppSystem DB) + `UserLogin_Ext` (Business DB)

**Connection strings dev:**

| Database | Host | Database |
|----------|------|----------|
| AppSystem | `deb-postgresql.allianceitsc.com` | `Sigo_Dev` |
| Business | `192.168.0.21` | `Sigo_Dev` |
| Category | Cùng host AppSystem | `Sigo_Dev` |
| AppData | Cùng host AppSystem | `Sigo_Dev` |

**CI/CD Pipeline:**
- Jenkinsfile: `SOURCE/Jenkinsfile`
- Docker image: `thanghatien/sigo-api-dev:1.0.0-{SVN_REVISION}`
- Deploy trigger: `SIGO_DEV_DEPLOY` job tại `jenkins-dev.allianceitsc.com:442`

---

*Xem thêm: [config-keys.md](./config-keys.md) | [deployment.md](../01_ARCHITECTURE/deployment.md)*
