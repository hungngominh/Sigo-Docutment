# API: CMS (Content Management System)

> **Module:** `Ezy.Module.CMS`
> **Controllers:** `DashboardController`, `DashboardForWebsiteController`, `EzyWeb_BlogPostController`, `EzyWeb_NewsPostController`, `EzyWeb_TopicController`, `EzyWeb_UrlRecordController`, `ArticleTypeMappingController`, `ReleatedArticleMappingController`, `InvalidSlugMappingController`, `ConfigArticleTypeController`, `ConfigSystemDomainController`, `ConfigTopicController`
> **Base:** `/api/v1/EzyWeb_*`, `/api/v1/Dashboard*`, `/api/v1/ArticleTypeMapping`, `/api/v1/Config*`

## Mục lục
- [Tổng quan](#tổng-quan)
- [Dashboard — Nội bộ](#post-apiv1dashboardmydashboard)
- [Dashboard — Website Public](#post-apiv1dashboardforwebsitemydashboardforwebsite)
- [Blog Post — Quản lý](#post-apiv1ezyweb_blogpostlist)
- [Blog Post — Public](#post-apiv1ezyweb_blogpost_publiclist)
- [News Post](#post-apiv1ezyweb_newspostlist)
- [Topic — Chủ đề](#post-apiv1ezyweb_topiclist)
- [URL Record — SEO](#post-apiv1ezyweb_urlrecordlist)
- [Article Type Mapping](#post-apiv1articletypemappinglist)
- [Related Article Mapping](#post-apiv1releatedarticlemappinglist)
- [Cấu hình CMS](#cấu-hình-cms)

---

## Tổng quan

Module CMS quản lý nội dung website:
- **Blog Post / News Post:** Bài viết, tin tức
- **Topic:** Chủ đề phân loại nội dung
- **URL Record:** Quản lý slug SEO cho từng trang
- **Dashboard:** API tổng hợp cho website (public) và admin (nội bộ)

### Controllers phân chia theo quyền truy cập

| Controller | Route | Quyền | Mục đích |
|-----------|-------|-------|---------|
| `DashboardController` | `/api/v1/Dashboard` | `[Authorize]` | Dashboard admin |
| `DashboardForWebsiteController` | `/api/v1/DashboardForWebsite` | Public | API cho website |
| `EzyWeb_BlogPostController` | `/api/v1/EzyWeb_BlogPost` | `[Authorize]` | Quản lý bài viết |
| `EzyWeb_BlogPost_PublicController` | `/api/v1/EzyWeb_BlogPost_Public` | Public | Lấy bài viết cho website |
| `EzyWeb_NewsPost_PublicController` | `/api/v1/EzyWeb_NewsPost_Public` | Public | Lấy tin tức cho website |
| `EzyWeb_Topic_DetailController` | `/api/v1/EzyWeb_Topic_Detail` | Public | Chi tiết chủ đề |

---

## `POST /api/v1/Dashboard/MyDashboard`

**Mô tả:** Lấy dữ liệu tổng quan dashboard nội bộ.

**Phân quyền:** `[Authorize]`

### Request Body — `DashboardParamModel`
```json
{
  "FromDate": "2026-01-01",
  "ToDate": "2026-01-31"
}
```

### Response — `EzyResultObject<DashboardModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "TotalBlogPosts": 150,
    "TotalTopics": 20,
    "TotalViews": 50000
  }
}
```

---

## `POST /api/v1/Dashboard/CreateDataWithSlug`

**Mô tả:** Tạo bài viết mới kèm slug SEO tự động.

**Phân quyền:** `[Authorize]`

### Request Body — `EzyWeb_CreatePostModel`
```json
{
  "Title": "Hướng dẫn thuê xe tự lái",
  "Content": "<p>Nội dung bài viết...</p>",
  "TopicId": "topic-guid",
  "IsActive": true,
  "MetaTitle": "Thuê xe tự lái - Sigo",
  "MetaDescription": "Hướng dẫn chi tiết..."
}
```

---

## `POST /api/v1/Dashboard/UpdateDataWithSlug`

**Mô tả:** Cập nhật bài viết kèm xử lý slug.

**Phân quyền:** `[Authorize]`

### Request Body — `EzyWeb_UpdatePostModel`
```json
{
  "PostId": "post-guid",
  "Title": "Tiêu đề mới",
  "Content": "<p>Nội dung mới...</p>"
}
```

---

## `POST /api/v1/Dashboard/UpdateActiveWithSlug`

**Mô tả:** Bật/tắt hiển thị bài viết trên website.

**Phân quyền:** `[Authorize]`

### Request Body — `EzyWeb_UpdatePostModel`
```json
{
  "PostId": "post-guid",
  "IsActive": true
}
```

---

## `POST /api/v1/Dashboard/DeleteImageWeb/{guid}`

**Mô tả:** Xoá ảnh bài viết theo GUID.

**Phân quyền:** `[Authorize]`

**Route param:** `{guid}` — ID của ảnh cần xoá.

---

## `POST /api/v1/Dashboard/GetListTopic4Website`

**Mô tả:** Lấy danh sách chủ đề để hiển thị trên website.

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<EzyDataSourceResult<EzyWeb_TopicInfo>>`

---

## `POST /api/v1/DashboardForWebsite/MyDashboardForWebSite`

**Mô tả:** API tổng hợp cho website — trả về nội dung trang chủ.

**Phân quyền:** Public (không cần token)

### Request Body — `DashboardParamModel`
```json
{
  "Domain": "sigo.vn"
}
```

---

## `POST /api/v1/DashboardForWebsite/GetListDataWithSlug`

**Mô tả:** Lấy dữ liệu trang theo slug URL — dùng cho SSR/routing phía website.

**Phân quyền:** Public

### Request Body — `EzyWeb_SlugParamModel`
```json
{
  "Slug": "thue-xe-tu-lai-ha-noi",
  "Domain": "sigo.vn"
}
```

### Response — `EzyResultObject<Dictionary<string, object>>`
Trả về dữ liệu động tuỳ theo loại trang (blog, topic, landing page).

---

## `POST /api/v1/DashboardForWebsite/GetDataListBlog4Website`

**Mô tả:** Lấy danh sách bài viết blog cho website (có phân trang).

**Phân quyền:** Public

### Request Body — `EzyWeb_BlogPostParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 10,
  "TopicId": "topic-guid"
}
```

---

## `POST /api/v1/DashboardForWebsite/GetListReportWebsite4BCT`

**Mô tả:** Lấy dữ liệu báo cáo website cho Bộ Công Thương (BCT).

**Phân quyền:** Public

---

## `POST /api/v1/EzyWeb_BlogPost/List`

**Mô tả:** Lấy danh sách bài viết blog (admin).

**Phân quyền:** `[Authorize]`

### Request Body — `EzyWeb_BlogPostParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "Keyword": "",
  "IsActive": null,
  "TopicId": "topic-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<EzyWeb_BlogPostModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 150,
    "Data": [
      {
        "PostId": "post-guid",
        "Title": "Hướng dẫn thuê xe",
        "Slug": "huong-dan-thue-xe",
        "IsActive": true,
        "ViewCount": 1200,
        "CreatedAt": "2026-01-01T10:00:00Z"
      }
    ]
  }
}
```

---

## `POST /api/v1/EzyWeb_BlogPost/AddNewEzyWeb_UrlRecord`

**Mô tả:** Tạo bài viết mới kèm URL record (slug SEO).

**Phân quyền:** `[Authorize]`

### Request Body — `EzyWeb_BlogPostAddNewConfigModel`
```json
{
  "Title": "Tiêu đề bài viết",
  "Content": "<p>Nội dung...</p>",
  "Domain": "sigo.vn",
  "Slug": "tieu-de-bai-viet"
}
```

---

## `POST /api/v1/EzyWeb_BlogPost/AddNewEzyWeb_UrlRecord_V2`

**Mô tả:** Tạo bài viết mới (phiên bản V2 — hỗ trợ đa domain).

---

## `POST /api/v1/EzyWeb_BlogPost/AddDomain`

**Mô tả:** Thêm domain cho bài viết (publish lên domain mới).

### Request Body — `EzyWeb_BlogPost_Domain_MappingAddModel`
```json
{
  "PostId": "post-guid",
  "Domain": "sigo.vn"
}
```

---

## `POST /api/v1/EzyWeb_BlogPost/DeleteDomain`

**Mô tả:** Xoá domain khỏi bài viết.

---

## `POST /api/v1/EzyWeb_BlogPost/AddLink`

**Mô tả:** Thêm internal link vào bài viết.

---

## `POST /api/v1/EzyWeb_BlogPost/DeleteImage`

**Mô tả:** Xoá ảnh trong bài viết.

---

## `POST /api/v1/EzyWeb_BlogPost/ReplaceLinkInContent`

**Mô tả:** Thay thế các link cũ trong nội dung bài viết (dùng khi migrate domain).

---

## `POST /api/v1/EzyWeb_BlogPost/AddTopicMapping`

**Mô tả:** Gán bài viết vào chủ đề.

### Request Body — `EzyWeb_BlogPostParamModel`
```json
{
  "PostId": "post-guid",
  "TopicId": "topic-guid"
}
```

---

## `POST /api/v1/EzyWeb_BlogPost/RemoveTopicMapping`

**Mô tả:** Gỡ bài viết khỏi chủ đề.

---

## `POST /api/v1/EzyWeb_BlogPost/SyncFromTopic`

**Mô tả:** Đồng bộ bài viết từ chủ đề (cập nhật hàng loạt theo topic).

---

## `POST /api/v1/EzyWeb_BlogPost/Restore`

**Mô tả:** Khôi phục bài viết đã xoá.

---

## `POST /api/v1/EzyWeb_BlogPost_Public/List`

**Mô tả:** Lấy danh sách bài viết blog cho website (không cần auth).

**Phân quyền:** Public

Tương tự `EzyWeb_BlogPost/List` nhưng chỉ trả về bài viết `IsActive = true`.

---

## `POST /api/v1/EzyWeb_NewsPost/List`

**Mô tả:** Lấy danh sách tin tức (admin).

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/EzyWeb_NewsPost_Public/List`

**Mô tả:** Lấy danh sách tin tức (public cho website).

**Phân quyền:** Public

---

## `POST /api/v1/EzyWeb_Topic/List`

**Mô tả:** Lấy danh sách chủ đề.

**Phân quyền:** `[Authorize]`

### Response — `EzyResultObject<EzyDataSourceResult<EzyWeb_TopicModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 20,
    "Data": [
      {
        "TopicId": "topic-guid",
        "Name": "Thuê xe tự lái",
        "Slug": "thue-xe-tu-lai",
        "PostCount": 30
      }
    ]
  }
}
```

---

## `POST /api/v1/EzyWeb_Topic/AddNewEzyWeb_UrlRecord`

**Mô tả:** Tạo chủ đề mới kèm URL record.

### Request Body — `EzyWeb_TopicAddNewConfigModel`
```json
{
  "Name": "Thuê xe tự lái",
  "Domain": "sigo.vn",
  "Slug": "thue-xe-tu-lai"
}
```

---

## `POST /api/v1/EzyWeb_Topic/GetListEzyWeb_UrlRecord_Topic`

**Mô tả:** Lấy danh sách URL records theo chủ đề.

---

## `POST /api/v1/EzyWeb_UrlRecord/List`

**Mô tả:** Lấy danh sách URL records (slug SEO) toàn bộ website.

**Phân quyền:** `[Authorize]`

### Request Body — `EzyWeb_UrlRecordParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 50,
  "Domain": "sigo.vn",
  "IsActive": true
}
```

---

## `POST /api/v1/EzyWeb_UrlRecord/CreateSitemap`

**Mô tả:** Tạo file sitemap.xml cho website.

### Response — `EzyResultObject<ReportExportResultToClientModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "FileUrl": "https://sigo.vn/sitemap.xml"
  }
}
```

---

## `POST /api/v1/EzyWeb_UrlRecord/UpdateSlug`

**Mô tả:** Cập nhật slug của URL record (đổi URL).

### Request Body — `EzyWeb_UrlRecordParamModel`
```json
{
  "UrlRecordId": "url-record-guid",
  "NewSlug": "thue-xe-tu-lai-ha-noi-gia-re"
}
```

### Response — `EzyResultObject<UpdateSlugResultModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "OldSlug": "thue-xe-ha-noi",
    "NewSlug": "thue-xe-tu-lai-ha-noi-gia-re",
    "RedirectCreated": true
  }
}
```

---

## `POST /api/v1/EzyWeb_UrlRecord/UpdateViewCount`

**Mô tả:** Tăng view count cho URL (gọi mỗi khi user truy cập trang).

---

## `POST /api/v1/EzyWeb_UrlRecord/UpdateSlugByTitle`

**Mô tả:** Tự động cập nhật slug từ tiêu đề bài viết (batch update).

---

## `POST /api/v1/ArticleTypeMapping/List`

**Mô tả:** Lấy danh sách mapping giữa loại bài viết và nội dung.

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/ArticleTypeMapping/AddArticleMapping_BlogPost`

