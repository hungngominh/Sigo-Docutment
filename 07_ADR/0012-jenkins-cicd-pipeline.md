# ADR-0012: CI/CD Pipeline dùng Jenkins + SVN Revision Tagging

## Status

Accepted

## Context

Team cần tự động hóa quy trình build và deploy để:
- Giảm lỗi do manual deployment
- Đảm bảo mọi deployment đều đã qua build và test
- Có thể rollback về bất kỳ version nào
- Audit trail cho mỗi deployment

Hệ thống dùng SVN (không phải Git) để version control.

## Decision Drivers

* **Automation** — không manual deploy
* **SVN integration** — phù hợp với SVN workflow hiện tại
* **Traceability** — mỗi Docker image map rõ ràng tới SVN revision
* **Rollback** — deploy lại version cũ dễ dàng
* **Team familiarity** — team đã có Jenkins infrastructure

## Considered Options

### Option 1: Jenkins + SVN + Docker
- **Pros:**
  - Jenkins đã có sẵn trong infrastructure
  - SVN plugin cho Jenkins native
  - Docker image tagging với SVN revision
  - Declarative Jenkinsfile trong repo
- **Cons:**
  - SVN không phải best practice hiện đại (Git phổ biến hơn)
  - Jenkins cần maintain infrastructure

### Option 2: GitHub Actions (nếu migrate sang Git)
- **Pros:**
  - Managed service, không maintain infrastructure
  - YAML workflow quen thuộc
  - Integration tốt với GitHub
- **Cons:**
  - Cần migrate từ SVN sang Git trước
  - Vendor lock-in GitHub

### Option 3: GitLab CI/CD
- **Pros:**
  - All-in-one: Git repo + CI/CD
  - Self-hosted option
- **Cons:**
  - Cần migrate từ SVN
  - Thêm infrastructure component

### Option 4: Manual Deployment Scripts
- **Pros:** Đơn giản
- **Cons:** Error-prone, không có audit trail, chậm

## Decision

Chúng ta sẽ dùng **Jenkins** với **Declarative Pipeline (Jenkinsfile)** và tag Docker images bằng **SVN revision number**.

## Rationale

1. **Existing infrastructure:** Jenkins đã có sẵn, không cần setup mới
2. **SVN compatibility:** Jenkins SVN plugin tích hợp native, không cần migrate VCS
3. **Revision as version:** SVN revision là monotonically increasing, dùng làm Docker tag đảm bảo traceability
4. **Declarative Jenkinsfile:** Pipeline as code, version control cùng source code

## Pipeline Stages thực tế

```groovy
// Jenkinsfile
pipeline {
    agent any
    stages {
        stage('Prepare NuGet Volume') {
            // Setup Docker volume cho NuGet cache
            // Tăng tốc build bằng cache packages
        }

        stage('Get SVN Revision') {
            steps {
                script {
                    SVN_REVISION = sh(
                        script: "svn info --show-item revision .",
                        returnStdout: true
                    ).trim()
                }
            }
        }

        stage('Build & Push Docker Image') {
            steps {
                // docker build -t thanghatien/sigo-api-dev:${RELEASE}-${SVN_REVISION} .
                // docker push thanghatien/sigo-api-dev:${RELEASE}-${SVN_REVISION}
                // docker tag ... :latest
                // docker push ... :latest
            }
        }

        stage('Cleanup Artifacts') {
            // Xóa Docker images cũ trên build server
            // Giữ N versions gần nhất
        }

        stage('Trigger CD Pipeline') {
            // Trigger deployment pipeline
            // Deploy lên staging/production
        }
    }
}
```

## Image Naming Convention

```
Registry: Docker Hub (thanghatien/)
Image:    sigo-api-dev
Tags:
  - {release}-{svn_revision}  → ví dụ: v1.2.0-1234
  - latest                    → luôn trỏ tới bản mới nhất
```

## Rollback Process

```bash
# Rollback về SVN revision 1200 (ví dụ)
docker pull thanghatien/sigo-api-dev:v1.2.0-1200
docker stop sigo-api
docker run -d --name sigo-api thanghatien/sigo-api-dev:v1.2.0-1200
```

## Consequences

### Tích cực
- Mỗi Docker image có thể trace ngược về SVN revision cụ thể
- NuGet volume cache giảm build time đáng kể
- Auto cleanup tránh disk full trên build server
- Declarative Jenkinsfile trong SVN → versioned pipeline

### Tiêu cực
- SVN không phải best practice 2024 (Git branching model tốt hơn)
- Jenkins cần maintain infrastructure riêng
- Không có parallel pipeline (vì SVN linear history)

### Rủi ro
- Docker Hub rate limiting (anonymous pull limit)
- **Giảm thiểu:** Authenticate khi pull, hoặc dùng private registry
- Jenkins single point of failure
- **Giảm thiểu:** Jenkins HA configuration, hoặc migrate sang managed CI
- Disk space Jenkins agent
- **Giảm thiểu:** Cleanup stage xóa images cũ sau mỗi build

## Future Consideration

Khi có cơ hội migrate sang Git:
- Thay SVN revision bằng Git commit SHA
- Migrate pipeline sang GitHub Actions hoặc GitLab CI
- Xem thêm ADR mới nếu thực hiện migration

## Related Decisions

- ADR-0011: Docker Multi-stage Build (được build và push trong pipeline)
- ADR-0001: ASP.NET Core 5.0 (dotnet build target)

---

*Ngày tạo: 2021-09-01*
