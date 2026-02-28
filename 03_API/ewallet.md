# API: EWallet (Ví điện tử)

> **Module:** `Ezy.Module.EWallet`
> **Controllers:** `WalletController`, `WalletActionController`, `WalletActionFundingController`, `WalletActionWithdrawalController`, `WalletActionTransferController`, `WalletActionProcessingController`, `WalletActionNewOrProcessingController`, `WalletTransactionController`, `WalletTransaction_ExpController`, `WalletTypeController`
> **Base:** `/api/v1/EWallet`
> **Phân quyền:** `[Authorize]` (tất cả endpoint)

## Mục lục
- [Tổng quan](#tổng-quan)
- [Ví — Danh sách](#post-apiv1ewalletwalletlist)
- [Ví — Nạp tiền](#post-apiv1ewalletwaallettopup)
- [Ví — Chuyển tiền (nội bộ)](#post-apiv1ewalletwaallettransfer)
- [Ví — Kiểm tra tính hợp lệ](#post-apiv1ewalletwalletvalidate)
- [Ví — Khôi phục số dư](#post-apiv1ewalletwalletrestore)
- [Giao dịch — Danh sách](#post-apiv1ewallettransactionlist)
- [Nạp tiền — Yêu cầu](#post-apiv1ewalletactionfundingrequest)
- [Rút tiền — Duyệt](#post-apiv1ewalletactionwithdrawalapprove)
- [Chuyển tiền nội bộ — Yêu cầu](#post-apiv1ewalletactiontransferrequest)
- [Loại ví](#post-apiv1ewallettypelist)

---

## Tổng quan

Module EWallet quản lý ví điện tử trong hệ thống:
- **Wallet:** Quản lý số dư ví của từng user
- **WalletAction:** Quản lý các giao dịch (nạp/rút/chuyển) theo từng loại và trạng thái
- **WalletTransaction:** Lịch sử giao dịch chi tiết

### Luồng giao dịch
```
Yêu cầu (Request) → Duyệt (Approve) → Xác nhận (Confirm) → Hoàn tất
                                     ↓
                              Huỷ (Cancel)
```

### Controllers phân chia theo loại giao dịch

| Controller | Base Route | Mục đích |
|-----------|-----------|---------|
| `WalletActionFundingController` | `/api/v1/EWallet/Action/Funding` | Nạp tiền vào ví |
| `WalletActionWithdrawalController` | `/api/v1/EWallet/Action/Withdrawal` | Rút tiền ra ngân hàng |
| `WalletActionTransferController` | `/api/v1/EWallet/Action/Transfer` | Chuyển tiền giữa các ví |
| `WalletActionProcessingController` | `/api/v1/EWallet/Action/Processing` | Giao dịch đang xử lý |
| `WalletActionNewOrProcessingController` | `/api/v1/EWallet/Action/NewOrProcessing` | Giao dịch mới hoặc đang xử lý |

---

## `POST /api/v1/EWallet/Wallet/List`

**Mô tả:** Lấy danh sách ví điện tử.

### Request Body — `WalletParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "UserId": "user-guid"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<WalletModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 1,
    "Data": [
      {
        "WalletId": "wallet-guid",
        "UserId": "user-guid",
        "Balance": 1500000,
        "WalletType": "Main"
      }
    ]
  }
}
```

---

## `POST /api/v1/EWallet/Wallet/TopUp`

**Mô tả:** Nạp tiền vào ví (admin/internal use).

### Request Body — `WalletInitiateActionModel`
```json
{
  "WalletId": "wallet-guid",
  "Amount": 500000,
  "Note": "Nạp tiền từ đơn hàng #123"
}
```

### Response — `EzyResultObject<WalletModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "WalletId": "wallet-guid",
    "Balance": 2000000
  }
}
```

---

## `POST /api/v1/EWallet/Wallet/Transfer`

**Mô tả:** Chuyển tiền từ ví này sang ví khác.

### Request Body — `WalletInitiateActionModel`
```json
{
  "FromWalletId": "wallet-guid-from",
  "ToWalletId": "wallet-guid-to",
  "Amount": 200000,
  "Note": "Thanh toán thuê xe"
}
```

### Response — `EzyResultObject<WalletModel>`
Trả về thông tin ví nguồn sau giao dịch.

---

## `POST /api/v1/EWallet/Wallet/Validate`

**Mô tả:** Kiểm tra tính hợp lệ của ví (số dư, trạng thái).

### Request Body — `WalletInitiateActionModel`
```json
{
  "WalletId": "wallet-guid"
}
```

### Response — `EzyResultObject<object>`
```json
{
  "StatusCode": 1,
  "Msg": "Ví hợp lệ"
}
```

---

## `POST /api/v1/EWallet/Wallet/ValidateAll`

**Mô tả:** Kiểm tra tính hợp lệ của toàn bộ ví trong hệ thống (admin job).

### Request Body
Không có body.

---

## `POST /api/v1/EWallet/Wallet/Restore`

**Mô tả:** Khôi phục số dư ví từ lịch sử giao dịch.

### Request Body — `WalletInitiateActionModel`
```json
{
  "WalletId": "wallet-guid"
}
```

### Response — `EzyResultObject<WalletModel>`

---

## `POST /api/v1/EWallet/Wallet/RestoreAll`

**Mô tả:** Khôi phục số dư toàn bộ ví (admin job — dùng khi xử lý sự cố).

---

## `POST /api/v1/EWallet/Transaction/List`

**Mô tả:** Lấy lịch sử giao dịch ví (lọc theo ngày).

### Request Body — `WalletTransactionParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "WalletId": "wallet-guid",
  "FromDate": "2026-01-01",
  "ToDate": "2026-01-31"
}
```

### Response — `EzyResultObject<EzyDataSourceResult<WalletTransactionModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 10,
    "Data": [
      {
        "TransactionId": "txn-guid",
        "Amount": 500000,
        "Type": "TopUp",
        "Status": "Completed",
        "CreatedAt": "2026-01-15T10:00:00Z"
      }
    ]
  }
}
```

---

## `POST /api/v1/EWallet/TransactionExp/List`

**Mô tả:** Lấy lịch sử giao dịch mở rộng (không lọc theo ngày — xuất toàn bộ lịch sử).

Tương tự `/Transaction/List` nhưng không giới hạn khoảng ngày (`DontFilterDate = true`).

---

## `POST /api/v1/EWallet/Action/List`

**Mô tả:** Lấy danh sách tất cả giao dịch (tổng hợp mọi loại và trạng thái).

### Request Body — `WalletActionParamModel`
```json
{
  "PageIndex": 1,
  "PageSize": 20,
  "ActionType": "Funding",
  "Status": "Processing"
}
```

---

## `POST /api/v1/EWallet/Action/Cancel`

**Mô tả:** Huỷ một giao dịch đang chờ xử lý.

### Request Body — `WalletActionModel`
```json
{
  "ActionId": "action-guid",
  "Reason": "User yêu cầu huỷ"
}
```

---

## `POST /api/v1/EWallet/Action/Approve`

**Mô tả:** Duyệt giao dịch (chuyển sang trạng thái Processing).

### Request Body — `WalletActionModel`
```json
{
  "ActionId": "action-guid"
}
```

---

## `POST /api/v1/EWallet/Action/Confirm`

**Mô tả:** Xác nhận hoàn tất giao dịch.

### Request Body — `WalletActionModel`
```json
{
  "ActionId": "action-guid",
  "TransactionRef": "bank-transaction-ref"
}
```

---

## `POST /api/v1/EWallet/Action/Funding/Request`

**Mô tả:** Tạo yêu cầu nạp tiền vào ví.

### Request Body — `WalletActionModel`
```json
{
  "WalletId": "wallet-guid",
  "Amount": 1000000,
  "BankTransactionRef": "MBBANK-20260101-001",
  "Note": "Nạp tiền"
}
```

### Response — `EzyResultObject<WalletActionModel>`
```json
{
  "StatusCode": 1,
  "Data": {
    "ActionId": "action-guid",
    "Status": "Pending",
    "Amount": 1000000
  }
}
```

---

## `POST /api/v1/EWallet/Action/Funding/RunJob`

**Mô tả:** Chạy job xử lý hàng loạt các yêu cầu nạp tiền đang chờ.

---

## `POST /api/v1/EWallet/Action/Withdrawal/Approve`

**Mô tả:** Duyệt yêu cầu rút tiền của user.

### Request Body — `WalletActionModel`
```json
{
  "ActionId": "action-guid"
}
```

---

## `POST /api/v1/EWallet/Action/Withdrawal/Confirm`

**Mô tả:** Xác nhận đã chuyển tiền ra ngân hàng thành công (hoàn tất rút tiền).

### Request Body — `WalletActionModel`
```json
{
  "ActionId": "action-guid",
  "BankTransactionRef": "MBBANK-OUT-20260101-001"
}
```

---

## `POST /api/v1/EWallet/Action/Transfer/Request`

**Mô tả:** Tạo yêu cầu chuyển tiền nội bộ giữa các ví.

---

## `POST /api/v1/EWallet/Action/Transfer/RunJob`

**Mô tả:** Chạy job xử lý hàng loạt các giao dịch chuyển tiền đang chờ.

---

## `POST /api/v1/EWallet/Type/List`

**Mô tả:** Lấy danh sách loại ví (WalletType).

### Response — `EzyResultObject<EzyDataSourceResult<WalletTypeModel>>`
```json
{
  "StatusCode": 1,
  "Data": {
    "Total": 3,
    "Data": [
      { "WalletTypeId": 1, "Name": "Ví chính" },
      { "WalletTypeId": 2, "Name": "Ví hoa hồng" }
    ]
  }
}
```

---

## Ghi chú

- Tất cả endpoint xử lý tiền đều yêu cầu `[Authorize]` — không có endpoint public.
- `Processing` controller chỉ hiển thị giao dịch ở trạng thái `Processing`.
- `NewOrProcessing` controller hiển thị giao dịch mới tạo hoặc đang xử lý — dùng cho dashboard.
- `RunJob` dùng để xử lý thủ công khi scheduled job bị lỗi.
