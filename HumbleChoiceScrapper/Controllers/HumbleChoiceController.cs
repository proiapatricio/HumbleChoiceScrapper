
using HumbleChoiceScrapper.Models;
using HumbleChoiceScrapper.Responses;
using HumbleChoiceScrapper.Services;
using Microsoft.AspNetCore.Mvc;

namespace HumbleChoiceScrapper.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HumbleChoiceController : ControllerBase
    {
        private readonly HumbleScraperService _humbleScraperService;

        public HumbleChoiceController(HumbleScraperService humbleScraperService)
        {
            _humbleScraperService = humbleScraperService;
        }

        [HttpGet("GetMothlyGames")]
        public async Task<ActionResult<GameResponse<GameInfo>>> GetMothlyGames(string month = "july-2024", bool showShortFormat = true,bool showFullResponse = true)
        {
            GameResponse<GameInfo> games = await _humbleScraperService.ScrapeHumbleChoiceAsync(month, showShortFormat);

            if (games == null || games.Data.Count() == 0)
            {
                if (showFullResponse)
                {
                    return games;
                }
                else 
                {
                    return NotFound("No games found.");
                }
            }

            return Ok(games);
        }

        [HttpGet("GetAllGamesBetweenDates")]
        public async Task<ActionResult<GameResponse<GameInfo>>> GetAllGamesBetweenDates(string startDate = "july-2024", string endDate = "august-2024", bool showShortFormat = true)
        {
            GameResponse<GameInfo> games = await _humbleScraperService.GetGameCollection(startDate, endDate, showShortFormat);

            return Ok(games);
        }
    }
}
