using HumbleChoiceScrapper.Helpers.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HumbleChoiceScrapper.Helpers
{
    public class CacheHelper : ICacheHelper
    {
        private readonly IMemoryCache _cache;

        // Configuración por defecto
        private readonly TimeSpan _defaultAbsoluteExpiration = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _defaultSlidingExpiration = TimeSpan.FromMinutes(10);

        public CacheHelper(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool TryGet<T>(string key, out T value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(absoluteExpiration ?? _defaultAbsoluteExpiration)
                .SetSlidingExpiration(slidingExpiration ?? _defaultSlidingExpiration);

            _cache.Set(key, value, cacheEntryOptions);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void Clear()
        {
            // MemoryCache no tiene método Clear directo
            // Puedes mantener una lista de keys o recrear el cache
            if (_cache is MemoryCache concreteCache)
            {
                concreteCache.Compact(1.0); // Fuerza limpieza
            }
        }
    }
}