**Mô tả:** Gán loại bài viết cho Blog Post.

---

## `POST /api/v1/ArticleTypeMapping/AddArticleMapping_Topic`

**Mô tả:** Gán loại bài viết cho Topic.

---

## `POST /api/v1/ArticleTypeMapping/AddArticleMapping_LandingPage`

**Mô tả:** Gán loại bài viết cho Landing Page.

---

## `POST /api/v1/ReleatedArticleMapping/List`

**Mô tả:** Lấy danh sách bài viết liên quan.

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/ReleatedArticleMapping/InsertOrUpdate`

**Mô tả:** Tạo hoặc cập nhật liên kết bài viết liên quan.

---

## `POST /api/v1/InvalidSlugMapping/List`

**Mô tả:** Lấy danh sách slug không hợp lệ (404 tracking).

**Phân quyền:** `[Authorize]`

---

## `POST /api/v1/InvalidSlugMapping/ReplaceLinkInContent`

**Mô tả:** Thay thế hàng loạt các link lỗi trong nội dung bài viết.

---

## Cấu hình CMS

### `POST /api/v1/ConfigArticleType/List`
Lấy danh sách loại bài viết.

### `POST /api/v1/ConfigArticleType/AddTopicMapping`
Gán chủ đề vào loại bài viết.

### `POST /api/v1/ConfigSystemDomain/List`
Lấy danh sách domain hệ thống.

### `POST /api/v1/ConfigTopic/List`
Lấy danh sách cấu hình chủ đề.

---

## Ghi chú

- Các controller `*_Public` không cần auth — dùng cho SSR website.
- `EzyWeb_BlogPost_Deleted` controller hiển thị bài viết đã xoá mềm (soft delete).
- `EzyWeb_BlogPost_InvalidLinks` / `EzyWeb_Topic_InvalidLinks` hiển thị nội dung chứa link lỗi.
- Mỗi lần đổi slug cần kiểm tra `InvalidSlugMapping` để tạo redirect tránh 404.
