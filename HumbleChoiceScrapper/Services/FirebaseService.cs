using Firebase.Database;
using Firebase.Database.Query;
using HumbleChoiceScrapper.Helpers;
using HumbleChoiceScrapper.Helpers.Interfaces;
using HumbleChoiceScrapper.Models;
using HumbleChoiceScrapper.Responses;
using HumbleChoiceScrapper.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text.RegularExpressions;


namespace HumbleChoiceScrapper.Services
{
    public class FirebaseService : IFirebaseService
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly ICacheHelper _cacheHelper;

        // Mapeo de meses
        private readonly Dictionary<string, string> _monthMapping = new()
    {
        {"january", "01"}, {"february", "02"}, {"march", "03"}, {"april", "04"},
        {"may", "05"}, {"june", "06"}, {"july", "07"}, {"august", "08"},
        {"september", "09"}, {"october", "10"}, {"november", "11"}, {"december", "12"},
        {"jan", "01"}, {"feb", "02"}, {"mar", "03"}, {"apr", "04"},
        {"jun", "06"}, {"jul", "07"}, {"aug", "08"}, {"sep", "09"},
        {"oct", "10"}, {"nov", "11"}, {"dec", "12"}
    };

        public FirebaseService(FirebaseClient firebaseClient, ICacheHelper cacheHelper)
        {
            _firebaseClient = firebaseClient;
            _cacheHelper = cacheHelper;
        }

        // Extraer año del BundleDate
        private string ExtractYearFromBundleDate(string bundleDate)
        {
            if (string.IsNullOrEmpty(bundleDate))
                return "unknown";

            var match = Regex.Match(bundleDate, @"\b(\d{4})\b");
            return match.Success ? match.Groups[1].Value : "unknown";
        }

