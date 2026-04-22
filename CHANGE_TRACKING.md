# Change Tracking — Documentation vs Code Alignment

> File này giúp QC và AI agents biết tài liệu nào cần review khi code thay đổi.

---

## Status Legend

- ✅ **VERIFIED** — Tài liệu khớp với code hiện tại
- ⚠️ **NEEDS_REVIEW** — Code đã thay đổi, tài liệu có thể outdated
- ❌ **OUTDATED** — Tài liệu chắc chắn sai, cần cập nhật

---

## Document Registry

### Architecture Documents (01_ARCHITECTURE/)

| Document | Last Verified | Status | Key Source Files | Review Priority |
|----------|--------------|--------|------------------|----------------|
| [overview.md](./01_ARCHITECTURE/overview.md) | 2026-03-02 | ✅ | WebAPI/Startup.cs, WebAPI/Program.cs | P2 |
| [project-structure.md](./01_ARCHITECTURE/project-structure.md) | 2026-03-02 | ✅ | Solution .sln, WebAPI/Startup.cs | P2 |
| [base-service-pattern.md](./01_ARCHITECTURE/base-service-pattern.md) | 2026-03-02 | ✅ | Service/Services/EFBaseCategoryService.cs, WebAPI/Controllers/BaseCategoryController.cs | P1 |
| [database-design.md](./01_ARCHITECTURE/database-design.md) | 2026-03-02 | ✅ | Core/Data/BusinessData/BusinessEntities.cs, EF Migrations | P1 |
| [stored-procedures.md](./01_ARCHITECTURE/stored-procedures.md) | 2026-03-02 | ✅ | Core/Data/Stores/EEzyStoredProcedureNames.cs, Core/Data/Stores/SPEntities/ | P1 |
| [helper-algorithms.md](./01_ARCHITECTURE/helper-algorithms.md) | 2026-03-02 | ✅ | Service/Helpers/RentCarHelper.cs, Service/Helpers/SearchingVehicleHelper.cs, Service/Helpers/ConfigDeliveryFeeHelper.cs | P0 |
| [background-engines.md](./01_ARCHITECTURE/background-engines.md) | 2026-03-02 | ✅ | Service/Engines/ProjectEngineAsyncHelper.cs, Service/Engines/*.cs | P1 |
| [notification-system.md](./01_ARCHITECTURE/notification-system.md) | 2026-03-02 | ✅ | Service/Helpers/PushNotificationHelper.cs, Service/Helpers/NotificationHelper.cs | P2 |
| [file-upload.md](./01_ARCHITECTURE/file-upload.md) | 2026-03-02 | ✅ | Service/Services/FileUploadService.cs | P2 |
| [security-middleware.md](./01_ARCHITECTURE/security-middleware.md) | 2026-03-02 | ✅ | WebAPI/Startup.cs (JWT config section) | P1 |
| [validation-error-catalog.md](./01_ARCHITECTURE/validation-error-catalog.md) | 2026-03-02 | ✅ | Core/Services/TextDisplayKeys.cs, Service/Services/MainBusiness/RentalServiceItem/RentalServiceItemService_Booking.cs:CheckCanBookRentalService() | P0 |
| [deployment.md](./01_ARCHITECTURE/deployment.md) | 2026-03-02 | ✅ | Dockerfile, Jenkinsfile | P2 |

### Business Flow Documents (04_BUSINESS_FLOWS/)

| Document | Last Verified | Status | Key Source Files | Review Priority |
|----------|--------------|--------|------------------|----------------|
| [booking-flow.md](./04_BUSINESS_FLOWS/booking-flow.md) | 2026-03-02 | ✅ | Service/Services/MainBusiness/Order/OrderService_UserAction.cs:BookRentalService(), Service/Services/MainBusiness/RentalServiceItem/RentalServiceItemService_Booking.cs:CheckCanBookRentalService() | P0 |
| [cancel-flow.md](./04_BUSINESS_FLOWS/cancel-flow.md) | 2026-03-02 | ✅ | Service/Engines/CancelBookingEngine.cs, Service/Helpers/RentCarHelper.cs:CalcRerturnDepositAmount() | P0 |
| [pricing-calculation.md](./04_BUSINESS_FLOWS/pricing-calculation.md) | 2026-03-02 | ✅ | Service/Helpers/RentCarHelper.cs (all pricing methods) | P0 |
| [order-status-machine.md](./04_BUSINESS_FLOWS/order-status-machine.md) | 2026-03-02 | ✅ | Service/Services/MainBusiness/Order/OrderService_UserAction.cs | P0 |
| [owner-onboarding.md](./04_BUSINESS_FLOWS/owner-onboarding.md) | 2026-03-02 | ✅ | Service/Services/MainBusiness/RentalServiceItem/RentalService_SelfdriveCarRentalBaseService.cs | P1 |
| [ewallet-withdraw.md](./04_BUSINESS_FLOWS/ewallet-withdraw.md) | 2026-03-02 | ✅ | Service/Services/EWallet/WalletActionService.cs, Service/Engines/AutoWithdrawEngine.cs | P1 |

### Test Coverage Documents (03_API/)

| Document | Last Verified | Status | Key Source Files | Review Priority |
|----------|--------------|--------|------------------|----------------|
| [test-coverage-matrix.md](./03_API/test-coverage-matrix.md) | 2026-03-03 | ✅ | All test scenario files | P0 |
| [rental-service.test-scenarios.md](./03_API/rental-service.test-scenarios.md) | 2026-03-02 | ✅ | Service/Services/MainBusiness/Order/OrderService_UserAction.cs:BookRentalService(), Service/Helpers/SearchingVehicleHelper.cs | P0 |
| [cancel-flow.test-scenarios.md](./03_API/cancel-flow.test-scenarios.md) | 2026-03-02 | ✅ | Service/Engines/CancelBookingEngine.cs, Service/Helpers/RentCarHelper.cs:CalcRerturnDepositAmount() | P0 |
| [ewallet-order-lifecycle.test-scenarios.md](./03_API/ewallet-order-lifecycle.test-scenarios.md) | 2026-03-02 | ✅ | Service/Helpers/WalletHelper.cs, Service/Engines/AutoCompleteOrderEngine.cs | P0 |
| [owner-vehicle-management.test-scenarios.md](./03_API/owner-vehicle-management.test-scenarios.md) | 2026-03-02 | ✅ | WebAPI/Controllers/MainBusiness/RentalServiceController.cs, Service/Services/MainBusiness/RentalServiceItem/InsertNewRentalService() | P1 |

### BDD Gherkin Files (03_API/bdd/)

| Document | Last Verified | Status | Source Scenarios |
|----------|--------------|--------|------------------|
| [booking-flow.bdd.md](./03_API/bdd/booking-flow.bdd.md) | 2026-03-02 | ✅ | B-*, OC-*, OP-* |
| [cancel-flow.bdd.md](./03_API/bdd/cancel-flow.bdd.md) | 2026-03-02 | ✅ | RCC-*, OCC-*, SCC-*, REF-* |
| [order-lifecycle.bdd.md](./03_API/bdd/order-lifecycle.bdd.md) | 2026-03-02 | ✅ | OBG-*, OEN-*, EDP-*, EWD-*, FIN-* |

### AI/System Documents

| Document | Last Verified | Status | Notes |
|----------|--------------|--------|-------|
| [llms.txt](./llms.txt) | 2026-03-03 | ✅ | Includes traceability matrix + coverage dashboard + worked pricing example |
| [SYSTEM_MAP.md](./SYSTEM_MAP.md) | 2026-03-03 | ✅ | Includes AI Agent Decision Tree + method business logic descriptions |
| [README.md](./README.md) | 2026-03-03 | ✅ | Main index with Quick Start + inline facts |

---

## How to Update

### Khi code thay đổi:
1. Xác định documents liên quan (dựa trên "Key Source Files" column)
2. Đánh dấu documents đó thành ⚠️ **NEEDS_REVIEW**
3. Ghi chú: "Code changed in [file] on [date]"

### Khi review tài liệu:
1. Đọc code hiện tại, so sánh với tài liệu
2. Cập nhật nội dung tài liệu nếu cần
3. Đổi status thành ✅ **VERIFIED** + cập nhật "Last Verified" date

### Cho AI agents:
1. Trước khi trust nội dung tài liệu → kiểm tra status trong bảng trên
2. Nếu status = ⚠️ hoặc ❌ → đọc source code trực tiếp thay vì dựa vào tài liệu
3. Sau khi cập nhật tài liệu → update bảng này
4. **Staleness rule:** nếu Last Verified date > 30 ngày → coi như ⚠️ NEEDS_REVIEW bất kể status hiện tại
5. Ưu tiên review theo Review Priority: P0 trước, P1 sau, P2 cuối

---

## Review Priority Legend

- **P0** — Business-critical: pricing, booking, cancel, coverage matrix. Sai = ảnh hưởng trực tiếp đến test/code generation.
- **P1** — Important: base patterns, engines, security, wallet. Sai = ảnh hưởng gián tiếp.
- **P2** — Low risk: overview, deployment, file upload. Thay đổi ít ảnh hưởng đến business logic.

---

## Change History

> Ghi lại khi documents chuyển status. Giúp track pattern drift theo thời gian.

| Date | Document | Old Status | New Status | Change Description |
|------|----------|-----------|------------|-------------------|
| 2026-03-03 | README.md | ✅ | ✅ VERIFIED | Added Quick Start section + "5 điều cần biết ngay" inline facts |
| 2026-03-03 | test-coverage-matrix.md | ✅ | ✅ VERIFIED | Added "Cách dùng" guide + inline scenario descriptions + TL;DR for test data |
| 2026-03-03 | llms.txt | ✅ | ✅ VERIFIED | Added worked pricing example (3-day Toyota Vios rental) |
| 2026-03-03 | SYSTEM_MAP.md | ✅ | ✅ VERIFIED | Added business logic descriptions to all 25 method contracts |
| 2026-03-02 | All documents | — | ✅ VERIFIED | Initial documentation creation and verification |

---

*Tạo lần đầu: 2026-03-02 | Last full review: 2026-03-03*
