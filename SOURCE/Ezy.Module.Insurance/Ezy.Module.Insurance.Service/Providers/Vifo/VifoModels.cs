using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ezy.Module.Insurance.Shared.Providers.Vifo
{
    // ============================================================
    //  Generic API wrapper (error responses share this shape)
    // ============================================================
    public class VifoErrorResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("errors")]
        public string Errors { get; set; }

        [JsonProperty("status_code")]
        public int StatusCode { get; set; }
    }

    // ============================================================
    //  GET /v2/families
    // ============================================================
    public class VifoFamiliesResponse
    {
        [JsonProperty("data")]
        public List<VifoFamilyData> Data { get; set; }

        [JsonProperty("meta")]
        public VifoMeta Meta { get; set; }

        // Present only on error
        [JsonProperty("success")]
        public bool? Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class VifoFamilyData
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("translation")]
        public Dictionary<string, VifoTranslation> Translation { get; set; }

        [JsonProperty("product_count")]
        public int ProductCount { get; set; }
    }

    public class VifoTranslation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("promotion_text")]
        public string PromotionText { get; set; }
    }

    public class VifoMeta
    {
        [JsonProperty("pagination")]
        public VifoPagination Pagination { get; set; }
    }

    public class VifoPagination
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("per_page")]
        public int PerPage { get; set; }

        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }

        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }
    }

    // ============================================================
    //  GET /v2/products?family_code=&page=&per_page=
    // ============================================================
    public class VifoProductsResponse
    {
        [JsonProperty("data")]
        public List<VifoProductData> Data { get; set; }

        [JsonProperty("meta")]
        public VifoMeta Meta { get; set; }

        [JsonProperty("success")]
        public bool? Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class VifoProductData
    {
        [JsonProperty("family_code")]
        public string FamilyCode { get; set; }

        [JsonProperty("provider_code")]
        public string ProviderCode { get; set; }

        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_vi")]
        public string NameVi { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("payment_term")]
        public int PaymentTerm { get; set; }

        [JsonProperty("options")]
        public List<VifoProductOptionGroup> Options { get; set; }

        [JsonProperty("product_detail_file")]
        public string ProductDetailFile { get; set; }

        [JsonProperty("product_term_file")]
        public string ProductTermFile { get; set; }
    }

    public class VifoProductOptionGroup
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("values")]
        public List<VifoProductOptionValue> Values { get; set; }
    }

    public class VifoProductOptionValue
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }
    }

    // ============================================================
    //  POST /v2/insurance/total-price
    // ============================================================
    public class VifoTotalPriceResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public VifoTotalPriceData Data { get; set; }

        // Auth / validation error fields
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("errors")]
        public object Errors { get; set; }

        [JsonProperty("status_code")]
        public int? StatusCode { get; set; }
    }

    public class VifoTotalPriceData
    {
        [JsonProperty("final_amount")]
        public long FinalAmount { get; set; }

        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        [JsonProperty("total_amount")]
        public long? TotalAmount { get; set; }

        [JsonProperty("discount_amount")]
        public long? DiscountAmount { get; set; }
    }

    // ============================================================
    //  Typed payload helpers – common insurance types
    //  (Pass directly as InsurancePriceRequest.Payload)
    // ============================================================

    /// <summary>BHYT / BHYTHGD – Health insurance payload</summary>
    public class VifoBhytPayload
    {
        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        [JsonProperty("distributor_order_number")]
        public string DistributorOrderNumber { get; set; }

        [JsonProperty("fullname")]
        public string Fullname { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>SKU codes for additional household members (VF2, VF3, …)</summary>
        [JsonProperty("options")]
        public List<string> Options { get; set; } = new List<string>();

        /// <summary>Set to 0 when only calculating price (not creating order)</summary>
        [JsonProperty("final_amount")]
        public long FinalAmount { get; set; } = 0;

        [JsonProperty("beneficiary_list")]
        public List<VifoBhytBeneficiary> BeneficiaryList { get; set; } = new List<VifoBhytBeneficiary>();
    }

    public class VifoBhytBeneficiary
    {
        [JsonProperty("fullname")]
        public string Fullname { get; set; }

        [JsonProperty("birthday")]
        public string Birthday { get; set; }

        [JsonProperty("nic")]
        public string Nic { get; set; }

        [JsonProperty("medical_id")]
        public string MedicalId { get; set; }

        [JsonProperty("hospital")]
        public string Hospital { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("old_card_start_date")]
        public string OldCardStartDate { get; set; }

        [JsonProperty("old_card_end_date")]
        public string OldCardEndDate { get; set; }

        [JsonProperty("renewal")]
        public bool Renewal { get; set; }

        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        [JsonProperty("five_year_date")]
        public string FiveYearDate { get; set; }

        [JsonProperty("social_family_id")]
        public string SocialFamilyId { get; set; }

        [JsonProperty("nation")]
        public string Nation { get; set; }

        [JsonProperty("ethnicity")]
        public string Ethnicity { get; set; }
    }

    /// <summary>TNDS xe máy – Motorbike civil liability payload</summary>
    public class VifoTndsMotorbikePayload
    {
        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        [JsonProperty("fullname")]
        public string Fullname { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("options")]
        public List<string> Options { get; set; } = new List<string>();

        [JsonProperty("final_amount")]
        public long FinalAmount { get; set; } = 0;

        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        [JsonProperty("license_plate")]
        public string LicensePlate { get; set; }

        [JsonProperty("chassis_number")]
        public string ChassisNumber { get; set; }

        [JsonProperty("engine_number")]
        public string EngineNumber { get; set; }
    }

    // ============================================================
    //  POST /v2/insurance  –  Create order response
    // ============================================================
    public class VifoCreateOrderResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public VifoCreateOrderData Data { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("errors")]
        public object Errors { get; set; }

        [JsonProperty("status_code")]
        public int? StatusCode { get; set; }
    }

    public class VifoCreateOrderData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("order_number")]
        public string OrderNumber { get; set; }

        [JsonProperty("provider_order_number")]
        public string ProviderOrderNumber { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("final_amount")]
        public long FinalAmount { get; set; }

        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        [JsonProperty("contract_files")]
        public List<VifoContractFileItem> ContractFiles { get; set; }

        [JsonProperty("created_at")]
        public VifoCreatedAt CreatedAt { get; set; }
    }

    public class VifoContractFileItem
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class VifoCreatedAt
    {
        [JsonProperty("date")]
        public DateTime? Date { get; set; }

        [JsonProperty("timezone_type")]
        public int? TimezoneType { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }
    }

    // ============================================================
    //  GET /v2/insurance/:order_number  –  Check order response
    // ============================================================
    public class VifoCheckOrderResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public VifoCheckOrderData Data { get; set; }

        [JsonProperty("status_code")]
        public int? StatusCode { get; set; }
    }

    public class VifoCheckOrderData
    {
        [JsonProperty("order_number")]
        public string OrderNumber { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        [JsonProperty("family_code")]
        public string FamilyCode { get; set; }

        [JsonProperty("final_amount")]
        public long FinalAmount { get; set; }

        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        [JsonProperty("end_date")]
        public string EndDate { get; set; }

        [JsonProperty("certificate_url")]
        public string CertificateUrl { get; set; }

        [JsonProperty("created_at")]
        public VifoCreatedAt CreatedAt { get; set; }
    }

    // ============================================================
    //  POST /v2/order/:order_number/terminate  –  Terminate response
    // ============================================================
    public class VifoTerminateOrderResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("status_code")]
        public int? StatusCode { get; set; }
    }

    // ============================================================
    //  GET /v2/order/:order_number/pvi  –  BHXH PVI status
    // ============================================================
    public class VifoPviStatusResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("status_code")]
        public int? StatusCode { get; set; }
    }

    /// <summary>
    /// CARSHORT – BH vật chất xe ô tô cho thuê theo chuyến (SIGO private product).
    /// Không dùng product_code — dùng family_code + provider_code.
    /// Endpoint: POST /v2/insurance/total-price và POST /v2/insurance
    /// </summary>
    public class VifoCarShortPayload
    {
        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("fullname")]
        public string Fullname { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("family_code")]
        public string FamilyCode { get; set; } = "CARSHORT";

        [JsonProperty("provider_code")]
        public string ProviderCode { get; set; }

        /// <summary>Ngày bắt đầu hiệu lực (yyyy-MM-dd). Không được là ngày quá khứ.</summary>
        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        /// <summary>Ngày kết thúc hiệu lực (yyyy-MM-dd).</summary>
        [JsonProperty("end_date")]
        public string EndDate { get; set; }

        [JsonProperty("plate_no")]
        public string PlateNo { get; set; }

        [JsonProperty("year")]
        public int? Year { get; set; }

        [JsonProperty("brand")]
        public string Brand { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("seat")]
        public int? Seat { get; set; }
    }

    /// <summary>TNCAR – Car civil liability payload. Uses product_code (not family_code/provider_code).</summary>
    public class VifoCarPayload
    {
        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        [JsonProperty("fullname")]
        public string Fullname { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("options")]
        public List<string> Options { get; set; } = new List<string>();

        [JsonProperty("final_amount")]
        public long FinalAmount { get; set; } = 0;

        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        [JsonProperty("end_date")]
        public string EndDate { get; set; }

        [JsonProperty("license_plate")]
        public string LicensePlate { get; set; }

        [JsonProperty("chassis_number")]
        public string ChassisNumber { get; set; }

        [JsonProperty("engine_number")]
        public string EngineNumber { get; set; }

        [JsonProperty("car_value")]
        public long? CarValue { get; set; }
    }
}
