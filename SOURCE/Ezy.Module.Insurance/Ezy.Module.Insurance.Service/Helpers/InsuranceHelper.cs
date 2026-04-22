using Ezy.APIService.Core.Services;
using Ezy.Module.Insurance.Shared.Models;
using Ezy.Module.Insurance.Shared.Providers;
using Ezy.Module.Insurance.Shared.Providers.Vifo;
using Ezy.Module.Library.Utilities;
using System;
using System.Collections.Concurrent;

namespace Ezy.Module.Insurance.Shared.Helpers
{
    /// <summary>
    /// Entry point for all insurance price operations.
    ///
    /// Usage:
    /// <code>
    ///   // 1. Đăng ký provider từ System Config (khuyến nghị – gọi trong Startup / HostedService)
    ///   InsuranceHelper.RegisterVifo();
    ///
    ///   // Hoặc truyền config thủ công:
    ///   InsuranceHelper.RegisterVifo(new VifoConfig
    ///   {
    ///       BaseUrl           = "https://sapi.vifo.vn",
    ///       UserName          = "VIFO_SIGO_demotest",
    ///       Password          = "sigo@123",
    ///       Email             = "SIGO_sale@email.com",
    ///       ProductFamilyCode = "CARSHORT",
    ///       ProductProviderCode = "VNI_SGD2"
    ///   });
    ///
    ///   // 2. Tính giá
    ///   var result = InsuranceHelper.GetTotalPrice(new InsurancePriceRequest
    ///   {
    ///       ProviderCode = "VIFO",
    ///       FamilyCode   = "BHYT",
    ///       Payload      = new VifoBhytPayload { ProductCode = "BHYT...", ... }
    ///   });
    /// </code>
    /// </summary>
    public static class InsuranceHelper
    {
        /// <summary>
        /// Config key lưu trong bảng System Config.
        /// Value là JSON object khớp với <see cref="VifoConfig"/>.
        /// Ví dụ: { "BaseUrl": "https://api.vifo.vn", "UserName": "...", "Password": "..." }
        /// </summary>
        public const string VIFO_CONFIG_KEY = "SYSTEM_VIFO_SETTINGS";

        private static readonly ConcurrentDictionary<string, IInsurancePriceProvider> _providers =
            new ConcurrentDictionary<string, IInsurancePriceProvider>(StringComparer.OrdinalIgnoreCase);

        // ------------------------------------------------------------------
        //  Config loading  (cùng pattern với MisaInvoiceHelper / VietQRHelper)
        // ------------------------------------------------------------------

        /// <summary>
        /// Đọc <see cref="VifoConfig"/> từ System Config (key: <see cref="VIFO_CONFIG_KEY"/>).
        /// Nếu chưa có entry trong config table, trả về object default (sandbox).
        /// </summary>
        public static VifoConfig GetVifoConfig()
        {
            var defaultConfig = new VifoConfig
            {
                BaseUrl             = "https://sapi.vifo.vn",
                UserName            = "VIFO_SIGO_demotest",
                Password            = "sigo@123",
                Email               = "SIGO_sale@email.com",
                ProductFamilyCode   = "CARSHORT",
                ProductProviderCode = "VNI_SGD2",
                IsGetVNIInsuranceInBackground = true,
                VifoCompanyName     = "VNI",
                VifoCompanyTaxCode  = "0102737963-034"
            };
            return SystemConfigHelper.GetValueFromConfig_Obj(VIFO_CONFIG_KEY, defaultConfig) ?? defaultConfig;
        }

        // ------------------------------------------------------------------
        //  Provider registration
        // ------------------------------------------------------------------

        /// <summary>
        /// Đăng ký VIFO provider bằng cách đọc config từ System Config table
        /// (key: <see cref="VIFO_CONFIG_KEY"/>).
        /// </summary>
        public static void RegisterVifo()
        {
            RegisterVifo(GetVifoConfig());
        }

        /// <summary>Đăng ký VIFO provider với config truyền vào thủ công.</summary>
        public static void RegisterVifo(VifoConfig config)
        {
            _providers["VIFO"] = new VifoInsuranceProvider(config);
        }