        // Extraer mes del BundleDate
        private string ExtractMonthFromBundleDate(string bundleDate)
        {
            if (string.IsNullOrEmpty(bundleDate))
                return "unknown";

            var lower = bundleDate.ToLower().Trim();

            foreach (var kvp in _monthMapping)
            {
                if (lower.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            return "unknown";
        }

        // ===========================================
        // MÉTODOS PRINCIPALES - AGRUPADOS POR AÑO
        // ===========================================

        // Agregar juego (automáticamente lo coloca en su año)
        public async Task<string> AddGameAsync(GameInfo game)
        {
            try
            {
                var year = ExtractYearFromBundleDate(game.BundleDate);

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                var json = JsonConvert.SerializeObject(game, settings);

                // Estructura: gamesByYear/2024/gameId
                var result = await _firebaseClient
                    .Child("gamesByYear")
                    .Child(year)
                    .PostAsync(json);

                return result.Key;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding game: {ex.Message}");
            }
        }

        // Obtener TODOS los juegos (optimizado - trae año por año)
        public async Task<List<GameInfo>> GetAllGamesAsync()
        {
            try
            {
                var allGames = new List<GameInfo>();

                // Obtener todos los años disponibles
                var yearsDict = await _firebaseClient
                    .Child("gamesByYear")
                    .OnceSingleAsync<Dictionary<string, Dictionary<string, GameInfo>>>();

                if (yearsDict == null)
                    return allGames;

                // Agregar juegos de todos los años
                foreach (var yearData in yearsDict.Values)
                {
                    if (yearData != null)
                    {
                        allGames.AddRange(yearData.Values);
                    }
                }

                return allGames.OrderByDescending(g => g.BundleDate).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting all games: {ex.Message}");
            }
        }

        // Obtener juegos por año específico (SÚPER RÁPIDO)
        public async Task<List<GameInfo>> GetGamesByYearAsync(string year)
        {
            try
            {
                Dictionary<string, GameInfo> gamesDict = null;               

                if (_cacheHelper.TryGet(year, out gamesDict))
                {
                    return gamesDict.Values.ToList();
                }

                gamesDict = await _firebaseClient
                    .Child("gamesByYear")
                    .Child(year)
                    .OnceSingleAsync<Dictionary<string, GameInfo>>();               

                _cacheHelper.Set(year, gamesDict);

                if (gamesDict == null || !gamesDict.Any())
                    return new List<GameInfo>();
               
                return gamesDict.Values.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting games by year: {ex.Message}");
            }
        }

        // Obtener juegos por múltiples años (RÁPIDO - solo los años necesarios)
        public async Task<List<GameInfo>> GetGamesByMultipleYearsAsync(params string[] years)
        {
            try
            {
                var allGames = new List<GameInfo>();

                foreach (var year in years)
                {
                    var yearGames = await GetGamesByYearAsync(year);
                    allGames.AddRange(yearGames);
                }

                return allGames.OrderByDescending(g => g.BundleDate).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting games by multiple years: {ex.Message}");
            }
        }

        // Obtener juegos por mes específico "september-2024" (RÁPIDO)
        public async Task<List<GameInfo>> GetGamesByMonthYearAsync(string monthYear)
        {
            try
            {
                var parts = monthYear.ToLower().Split('-');
                if (parts.Length != 2)
                    throw new ArgumentException("Format should be 'september-2024'");

                var monthName = parts[0].Trim();
                var year = parts[1].Trim();

                // Validar año
                if (year.Length != 4 || !int.TryParse(year, out _))
                    throw new ArgumentException("Year must be 4 digits");

                // Obtener todos los juegos del año
                var yearGames = await GetGamesByYearAsync(year);

                // Filtrar por mes específico
                var filteredGames = yearGames.Where(game =>
                {
                    var gameMonth = ExtractMonthFromBundleDate(game.BundleDate);
                    var targetMonth = _monthMapping.ContainsKey(monthName) ? _monthMapping[monthName] : "unknown";
                    return gameMonth == targetMonth;
                }).ToList();

                return filteredGames;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting games by month-year: {ex.Message}");
            }
        }

        // Obtener juegos por rango de fechas (OPTIMIZADO)
        public async Task<List<GameInfo>> GetGamesByMonthYearRangeAsync(string startMonthYear, string endMonthYear)
        {
            try
            {
                // Extraer años del rango
                var startYear = int.Parse(startMonthYear.Split('-')[1]);
                var endYear = int.Parse(endMonthYear.Split('-')[1]);

                var allGames = new List<GameInfo>();

                // Traer solo los años del rango (no todos los años)
                for (int year = startYear; year <= endYear; year++)
                {
                    var yearGames = await GetGamesByYearAsync(year.ToString());
                    allGames.AddRange(yearGames);
                }

                // Filtrar por el rango específico de meses
                var filteredGames = allGames.Where(game =>
                {
                    var gameYear = ExtractYearFromBundleDate(game.BundleDate);
                    var gameMonth = ExtractMonthFromBundleDate(game.BundleDate);

                    if (!int.TryParse(gameYear, out int gYear) || gameMonth == "unknown")
                        return false;

                    var gameDate = $"{gameMonth}-{gameYear}";
                    var startDate = ConvertMonthYearToComparable(startMonthYear);
                    var endDate = ConvertMonthYearToComparable(endMonthYear);
                    var currentDate = ConvertMonthYearToComparable($"{GetMonthNameFromNumber(gameMonth)}-{gameYear}");

                    return currentDate >= startDate && currentDate <= endDate;
                }).ToList();

                return filteredGames.OrderByDescending(g => g.BundleDate).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting games by date range: {ex.Message}");
            }
        }

        // ===========================================
        // MÉTODOS DE ESTADÍSTICAS Y UTILIDAD
        // ===========================================

        // Obtener años disponibles
        public async Task<List<string>> GetAvailableYearsAsync()
        {
            try
            {
                var yearsDict = await _firebaseClient
                    .Child("gamesByYear")
                    .OnceSingleAsync<Dictionary<string, object>>();

                if (yearsDict == null)
                    return new List<string>();

                return yearsDict.Keys
                    .Where(key => key != "unknown" && key.Length == 4)
                    .OrderByDescending(year => year)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting available years: {ex.Message}");
            }
        }

        // Obtener períodos disponibles en formato "september-2024"
        public async Task<List<string>> GetAvailableMonthYearPeriodsAsync()
        {
            try
            {
                var allPeriods = new List<string>();
                var years = await GetAvailableYearsAsync();

                foreach (var year in years)
                {
                    var yearGames = await GetGamesByYearAsync(year);

                    var monthsInYear = yearGames
                        .Select(game => ExtractMonthFromBundleDate(game.BundleDate))
                        .Where(month => month != "unknown")
                        .Distinct()
                        .Select(monthNum => GetMonthNameFromNumber(monthNum))
                        .Where(monthName => monthName != "unknown")
                        .Select(monthName => $"{monthName}-{year}");

                    allPeriods.AddRange(monthsInYear);
                }

                return allPeriods
                    .Distinct()
                    .OrderByDescending(period => ConvertMonthYearToComparable(period))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting available periods: {ex.Message}");
            }
        }

        // Estadísticas por año
        public async Task<Dictionary<string, int>> GetGameCountByYearAsync()
        {
            try
            {
                var yearsDict = await _firebaseClient
                    .Child("gamesByYear")
                    .OnceSingleAsync<Dictionary<string, Dictionary<string, GameInfo>>>();

                if (yearsDict == null)
                    return new Dictionary<string, int>();

                return yearsDict.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Count ?? 0
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting game count by year: {ex.Message}");
            }
        }

        // ===========================================
        // MÉTODOS DE MIGRACIÓN Y MANTENIMIENTO
        // ===========================================

        // Migrar estructura antigua a nueva estructura por año
        public async Task<string> MigrateToYearStructureAsync()
        {
            try
            {
                // Obtener juegos de estructura anterior
                var oldGames = await _firebaseClient
                    .Child("games")
                    .OnceSingleAsync<Dictionary<string, GameInfo>>();

                if (oldGames == null || !oldGames.Any())
                    return "No games to migrate";

                var migratedCount = 0;
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                foreach (var game in oldGames.Values)
                {
                    var year = ExtractYearFromBundleDate(game.BundleDate);
                    var json = JsonConvert.SerializeObject(game, settings);

                    await _firebaseClient
                        .Child("gamesByYear")
                        .Child(year)
                        .PostAsync(json);

                    migratedCount++;
                }

                return $"Migrated {migratedCount} games to year-based structure";
            }
            catch (Exception ex)
            {
                throw new Exception($"Migration error: {ex.Message}");
            }
        }

        // ===========================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ===========================================

        private string GetMonthNameFromNumber(string monthNumber)
        {
            var monthNames = new Dictionary<string, string>
        {
            {"01", "january"}, {"02", "february"}, {"03", "march"}, {"04", "april"},
            {"05", "may"}, {"06", "june"}, {"07", "july"}, {"08", "august"},
            {"09", "september"}, {"10", "october"}, {"11", "november"}, {"12", "december"}
        };

            return monthNames.ContainsKey(monthNumber) ? monthNames[monthNumber] : "unknown";
        }

        private int ConvertMonthYearToComparable(string monthYear)
        {
            try
            {
                var parts = monthYear.ToLower().Split('-');
                if (parts.Length != 2) return 0;

                var monthName = parts[0];
                var year = int.Parse(parts[1]);
                var monthNumber = _monthMapping.ContainsKey(monthName) ? int.Parse(_monthMapping[monthName]) : 0;

                return year * 100 + monthNumber; // Ejemplo: september-2024 = 202409
            }
            catch
            {
                return 0;
            }
        }

        // ===========================================
        // MÉTODOS DE INTERFAZ GENÉRICOS
        // ===========================================

        public async Task<T> GetAsync<T>(string path)
        {
            return await _firebaseClient
                .Child(path)
                .OnceSingleAsync<T>();
        }

        public async Task<string> PostAsync<T>(string path, T data)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(data, settings);
            var result = await _firebaseClient
                .Child(path)
                .PostAsync(json);
            return result.Key;
        } 

        public async Task PutAsync<T>(string path, T data)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            await _firebaseClient
                .Child(path)
                .PutAsync(JsonConvert.SerializeObject(data, settings));
        }

        public async Task DeleteAsync(string path)
        {
            await _firebaseClient
                .Child(path)
                .DeleteAsync();
        }

        public async Task UpdateGameAsync(string gameId, GameInfo game)
        {
            var year = ExtractYearFromBundleDate(game.BundleDate);
            await PutAsync($"gamesByYear/{year}/{gameId}", game);
        }

        public async Task DeleteGameAsync(string gameId)
        {
            // Para eliminar, necesitamos saber en qué año está
            // Esto requiere buscar en todos los años o mantener un índice
            var years = await GetAvailableYearsAsync();

            foreach (var year in years)
            {
                try
                {
                    await DeleteAsync($"gamesByYear/{year}/{gameId}");
                    return; // Si encuentra y elimina, salir
                }
                catch
                {
                    // Continuar con el siguiente año
                }
            }
        }

        // Método específico para eliminar sabiendo el año
        public async Task DeleteGameByYearAsync(string gameId, string year)
        {
            await DeleteAsync($"gamesByYear/{year}/{gameId}");
        }
    }
}