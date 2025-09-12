using HumbleChoiceScrapper.Models;

namespace HumbleChoiceScrapper.Services.Interfaces
{
    public interface IFirebaseService
    {
        Task<T> GetAsync<T>(string path);
        Task<string> PostAsync<T>(string path, T data);
        Task PutAsync<T>(string path, T data);
        Task DeleteAsync(string path);

        // Métodos específicos para GameInfo (OPTIMIZADOS)
        Task<List<GameInfo>> GetAllGamesAsync();
        Task<string> AddGameAsync(GameInfo game);
        Task UpdateGameAsync(string gameId, GameInfo game);
        Task DeleteGameAsync(string gameId);
        Task DeleteGameByYearAsync(string gameId, string year);

        // Métodos de búsqueda optimizada
        Task<List<GameInfo>> GetGamesByYearAsync(string year);
        Task<List<GameInfo>> GetGamesByMultipleYearsAsync(params string[] years);
        Task<List<GameInfo>> GetGamesByMonthYearAsync(string monthYear);
        Task<List<GameInfo>> GetGamesByMonthYearRangeAsync(string startMonthYear, string endMonthYear);

        // Métodos de estadísticas
        Task<List<string>> GetAvailableYearsAsync();
        Task<List<string>> GetAvailableMonthYearPeriodsAsync();
        Task<Dictionary<string, int>> GetGameCountByYearAsync();

        // Migración
        Task<string> MigrateToYearStructureAsync();
    }
}
