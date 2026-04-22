using Ezy.Module.Insurance.Shared.Models;
using Ezy.Module.Library.Utilities;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ezy.Module.Insurance.Shared.Providers.Vifo
{
    /// <summary>
    /// Insurance price provider backed by the VIFO partner API.
    /// Docs: https://docs.vifo.vn/docs/api/total-price-order
    /// </summary>
    public class VifoInsuranceProvider : IInsurancePriceProvider
    {
        private readonly VifoConfig _config;
        private string _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public VifoInsuranceProvider(VifoConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string ProviderCode => "VIFO";

        private string GetBearerToken()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            try
            {
                var loginUrl = BuildUrl("/v1/clients/web/admin/login");
                var client = new RestClient(loginUrl);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Accept", "application/json");
                request.AddJsonBody(new { username = _config.UserName, password = _config.Password });

                var resp = client.Execute(request);
                if (resp.IsSuccessful && !string.IsNullOrEmpty(resp.Content))
                {
                    var token = JsonConvert.DeserializeObject<VifoTokenResponse>(resp.Content);
                    if (token != null && !string.IsNullOrEmpty(token.access_token))
                    {
                        _cachedToken = token.access_token;
                        _tokenExpiry = DateTime.UtcNow.AddMinutes(50);
                        return _cachedToken;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.GetBearerToken");
            }
            return null;
        }

        // ------------------------------------------------------------------
        //  POST /v2/insurance/total-price
        // ------------------------------------------------------------------
        public InsurancePriceResult GetTotalPrice(InsurancePriceRequest request)
        {
            var result = new InsurancePriceResult { ProviderCode = ProviderCode };
            try
            {
                var url = BuildUrl("/v2/insurance/total-price");
                var response = Post(url, request.Payload);

                if (!response.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = response.Error ?? $"HTTP {response.StatusCode}";
                    return result;
                }

                var data = JsonConvert.DeserializeObject<VifoTotalPriceResponse>(response.Content);

                if (data == null)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = "Empty response from VIFO";
                    return result;
                }

                // Auth / validation error path (HTTP 200 but success=false)
                if (!data.Success)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = data.Message ?? "VIFO returned success=false";
                    result.RawData = data;
                    return result;
                }

                result.IsSuccessful = true;
                result.FinalAmount = data.Data?.FinalAmount ?? 0;
                result.RawData = data.Data;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.GetTotalPrice");
                result.IsSuccessful = false;
                result.ErrorMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        // ------------------------------------------------------------------
        //  POST /v2/insurance  –  Create order
        // ------------------------------------------------------------------
        public InsuranceCreateOrderResult CreateOrder(InsuranceCreateOrderRequest request)
        {
            var result = new InsuranceCreateOrderResult { ProviderCode = ProviderCode };
            try
            {
                var url = BuildUrl("/v2/insurance");
                var response = Post(url, request.Payload);

                if (!response.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = response.Error ?? $"HTTP {response.StatusCode}";
                    return result;
                }

                var data = JsonConvert.DeserializeObject<VifoCreateOrderResponse>(response.Content);

                if (data == null)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = "Empty response from VIFO";
                    return result;
                }

                if (!data.Success)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = data.Message ?? "VIFO returned success=false";
                    result.RawData = data;
                    return result;
                }

                result.IsSuccessful = true;
                result.OrderNumber         = data.Data?.OrderNumber;
                result.ExternalId          = data.Data?.Id;
                result.ProviderOrderNumber = data.Data?.ProviderOrderNumber;
                result.ContractUrl         = data.Data?.ContractFiles?.FirstOrDefault()?.Url;
                result.CreatedAt           = data.Data?.CreatedAt?.Date;
                result.RawData             = data.Data;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.CreateOrder");
                result.IsSuccessful = false;
                result.ErrorMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        // ------------------------------------------------------------------
        //  GET /v2/insurance/:order_number  –  Check order
        // ------------------------------------------------------------------
        public InsuranceOrderDetailResult CheckOrder(string orderNumber)
        {
            var result = new InsuranceOrderDetailResult { ProviderCode = ProviderCode, OrderNumber = orderNumber };
            try
            {
                var url = BuildUrl($"/v2/insurance/{orderNumber}");
                var response = Get(url);

                if (!response.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = response.Error ?? $"HTTP {response.StatusCode}";
                    return result;
                }

                var data = JsonConvert.DeserializeObject<VifoCheckOrderResponse>(response.Content);

                if (data == null)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = "Empty response from VIFO";
                    return result;
                }

                if (!data.Success)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = data.Message ?? "VIFO returned success=false";
                    result.RawData = data;
                    return result;
                }

                result.IsSuccessful = true;
                result.Status = data.Data?.Status;
                result.RawData = data.Data;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.CheckOrder");
                result.IsSuccessful = false;
                result.ErrorMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        // ------------------------------------------------------------------
        //  POST /v2/order/:order_number/terminate  –  Cancel order
        // ------------------------------------------------------------------
        public InsuranceTerminateResult TerminateOrder(string orderNumber)
        {
            var result = new InsuranceTerminateResult { ProviderCode = ProviderCode, OrderNumber = orderNumber };
            try
            {
                var url = BuildUrl($"/v2/order/{orderNumber}/terminate");
                var response = Post(url, new { });

                if (!response.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = response.Error ?? $"HTTP {response.StatusCode}";
                    return result;
                }

                var data = JsonConvert.DeserializeObject<VifoTerminateOrderResponse>(response.Content);

                if (data == null)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = "Empty response from VIFO";
                    return result;
                }

                result.IsSuccessful = data.Success;
                result.Message = data.Message;
                result.RawData = data.Data;

                if (!data.Success)
                    result.ErrorMessage = data.Message;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.TerminateOrder");
                result.IsSuccessful = false;
                result.ErrorMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        // ------------------------------------------------------------------
        //  GET /v2/order/:order_number/pvi  –  BHXH status from PVI
        // ------------------------------------------------------------------
        public InsurancePviStatusResult CheckOrderPviStatus(string orderNumber)
        {
            var result = new InsurancePviStatusResult { ProviderCode = ProviderCode, OrderNumber = orderNumber };
            try
            {
                var url = BuildUrl($"/v2/order/{orderNumber}/pvi");
                var response = Get(url);

                if (!response.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = response.Error ?? $"HTTP {response.StatusCode}";
                    return result;
                }

                var data = JsonConvert.DeserializeObject<VifoPviStatusResponse>(response.Content);

                if (data == null)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = "Empty response from VIFO";
                    return result;
                }

                result.IsSuccessful = data.Success;
                result.RawData = data.Data;

                if (!data.Success)
                    result.ErrorMessage = data.Message;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.CheckOrderPviStatus");
                result.IsSuccessful = false;
                result.ErrorMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        // ------------------------------------------------------------------
        //  GET /v2/families
        // ------------------------------------------------------------------
        public InsuranceFamiliesResult GetFamilies()
        {
            var result = new InsuranceFamiliesResult { ProviderCode = ProviderCode };
            try
            {
                var url = BuildUrl("/v2/families");
                var response = Get(url);

                if (!response.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = response.Error ?? $"HTTP {response.StatusCode}";
                    return result;
                }

                var data = JsonConvert.DeserializeObject<VifoFamiliesResponse>(response.Content);

                if (data?.Data == null)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = data?.Message ?? "No data returned";
                    return result;
                }

                result.IsSuccessful = true;
                result.Families = data.Data.Select(MapFamily).ToList();
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.GetFamilies");
                result.IsSuccessful = false;
                result.ErrorMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        // ------------------------------------------------------------------
        //  GET /v2/products?family_code=&page=&per_page=
        // ------------------------------------------------------------------
        public InsuranceProductsResult GetProducts(string familyCode, int page = 1, int perPage = 50)
        {
            var result = new InsuranceProductsResult { ProviderCode = ProviderCode };
            try
            {
                var url = BuildUrl("/v2/products");
                var queryParams = new Dictionary<string, string>
                {
                    { "family_code", familyCode },
                    { "page", page.ToString() },
                    { "per_page", perPage.ToString() }
                };
                var response = Get(url, queryParams);

                if (!response.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = response.Error ?? $"HTTP {response.StatusCode}";
                    return result;
                }

                var data = JsonConvert.DeserializeObject<VifoProductsResponse>(response.Content);

                if (data?.Data == null)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = data?.Message ?? "No data returned";
                    return result;
                }

                result.IsSuccessful = true;
                result.Products = data.Data.Select(MapProduct).ToList();
                result.TotalCount = data.Meta?.Pagination?.Total ?? result.Products.Count;
                result.TotalPages = data.Meta?.Pagination?.TotalPages ?? 1;
                result.CurrentPage = data.Meta?.Pagination?.CurrentPage ?? page;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.GetProducts");
                result.IsSuccessful = false;
                result.ErrorMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        // ------------------------------------------------------------------
        //  HTTP helpers (RestSharp)
        // ------------------------------------------------------------------
        private (bool IsSuccessful, int StatusCode, string Content, string Error) Get(
            string url, Dictionary<string, string> queryParams = null)
        {
            try
            {
                var client = new RestClient(url);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", $"Bearer {GetBearerToken()}");
                request.AddHeader("Accept", "application/json");

                if (queryParams != null)
                    foreach (var kv in queryParams)
                        if (kv.Value != null)
                            request.AddQueryParameter(kv.Key, kv.Value);

                var resp = client.Execute(request);
                return (resp.IsSuccessful, (int)resp.StatusCode, resp.Content, resp.ErrorMessage);
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.Get");
                return (false, 0, null, ExceptionHelper.GetMessage(ex));
            }
        }

        private (bool IsSuccessful, int StatusCode, string Content, string Error) Post(
            string url, object body)
        {
            try
            {
                var client = new RestClient(url);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {GetBearerToken()}");
                request.AddHeader("Accept", "application/json");
                request.AddJsonBody(body);

                var resp = client.Execute(request);
                return (resp.IsSuccessful, (int)resp.StatusCode, resp.Content, resp.ErrorMessage);
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "VifoInsuranceProvider.Post");
                return (false, 0, null, ExceptionHelper.GetMessage(ex));
            }
        }

        // ------------------------------------------------------------------
        //  Mapping helpers
        // ------------------------------------------------------------------
        private static InsuranceFamilyInfo MapFamily(VifoFamilyData f) =>
            new InsuranceFamilyInfo
            {
                Code = f.Code,
                Icon = f.Icon,
                ProductCount = f.ProductCount,
                Name = f.Translation != null && f.Translation.TryGetValue("vi", out var vi) ? vi.Name : f.Code,
                NameEn = f.Translation != null && f.Translation.TryGetValue("en", out var en) ? en.Name : f.Code
            };

        private static InsuranceProductInfo MapProduct(VifoProductData p) =>
            new InsuranceProductInfo
            {
                FamilyCode = p.FamilyCode,
                ProviderCode = p.ProviderCode,
                ProductCode = p.ProductCode,
                Name = p.Name,
                NameVi = p.NameVi,
                Icon = p.Icon,
                Price = p.Price,
                PaymentTerm = p.PaymentTerm,
                ProductDetailFile = p.ProductDetailFile,
                ProductTermFile = p.ProductTermFile,
                Options = p.Options?.Select(g => new InsuranceProductOption
                {
                    Title = g.Title,
                    Type = g.Type,
                    Values = g.Values?.Select(v => new InsuranceProductOptionValue
                    {
                        Key = v.Key,
                        Title = v.Title,
                        Sku = v.Sku,
                        Price = v.Price
                    }).ToList()
                }).ToList()
            };

        private string BuildUrl(string path) =>
            _config.BaseUrl.TrimEnd('/') + path;
    }

    internal class VifoTokenResponse
    {
        public string access_token { get; set; }
    }
}
