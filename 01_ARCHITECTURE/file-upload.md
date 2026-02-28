# File Upload System

## Mục lục
- [Tổng quan](#tổng-quan)
- [Upload Endpoints](#upload-endpoints)
- [Google Drive Integration](#google-drive-integration)
- [Image Compression Engine](#image-compression-engine)
- [SFTP File Transfer](#sftp-file-transfer)

---

## Tổng quan

```
[Client Upload]
     │ multipart/form-data
     ▼
[UploadFileController]
     │
     ├── Save to local disk
     │
     ├── Upload to Google Drive → store FileId
     │
     └── Enqueue ImageCompressionQueue (async)
           │
           ▼
     [ImageCompressionEngine]
           │
           ├── POST to compression API
           ├── Poll status
           └── Replace original file
```

**Storage strategy:**
- **Primary:** Google Drive (FileId-based)
- **Fallback:** Local disk
- **Report files:** SFTP
- **Image optimization:** External compression API (async)

---

## Upload Endpoints

**File:** `AllianceMiddlemanWebAPI/Controllers/MainBusiness/UploadFileController.cs`

### API Routes

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/v1/Upload/User/MyAvatar` | Upload avatar user (file) |
| POST | `/api/v1/Upload/User/MyAvatar64` | Upload avatar user (base64) |
| POST | `/api/v1/Upload/User/FBAvatar` | Import avatar từ Facebook URL |
| GET | `/api/v1/Upload/User/MyAvatars` | Lấy danh sách avatars |
| POST | `/api/v1/Upload/Project/Logo/{id}` | Upload logo dự án |
| POST | `/api/v1/Upload/Project/Photos` | Upload ảnh dự án |

### Request Headers

| Header | Mô tả | Bắt buộc |
|--------|-------|----------|
| `ProjectId` | Target project | Có |
| `ScreenCode` | Upload category | Có |
| `FieldName` | Database field mapping | Có |
| `Id` | Record identifier | Có |
| `Authorization` | Bearer JWT token | Có |

### File Size Limit

```csharp
// Startup.ConfigureServices()
services.Configure<FormOptions>(options => {
    options.MultipartBodyLengthLimit = long.MaxValue;  // Unlimited
});
```

---

## Google Drive Integration

**Library:** `Ezy.ModuleGoogleService.dll`

### Upload Flow

```csharp
// 1. Save file to local temp
var localPath = SaveToLocal(file);

// 2. Upload to Google Drive
var param = new UploadParam { FilePath = localPath, ... };
var fileId = EzyPictureHelper.UploadToGoogleDrive(param);

// 3. Store FileId in database
user.PicturePath = fileId;

// 4. Generate CDN URL for display
var url = EzyPictureHelper.GetFullUrlThumbnail(fileId, "");
```

### URL Pattern

```
// Google Drive thumbnail URL:
https://drive.google.com/thumbnail?id={FileId}&sz=w{width}

// Hoặc CDN URL configured trong system
```

### Fallback

```csharp
IF GoogleDrive upload fails:
    // Keep local file path
    user.PicturePath = localRelativePath;
    // Serve from local static files
```

---

## Image Compression Engine

**File:** `AllianceMiddlemanWebAPI.Service/Helper/ImageCompressHelper.cs`

### Architecture

```
[Upload/CMS content]
     │
     ▼
[Stage 1: InsertImageCompressQueue]
     │ Scan entities for images
     │ Create queue entries
     ▼
[Stage 2: CompressImage]
     │ Upload to compression API
     │ Get ImageGuid
     ▼
[Stage 3: GetCompressImage]
     │ Poll compression status
     │ Download result
     │ Replace original file
     ▼
[Done]
```

### Configuration (IMAGE_COMPRESSION_SETTING)

```json
{
  "APIUrl": "https://compressimage-api.allianceitsc.com",
  "ResizeAndCompress": "api/v1/ResizeAndCompress",
  "Compress": "api/v1/Compress",
  "GetImageGuids": "api/v1/GetImageGuids",
  "AuthKey": "...",
  "MaxReSize": 1200,
  "IsRunImageCompression": true,
  "MaxRetryGetCompress": 20,
  "MaxRetryWhenFailed": 3
}
```

### Stage 1: InsertImageCompressQueue

```csharp
// Scan sources for uncompressed images:
// - BlogPost.Body (HTML content)
// - Topic.Body
// - ConfigLandingPage_File
// - ConfigSlide_File

// Regex pattern to find images:
Regex: https?://.*/(Upload/WEBSITE_POSTS/[^"]+)

// Priority calculation:
// By view count: 1-5 scale
// By file size:
//   < 100KB  → priority 1
//   < 500KB  → priority 3
//   < 1MB    → priority 5
//   < 5MB    → priority 7
//   < 9MB    → priority 9
//   >= 9MB   → priority 10
```

### Stage 2: CompressImage

```csharp
// Filter: ImageCompressionQueue WHERE IsSuccess == null (pending)
// Lock: CompressImageSemaphore (one at a time)

// API call:
POST {APIUrl}/{CompressMethod}
Content-Type: multipart/form-data
Authorization: {AuthKey}
Body: image file

// CompressMethod: "ResizeAndCompress" hoặc "Compress"
// Response: { "ImageGuid": "...", "Status": "Processing" }

// Update queue:
queue.ImageGuid = response.ImageGuid;
queue.CompressStatus = "Processing";
queue.AttempCount++;
queue.LastRunAt = DateTime.UtcNow;
```

### Stage 3: GetCompressImage

```csharp
// Poll status:
GET {APIUrl}/{GetImageGuids}?guid={ImageGuid}

// Status values:
// "Processing" → retry later
// "Success"    → download compressed image
// "Fail"       → mark as failed

// On success:
// 1. Download compressed file
// 2. Replace original file on disk
// 3. Update metadata: CompressWidth, CompressHeight, CompressSizeBytes
// 4. Mark IsSuccess = true

// Retry: MaxRetryGetCompress (default 20) attempts
// Failed retry: MaxRetryWhenFailed (default 3)
```

### ImageCompressionQueue Entity

| Field | Type | Mô tả |
|-------|------|-------|
| `Table` | string | Source table name |
| `TableId` | long | Record ID |
| `SubPath` | string | Relative file path |
| `ImageGuid` | string | Compression job ID |
| `CompressMethod` | string | "Compress" / "ResizeAndCompress" |
| `Priority` | int | 1-10 (higher = sooner) |
| `CompressStatus` | string | "Processing" / "Success" / "Fail" |
| `IsSuccess` | bool? | null=pending, true=done, false=failed |
| `IsWaitingCompressFile` | bool | Waiting for download |
| `IsReplaceOldFile` | bool | Should replace original |
| `RetryWhenFailed` | bool | Auto-retry on failure |
| `MaxRetryWhenFailed` | int | Max retry count |
| `AttempCount` | int | Current attempt count |
| `LastRunAt` | DateTime? | Last processing time |
| `LastRunResult` | string | Last result description |
| `CompressWidth` | int? | Result image width |
| `CompressHeight` | int? | Result image height |
| `CompressSizeBytes` | long? | Result file size |

### Thumbnail Creation (Service Item Images)

```csharp
// AutoCompressServiceItemImageEngine
// → IServiceItem_ImageService.CreateThumbnail()
// Creates smaller versions for vehicle listing thumbnails
```

---

## SFTP File Transfer

**File:** `AllianceMiddlemanWebAPI.Service/Helper/SFTPHelper.cs`
**Library:** `Renci.SshNet.dll`

### Configuration Entity: ConfigSFTP

| Field | Mô tả |
|-------|-------|
| `Code` | Unique identifier |
| `Host` | SFTP server hostname |
| `Port` | SFTP port (default: 22) |
| `Username` | Login username |
| `Password` | Login password |
| `BasePath` | Base directory on server |

### Download

```csharp
public static string SftpDownloadFile(
    string[] fileList,       // Files to download
    string ftpPath,          // Remote directory
    string host,             // SFTP host
    int port,                // SFTP port
    string userName,         // Username
    string password,         // Password
    out string sMessage)     // Error output
{
    // 1. Connect to SFTP server
    // 2. Create local directory if missing
    // 3. Stream files from remote to local
    // 4. Return local file path
}
```

### Upload

```csharp
public static void SftpUploadFiles(
    string[] fileList,       // Local files to upload
    string ftpPath,          // Remote directory
    string host, int port,
    string userName, string password,
    out string sMessage)
{
    // 1. Connect to SFTP server
    // 2. Create remote directory if needed
    // 3. Upload with buffer size = file size
    // 4. Auto-overwrite enabled
}
```

### Use Cases

| Use Case | Mô tả |
|----------|-------|
| MB Bank Reports | Download/upload đối soát files via SFTP |
| Bulk Data Exchange | Batch file transfers |
| Backup | File backup to remote server |

### ConfigSFTP Reference in MB Bank

```csharp
// ConfigMBBank entity has:
public string SFTPCode { get; set; }         // Reference to ConfigSFTP
public string SFTPInputFolder { get; set; }  // Download folder
public string SFTPOutputFolder { get; set; } // Upload folder
```

---

## Static File Serving

### Middleware Setup

```csharp
// Startup.Configure()
// Lần 1: Serve /Views/Home as /home
app.SetupUseStaticFiles();  // → /home request path

// Lần 2: Standard static files
app.UseStaticFiles();       // → wwwroot, /Upload, etc.
```

### Static File Directories

```
wwwroot/
├── Upload/
│   ├── WEBSITE_POSTS/     # CMS blog images
│   ├── QRCode/            # Generated QR codes
│   └── ...
├── Views/
│   └── Home/              # Static HTML pages
├── Avatar/                # A-Z default avatars
├── Icons/                 # Vehicle makes, types, features
└── Logos/                 # App logos (default, sigo)
```

---

*Xem thêm: [background-engines.md](./background-engines.md) | [notification-system.md](./notification-system.md)*
