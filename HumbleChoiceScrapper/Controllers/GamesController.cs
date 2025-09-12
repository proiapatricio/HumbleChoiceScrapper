using HumbleChoiceScrapper.Models;
using HumbleChoiceScrapper.Services;
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

        // GET: api/games - Obtener todos los juegos
        [HttpGet]
        public async Task<ActionResult<List<GameInfo>>> GetAllGames()
        {
            try
            {
                var games = await _firebaseService.GetAllGamesAsync();
                return Ok(games);
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
                var game = await _firebaseService.GetAsync<GameInfo>($"games/{id}");

                if (game == null || string.IsNullOrEmpty(game.Title))
                    return NotFound($"Game with ID {id} not found");

                return Ok(game);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting game: {ex.Message}");
            }
        }

        // POST: api/games - Crear un nuevo juego
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

        // POST: api/games/bulk - Crear múltiples juegos de una vez
        [HttpPost("bulk")]
        public async Task<ActionResult> CreateMultipleGames([FromBody] List<GameInfo> games)
        {
            try
            {
                if (games == null || !games.Any())
                    return BadRequest("Games list cannot be empty");

                var createdGameIds = new List<string>();

                foreach (var game in games)
                {
                    if (!string.IsNullOrEmpty(game.Title))
                    {
                        var gameId = await _firebaseService.AddGameAsync(game);
                        createdGameIds.Add(gameId);
                    }
                }

                return Ok(new
                {
                    message = $"{createdGameIds.Count} games created successfully",
                    gameIds = createdGameIds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating games: {ex.Message}");
            }
        }
    }
}
