namespace Ezy.Module.Insurance.Shared.Providers.Vifo
{
    /// <summary>
    /// Runtime configuration for the VIFO insurance provider.
    /// Load from SystemConfig with key "SYSTEM_VIFO_SETTINGS" (JSON object).
    /// </summary>
    public class VifoConfig
    {
        /// <summary>
        /// API base URL.
        /// Sandbox: https://sapi.vifo.vn
        /// Production: https://api.vifo.vn
        /// </summary>
        public string BaseUrl { get; set; } = "https://sapi.vifo.vn";

        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string ProductFamilyCode { get; set; }
        public string ProductProviderCode { get; set; }
        public bool IsGetVNIInsuranceInBackground { get; set; }
        /// <summary>
        /// Khi true, gọi Vifo /v2/insurance/total-price để lấy phí bảo hiểm thực
        /// thay vì tính theo % cấu hình. Fallback về % nếu Vifo lỗi.
        /// </summary>
        public bool IsUsingBooking { get; set; }
        public string VifoCompanyName { get; set; }
        public string VifoCompanyTaxCode { get; set; }
    }
}
