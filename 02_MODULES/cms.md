# Module: CMS — Quản lý nội dung

> **Solution:** `Ezy.Module.CMS` | **Trạng thái:** Active

## Mục lục
- [Tổng quan](#tổng-quan)
- [Cấu trúc](#cấu-trúc)
- [Entities & Models](#entities--models)
- [Services](#services)
- [API Endpoints](#api-endpoints)
- [URL Slug System](#url-slug-system)

---

## Tổng quan

Module CMS (Content Management System) quản lý **toàn bộ nội dung web/marketing** của Sigo platform:

- **Blog posts:** Bài viết hướng dẫn, tips thuê xe, review địa điểm
- **News posts:** Tin tức sự kiện, cập nhật tính năng
- **Dashboard:** Trang web tổng hợp dữ liệu business cho staff và website
- **URL Slugs:** SEO URL mapping (`/thue-xe-ha-noi` → content)
- **Media files:** Ảnh, video cho website
- **Topics & Categories:** Phân loại nội dung

**Đối tượng sử dụng:**
- **Admin/Content Editor:** Tạo, quản lý nội dung qua CMS portal
- **Frontend Website:** Đọc nội dung qua API (anonymous endpoints)
- **BCT Reporting:** Xuất báo cáo website cho Bộ Công Thương

**Tích hợp với:**
- `UpdateSlugEngine` — tự động cập nhật URL slugs
- `UpdateBlogViewCountEngine` — batch update lượt xem blog
- Main API (`AllianceMiddlemanWebAPI`) — share một số service

---

## Cấu trúc

```
Ezy.Module.CMS/
├── Ezy.Module.CMS.API/
│   └── Controllers/
│       ├── MainBusiness/
│       │   ├── DashboardController.cs           # Dashboard chính
│       │   ├── DashboardForWebsiteController.cs # Dashboard cho website public
│       │   ├── InvalidSlugMappingController.cs  # Redirect 404 → đúng URL
│       │   └── ProjectPhotoController.cs        # Quản lý ảnh dự án
│       ├── Categories/
│       │   ├── EzyWeb_BlogPostController.cs      # CRUD blog posts
│       │   ├── EzyWeb_NewsPostController.cs      # CRUD news posts
│       │   ├── EzyWeb_TopicController.cs         # Topics phân loại nội dung
│       │   ├── EzyWeb_UrlRecordController.cs     # URL slug records
│       │   ├── Website_FileController.cs         # Media file management
│       │   ├── ConfigArticleTypeController.cs    # Loại bài viết
│       │   ├── ConfigTopicController.cs          # Config topics
│       │   ├── ConfigSystemDomainController.cs   # Domain config
│       │   ├── ArticleTypeMappingController.cs   # Article-type mapping
│       │   └── ReleatedArticleMappingController.cs  # Related articles
│       └── Base/
│           └── BaseCategoryController.cs        # Base CRUD controller
│
├── Ezy.Module.CMS.Core/
│   └── Data/
│       ├── AlliancePos/                         # POS-related data contexts
│       └── Stores/                              # Stored procedures
│
├── Ezy.Module.CMS.Service/
│   └── Services/                               # Business logic services
│
├── Ezy.Module.CMS.DataShared/                  # DTOs dùng chung
├── Ezy.Module.CMS.Shared/                      # Shared logic + helpers
└── Script/                                      # DB migration scripts
```

**Tổng số C# files:** ~266

---

## Entities & Models

### `EzyWeb_BlogPost` — Bài viết Blog

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `ID_GUID` | Guid | Business key |
| `Title` | string | Tiêu đề bài viết |
| `Content` | string (text) | Nội dung HTML đầy đủ |
| `Summary` | string | Tóm tắt ngắn |
| `Author` | string | Tác giả |
| `PublishDate` | DateTime | Ngày xuất bản |
| `FeaturedImageUrl` | string | URL ảnh đại diện |
| `TopicId` | int | FK → EzyWeb_Topic |
| `ArticleTypeId` | int | FK → ConfigArticleType |
| `Slug` | string | URL slug (SEO) |
| `ViewCount` | int | Lượt xem (batch-updated) |
| `IsPublished` | bool | Trạng thái xuất bản |
| `IsDeleted` | bool | Soft delete |
| `Log_CreatedDate` | DateTime | |
| `Log_UpdatedDate` | DateTime | |

### `EzyWeb_NewsPost` — Bài viết Tin tức

Cấu trúc tương tự `EzyWeb_BlogPost`, dùng cho news category riêng biệt.

### `EzyWeb_Topic` — Chủ đề

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `Name` | string | Tên topic |
| `Slug` | string | URL slug |
| `Description` | string | Mô tả |
| `ParentTopicId` | int? | FK → EzyWeb_Topic (topic cha) |
| `DisplayOrder` | int | Thứ tự hiển thị |
| `IsActive` | bool | Trạng thái hoạt động |

### `EzyWeb_UrlRecord` — URL Slug Record

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `Slug` | string | URL slug (unique) |
| `EntityId` | int | ID của content entity |
| `EntityType` | string | Loại entity: `BlogPost`, `NewsPost`, `Topic`, `ServiceItem` |
| `IsActive` | bool | Slug đang dùng |
| `Language` | string | Ngôn ngữ (vi, en) |

### `Website_File` — Media Files

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `FileName` | string | Tên file |
| `FilePath` | string | Đường dẫn SFTP |
| `FileSize` | long | Kích thước (bytes) |
| `ContentType` | string | MIME type |
| `AltText` | string | Alt text cho SEO |
| `AssociatedEntityGuid` | Guid | Content entity liên kết |
| `UploadedBy` | string | UserGuid |
| `UploadedDate` | DateTime | |

### `InvalidSlugMapping` — Redirect mapping

| Column/Field | Type | Mô tả |
|-------------|------|-------|
| `Id` | int | PK |
| `OldSlug` | string | URL cũ (404) |
| `NewSlug` | string | URL mới redirect tới |
| `IsActive` | bool | Đang dùng |

---

## Services

| Service | Mô tả |
|---------|-------|
| `IBlogPostService` | CRUD blog posts, publish/unpublish |
| `INewsPostService` | CRUD news posts |
| `IDashboardService` | Tổng hợp dữ liệu dashboard |
| `ITopicService` | Quản lý topics và hierarchy |
| `IUrlRecordService` | Quản lý slug mapping, resolve slug → content |
| `IWebsiteFileService` | Upload, retrieve, delete media files |
| `IInvalidSlugMappingService` | Quản lý redirect rules |

---

## API Endpoints

### Dashboard

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Dashboard/MyDashboard` | Dashboard tổng quan cho staff | Required |
| POST | `/api/v1/Dashboard/UpdateDataWithSlug` | Cập nhật nội dung theo slug | Required |
| POST | `/api/v1/Dashboard/CreateDataWithSlug` | Tạo nội dung mới với slug | Required |
| POST | `/api/v1/Dashboard/UpdateActiveWithSlug` | Bật/tắt hiển thị nội dung | Required |
| POST | `/api/v1/Dashboard/DeleteImageWeb` | Xoá ảnh web | Required |
| POST | `/api/v1/Dashboard/GetListTopic4Website` | Lấy danh sách topics | Required |

### Dashboard (Public / Website)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/DashboardForWebsite/MyDashboardForWebSite` | Dashboard website | Required |
| POST | `/api/v1/DashboardForWebsite/GetListReportWebsite4BCT` | Báo cáo BCT | Required |
| POST | `/api/v1/DashboardForWebsite/GetListDataWithSlug` | Lấy nội dung theo slug | **Public** |
| POST | `/api/v1/DashboardForWebsite/GetDataListBlog4Website` | Danh sách blog cho website | **Public** |

### Blog & News

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Blog/Post/List` | Danh sách blog posts | Required |
| POST | `/api/v1/Blog/Post/Add` | Tạo blog post mới | Required |
| POST | `/api/v1/Blog/Post/Update` | Cập nhật blog post | Required |
| POST | `/api/v1/Blog/Post/Delete` | Xoá blog post | Required |
| POST | `/api/v1/News/Post/List` | Danh sách news posts | Required |
| POST | `/api/v1/News/Post/Add` | Tạo news post | Required |
| POST | `/api/v1/News/Post/Update` | Cập nhật news post | Required |

### URL & Topics

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/URL/Record/List` | Danh sách slug records | Required |
| POST | `/api/v1/URL/Record/Add` | Thêm slug | Required |
| POST | `/api/v1/URL/Record/Update` | Cập nhật slug | Required |
| POST | `/api/v1/Config/Topic/List` | Danh sách topics | Required |
| POST | `/api/v1/Config/ArticleType/List` | Loại bài viết | Required |

### Media & Files

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Website/File/List` | Danh sách media files | Required |
| POST | `/api/v1/Website/File/Upload` | Upload file mới | Required |
| POST | `/api/v1/Website/File/Delete` | Xoá file | Required |

### Slug Redirect

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/InvalidSlugMapping/List` | Danh sách redirects | Required |
| POST | `/api/v1/InvalidSlugMapping/Add` | Thêm redirect rule | Required |

---

## URL Slug System

Sigo dùng URL slugs thân thiện SEO cho tất cả content:

```
Ví dụ:
  https://sigo.vn/thue-xe-ha-noi          → ServiceItem slug
  https://sigo.vn/blog/meo-thue-xe-an-toan → BlogPost slug
  https://sigo.vn/tin-tuc/sigo-mo-rong      → NewsPost slug
```

### Quy trình tạo slug

```
1. Content được tạo (BlogPost, ServiceItem...)
        │
        ▼
2. UpdateSlugEngine chạy (background)
        │ Generate slug từ title/name
        │ Kiểm tra uniqueness
        │ Tạo EzyWeb_UrlRecord
        ▼
3. Slug được active, sẵn sàng resolve
        │
        ▼
4. Frontend gọi GetListDataWithSlug(slug)
        → API resolve slug → entity → return content
```

### Xử lý slug thay đổi

Khi nội dung đổi tên hoặc URL thay đổi:
1. Slug cũ → `IsActive = false`
2. Slug mới được tạo → `IsActive = true`
3. Thêm record vào `InvalidSlugMapping`: old → new
4. Frontend redirect 301 theo mapping

---

*Xem thêm: [overview.md](../01_ARCHITECTURE/overview.md) | [background-engines.md](../01_ARCHITECTURE/background-engines.md)*
