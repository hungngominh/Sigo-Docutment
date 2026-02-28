# ADR-0015: Tích hợp MB Bank API cho xử lý thanh toán

## Status

Accepted

## Context

Sigo platform cần xử lý thanh toán tài chính thực tế:
- Owner (chủ xe) nhận tiền sau khi hoàn tất chuyến
- Renter (người thuê) thanh toán qua chuyển khoản
- Hệ thống cần tự động rút tiền từ ví về tài khoản ngân hàng
- Đối soát (reconciliation) giao dịch định kỳ

Cần lựa chọn đối tác ngân hàng và phương thức tích hợp phù hợp với thị trường Việt Nam.

## Decision Drivers

* **Vietnamese market** — ngân hàng Việt Nam có API mở
* **Automated transfer** — tự động chuyển tiền, không cần thủ công
* **Reconciliation** — đối soát giao dịch định kỳ
* **Real-time notification** — nhận callback khi có giao dịch
* **VietQR support** — người dùng quét QR để thanh toán
* **API maturity** — API stable, document rõ ràng

## Considered Options

### Option 1: MB Bank (Ngân hàng Quân đội)
- **Pros:**
  - API mở (MB Bank Open API)
  - Hỗ trợ VietQR
  - Tự động chuyển khoản (B2C)
  - Callback webhook khi có giao dịch
  - Đã có relationship/contract với MB Bank
  - SMS notification tích hợp
- **Cons:**
  - Phụ thuộc một ngân hàng duy nhất
  - API có thể thay đổi, cần monitor

### Option 2: Vietcombank / VCB
- **Pros:** Ngân hàng lớn, uy tín
- **Cons:** API ít documented hơn, chưa có relationship

### Option 3: Payment Gateway (VNPay, Momo, ZaloPay)
- **Pros:**
  - Multi-bank support
  - User quen dùng
  - Managed security/PCI DSS
- **Cons:**
  - Phí transaction cao hơn direct bank transfer
  - Không hỗ trợ B2C auto-transfer cho owner payout
  - Thêm intermediary

### Option 4: Stripe (International)
- **Pros:** Developer-friendly API, global standard
- **Cons:**
  - Không phổ biến ở Việt Nam
  - Không hỗ trợ VietQR
  - Phí cao cho thị trường Việt Nam
  - Vietnamese payment methods không supported

## Decision

Chúng ta sẽ tích hợp **MB Bank API** làm payment provider chính, hỗ trợ:
1. Auto-withdraw (B2C transfer từ system account → owner account)
2. Transaction monitoring qua SMS mapping
3. VietQR payment cho renter
4. Báo cáo đối soát định kỳ

## Rationale

1. **Market fit:** MB Bank phổ biến tại Việt Nam, user base rộng
2. **API capabilities:** Hỗ trợ đầy đủ: transfer, webhook, VietQR, statement
3. **B2C payout:** Tự động chuyển tiền cho owner — payment gateway không làm được điều này trực tiếp
4. **Existing contract:** Team đã có agreement với MB Bank, không cần negotiation mới

## Tích hợp thực tế

### Components

```
Controllers/MainBusiness/MBBank/
    MBBank_TransferFundController     # Chuyển khoản tự động
    MBBank_SMSMappingController       # Mapping SMS giao dịch
    MBBank_CompareReportController    # Báo cáo đối soát

Services/MainBusiness/MBBank/
    AutoWithdrawEngine                # Engine tự động rút tiền
    AutoUpdateWithdrawStatus_MBBankEngine  # Update trạng thái
    AutoMappingSMS_CompanyBankAccountActivityEngine  # SMS mapping
    MBBank_CompareReportDataJob       # Đối soát định kỳ
```

### Configuration
```json
{
  "MBBank": {
    "ApiUrl": "https://api.mbbank.com.vn",
    "ClientId": "...",
    "ClientSecret": "...",
    "AccountNumber": "...",
    "CallbackUrl": "https://api.sigo.vn/api/v1/MBBank/Callback"
  }
}
```

### Auto-Withdraw Flow
```
User yêu cầu rút tiền từ ví
    │
    ▼
[WalletService] → Validate balance, create withdrawal record
    │
    ▼
[AutoWithdrawEngine] → Poll pending withdrawals (Background Engine)
    │
    ▼
[MB Bank API] → POST /transfer {amount, toAccount, ...}
    │
    ▼
[AutoUpdateWithdrawStatus_MBBankEngine] → Poll transfer status
    │
    ▼
[NotificationService] → SMS + push notification kết quả
```

### VietQR Flow
```
Renter chọn thanh toán qua QR
    │
    ▼
[VietQRController] → Generate QR code với payment info
    │
    ▼
Renter quét QR, chuyển khoản vào MB Bank company account
    │
    ▼
[AutoMappingSMS_CompanyBankAccountActivityEngine]
    → Parse SMS từ MB Bank
    → Match với order
    → Update payment status
```

## Consequences

### Tích cực
- B2C payout tự động, không cần thao tác thủ công cho owner payouts
- VietQR phổ biến tại Việt Nam, UX quen thuộc cho user
- SMS mapping cho phép detect payment không cần webhooks phức tạp
- `MBBank/AutoWithdraw` 2,399ms avg — có room để optimize

### Tiêu cực
- Phụ thuộc MB Bank — nếu API down, payment bị ảnh hưởng
- `AutoWithdraw` 2.4s trung bình — chậm, cần optimize
- SMS parsing brittle — format SMS thay đổi sẽ break mapping
- Không có fallback payment provider

### Rủi ro
- MB Bank API outage
- **Giảm thiểu:** Queue withdrawal requests, retry mechanism, manual override process
- SMS format thay đổi break payment detection
- **Giảm thiểu:** Unit tests cho SMS parser, monitoring cho unmapped transactions
- Transfer to wrong account
- **Giảm thiểu:** Verification step trước khi transfer, whitelist tài khoản

## Performance Issue (known)

`POST /api/v1/MBBank/TransferFund/AutoWithdraw_MBBank` — **2,399ms avg**

Điều tra:
- Có thể do MB Bank API latency
- Có thể do synchronous processing trong request cycle
- Xem `05_PERFORMANCE/db-optimization-notes.md` để biết thêm

## Related Decisions

- ADR-0009: Background Engines (AutoWithdrawEngine, SMSMappingEngine)
- ADR-0007: Kafka (withdrawal events qua Kafka)
- ADR-0006: Redis (cache bank account validation)

---

*Ngày tạo: 2022-01-01*
