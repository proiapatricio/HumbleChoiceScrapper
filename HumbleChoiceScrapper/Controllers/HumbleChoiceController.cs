
using HumbleChoiceScrapper.Responses;
using HumbleChoiceScrapper.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        [HttpGet]
        public async Task<ActionResult<GameResponse<GameInfo>>> Get(string month = "july-2024", bool showFullResponse = true)
        {
            GameResponse<GameInfo> games = await _humbleScraperService.ScrapeHumbleChoiceAsync(month); //july-2024

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
    }
}