        /// <summary>Register any custom provider implementation.</summary>
        public static void RegisterProvider(IInsurancePriceProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _providers[provider.ProviderCode] = provider;
        }

        /// <summary>Returns true if a provider with the given code is registered.</summary>
        public static bool HasProvider(string providerCode) =>
            _providers.ContainsKey(providerCode ?? "");

        // ------------------------------------------------------------------
        //  Price calculation
        // ------------------------------------------------------------------

        /// <summary>
        /// Calculate the insurance premium through the specified provider.
        /// Returns <see cref="InsurancePriceResult.IsSuccessful"/> = false when the
        /// provider is not registered or the API call fails.
        /// </summary>
        public static InsurancePriceResult GetTotalPrice(InsurancePriceRequest request)
        {
            if (request == null)
                return Fail("Request must not be null");

            if (!_providers.TryGetValue(request.ProviderCode ?? "", out var provider))
                return Fail($"Provider '{request.ProviderCode}' is not registered. Call InsuranceHelper.Register*() first.");

            try
            {
                return provider.GetTotalPrice(request);
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "InsuranceHelper.GetTotalPrice");
                return Fail(ExceptionHelper.GetMessage(ex), request.ProviderCode);
            }
        }

        // ------------------------------------------------------------------
        //  Families & Products
        // ------------------------------------------------------------------

        /// <summary>Retrieve the list of insurance families from a provider.</summary>
        public static InsuranceFamiliesResult GetFamilies(string providerCode)
        {
            if (!_providers.TryGetValue(providerCode ?? "", out var provider))
                return new InsuranceFamiliesResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Provider '{providerCode}' is not registered."
                };

