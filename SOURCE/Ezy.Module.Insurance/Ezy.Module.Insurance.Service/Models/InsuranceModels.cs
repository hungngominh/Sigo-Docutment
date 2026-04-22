using System;
using System.Collections.Generic;

namespace Ezy.Module.Insurance.Shared.Models
{
    // ============================================================
    //  Unified result wrapper
    // ============================================================
    public class InsuranceResult<T>
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public int StatusCode { get; set; }
        public T Data { get; set; }
        public string ProviderCode { get; set; }

        public static InsuranceResult<T> Success(T data, string providerCode = null) =>
            new InsuranceResult<T> { IsSuccessful = true, Data = data, ProviderCode = providerCode };

        public static InsuranceResult<T> Fail(string error, int statusCode = 0, string providerCode = null) =>
            new InsuranceResult<T> { IsSuccessful = false, ErrorMessage = error, StatusCode = statusCode, ProviderCode = providerCode };
    }

    // ============================================================
    //  Price calculation – request / result
    // ============================================================

    /// <summary>
    /// Unified request to calculate insurance price.
    /// <see cref="Payload"/> must match the provider's expected schema for the given FamilyCode.
    /// </summary>
    public class InsurancePriceRequest
    {
        /// <summary>Provider code, e.g. "VIFO"</summary>
        public string ProviderCode { get; set; }

        /// <summary>Insurance family code, e.g. "BHYT", "CARSHORT", "TNDS_XE_MAY"</summary>
        public string FamilyCode { get; set; }

        /// <summary>
        /// Provider-specific payload object.
        /// For VIFO: use <see cref="VifoPayloads"/> typed classes or a plain Dictionary.
        /// </summary>
        public object Payload { get; set; }
    }

    public class InsurancePriceResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public string ProviderCode { get; set; }

        /// <summary>Calculated premium amount (VND)</summary>
        public long FinalAmount { get; set; }

        /// <summary>Full raw data from the provider for debugging / logging</summary>
        public object RawData { get; set; }
    }

    // ============================================================
    //  Families
    // ============================================================
    public class InsuranceFamilyInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string Icon { get; set; }
        public int ProductCount { get; set; }
    }

    public class InsuranceFamiliesResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public string ProviderCode { get; set; }
        public List<InsuranceFamilyInfo> Families { get; set; } = new List<InsuranceFamilyInfo>();
    }

    // ============================================================
    //  Products
    // ============================================================
    public class InsuranceProductInfo
    {
        public string FamilyCode { get; set; }
        public string ProviderCode { get; set; }
        public string ProductCode { get; set; }
        public string Name { get; set; }
        public string NameVi { get; set; }
        public string Icon { get; set; }

        /// <summary>Base price (unit varies per product – see payment_term)</summary>
        public double Price { get; set; }

        public int PaymentTerm { get; set; }
        public List<InsuranceProductOption> Options { get; set; }
        public string ProductDetailFile { get; set; }
        public string ProductTermFile { get; set; }
    }

    public class InsuranceProductOption
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public List<InsuranceProductOptionValue> Values { get; set; }
    }

    public class InsuranceProductOptionValue
    {
        public string Key { get; set; }
        public string Title { get; set; }

        /// <summary>SKU used inside the order/price payload's options array</summary>
        public string Sku { get; set; }

        public double Price { get; set; }
    }

    public class InsuranceProductsResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public string ProviderCode { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public List<InsuranceProductInfo> Products { get; set; } = new List<InsuranceProductInfo>();
    }

    // ============================================================
    //  Create order  – POST /v2/insurance
    // ============================================================

    /// <summary>
    /// Request tạo đơn bảo hiểm – payload giống GetTotalPrice nhưng với dữ liệu thật.
    /// </summary>
    public class InsuranceCreateOrderRequest
    {
        public string ProviderCode { get; set; }
        public string FamilyCode { get; set; }

        /// <summary>
        /// Provider-specific payload (giống InsurancePriceRequest.Payload).
        /// Với VIFO: dùng các typed class VifoBhytPayload, VifoCarPayload, … với final_amount thật.
        /// </summary>
        public object Payload { get; set; }
    }

    public class InsuranceCreateOrderResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public string ProviderCode { get; set; }

        /// <summary>Mã đơn hàng từ provider (dùng để check / hủy đơn)</summary>
        public string OrderNumber { get; set; }

        /// <summary>ID nội bộ của provider (e.g. VIFO internal id)</summary>
        public string ExternalId { get; set; }

        /// <summary>Mã đơn từ công ty bảo hiểm (e.g. VNI order number)</summary>
        public string ProviderOrderNumber { get; set; }

        /// <summary>URL file hợp đồng/chứng nhận bảo hiểm đầu tiên</summary>
        public string ContractUrl { get; set; }

        /// <summary>Thời điểm tạo đơn (timezone từ provider; normalize về UTC trước khi lưu)</summary>
        public DateTime? CreatedAt { get; set; }

        public object RawData { get; set; }
    }

    // ============================================================
    //  Check order  – GET /v2/insurance/:order_number
    // ============================================================
    public class InsuranceOrderDetailResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public string ProviderCode { get; set; }
        public string OrderNumber { get; set; }

        /// <summary>Trạng thái đơn: pending / active / expired / cancelled…</summary>
        public string Status { get; set; }

        public object RawData { get; set; }
    }

    // ============================================================
    //  Terminate order  – POST /v2/order/:order_number/terminate
    // ============================================================
    public class InsuranceTerminateResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public string ProviderCode { get; set; }
        public string OrderNumber { get; set; }
        public string Message { get; set; }
        public object RawData { get; set; }
    }

    // ============================================================
    //  Check BHXH order status from PVI  – GET /v2/order/:order_number/pvi
    // ============================================================
    public class InsurancePviStatusResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public string ProviderCode { get; set; }
        public string OrderNumber { get; set; }

        /// <summary>Thông tin tờ khai từ phía PVI</summary>
        public object RawData { get; set; }
    }
}
