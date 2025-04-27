using Microsoft.Extensions.Caching.Memory;
using VericateChat.Interfaces;

namespace VericateChat.Services
{
    public class MemoryStateCacheService : IStateCacheService
    {
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public bool IsDuplicate(string state)
        {
            if (_cache.TryGetValue(state, out _))
            {
                return true;
            }
            _cache.Set(state, true, TimeSpan.FromMinutes(5));
            return false;
        }
    }
}
