using Microsoft.Extensions.Caching.Memory;

namespace GAM106_ASM.Services
{
    public interface IOtpService
    {
        string GenerateOtp(string email, TimeSpan lifetime);
        bool ValidateOtp(string email, string otp);
    }

    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private const string CachePrefix = "otp:";

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateOtp(string email, TimeSpan lifetime)
        {
            var code = Random.Shared.Next(100000, 999999).ToString();
            var cacheKey = CachePrefix + email.ToLowerInvariant();

            _cache.Set(cacheKey, code, lifetime);
            return code;
        }

        public bool ValidateOtp(string email, string otp)
        {
            var cacheKey = CachePrefix + email.ToLowerInvariant();
            if (_cache.TryGetValue(cacheKey, out string? stored) && stored == otp)
            {
                _cache.Remove(cacheKey); // one-time use
                return true;
            }
            return false;
        }
    }
}
