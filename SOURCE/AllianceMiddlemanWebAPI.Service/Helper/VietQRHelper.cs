using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AllianceMiddlemanWebAPI.Shared.Models;
using Ezy.APIService.Core.Services;
using Ezy.Module.Library.Utilities;

namespace AllianceMiddlemanWebAPI.Shared.Helper
{
    public class VietQRHelper
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<
            string,
            (VietQR_Response<VietQR_ResponseData_Business> data, DateTime cachedAt)>
            _businessCache = new();

        private const double _businessCacheTtlHours = 24;

        /// <summary>
        /// Strip mọi ký tự không phải số để tạo cache key nhất quán.
        /// Internal để test có thể gọi trực tiếp.
        /// </summary>
        internal static string NormalizeTaxCodeForCache(string taxCode)
        {
            if (string.IsNullOrEmpty(taxCode)) return string.Empty;
            return Regex.Replace(taxCode, "[^0-9]", "");
        }

        /// <summary>
        /// Kiểm tra cache hit: entry tồn tại và chưa quá TTL.
        /// Khi trả về true, caller KHÔNG được ghi đè sMessage (vì không có HTTP call).
        /// Internal để test có thể gọi trực tiếp.
        /// </summary>
        internal static bool IsBusinessCacheHit(string normalizedTaxCode, out VietQR_Response<VietQR_ResponseData_Business> cached)
        {
            cached = null;
            if (_businessCache.TryGetValue(normalizedTaxCode, out var entry))
            {
                if ((DateTime.Now - entry.cachedAt).TotalHours < _businessCacheTtlHours)
                {
                    cached = entry.data;
                    return true;
                }
                // Evict stale entry so the dictionary does not grow unbounded
                _businessCache.TryRemove(normalizedTaxCode, out _);
            }
            return false;
        }

        public static VietQR_Response<VietQR_ReponseData_Bank[]> GetBankList(out string sMessage)
        {
            var settings = GetVietQRSettings();
            return GetBankList(settings.ClientId, settings.ApiKey, out sMessage);
        }

        public static VietQR_Response<VietQR_ResponseData_BankAccount> LookupBankAccount(string bin, string accountNumber, out string sMessage)
        {
            var settings = GetVietQRSettings();
            return LookupBankAccount(bin, accountNumber, settings.ClientId, settings.ApiKey, out sMessage);
        }

        public static VietQR_Response<VietQR_ResponseData_Citizen> LookupCitizen(string legalId, string legalName, out string sMessage)
        {
            var settings = GetVietQRSettings();
            return LookupCitizen(legalId, legalName, settings.ClientId, settings.ApiKey, out sMessage);
        }

