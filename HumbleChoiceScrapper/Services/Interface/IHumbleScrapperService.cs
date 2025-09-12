using HumbleChoiceScrapper.Models;
using HumbleChoiceScrapper.Responses;

namespace HumbleChoiceScrapper.Services.Interface
{
    public interface IHumbleScrapperService
    {
        Task<GameResponse<GameInfo>> ScrapeHumbleChoiceAsync(string month, bool shortFormat);
        Task<GameResponse<GameInfo>> GetGameCollection(string startDate, string endDate, bool shortFormat);
    }
}
