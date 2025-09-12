namespace HumbleChoiceScrapper.Helpers.Interfaces
{
    public interface ICacheHelper
    {
        bool TryGet<T>(string key, out T value);
        void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
        void Remove(string key);
        void Clear();
    }
}
