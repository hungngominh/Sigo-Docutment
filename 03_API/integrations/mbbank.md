# API: MB Bank Integration — Chi tiết Implementation

> **Controllers:** `MBBankController`, `MBBank_TransferFundController`, `MBBank_TransactionStatusController`, `MBBank_AccountInfoController`, `MBBank_ConvertToCardIdController`
> **Base:** `/api/v1/MBBank`
> **Phân quyền:** `[Authorize]` (tất cả endpoint)

## Mục lục
- [Tổng quan](#tổng-quan)
- [ConfigMBBank — Configuration](#configmbbank--configuration)
- [OAuth2 Token Flow](#oauth2-token-flow)
- [TransferFund API — Chi tiết](#transferfund-api--chi-tiết)
- [RSA Signature Generation](#rsa-signature-generation)
- [Transaction ID Format](#transaction-id-format)
- [AccountInfo API](#accountinfo-api)
- [TransactionStatus API — Polling](#transactionstatus-api--polling)
- [ConvertToCardId API](#converttocardid-api)
- [API Endpoints (Internal)](#api-endpoints-internal)
- [API Call Logging](#api-call-logging)
- [Withdrawal Flow — End to End](#withdrawal-flow--end-to-end)

---

## Tổng quan

Tích hợp MB Bank để chuyển tiền tự động cho chủ xe và xử lý rút tiền ví.

**Đặc điểm quan trọng:**
- **Không có webhook** — hệ thống dùng **polling** để check trạng thái giao dịch
- OAuth2 client_credentials flow cho authentication
- RSA SHA256 signature cho security
- Mọi API call được log đầy đủ vào `MBBank_APICall_Log`

---

## ConfigMBBank — Configuration

**Entity:** `ConfigMBBank` (cached via `CachedDataManagement`)

| Field | Type | Mô tả |
|-------|------|-------|
| `ClientId` | string | OAuth2 client ID |
| `ClientSecret` | string | OAuth2 client secret |
| `GetAccessTokenAPI` | string | Token endpoint URL |
| `TransferFundAPI` | string | Transfer fund endpoint URL |
| `AccountInfoAPI` | string | Account info endpoint URL |
| `TransactionStatusAPI` | string | Transaction status endpoint URL |
| `ConvertToCardIdAPI` | string | Card ID conversion endpoint URL |
| `RSAKeyCode` | string | Reference to `ConfigRSAKey` (private key) |
| `HMACKeyCode` | string | Reference to `ConfigHMACKey` (secret) |
| `ConvertToCardIdUser` | string | HMAC user header value |
| `ChannelCode` | string | Channel code cho TransactionId |
| `BusinessCode` | string | Business code cho TransactionId |
| `RemarkPrefix` | string | Prefix cho transfer remark |
| `SFTPCode` | string | Reference to `ConfigSFTP` (report files) |
| `SFTPInputFolder` | string | SFTP download folder |
| `SFTPOutputFolder` | string | SFTP upload folder |

**API Endpoints (Production):**
- Sandbox: `https://api-sandbox.mbbank.com.vn/private/...`
- Private: `https://api-private.mbbank.com.vn/private/canary/...`
- Token: `https://api-private.mbbank.com.vn/private/oauth2/v1/token`

---

## OAuth2 Token Flow

### Request

```
POST {ConfigMBBank.GetAccessTokenAPI}
Authorization: Basic base64({ClientId}:{ClientSecret})
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
```

### Implementation

```csharp
// MBBankHelper.cs
public static string GetBasicAuthorization(string clientId, string clientSecret)
{
    var credentials = $"{clientId}:{clientSecret}";
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
}
```

### Response

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI...",
  "errorCode": null,
  "errorDesc": null
}
```

```csharp
public class MBBank_TokenResponse
{
    public string access_token { get; set; }
    public string errorCode { get; set; }
    public string[] errorDesc { get; set; }
}
```

### Token Lifecycle
- Expiry: ~3600 seconds (1 hour)
- Renewal: Auto-renew trước khi gọi API nếu hết hạn
- Caching: Token được cache in-memory

---

## TransferFund API — Chi tiết

### Endpoint

```
POST {ConfigMBBank.TransferFundAPI}
// Production: https://api-private.mbbank.com.vn/private/canary/ms/funds-partner/transfer-fund/v1.0/make-transfer-partner-async
```

### Request Headers

| Header | Value | Mô tả |
|--------|-------|-------|
| `Authorization` | `Bearer {access_token}` | OAuth2 bearer token |
| `clientMessageId` | `{GUID without hyphens}` | Unique message ID |
| `transactionId` | `{ChannelCode}{BusinessCode}{Random8}` | Unique transaction ID |
| `signature` | `{RSA_SHA256_Base64}` | RSA signature |
| `Content-Type` | `application/json` | |

### Request Body

```json
{
  "serviceType": "CHI_HO",
  "customerType": "DOANH_NGHIEP",
  "customerLevel": 1,
  "debitType": "ACCOUNT",
  "debitResourceNumber": "0123456789",
  "debitName": "CONG TY SIGO",
  "creditType": "ACCOUNT",
  "creditResourceNumber": "9876543210",
  "creditName": "NGUYEN VAN A",
  "transferType": "FAST",
  "bankCode": "970415",
  "transferAmount": "500000",
  "transferFee": "0",
  "remark": "SIGO chuyen tien don ORD-001"
}
```

```csharp
public class MBBank_TransferFundRequest
{
    public string serviceType { get; set; }          // "CHI_HO" (disbursement)
    public string customerType { get; set; }         // "DOANH_NGHIEP" (business)
    public int customerLevel { get; set; }           // 1
    public string debitType { get; set; }            // "ACCOUNT", "CARD", "WALLETID"
    public string debitResourceNumber { get; set; }  // Source account number
    public string debitName { get; set; }            // Source account name
    public string creditType { get; set; }           // "ACCOUNT", "CARD", "WALLETID"
    public string creditResourceNumber { get; set; } // Destination account/card
    public string creditName { get; set; }           // Destination name
    public string transferType { get; set; }         // "INHOUSE", "FAST", "IBPS"
    public string bankCode { get; set; }             // Required if not INHOUSE
    public string transferAmount { get; set; }       // Numeric string, min 2000 VND
    public string transferFee { get; set; }          // Numeric string
    public string remark { get; set; }               // Max 140 chars
}
```

### Transfer Types

| Type | Mô tả | bankCode |
|------|-------|----------|
| `INHOUSE` | Chuyển nội bộ MB Bank | Không cần |
| `FAST` | Chuyển nhanh liên ngân hàng (NAPAS) | Bắt buộc (BIN) |
| `IBPS` | Chuyển liên ngân hàng thường | Bắt buộc (Citad code) |

### Response

```json
{
  "clientMessageId": "abc123",
  "errorCode": "00",
  "errorDesc": [],
  "data": {
    "status": "SUCCESS",
    "ftNumber": "FT12345678",
    "rrn": "123456789012"
  }
}
```

```csharp
public class MBBank_Response<T>
{
    public string clientMessageId { get; set; }
    public string errorCode { get; set; }
    public string[] errorDesc { get; set; }
    public T data { get; set; }
}

public class MBBank_TransferFundResponse
{
    public string status { get; set; }       // SUCCESS, FAILED, PROCESSING
    public string ftNumber { get; set; }     // FT reference number
    public string rrn { get; set; }          // Retrieval reference number
}
```

### Validation Rules

| Rule | Mô tả |
|------|-------|
| `transferAmount >= 2000` | Minimum 2,000 VND |
| `remark.Length <= 140` | Max 140 characters |
| `bankCode` required | If transferType != "INHOUSE" |
| `creditResourceNumber` required | Destination account |
| `debitResourceNumber` required | Source account |

---

## RSA Signature Generation

**File:** `AllianceMiddlemanWebAPI.Service/Helper/SignatureHelper.cs`

### Data to Sign

```
signatureData = "{debitResourceNumber}{debitName}{creditResourceNumber}{creditName}{transferAmount}"
```

**Ví dụ:**
```
"0123456789CONG TY SIGO9876543210NGUYEN VAN A500000"
```

### Algorithm

```csharp
public static byte[] SignDataRSA(string data, string privateKey)
{
    using (RSA rsa = RSA.Create())
    {
        // Clean PEM format
        privateKey = privateKey.Replace("\n", "").Replace("\r", "");
        rsa.ImportFromPem(privateKey);

        // Sign with SHA256 + PKCS1 padding
        return rsa.SignData(
            Encoding.UTF8.GetBytes(data),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
    }
}

// Final signature = Base64(SignDataRSA(signatureData, privateKey))
```

### Key Storage

- Private key: `ConfigRSAKey` entity (PEM format, PKCS#1 or PKCS#8)
- Lookup: `CachedDataManagement.ConfigRSAKey_Get_Instance_Code(ConfigMBBank.RSAKeyCode)`

---

## Transaction ID Format

```
TransactionId = {ChannelCode}{BusinessCode}{RandomString(8)}
```

- **ChannelCode:** từ `ConfigMBBank.ChannelCode`
- **BusinessCode:** từ `ConfigMBBank.BusinessCode`
- **RandomString(8):** 8 ký tự random từ charset `ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`

**Ví dụ:** `SIGOBIZ01XKAM3N7P`

---

## AccountInfo API

### Request

```
GET {ConfigMBBank.AccountInfoAPI}
  ?accountNumber={accountNumber}
  &accountType={type}
  &transferType={transferType}
  &bankCode={bankCode}

Headers:
  Authorization: Bearer {access_token}
  clientMessageId: {GUID}
```

### Response

```json
{
  "clientMessageId": "...",
  "errorCode": "00",
  "data": {
    "accountName": "NGUYEN VAN A"
  }
}
```

```csharp
public class MBBank_AccountInfoResponse
{
    public string accountName { get; set; }
}
```

---

## TransactionStatus API — Polling

**Không có webhook** → dùng polling qua `AutoUpdateWithdrawStatus_MBBankEngine`.

### Request

```
GET {ConfigMBBank.TransactionStatusAPI}
  ?transactionId={transactionId}

Headers:
  Authorization: Bearer {access_token}
  clientMessageId: {GUID}
```

### Response

```json
{
  "clientMessageId": "...",
  "errorCode": "00",
  "data": {
    "transStatus": "SUCCESS",
    "ft": "FT12345678",
    "retRefNumber": "123456789012",
    "actionTime": "2026-01-15T10:30:00+07:00"
  }
}
```

```csharp
public class MBBank_TransactionStatusResponse
{
    public string transStatus { get; set; }     // SUCCESS, FAILED, PROCESSING
    public string ft { get; set; }
    public string retRefNumber { get; set; }
    public string actionTime { get; set; }
}
```

### Polling Pattern

```
AutoUpdateWithdrawStatus_MBBankEngine.DoJob():
1. Query WithdrawalHistory WHERE Status = "PROCESSING"
2. FOR EACH pending withdrawal:
   a. Call TransactionStatus API
   b. IF transStatus == "SUCCESS":
      - Update WithdrawalHistory.Status = "SUCCESS"
      - Update WalletAction.Status = "Completed"
   c. IF transStatus == "FAILED":
      - Update WithdrawalHistory.Status = "FAILED"
      - Rollback wallet balance
      - Update WalletAction.Status = "Failed"
   d. Log to MBBank_APICall_Log
```

---

## ConvertToCardId API

### Authentication: HMAC (NOT Bearer token)

```csharp
// Different auth method: HMAC SHA256
public static byte[] SignDataHMAC(string message, string secretKey)
{
    var dataBytes = Encoding.UTF8.GetBytes(message);
    var keyBytes = Encoding.UTF8.GetBytes(secretKey);
    using (var hmac = new HMACSHA256(keyBytes))
    {
        return hmac.ComputeHash(dataBytes);
    }
}
```

### Request

```
POST {ConfigMBBank.ConvertToCardIdAPI}

Headers:
  user: {ConfigMBBank.ConvertToCardIdUser}
  hmac: {HMAC_SHA256_Hex}
  Content-Type: application/json

Body:
{
  "accountNumber": "0123456789"
}
```

---

## API Endpoints (Internal)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/v1/MBBank/TransferFund/List` | Danh sách giao dịch |
| POST | `/api/v1/MBBank/TransferFund/AutoWithdraw_MBBank` | Auto withdraw (internal) |
| POST | `/api/v1/MBBank/AccountInfo/AccountInfo` | Tra cứu tài khoản |
| POST | `/api/v1/MBBank/TransactionStatus/TransactionStatus` | Kiểm tra trạng thái |
| POST | `/api/v1/MBBank/ConvertToCardId/ConvertToCardId` | Chuyển đổi số thẻ |
| POST | `/api/v1/MBBank/TransferFund/ExportReportFiles` | Xuất báo cáo v1 |
| POST | `/api/v1/MBBank/TransferFund/ExportReportFiles_V2` | Xuất báo cáo v2 |

---

## API Call Logging

### MBBank_APICall_Log Entity

Mọi API call đều được log đầy đủ:

| Field | Type | Mô tả |
|-------|------|-------|
| `ClientMessageId` | string | Unique message ID |
| `TransactionServiceType` | string | API type |
| `TransactionRequest` | string | Full request body JSON |
| `TransactionId` | string | Transaction ID |
| `TransactionStatus` | string | SUCCESS/FAILED |
| `TransactionResponse` | string | Full response body JSON |
| `TransactionErrorCode` | string | Error code if any |
| `TransactionIsSuccessful` | bool | Success flag |
| `APIType` | string | TransferFund, AccountInfo, etc. |
| `RequestUrl` | string | Full API URL |
| `RequestHeaders` | string | Request headers JSON |
| `TransactionQueryResponse` | string | Polling responses |
| `TransactionCurrentStatus` | string | Current status |

### Cross-Reference Logging

```csharp
// Log_EWallet_MBBank — links wallet actions to MB Bank transactions
public partial class Log_EWallet_MBBank
{
    public Guid? ActionID_GUID { get; set; }     // → WalletAction.ID_GUID
    public Guid? MBBankID_GUID { get; set; }     // → MBBank_APICall_Log.ID_GUID
}
```

---

## Withdrawal Flow — End to End

```
1. Owner clicks "Withdraw" in app
     │
2. POST /api/v1/EWallet/Action/Withdrawal/Request
     │ → Validate balance, bank account
     │ → Create WalletAction{Withdrawal, New}
     │
3. Staff/System approves
     │ POST /api/v1/EWallet/Action/Withdrawal/Approve
     │ → WalletAction{Processing}
     │
4. Staff/System confirms
     │ POST /api/v1/EWallet/Action/Withdrawal/Confirm
     │ → WalletService.Withdraw() — debit balance
     │ → Create WalletTransaction
     │ → Enqueue to AutoWithdrawQueue
     │
5. AutoWithdrawEngine picks up
     │ → Get OAuth2 token (cache/renew)
     │ → Build TransferFund request
     │ → Generate RSA signature
     │ → Generate TransactionId
     │ → POST to MB Bank TransferFund API
     │ → Log everything to MBBank_APICall_Log
     │
6. AutoUpdateWithdrawStatus_MBBankEngine polls
     │ → GET TransactionStatus API
     │ → Update WithdrawalHistory
     │ → Update WalletAction status
     │
7. Notification sent to owner
     │ → FCM push + SignalR
```

---

## SMS Notification

Sau khi chuyển tiền thành công, hệ thống xử lý bank SMS:
- `AutoMappingSMS_CompanyBankAccountActivityEngine` → đọc SMS từ tài khoản ngân hàng
- Map SMS content với đơn hàng
- Xác nhận thanh toán tự động

---

*Xem thêm: [ewallet.md](../../02_MODULES/ewallet.md) | [ewallet-withdraw.md](../../04_BUSINESS_FLOWS/ewallet-withdraw.md)*
