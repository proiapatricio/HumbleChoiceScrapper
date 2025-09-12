using HumbleChoiceScrapper.Models;
using HumbleChoiceScrapper.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace HumbleChoiceScrapper.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public GamesController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // GET: api/games - Obtener todos los juegos (OPTIMIZADO)
        [HttpGet]
        public async Task<ActionResult<List<GameInfo>>> GetAllGames()
        {
            try
            {
                var games = await _firebaseService.GetAllGamesAsync();
                return Ok(new
                {
                    count = games.Count,
                    games = games
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting games: {ex.Message}");
            }
        }

        // GET: api/games/{id} - Obtener un juego específico
        [HttpGet("{id}")]
        public async Task<ActionResult<GameInfo>> GetGame(string id)
        {
            try
            {
                // Buscar en toda la estructura por años
                var years = await _firebaseService.GetAvailableYearsAsync();

                foreach (var year in years)
                {
                    var game = await _firebaseService.GetAsync<GameInfo>($"gamesByYear/{year}/{id}");
                    if (game != null && !string.IsNullOrEmpty(game.Title))
                    {
                        return Ok(game);
                    }
                }

                return NotFound($"Game with ID {id} not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting game: {ex.Message}");
            }
        }

        // GET: api/games/year/{year} - Obtener juegos por año (SÚPER RÁPIDO)
        [HttpGet("year/{year}")]
        public async Task<ActionResult<List<GameInfo>>> GetGamesByYear(string year)
        {
            try
            {
                if (year.Length != 4 || !int.TryParse(year, out _))
                {
                    return BadRequest("Year must be a 4-digit number (e.g., 2024)");
                }

                var games = await _firebaseService.GetGamesByYearAsync(year);

                return Ok(new
                {
                    year = year,
                    count = games.Count,
                    games = games
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting games by year: {ex.Message}");
            }
        }

        // GET: api/games/years?years=2024,2023,2022 - Múltiples años
        [HttpGet("years")]
        public async Task<ActionResult<List<GameInfo>>> GetGamesByMultipleYears([FromQuery] string years)
        {
            try
            {
                if (string.IsNullOrEmpty(years))
                {
                    return BadRequest("Please provide years parameter (e.g., ?years=2024,2023)");
                }

                var yearArray = years.Split(',')
                                     .Select(y => y.Trim())
                                     .Where(y => y.Length == 4 && int.TryParse(y, out _))
                                     .ToArray();

                if (!yearArray.Any())
                {
                    return BadRequest("Please provide valid 4-digit years");
                }

                var games = await _firebaseService.GetGamesByMultipleYearsAsync(yearArray);

                return Ok(new
                {
                    years = yearArray,
                    count = games.Count,
                    games = games
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting games by multiple years: {ex.Message}");
            }
        }

        // GET: api/games/month/september-2024 - Por mes específico
        [HttpGet("month/{monthYear}")]
        public async Task<ActionResult> GetGamesByMonthYear(string monthYear)
        {
            try
            {
                if (!IsValidMonthYearFormat(monthYear))
                {
                    return BadRequest("Date format must be month-YYYY (e.g., september-2024)");
                }

                var games = await _firebaseService.GetGamesByMonthYearAsync(monthYear);

                return Ok(new
                {
                    period = monthYear,
                    count = games.Count,
                    games = games
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting games by month-year: {ex.Message}");
            }
        }

        // GET: api/games/range?start=june-2023&end=march-2024 - Rango de fechas
        [HttpGet("range")]
        public async Task<ActionResult> GetGamesByMonthYearRange(
            [FromQuery] string start,
            [FromQuery] string end)
        {
            try
            {
                if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
                {
                    return BadRequest("Please provide start and end parameters (format: september-2024)");
                }

                if (!IsValidMonthYearFormat(start) || !IsValidMonthYearFormat(end))
                {
                    return BadRequest("Date format must be month-YYYY (e.g., september-2024)");
                }

                var games = await _firebaseService.GetGamesByMonthYearRangeAsync(start, end);

                return Ok(new
                {
                    dateRange = new { start, end },
                    count = games.Count,
                    games = games
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting games by date range: {ex.Message}");
            }
        }

        // GET: api/games/stats - Estadísticas por año
        [HttpGet("stats")]
        public async Task<ActionResult> GetGameStats()
        {
            try
            {
                var stats = await _firebaseService.GetGameCountByYearAsync();
                var years = await _firebaseService.GetAvailableYearsAsync();

                var totalGames = stats.Values.Sum();

                return Ok(new
                {
                    totalGames = totalGames,
                    totalYears = years.Count,
                    gamesByYear = stats,
                    availableYears = years
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting game stats: {ex.Message}");
            }
        }

        // GET: api/games/periods - Períodos disponibles en formato "september-2024"
        [HttpGet("periods")]
        public async Task<ActionResult> GetAvailablePeriods()
        {
            try
            {
                var periods = await _firebaseService.GetAvailableMonthYearPeriodsAsync();
                var stats = await _firebaseService.GetGameCountByYearAsync();

                return Ok(new
                {
                    periods = periods,
                    yearStats = stats,
                    totalPeriods = periods.Count,
                    example = "Use format like: september-2024, january-2023"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting available periods: {ex.Message}");
            }
        }

        // GET: api/games/recent?months=6 - Juegos recientes
        [HttpGet("recent")]
        public async Task<ActionResult> GetRecentGames([FromQuery] int months = 6)
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-months);

                // Convertir a formato "september-2024"
                var startMonthYear = $"{startDate.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture).ToLower()}-{startDate.Year}";
                var endMonthYear = $"{endDate.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture).ToLower()}-{endDate.Year}";

                var games = await _firebaseService.GetGamesByMonthYearRangeAsync(startMonthYear, endMonthYear);

                return Ok(new
                {
                    period = $"Last {months} months",
                    dateRange = new { start = startMonthYear, end = endMonthYear },
                    count = games.Count,
                    games = games
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting recent games: {ex.Message}");
            }
        }

        // POST: api/games - Crear un nuevo juego (automáticamente va al año correcto)
        [HttpPost]
        public async Task<ActionResult> CreateGame([FromBody] GameInfo game)
        {
            try
            {
                if (game == null || string.IsNullOrEmpty(game.Title))
                    return BadRequest("Game title is required");

                var gameId = await _firebaseService.AddGameAsync(game);

                return CreatedAtAction(nameof(GetGame),
                    new { id = gameId },
                    new { id = gameId, game });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating game: {ex.Message}");
            }
        }

        // PUT: api/games/{id} - Actualizar un juego
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateGame(string id, [FromBody] GameInfo game)
        {
            try
            {
                if (game == null || string.IsNullOrEmpty(game.Title))
                    return BadRequest("Game title is required");

                await _firebaseService.UpdateGameAsync(id, game);
                return Ok(new { message = "Game updated successfully", id, game });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating game: {ex.Message}");
            }
        }

        // DELETE: api/games/{id} - Eliminar un juego
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteGame(string id)
        {
            try
            {
                await _firebaseService.DeleteGameAsync(id);
                return Ok(new { message = "Game deleted successfully", id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting game: {ex.Message}");
            }
        }

        // DELETE: api/games/{id}/year/{year} - Eliminar juego sabiendo el año (MÁS RÁPIDO)
        [HttpDelete("{id}/year/{year}")]
        public async Task<ActionResult> DeleteGameByYear(string id, string year)
        {
            try
            {
                if (year.Length != 4 || !int.TryParse(year, out _))
                {
                    return BadRequest("Year must be a 4-digit number");
                }

                await _firebaseService.DeleteGameByYearAsync(id, year);
                return Ok(new { message = "Game deleted successfully", id, year });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting game: {ex.Message}");
            }
        }

        // POST: api/games/bulk - Crear múltiples juegos de una vez (OPTIMIZADO)
        [HttpPost("bulk")]
        public async Task<ActionResult> CreateMultipleGames([FromBody] List<GameInfo> games)
        {
            try
            {
                if (games == null || !games.Any())
                    return BadRequest("Games list cannot be empty");

                var createdGameIds = new List<string>();
                var gamesByYear = new Dictionary<string, int>();

                foreach (var game in games)
                {
                    if (!string.IsNullOrEmpty(game.Title))
                    {
                        var gameId = await _firebaseService.AddGameAsync(game);
                        createdGameIds.Add(gameId);

                        // Contar por año para estadísticas
                        var year = ExtractYearFromBundleDate(game.BundleDate);
                        gamesByYear[year] = gamesByYear.ContainsKey(year) ? gamesByYear[year] + 1 : 1;
                    }
                }

                return Ok(new
                {
                    message = $"{createdGameIds.Count} games created successfully",
                    totalCreated = createdGameIds.Count,
                    gameIds = createdGameIds,
                    distributionByYear = gamesByYear
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating games: {ex.Message}");
            }
        }

        // POST: api/games/migrate - Migrar estructura anterior
        [HttpPost("migrate")]
        public async Task<ActionResult> MigrateToYearStructure()
        {
            try
            {
                var result = await _firebaseService.MigrateToYearStructureAsync();
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Migration error: {ex.Message}");
            }
        }

        // ===========================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ===========================================

        private bool IsValidMonthYearFormat(string monthYear)
        {
            if (string.IsNullOrEmpty(monthYear))
                return false;

            var parts = monthYear.ToLower().Split('-');
            if (parts.Length != 2)
                return false;

            var monthName = parts[0].Trim();
            var year = parts[1].Trim();

            // Validar año de 4 dígitos
            if (year.Length != 4 || !int.TryParse(year, out _))
                return false;

            // Validar que el mes existe
            var validMonths = new[]
            {
                "january", "february", "march", "april", "may", "june",
                "july", "august", "september", "october", "november", "december",
                "jan", "feb", "mar", "apr", "jun", "jul", "aug", "sep", "oct", "nov", "dec"
            };

            return validMonths.Contains(monthName);
        }

        private string ExtractYearFromBundleDate(string bundleDate)
        {
            if (string.IsNullOrEmpty(bundleDate))
                return "unknown";

            var match = System.Text.RegularExpressions.Regex.Match(bundleDate, @"\b(\d{4})\b");
            return match.Success ? match.Groups[1].Value : "unknown";
        }
    }
}