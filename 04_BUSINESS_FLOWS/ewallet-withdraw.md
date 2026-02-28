# Luồng nghiệp vụ: Rút tiền ví

## Tổng quan

Luồng chủ xe rút tiền từ ví AllianceMiddleman về tài khoản ngân hàng.

---

## Sơ đồ luồng

```
[Chủ xe]                  [AllianceMiddleman]            [MB Bank]
     │                           │                           │
     │── Yêu cầu rút tiền ──────►│ EWallet/Withdraw          │
     │                           │ Kiểm tra số dư            │
     │                           │ Tạo WithdrawRequest       │
     │◄─ Xác nhận yêu cầu ───────│                           │
     │                           │                           │
     │                    [AutoWithdrawEngine]               │
     │                           │── Chuyển tiền ───────────►│
     │                           │◄─ Kết quả giao dịch ──────│
     │                           │ Cập nhật WalletTransaction│
     │◄─ SMS/Push thông báo ─────│                           │
```

---

## Chi tiết

### Yêu cầu rút tiền
- Module: `Ezy.Module.EWallet`
- Kiểm tra: số dư tối thiểu, tài khoản ngân hàng đã đăng ký

### Xử lý tự động
- Engine: `AutoWithdrawEngine`
- Gọi: `MBBank/TransferFund/AutoWithdraw_MBBank`
- **Hiệu năng:** avg 2,399ms/lần

### Điều kiện rút tiền

| Điều kiện | Giá trị | Source |
|-----------|---------|--------|
| Số tiền tối thiểu | **≥ 2,000 VND** | `MBBank_TransferFundService_AutoWithdraw.cs:84` |
| Giới hạn/ngày | **60,000,000 VND** (default, cấu hình qua `ConfigWalletType`) | `WalletDefaultValues.DailyLimit` |
| Phí rút tiền | **0 VND** (default, cấu hình qua `USER_WALLET_SETTING`) | `WalletHelper.AutoWithdrawFee` |
| Thời gian xử lý | **~2.4 giây** (avg) qua `AutoWithdrawEngine` | `background-engines.md` |

**Lưu ý:**
- Số tiền < 2,000 VND → tự động cancel với `IsIgnore = true`, message: *"Số tiền muốn rút phải lớn hơn hoặc bằng 2000"*
- Phí rút có thể cấu hình qua SystemConfig key `USER_WALLET_SETTING` (`AutoWithdrawFee`, `WithdrawFee`)
- Daily limit hiện tại commented out trong `WalletTransactionService` (lines 273-281) nhưng infrastructure sẵn sàng

### Yêu cầu trước khi rút

| Yêu cầu | Chi tiết |
|----------|----------|
| Ví phải tồn tại | Auto-create nếu chưa có (`WalletHelper.GetUserWalletPayment`) |
| Tài khoản ngân hàng | Phải có `UserLogin_BeneficiaryBank_Mapping` với `IsVerified = true` |
| Số dư khả dụng | `AvailableBalance ≥ RequestAmount` (trừ pending withdrawals/transfers) |
| AutoWithdraw config | Phải enabled trong system config |

### Loại chuyển khoản

| Loại | Điều kiện | Mô tả |
|------|-----------|-------|
| `INHOUSE` | Cùng ngân hàng MB Bank | Chuyển nội bộ |
| `FAST` | Khác ngân hàng | Chuyển liên ngân hàng (Napas) |

Default bank: **MBBANK** (`WalletHelper.TopUpBankCode`)

---

## Trạng thái WithdrawRequest

```
NEW ──► PROCESSING ──► SUCCESS
              │
              └───────► FAILED ──► Retry (thủ công qua admin)
              │
              └───────► CANCELLED (user huỷ)
```

| Status | Mô tả |
|--------|-------|
| `NEW` | Vừa tạo, chờ auto-process |
| `PROCESSING` | Đã approved, đang chuyển tiền qua MB Bank |
| `SUCCESS` | Chuyển thành công |
| `FAILED` | Lỗi giao dịch (check `MBBank_APICall_Log`) |
| `CANCELLED` | User huỷ yêu cầu |

---

## Files tham khảo

| File | Nội dung |
|------|----------|
| `Ezy.Module.EWallet.Core/DataCommon/WalletDefaultValues.cs` | Constants (DailyLimit) |
| `AllianceMiddlemanWebAPI.Service/Helper/WalletHelper.cs` | Wallet operations, default config |
| `AllianceMiddlemanWebAPI.Service/Services/MainBusiness/MBBank/MBBank_Transfer/MBBank_TransferFundService_AutoWithdraw.cs` | Auto withdraw logic |
| `Ezy.Module.EWallet.API/Controllers/MainBusiness/WalletActionController.cs` | API endpoints |

---

*API liên quan: [integrations/mbbank.md](../03_API/integrations/mbbank.md) | [ewallet.md](../02_MODULES/ewallet.md)*
*Test scenarios chi tiết: [ewallet-order-lifecycle.test-scenarios.md](../03_API/ewallet-order-lifecycle.test-scenarios.md)*

---

## QC Test Checkpoints

| # | Checkpoint | DB Assertions | API Response | Side Effects |
|---|-----------|---------------|-------------|-------------|
| 1 | Yêu cầu rút tiền | WithdrawRequest created (Status=NEW); Wallet.Balance decreased by amount | Status=1, RequestId returned | — |
| 2 | Auto-process | WithdrawRequest.Status=PROCESSING; AutoWithdrawQueue entry created | — (engine internal) | MB Bank API called (TransferFund) |
| 3 | Chuyển thành công | WithdrawRequest.Status=SUCCESS; MBBank_APICall_Log created | — | SMS/Push notification to user |
| 4 | Chuyển thất bại | WithdrawRequest.Status=FAILED; MBBank_APICall_Log with error | — | Google Chat alert to admin; Wallet.Balance restored |

## Edge Cases

| # | Edge Case | Mô tả | Expected Behavior |
|---|-----------|-------|-------------------|
| 1 | Amount < 2,000 VND | Rút dưới mức tối thiểu | Auto-cancel: IsIgnore=true, message "Số tiền muốn rút phải lớn hơn hoặc bằng 2000" |
| 2 | Daily limit exceeded | Tổng rút trong ngày > 60,000,000 VND | Error (ConfigWalletType.DailyLimit) — infrastructure sẵn sàng nhưng hiện tại commented out |
| 3 | Concurrent withdrawals | 2 request rút cùng lúc cho 1 user | EF Core optimistic concurrency (LastTransactionGuid) → 1 fail, retry |
| 4 | INHOUSE vs FAST | Cùng ngân hàng MB vs khác ngân hàng | INHOUSE: nội bộ MB Bank. FAST: qua Napas liên ngân hàng |
| 5 | Unverified bank account | UserLogin_BeneficiaryBank_Mapping.IsVerified=false | Error: phải verify tài khoản trước |
| 6 | Auto-create wallet | User chưa có wallet, request rút lần đầu | WalletHelper.GetUserWalletPayment() auto-creates wallet → nhưng balance=0 → insufficient |
| 7 | MB Bank API timeout | TransferFund call timeout | Status=FAILED, MBBank_APICall_Log ghi error; AutoUpdateWithdrawStatus_MBBankEngine sẽ poll lại |
| 8 | Zero withdraw fee | WithdrawFee=0, AutoWithdrawFee=0 (default) | Toàn bộ amount chuyển cho user, không trừ phí |
