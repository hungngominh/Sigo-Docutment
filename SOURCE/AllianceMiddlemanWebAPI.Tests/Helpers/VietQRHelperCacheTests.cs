using AllianceMiddlemanWebAPI.Shared.Helper;
using AllianceMiddlemanWebAPI.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit;

namespace AllianceMiddlemanWebAPI.Tests.Helpers
{
    /// <summary>
    /// Unit tests cho VietQRHelper.LookupBusiness cache behavior.
    /// Không cần HTTP — test thuần logic cache key normalization và TTL.
    /// </summary>
    public class VietQRHelperCacheTests : IDisposable
    {
        public VietQRHelperCacheTests() => ClearCache();
        public void Dispose() => ClearCache();

        private static ConcurrentDictionary<string, (VietQR_Response<VietQR_ResponseData_Business> data, DateTime cachedAt)> GetCache()
        {
            var field = typeof(VietQRHelper).GetField(
                "_businessCache",
                BindingFlags.Static | BindingFlags.NonPublic);
            return field?.GetValue(null)
                as ConcurrentDictionary<string, (VietQR_Response<VietQR_ResponseData_Business> data, DateTime cachedAt)>;
        }

        private static void ClearCache() => GetCache()?.Clear();

        // --- Normalization tests ---

        [Theory]
        [InlineData("0101283873", "0101283873")]
        [InlineData("0101-283.873", "0101283873")]
        [InlineData("010 128 3873", "0101283873")]
        [InlineData("MST: 0101283873", "0101283873")]
        public void NormalizeTaxCode_ShouldStripNonDigits(string input, string expected)
        {
            var result = VietQRHelper.NormalizeTaxCodeForCache(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NormalizeTaxCode_Null_ShouldReturnEmpty()
        {
            var result = VietQRHelper.NormalizeTaxCodeForCache(null);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void NormalizeTaxCode_Empty_ShouldReturnEmpty()
        {
            var result = VietQRHelper.NormalizeTaxCodeForCache(string.Empty);
            Assert.Equal(string.Empty, result);
        }

        // --- Cache TTL tests ---

        [Fact]
        public void CacheIsHit_WhenEntryExistsAndWithin24Hours()
        {
            var taxCode = "0101283873";
            var cache = GetCache();
            cache[taxCode] = (new VietQR_Response<VietQR_ResponseData_Business>(), DateTime.Now.AddHours(-1));

            bool isHit = VietQRHelper.IsBusinessCacheHit(taxCode, out _);

            Assert.True(isHit);
        }

        [Fact]
        public void CacheIsHit_ReturnedDataIsNotNull()
        {
            // Verifies the out parameter is populated on a cache hit
            var taxCode = "0101283873";
            var cache = GetCache();
            cache[taxCode] = (new VietQR_Response<VietQR_ResponseData_Business>(), DateTime.Now.AddHours(-1));

            VietQRHelper.IsBusinessCacheHit(taxCode, out var cached);
            Assert.NotNull(cached);
        }

        [Fact]
        public void CacheIsMiss_WhenEntryOlderThan24Hours()
        {
            var taxCode = "0101283873";
            var cache = GetCache();
            cache[taxCode] = (new VietQR_Response<VietQR_ResponseData_Business>(), DateTime.Now.AddHours(-25));

            bool isHit = VietQRHelper.IsBusinessCacheHit(taxCode, out _);

            Assert.False(isHit);
        }

        [Fact]
        public void CacheIsMiss_WhenNoEntryExists()
        {
            bool isHit = VietQRHelper.IsBusinessCacheHit("9999999999", out _);
            Assert.False(isHit);
        }
    }
}
