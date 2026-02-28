# Core Module: AppSystem — Quản trị hệ thống

> **Project:** `AllianceMiddlemanWebAPI` | **Domain:** Hệ thống nội bộ

## Mục lục
- [Tổng quan](#tổng-quan)
- [Controllers](#controllers)
- [API Endpoints](#api-endpoints)
- [Categories (50+ Config)](#categories)
- [Reports](#reports)
- [System utilities](#system-utilities)

---

## Tổng quan

AppSystem bao gồm các chức năng quản trị hệ thống, dùng bởi admin và staff:
- Quản lý staff (nhân viên nội bộ)
- Quản lý email templates
- System testing & integration tests
- Quản lý user devices, roles
- Thông tin thuế
- 50+ Config controllers (lookup data)
- Báo cáo tài chính (công nợ, thuế)

---

## Controllers

### AppSystem Controllers

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `StaffController` | `api/v1/Staff` | CRUD nhân viên nội bộ |
| `EmailTemplateController` | `api/v1/EmailTemplate` | Quản lý mẫu email |
| `BCTController` | `api/v1/BCT` | Báo cáo BCT (Bộ Công Thương) |
| `UserAccessRegisterTokenController` | `api/v1/UserAccessRegisterToken` | Token đăng ký device |
| `UserRoleController` | `api/v1/UserRole` | Phân quyền user |
| `UserUseDeviceController` | `api/v1/UserUseDevice` | Lịch sử thiết bị |
| `UserLogin_TaxInfoController` | `api/v1/UserLogin_TaxInfo` | Thông tin thuế user |
| `SystemActionSimpleController` | `api/v1/SystemActionSimple` | Actions hệ thống |
| `SystemTestUnitController` | `api/v1/SystemTestUnit` | Unit test runner |
| `SystemTestUnitCaseController` | `api/v1/SystemTestUnitCase` | Test cases |
| `RentCarHelperIntergrationTestController` | `api/v1/RentCarHelperTest` | Integration tests |
| `ProjectReportSimpleController` | `api/v1/ProjectReportSimple` | Báo cáo đơn giản |

### Root Controllers

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `AccountController` | `api/v1/Account` | Auth (xem [core-user.md](./core-user.md)) |
| `CacheController` | `api/v1/Cache` | Quản lý Redis cache |
| `ExceptionController` | `api/v1/Exception` | Log exceptions |
| `GlobalAppSettingController` | `api/v1/GlobalAppSetting` | App settings |
| `LoadTestController` | `api/v1/LoadTest` | Load testing |

---

## API Endpoints

### Staff Management

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Staff/List` | Danh sách nhân viên | Admin |
| POST | `/api/v1/Staff/Add` | Thêm nhân viên | Admin |
| POST | `/api/v1/Staff/Update` | Cập nhật nhân viên | Admin |
| POST | `/api/v1/Staff/Delete` | Xoá nhân viên | Admin |

### Cache Management

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Cache/Refresh` | Refresh toàn bộ cache | Admin |
| POST | `/api/v1/Cache/GetCache` | Xem cache content | Admin |
| POST | `/api/v1/Cache/Test` | Test cache connection | Admin |
| POST | `/api/v1/Cache/GetVersion` | Version info | Admin |

### System Utilities

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/v1/Exception/SetHandel` | Cấu hình exception handler | Admin |
| POST | `/api/v1/Exception/SaveLog` | Ghi log exception | Required |
| GET | `/api/v1/LoadTest/Get` | Smoke test endpoint | Public |

---

## Categories

Hệ thống có **50+ Config controllers** quản lý dữ liệu lookup. Tất cả đều follow pattern CRUD chuẩn: `List`, `Add`, `Update`, `Delete`.

### Phân nhóm Config

#### Xe & Dịch vụ cho thuê

| Config | Mô tả |
|--------|-------|
| `ConfigVehicleMake` | Hãng xe (Toyota, Honda...) |
| `ConfigVehicleModel` | Model xe (Vios, City...) |
| `ConfigVehicleType` | Loại xe (Sedan, SUV...) |
| `ConfigVehicleTypeSub` | Chi tiết loại xe |
| `ConfigVehicleSegment` | Phân khúc xe |
| `ConfigVehicleNoOfSeat` | Số chỗ ngồi |
| `ConfigVehicleColor` | Màu xe |
| `ConfigVehicleFuelType` | Loại nhiên liệu |
| `ConfigVehicleTransmissionType` | Hộp số |
| `ConfigVehicleInsuranceCompany` | Công ty bảo hiểm |
| `ConfigVehicleMultidayRentalDiscount` | Quy tắc giảm giá nhiều ngày |
| `ConfigVehicleRentalCancelReason` | Lý do huỷ cho thuê |
| `ConfigVehicleDiscountSuggestion` | Cài đặt gợi ý giảm giá |

#### Đơn hàng & Thanh toán

| Config | Mô tả |
|--------|-------|
| `ConfigOrderStatus` | Trạng thái đơn hàng |
| `ConfigPaymentType` | Phương thức thanh toán |
| `ConfigCompletionFee` | Phí hoàn tất |
| `ConfigDeliveryFee` | Phí giao xe |
| `ConfigDiscountCodeGroup` | Nhóm mã giảm giá |
| `ConfigDiscountMethod` | Phương thức giảm giá |
| `ConfigMyOrderGroup` | Nhóm đơn hàng |
| `ConfigReportOrderReason` | Lý do báo cáo đơn |
| `ConfigReportUserReason` | Lý do báo cáo user |
| `DiscountCode` | Mã giảm giá |

#### Dịch vụ

| Config | Mô tả |
|--------|-------|
| `ConfigRentalServiceCategory` | Danh mục dịch vụ |
| `ConfigRentalServiceCategorySetting` | Cài đặt danh mục |
| `ConfigRentalServiceRatingComment` | Mẫu comment đánh giá |
| `ConfigRentalServiceRatingPoint` | Cài đặt điểm rating |
| `ConfigRentalRequiredItem` | Vật dụng yêu cầu khi thuê |
| `ConfigServiceDocumentType` | Loại tài liệu xe |
| `ConfigServiceImageType` | Loại ảnh xe |
| `ConfigServiceItemFeature` | Tính năng xe (GPS, Camera...) |
| `ConfigCriteriaPriority` | Tiêu chí ưu tiên hiển thị |

#### Hệ thống & Địa chỉ

| Config | Mô tả |
|--------|-------|
| `ConfigAddress` | Địa chỉ (tỉnh/thành, quận/huyện, phường/xã) |
| `ConfigBank` | Danh sách ngân hàng |
| `ConfigBeneficiaryBank` | Ngân hàng thụ hưởng |
| `ConfigCountry` | Danh sách quốc gia |
| `ConfigAppVersion` | Version app (force update) |
| `ConfigHMACKey` | HMAC keys cho security |
| `ConfigRSAKey` | RSA keys cho encryption |
| `ConfigSFTP` | Cấu hình SFTP server |
| `ConfigMBBank` | Cấu hình MB Bank API |
| `ConfigMISAInvoice` | Cấu hình MISA Invoice |

#### Giao diện

| Config | Mô tả |
|--------|-------|
| `ConfigLandingPage` | Cấu hình landing page |
| `ConfigLandingPageSetting` | Cài đặt chi tiết landing page |
| `ConfigSlide` | Slide banner |
| `ConfigSlide_File` | File ảnh slide |
| `ConfigSlideScreenMapping` | Mapping slide → screen |
| `ConfigUserImageType` | Loại ảnh user |
| `ConfigUserImageTypeDetail` | Chi tiết loại ảnh user |
| `ConfigUserVerified` | Loại xác minh user |
| `ConfigMenu` | Menu cấu hình |

---

## Reports

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `BangTinhCongNoController` | `api/v1/BangTinhCongNo` | Bảng tính công nợ (debt report) |
| `TongHopThongTinKhaiThueSanTMDTController` | `api/v1/TongHopKhaiThue` | Tổng hợp khai thuế sàn TMĐT |

Báo cáo tài chính phục vụ:
- Đối soát công nợ giữa Sigo và các owner
- Khai thuế cho sàn thương mại điện tử (theo quy định)

---

## System utilities

### Uploaded File Management

| Controller | Route | Mô tả |
|-----------|-------|-------|
| `UploadFileController` | `api/v1/Upload` | Upload file chung |
| `ImageCompressionQueueController` | `api/v1/ImageCompressionQueue` | Quản lý queue nén ảnh |
| `HostAddressController` | `api/v1/HostAddress` | Quản lý host addresses |
| `CoinReward_ProcessController` | `api/v1/CoinReward_Process` | Quản lý coin/điểm thưởng |
| `GetOTPBlacklistController` | `api/v1/GetOTPBlacklist` | Blacklist OTP spam |

### Staff Management

Staff (nhân viên) Sigo có 2 loại:
- **Admin** — toàn quyền, dùng tất cả admin controllers
- **Staff** — xem và xử lý đơn, duyệt xe, duyệt user, nhưng không thể thay đổi config/system

Staff được tạo tự động qua SP `sp_AutoInsertStaff_Json` khi UserLogin có role Admin/Staff. Mỗi staff liên kết với UserLogin qua `UserLoginId`.

### Device Management

`UserUseDevice` theo dõi thiết bị đã đăng nhập:
- Mỗi device có `DeviceId`, `DeviceType` (iOS/Android/Web), `FCMToken`
- `DeleteUserDeviceNotActiveEngine` tự động xoá device không hoạt động
- Dùng cho push notification targeting (gửi FCM đúng device)

### Cache Management

Redis cache pattern:
```
api/v1/Cache/Refresh → xoá toàn bộ cache → reload lại từ DB
api/v1/Cache/GetCache → debug: xem cache entries
```

Cache được dùng cho: config data (Categories), user session, search results tạm.

### GlobalAppSetting

`/api/v1/GlobalAppSetting/KeepSessionAlive` — endpoint được `KeepServerAliveEngine` gọi liên tục để giữ server không idle timeout. Đồng thời dùng để kiểm tra system health.

---

### Error Handling

| Controller | Endpoint | Mô tả |
|-----------|----------|-------|
| `ExceptionController` | `/SaveLog` | Client gửi crash log, JS errors |
| `ExceptionController` | `/SetHandel` | Admin toggle exception handler mode |

Exception logs lưu vào DB → admin xem qua portal.

---

*Xem thêm: [config-keys.md](../06_OPERATIONS/config-keys.md) | [overview.md](../01_ARCHITECTURE/overview.md)*