            try
            {
                return provider.GetFamilies();
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "InsuranceHelper.GetFamilies");
                return new InsuranceFamiliesResult
                {
                    IsSuccessful = false,
                    ProviderCode = providerCode,
                    ErrorMessage = ExceptionHelper.GetMessage(ex)
                };
            }
        }

        /// <summary>Retrieve the product list for a family from a provider.</summary>
        public static InsuranceProductsResult GetProducts(string providerCode, string familyCode, int page = 1, int perPage = 50)
        {
            if (!_providers.TryGetValue(providerCode ?? "", out var provider))
                return new InsuranceProductsResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Provider '{providerCode}' is not registered."
                };

            try
            {
                return provider.GetProducts(familyCode, page, perPage);
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "InsuranceHelper.GetProducts");
                return new InsuranceProductsResult
                {
                    IsSuccessful = false,
                    ProviderCode = providerCode,
                    ErrorMessage = ExceptionHelper.GetMessage(ex)
                };
            }
        }

        // ------------------------------------------------------------------
        //  Create order
        // ------------------------------------------------------------------

        /// <summary>
        /// Tạo đơn bảo hiểm qua provider.
        /// POST /v2/insurance (VIFO)
        /// </summary>
        public static InsuranceCreateOrderResult CreateOrder(InsuranceCreateOrderRequest request)
        {
            if (request == null)
                return new InsuranceCreateOrderResult { IsSuccessful = false, ErrorMessage = "Request must not be null" };

            if (!_providers.TryGetValue(request.ProviderCode ?? "", out var provider))
                return new InsuranceCreateOrderResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Provider '{request.ProviderCode}' is not registered."
                };

            try { return provider.CreateOrder(request); }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "InsuranceHelper.CreateOrder");
                return new InsuranceCreateOrderResult
                {
                    IsSuccessful = false,
                    ProviderCode = request.ProviderCode,
                    ErrorMessage = ExceptionHelper.GetMessage(ex)
                };
            }
        }

        /// <summary>
        /// Tạo đơn BH CARSHORT (cho thuê xe theo chuyến – SIGO).
        /// Caller chịu trách nhiệm normalize ngày: UTC→VN timezone, clamp start_date về hôm nay nếu quá khứ.
        /// </summary>
        /// <param name="payload">Payload CARSHORT đã điền đầy đủ.</param>
        public static InsuranceCreateOrderResult CreateCarShortOrder(
            VifoCarShortPayload payload)
        {
            return CreateOrder(new InsuranceCreateOrderRequest
            {
                ProviderCode = "VIFO",
                FamilyCode   = "CARSHORT",
                Payload      = payload
            });
        }

        // ------------------------------------------------------------------
        //  Check order
        // ------------------------------------------------------------------

        /// <summary>
        /// Kiểm tra trạng thái / chi tiết đơn bảo hiểm.
        /// GET /v2/insurance/:order_number (VIFO)
        /// </summary>
        public static InsuranceOrderDetailResult CheckOrder(string providerCode, string orderNumber)
        {
            if (!_providers.TryGetValue(providerCode ?? "", out var provider))
                return new InsuranceOrderDetailResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Provider '{providerCode}' is not registered."
                };

            try { return provider.CheckOrder(orderNumber); }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "InsuranceHelper.CheckOrder");
                return new InsuranceOrderDetailResult
                {
                    IsSuccessful = false,
                    ProviderCode = providerCode,
                    OrderNumber = orderNumber,
                    ErrorMessage = ExceptionHelper.GetMessage(ex)
                };
            }
        }

        // ------------------------------------------------------------------
        //  Terminate order
        // ------------------------------------------------------------------

        /// <summary>
        /// Hủy đơn bảo hiểm (chỉ áp dụng một số sản phẩm).
        /// POST /v2/order/:order_number/terminate (VIFO)
        /// </summary>
        public static InsuranceTerminateResult TerminateOrder(string providerCode, string orderNumber)
        {
            if (!_providers.TryGetValue(providerCode ?? "", out var provider))
                return new InsuranceTerminateResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Provider '{providerCode}' is not registered."
                };

            try { return provider.TerminateOrder(orderNumber); }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "InsuranceHelper.TerminateOrder");
                return new InsuranceTerminateResult
                {
                    IsSuccessful = false,
                    ProviderCode = providerCode,
                    OrderNumber = orderNumber,
                    ErrorMessage = ExceptionHelper.GetMessage(ex)
                };
            }
        }

        // ------------------------------------------------------------------
        //  BHXH PVI status
        // ------------------------------------------------------------------

        /// <summary>
        /// Kiểm tra trạng thái đơn BHXH từ PVI.
        /// GET /v2/order/:order_number/pvi (VIFO)
        /// </summary>
        public static InsurancePviStatusResult CheckOrderPviStatus(string providerCode, string orderNumber)
        {
            if (!_providers.TryGetValue(providerCode ?? "", out var provider))
                return new InsurancePviStatusResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Provider '{providerCode}' is not registered."
                };

            try { return provider.CheckOrderPviStatus(orderNumber); }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "InsuranceHelper.CheckOrderPviStatus");
                return new InsurancePviStatusResult
                {
                    IsSuccessful = false,
                    ProviderCode = providerCode,
                    OrderNumber = orderNumber,
                    ErrorMessage = ExceptionHelper.GetMessage(ex)
                };
            }
        }

        // ------------------------------------------------------------------
        //  Products (convenience)
        // ------------------------------------------------------------------

        /// <summary>
        /// Convenience: get products across all pages for the given provider + family.
        /// Use with caution on families with many products.
        /// </summary>
        public static InsuranceProductsResult GetAllProducts(string providerCode, string familyCode, int perPage = 50)
        {
            var first = GetProducts(providerCode, familyCode, 1, perPage);
            if (!first.IsSuccessful || first.TotalPages <= 1)
                return first;

            for (int p = 2; p <= first.TotalPages; p++)
            {
                var page = GetProducts(providerCode, familyCode, p, perPage);
                if (page.IsSuccessful)
                    first.Products.AddRange(page.Products);
            }
            return first;
        }

        // ------------------------------------------------------------------
        //  Private helpers
        // ------------------------------------------------------------------
        private static InsurancePriceResult Fail(string error, string providerCode = null) =>
            new InsurancePriceResult
            {
                IsSuccessful = false,
                ErrorMessage = error,
                ProviderCode = providerCode
            };
    }
}
