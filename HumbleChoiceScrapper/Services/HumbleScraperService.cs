using HtmlAgilityPack;
using HumbleChoiceScrapper.Helpers;
using HumbleChoiceScrapper.Helpers.Interfaces;
using HumbleChoiceScrapper.Models;
using HumbleChoiceScrapper.Responses;
using HumbleChoiceScrapper.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HumbleChoiceScrapper.Services
{
    public class HumbleScraperService : IHumbleScrapperService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheHelper _cacheHelper;
        private readonly IFirebaseService _firebaseService;

        private static readonly SemaphoreSlim _gate = new(1, 1);
        private static DateTime _last = DateTime.MinValue;
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(10);

        private const string humble_url = "https://www.humblebundle.com/membership/{0}";

        public HumbleScraperService(HttpClient httpClient, ICacheHelper cacheHelper, IFirebaseService firebaseService)
        {
            _httpClient = httpClient;
            _cacheHelper = cacheHelper;
            _firebaseService = firebaseService;
        }

        public async Task<GameResponse<GameInfo>> ScrapeHumbleChoiceAsync(string month, bool shortFormat)
        {
            string message = string.Empty;
            GameResponse<GameInfo> gameResponse = new GameResponse<GameInfo>();

            string cacheKey = $"humble_{month}";
            // ---- try to get from cache ----           
            if (_cacheHelper.TryGet(cacheKey, out gameResponse))
            {
                return gameResponse;
            }

            // ---- try to get from firebase ----
            IEnumerable<GameInfo> savedGames = _firebaseService.GetGamesByMonthYearAsync(month).Result;
            if (savedGames != null && savedGames.Count() > 0) { return new GameResponse<GameInfo>(message, savedGames); } 

            // Cooldown
            await _gate.WaitAsync();
            try
            {
                var elapsed = DateTime.UtcNow - _last;
                if (elapsed < Cooldown) await Task.Delay(Cooldown - elapsed);
                _last = DateTime.UtcNow;
            }
            finally { _gate.Release(); }

            var games = await GetMonthGames(month, shortFormat);

            gameResponse = new GameResponse<GameInfo>(message, games);

            // ---- store to firebase ----
            foreach (var game in games)
            {
                await _firebaseService.AddGameAsync(game);
            }

            // ---- cache to avoid traffic ----
            _cacheHelper.Set(cacheKey, gameResponse);

            return gameResponse;
        }

        public async Task<GameResponse<GameInfo>> GetGameCollection(string startDate, string endDate, bool shortFormat)
        {
            GameResponse<GameInfo> gameResponse = new GameResponse<GameInfo>();
            string message = string.Empty;

            List<GameInfo> games = new List<GameInfo>();
            var result = DateHelper.GetDatesBetween(startDate, endDate);

            foreach (var date in result)
            {
                List<GameInfo> data = ScrapeHumbleChoiceAsync(date, shortFormat).Result.Data.ToList();
                games.AddRange(data);
            }

            gameResponse = new GameResponse<GameInfo>(message, games);

            return gameResponse;
        }


        private async Task<List<GameInfo>> GetMonthGames(string month, bool shortFormat)
        {
            var url = string.Format(humble_url, month);
            var response = await _httpClient.GetStringAsync(url);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);

            string page = response.ToString();

            //var jsonObj = JObject.Parse(page);

            var games = new List<GameInfo>();

            //// Adjust selectors according to the HTML structure of Humble Bundle's website
            var gameNodes = htmlDoc.DocumentNode.SelectNodes("//script[contains(@id, 'webpack-monthly-product-data')]");

            var jsonResponse = JObject.Parse(gameNodes[0].InnerText);

            games = FindTpkdsObjects(jsonResponse, shortFormat);

            foreach (var game in games)
            {
                game.BundleDate = month;
            }

            return games;
        }

        private static List<GameInfo> FindTpkdsObjects(JToken jsonToken, bool shortFormat)
        {
            try
            {
                var gameTokens = jsonToken.SelectTokens("$.contentChoiceOptions.contentChoiceData.game_data.*");

                if (shortFormat)
                {
                    return gameTokens.Select(static game => new GameInfo
                    {
                        Title = game["title"]?.ToString(),
                        Image = game["image"]?.ToString()
                    }
                    ).ToList();
                }
                else
                {
                    return gameTokens.Select(static game => new GameInfo
                    {
                        Title = game["title"]?.ToString(),
                        Description = game["description"]?.ToString(),
                        Image = game["image"]?.ToString(),  // Mapeo de la imagen
                        Price = game["msrp|money"]?["amount"]?.ToObject<decimal>() ?? 0,
                        Platforms = game["platforms"]?.ToObject<List<string>>() ?? new List<string>(),
                        Genres = game["genres"]?.ToObject<List<string>>() ?? new List<string>(),
                        Developer = game["developers"]?.FirstOrDefault()?.ToString(),
                        UserRating = new UserRating
                        {
                            SteamPercent = game["user_rating"]?["steam_percent|decimal"]?.ToObject<decimal>() ?? 0,
                            ReviewText = game["user_rating"]?["review_text"]?.ToString(),
                            SteamCount = game["user_rating"]?["steam_count"]?.ToObject<int>() ?? 0
                        }
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                return new List<GameInfo>();
            }
        }
    }
}