        public static VietQR_Response<VietQR_ReponseData_Bank[]> GetBankList(string clientId, string apiKey, out string sMessage)
        {
            VietQR_Response<VietQR_ReponseData_Bank[]> result = null;
            sMessage = string.Empty;
            try
            {
                var requestUrl = GetVietQRSettings().ApiBaseUrl;
                requestUrl += "v2/banks";
                var response = SendGetHttpRequestAsync(requestUrl, clientId, apiKey);
                sMessage = response.Error;
                result = JsonHelper.DeserializeObject<VietQR_Response<VietQR_ReponseData_Bank[]>>(response.Content);

                result.json = response.Content;
                result.status ??= response.StatusCode;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "GetBankList");
                sMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        public static VietQR_Response<VietQR_ResponseData_BankAccount> LookupBankAccount(string bin, string accountNumber, string clientId, string apiKey, out string sMessage)
        {
            VietQR_Response<VietQR_ResponseData_BankAccount> result = null;
            sMessage = string.Empty;
            try
            {
                var requestUrl = GetVietQRSettings().ApiBaseUrl;
                requestUrl += "v2/lookup";

                var content = new
                {
                    bin = bin,
                    accountNumber = accountNumber
                };

                var response = SendPostHttpRequestAsync(requestUrl, content, clientId, apiKey);
                sMessage = response.Error;
                result = JsonHelper.DeserializeObject<VietQR_Response<VietQR_ResponseData_BankAccount>>(response.Content);

                result.json = response.Content;
                result.status ??= response.StatusCode;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "LookupBankAccount");
                sMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        public static VietQR_Response<VietQR_ResponseData_Citizen> LookupCitizen(string legalId, string legalName, string clientId, string apiKey, out string sMessage)
        {
            VietQR_Response<VietQR_ResponseData_Citizen> result = null;
            sMessage = string.Empty;
            try
            {
                var requestUrl = GetVietQRSettings().ApiBaseUrl;
                requestUrl += "v2/citizen";

                var content = new
                {
                    legalId = legalId,
                    legalName = legalName.ToUpperInvariant()
                };

                var response = SendPostHttpRequestAsync(requestUrl, content, clientId, apiKey);
                sMessage = response.Error;
                result = JsonHelper.DeserializeObject<VietQR_Response<VietQR_ResponseData_Citizen>>(response.Content);

                result.json = response.Content;
                result.status ??= response.StatusCode;
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "LookupCitizen");
                sMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        public static VietQR_Response<VietQR_ResponseData_Business> LookupBusiness(string taxCode, out string sMessage)
        {
            VietQR_Response<VietQR_ResponseData_Business> result = null;
            sMessage = string.Empty;
            try
            {
                var normalizedKey = NormalizeTaxCodeForCache(taxCode);

                // Cache hit: sMessage giữ nguyên string.Empty (không có HTTP call)
                if (!string.IsNullOrEmpty(normalizedKey) && IsBusinessCacheHit(normalizedKey, out var cached))
                {
                    return cached;
                }

                var requestUrl = GetVietQRSettings().ApiBaseUrl;
                requestUrl += $"v2/business/{taxCode}";

                var response = SendGetHttpRequestAsync(requestUrl);
                sMessage = response.Error;
                result = JsonHelper.DeserializeObject<VietQR_Response<VietQR_ResponseData_Business>>(response.Content);
                if (result == null)
                {
                    result = new VietQR_Response<VietQR_ResponseData_Business>();
                }
                result.json = response.Content;
                result.status ??= response.StatusCode;

                // Chỉ cache khi có dữ liệu hợp lệ; lỗi (429, not found, network) không được cache
                if (!string.IsNullOrEmpty(normalizedKey) && result.data != null)
                {
                    _businessCache[normalizedKey] = (result, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "LookupBusiness");
                sMessage = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        public static VietQRSettings GetVietQRSettings()
        {
            var defaultSettings = new VietQRSettings()
            {
                ApiBaseUrl = "https://api.vietqr.io/",
                ApiKey = "0f9a3750-2d66-4ba7-ab99-9f48cbfe6178",
                ClientId = "bb7c6450-a275-4bf7-b1af-0de31da0ccbd"
            };
            var currentSettings = SystemConfigHelper.GetValueFromConfig_Obj("SYSTEM_VIETQR_SETTINGS", defaultSettings);
            return currentSettings ?? defaultSettings;
        }

        #region Http Request methods
        private static HttpRequestResponse SendGetHttpRequestAsync(string requestUrl, string clientId = null, string apiKey = null)
        {
            var result = new HttpRequestResponse();

            try
            {
                #region Old code
                //using (var client = new HttpClient())
                //{
                //    client.DefaultRequestHeaders.Add("X-Client-Id", clientId);
                //    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                //    var response = await client.GetAsync(requestUrl);

                //    result.Status = response.StatusCode.ToString();
                //    result.StatusCode = (int)response.StatusCode;

                //    result.Content = await response.Content.ReadAsStringAsync();
                //    result.Error = response.ReasonPhrase;
                //}
                #endregion

                var headers = new Dictionary<string, string>();
                if (clientId != null)
                {
                    headers.Add("x-client-id", clientId);
                }
                if (apiKey != null)
                {
                    headers.Add("x-api-key", apiKey);
                }

                result = HttpRequestHelper.SendGetHttpRequest(requestUrl, headers);
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "SendGetHttpRequest");
                result.Error = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }

        private static HttpRequestResponse SendPostHttpRequestAsync(string requestUrl, object content, string clientId, string apiKey)
        {
            var result = new HttpRequestResponse();

            try
            {
                #region Old codes
                //using (var client = new HttpClient())
                //{
                //    client.DefaultRequestHeaders.Add("X-Client-Id", clientId);
                //    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                //    var json = JsonHelper.SerializeObject(content);
                //    var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                //    var response = await client.PostAsync(requestUrl, stringContent);

                //    result.Status = response.StatusCode.ToString();
                //    result.StatusCode = (int)response.StatusCode;

                //    result.Content = await response.Content.ReadAsStringAsync();
                //    result.Error = response.ReasonPhrase;
                //}
                #endregion

                var headers = new Dictionary<string, string>();
                if (clientId != null)
                {
                    headers.Add("x-client-id", clientId);
                }
                if (apiKey != null)
                {
                    headers.Add("x-api-key", apiKey);
                }

                result = HttpRequestHelper.SendPostHttpRequest(requestUrl, headers: headers, jsonContent: content);
            }
            catch (Exception ex)
            {
                ExceptionHelper.SaveException(ex, "SendPostHttpRequestAsync");
                result.Error = ExceptionHelper.GetMessage(ex);
            }
            return result;
        }
        #endregion
    }
}
