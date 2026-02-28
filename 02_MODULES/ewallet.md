# Module: EWallet — Ví điện tử

> **Solution:** `Ezy.Module.EWallet` | **Trạng thái:** Active

## Mục lục
- [Tổng quan](#tổng-quan)
- [Cấu trúc](#cấu-trúc)
- [Entities](#entities)
- [Services](#services)
- [API Endpoints](#api-endpoints)
- [Luồng giao dịch](#luồng-giao-dịch)
- [WalletAction State Machine](#walletaction-state-machine)

---

## Tổng quan

Module EWallet quản lý **ví điện tử nội bộ** của Sigo platform. Mỗi user (chủ xe và người thuê) có một ví điện tử dùng để:

- **Người thuê (Renter):** Nạp tiền vào ví để thanh toán đơn thuê xe
- **Chủ xe (Owner):** Nhận tiền từ đơn hoàn tất, rút về tài khoản ngân hàng qua MB Bank
- **Hệ thống:** Chuyển tiền nội bộ giữa các ví sau khi đơn hoàn tất

**Tích hợp với:**
- MB Bank API — xử lý nạp tiền và rút tiền thực tế
- `AutoWithdrawEngine` — tự động xử lý hàng chờ rút tiền (avg 2,399ms/lần)
- VietQR — tạo mã QR nạp tiền
- `TransferMoneyEngine` — chuyển tiền giữa ví sau khi order hoàn tất

---

## Cấu trúc

```
Ezy.Module.EWallet/
├── Ezy.Module.EWallet.API/
│   └── Controllers/
│       ├── MainBusiness/
│       │   ├── WalletController.cs          # Quản lý ví
│       │   ├── WalletActionController.cs    # Xử lý action (nạp/rút/chuyển)
│       │   └── WalletTransactionController.cs  # Lịch sử giao dịch
│       └── Categories/
│           └── WalletTypeController.cs      # Loại ví
│
├── Ezy.Module.EWallet.Core/
│   └── Data/
│       ├── BusinessData/
│       │   ├── BusinessDataContext.cs       # DbContext chính
│       │   ├── BusinessEntities.Wallet.cs
│       │   ├── BusinessEntities.WalletTransaction.cs
│       │   └── BusinessEntities.WalletAction.cs
│       └── Categories/
│           └── CategoryDataContext.cs       # ConfigWalletType, Currency
│
├── Ezy.Module.EWallet.Service/
│   └── Services/MainBusiness/
│       ├── Wallet/
│       │   ├── IWalletService.cs
│       │   └── WalletService.cs
│       ├── WalletAction/
│       │   ├── IWalletActionService.cs
│       │   └── WalletActionService.cs
│       └── WalletTransaction/
│           ├── IWalletTransactionService.cs
│           └── WalletTransactionService.cs
│
├── Ezy.Module.EWallet.DataShared/           # DTOs dùng chung
└── Script/                                  # Database migration scripts
```

---

## Entities

### `Wallet` — Ví điện tử

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | long | PK auto-increment |
| `ID_GUID` | Guid | Business key (dùng trong API) |
| `UserGuid` | Guid | FK → User |
| `WalletTypeId` | long | FK → ConfigWalletType |
| `Balance` | decimal | Số dư hiện tại |
| `LastTransactionGuid` | Guid | Tham chiếu giao dịch gần nhất |
| `HashValue` | string | Hash kiểm tra tính toàn vẹn số dư |
| `Status` | string | Trạng thái ví (`Active`, `Locked`, `Deleted`) |
| `IsDeleted` | bool | Soft delete |
| `IsValid` | bool | Cờ hợp lệ (sau validate) |
| `DeletedUserInfoJson` | string | Thông tin user khi xoá (JSON) |
| `Log_CreatedDate` | DateTime | Thời điểm tạo |
| `Log_CreatedBy` | string | Người tạo |
| `Log_UpdatedDate` | DateTime | Thời điểm cập nhật |
| `Log_UpdatedBy` | string | Người cập nhật |

**Quan hệ:** `Wallet` 1→N `WalletTransaction`

---

### `WalletTransaction` — Lịch sử giao dịch

Mỗi thay đổi số dư tạo ra 1 bản ghi transaction (immutable audit trail).

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | long | PK |
| `ID_GUID` | Guid | Business key |
| `WalletGuid` | Guid | FK → Wallet |
| `ActionGuid` | Guid | FK → WalletAction (nếu có) |
| `Action` | string | Loại giao dịch: `TopUp`, `Withdraw`, `Transfer`, `Receive`, `OrderPayment`, `OrderRefund` |
| `BalanceChange` | decimal | Số tiền thay đổi (dương: cộng, âm: trừ) |
| `BalanceBeforeTransaction` | decimal | Số dư trước giao dịch |
| `BalanceAfterTransaction` | decimal | Số dư sau giao dịch |
| `TranctionDate` | DateTime | Thời điểm giao dịch |
| `Initiator` | string | UserGuid khởi tạo |
| `RelatedWalletGuid` | Guid | Ví liên quan (khi chuyển tiền) |
| `RelatedUserGuid` | Guid | User liên quan |
| `UserGuid` | Guid | Owner của ví |
| `IsDeleted` | bool | Soft delete |
| `Log_CreatedDate` | DateTime | |
| `Log_UpdatedDate` | DateTime | |

---

### `WalletAction` — Yêu cầu tài chính

Đại diện cho một yêu cầu tài chính (nạp / rút / chuyển) với vòng đời phê duyệt.

| Column | Type | Mô tả |
|--------|------|-------|
| `Id` | long | PK |
| `ID_GUID` | Guid | Business key |
| `WalletId` | Guid | FK → Wallet nguồn |
| `DestinationWalletId` | Guid | FK → Wallet đích (khi transfer) |
| `Type` | string | Loại: `TopUp`, `Withdrawal`, `Transfer` |
| `BalanceChange` | decimal | Số tiền |
| `Status` | string | `New` → `Processing` → `Completed` / `Failed` / `Cancelled` |
| `InitiatedBy` | string | UserGuid khởi tạo |
| `InitiatedAt` | DateTime | Thời điểm tạo |
| `ApprovedBy` | string | UserGuid phê duyệt |
| `ApprovedAt` | DateTime | Thời điểm phê duyệt |
| `ConfirmedBy` | string | UserGuid xác nhận |
| `ConfirmedAt` | DateTime | Thời điểm xác nhận |
| `CancelledBy` | string | UserGuid huỷ |
| `CancelledAt` | DateTime | Thời điểm huỷ |
| `CancellationReason` | string | Lý do huỷ |
| `Reference` | string | External reference (MB Bank transaction ID) |
| `Note` | string | Ghi chú |
| `Description` | string | Mô tả chi tiết |
| `IsDeleted` | bool | Soft delete |

**Quan hệ:** `WalletAction` 1→N `WalletTransaction`

---

## Services

### `IWalletService` — Thao tác số dư

| Method | Mô tả |
|--------|-------|
| `TopUp(walletGuid, balance, out msg, actionGuid)` | Nạp tiền vào ví |
| `Withdraw(walletGuid, balance, out msg, actionGuid)` | Trừ tiền khỏi ví (rút) |
| `Transfer(srcGuid, balance, dstGuid, out msg, actionGuid)` | Chuyển tiền đi |
| `Receive(srcGuid, balance, dstGuid, out msg, actionGuid)` | Nhận tiền chuyển đến |
| `TransferCredits(srcGuid, balance, dstGuid, out msg, actionGuid)` | Chuyển credits |
| `Validate(walletId, out msg)` | Kiểm tra hash toàn vẹn ví |
| `ValidateAll(out msg)` | Kiểm tra tất cả ví |
| `Restore(walletId, out msg)` | Khôi phục ví đã xoá |
| `DeleteWallet(param, user)` | Xoá ví (soft delete) |

### `IWalletActionService` — Workflow phê duyệt

| Nhóm | Method | Mô tả |
|------|--------|-------|
| **Nạp tiền** | `FundingRequest(model, out msg)` | Tạo yêu cầu nạp |
| | `FundingApprove(id, out msg, needConfirm)` | Phê duyệt nạp |
| | `FundingConfirm(id, out msg)` | Xác nhận nạp |
| | `FundingRunJob(model, out msg)` | Chạy job nạp |
| **Rút tiền** | `WithdrawalRequest(model, out msg)` | Tạo yêu cầu rút |
| | `WithdrawalApprove(id, out msg)` | Phê duyệt rút |
| | `WithdrawalConfirm(id, out msg)` | Xác nhận rút → gọi MB Bank |
| **Chuyển tiền** | `TransferRequest(model, out msg)` | Tạo yêu cầu chuyển |
| | `TransferApprove(id, out msg, needConfirm)` | Phê duyệt chuyển |
| | `TransferConfirm(id, out msg)` | Xác nhận chuyển |
| | `TransferRunJob(model, out msg)` | Chạy job chuyển |
| **Chung** | `ActionCancel(id, reason, out msg)` | Huỷ action |

### `IWalletTransactionService` — Giao dịch

| Method | Mô tả |
|--------|-------|
| `SaveTransaction(trans, walletTypeId, out msg)` | Ghi nhận giao dịch mới |
| `RollBackTransaction(id, out msg)` | Hoàn tác giao dịch |
| `DontFilterDate` (property) | Bật/tắt filter theo ngày khi list |

---

## API Endpoints

Base URL: `/api/v1/EWallet/`

### Wallet

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Wallet/List` | Danh sách ví với filter | Required |
| POST | `/Wallet/TopUp` | Nạp tiền vào ví | Required |
| POST | `/Wallet/Transfer` | Chuyển tiền giữa ví | Required |
| POST | `/Wallet/Validate` | Kiểm tra tính toàn vẹn ví | Required |
| POST | `/Wallet/ValidateAll` | Kiểm tra tất cả ví | Admin |
| POST | `/Wallet/Restore` | Khôi phục ví đã xoá | Admin |

### WalletAction — Quản lý yêu cầu

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Action/List` | Danh sách tất cả actions | Required |
| POST | `/Action/Cancel` | Huỷ action | Required |
| POST | `/Action/Approve` | Phê duyệt action | Required |
| POST | `/Action/Confirm` | Xác nhận action | Required |
| POST | `/Action/Processing/List` | Actions đang xử lý | Required |
| POST | `/Action/NewOrProcessing/List` | Actions mới hoặc đang xử lý | Required |

### WalletAction — Nạp tiền (Funding)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Action/Funding/Request` | Tạo yêu cầu nạp | Required |
| POST | `/Action/Funding/Approve` | Phê duyệt nạp | Required |
| POST | `/Action/Funding/Confirm` | Xác nhận nạp tiền | Required |
| POST | `/Action/Funding/RunJob` | Chạy job nạp tự động | Admin |

### WalletAction — Rút tiền (Withdrawal)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Action/Withdrawal/Approve` | Phê duyệt rút tiền | Required |
| POST | `/Action/Withdrawal/Confirm` | Xác nhận rút → trigger MB Bank | Required |

### WalletAction — Chuyển tiền (Transfer)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Action/Transfer/Request` | Tạo yêu cầu chuyển | Required |
| POST | `/Action/Transfer/Approve` | Phê duyệt chuyển | Required |
| POST | `/Action/Transfer/Confirm` | Xác nhận chuyển | Required |
| POST | `/Action/Transfer/RunJob` | Chạy job chuyển tự động | Admin |

### WalletTransaction — Lịch sử

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/Transaction/List` | Lịch sử giao dịch (có filter ngày) | Required |
| POST | `/TransactionExp/List` | Lịch sử giao dịch (không filter ngày) | Required |

---

## Luồng giao dịch

### Rút tiền (Owner Withdrawal)

```
[Owner App]                   [EWallet API]               [AutoWithdrawEngine]       [MB Bank]
     │                              │                              │                     │
     ├─ POST /Withdrawal/Request ──►│                              │                     │
     │                              │ Validate balance             │                     │
     │                              │ Validate bank account        │                     │
     │                              │ Create WalletAction{New}     │                     │
     │◄─ action_guid ───────────────│                              │                     │
     │                              │                              │                     │
     │  [Staff/Auto]                │                              │                     │
     ├─ POST /Withdrawal/Approve ──►│                              │                     │
     │                              │ WalletAction{Processing}     │                     │
     │◄─ OK ────────────────────────│                              │                     │
     │                              │                              │                     │
     ├─ POST /Withdrawal/Confirm ──►│                              │                     │
     │                              │ WalletService.Withdraw()     │                     │
     │                              │ → Debit balance              │                     │
     │                              │ → Create WalletTransaction   │                     │
     │                              │                              │                     │
     │                              │──── Enqueue withdraw ───────►│                     │
     │                              │                              ├─ Call TransferFund─►│
     │                              │                              │◄─ Transaction ID ───│
     │                              │                              │ Update WalletAction  │
     │◄─ SMS + Push Notification ───│◄──── Status update ─────────│                     │
```

### Nạp tiền (TopUp)

```
User → POST /Action/Funding/Request
    → Create WalletAction {TopUp, New}
    → Trả về QR / bank transfer info

[User chuyển khoản thật qua MB Bank / VietQR]

AutoMappingSMS_Engine → Detect incoming transfer via SMS
    → Match với WalletAction
    → POST /Action/Funding/Confirm (tự động)
    → WalletService.TopUp() → Cộng balance
    → Create WalletTransaction
    → Push notification tới user
```

### Thanh toán đơn hàng

```
Order hoàn tất
    │
    ▼
TransferMoneyEngine
    ├─ WalletService.Withdraw(renter_wallet, amount) → trừ tiền renter
    └─ WalletService.Receive(owner_wallet, amount)   → cộng tiền owner
    │
    ▼
2 WalletTransaction records tạo ra (debit + credit)
    │
    ▼
Notification gửi cho cả 2 bên
```

---

## WalletAction State Machine

```
                     ┌─────────┐
    User request ───►│   New   │
                     └────┬────┘
                          │ Staff/Auto Approve
                     ┌────▼──────────┐
                     │  Processing   │
                     └────┬──────────┘
              ┌───────────┼───────────┐
              │ Confirm   │ Fail      │ Cancel
         ┌────▼────┐ ┌────▼────┐ ┌───▼──────┐
         │Completed│ │ Failed  │ │Cancelled │
         └─────────┘ └─────────┘ └──────────┘
```

| Trạng thái | Ý nghĩa |
|-----------|---------|
| `New` | Vừa tạo, chờ phê duyệt |
| `Processing` | Đã phê duyệt, đang xử lý |
| `Completed` | Hoàn tất |
| `Failed` | Thất bại (ngân hàng từ chối, lỗi kỹ thuật) |
| `Cancelled` | Bị huỷ bởi user hoặc staff |

---

## Wallet Hash Integrity

### Hash Algorithm

```csharp
// HashValue dùng để kiểm tra tính toàn vẹn số dư
// Khi Balance thay đổi → HashValue được tính lại

// Validate():
// 1. Recalculate expected hash from Balance + LastTransactionGuid
// 2. Compare with stored HashValue
// 3. If mismatch → wallet may be tampered → set IsValid = false
```

### ValidateAll Flow

```
POST /api/v1/EWallet/Wallet/ValidateAll (Admin only)
1. Get all wallets
2. FOR EACH wallet:
   a. Recalculate hash from current Balance
   b. Compare with stored HashValue
   c. If mismatch → flag wallet as invalid
3. Return summary report
```

---

## Wallet Deletion

### Soft Delete Flow

```csharp
// WalletService.DeleteWallet()
1. Validate: Balance must be 0 (or within tolerance)
2. Save user info snapshot to DeletedUserInfoJson
3. Set IsDeleted = true
4. Set Status = "Deleted"
5. Keep all WalletTransaction history (audit trail)
```

### Restore Flow

```csharp
// WalletService.Restore(walletId)
1. Find wallet by ID (including deleted)
2. Set IsDeleted = false
3. Set Status = "Active"
4. Recalculate HashValue
5. Revalidate balance against transaction history
```

---

## Wallet Transfer Limits (USER_WALLET_SETTING)

```json
{
  "DailyLimit": 60000000,
  "TransferUpperLimit": 20000000,
  "TransferLowerLimit": 10000,
  "BalanceUpperLimit": 100000000
}
```

| Rule | Value | Mô tả |
|------|-------|-------|
| Daily limit | 60,000,000 VND | Tổng giới hạn giao dịch/ngày |
| Transfer upper | 20,000,000 VND | Giới hạn 1 lần chuyển |
| Transfer lower | 10,000 VND | Số tiền tối thiểu/lần chuyển |
| Balance upper | 100,000,000 VND | Số dư tối đa trong ví |

---

*Xem thêm: [ewallet-withdraw.md](../04_BUSINESS_FLOWS/ewallet-withdraw.md) | [mbbank.md](../03_API/integrations/mbbank.md) | [ADR-0010](../07_ADR/0010-modular-design.md)*
