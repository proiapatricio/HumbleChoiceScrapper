using HtmlAgilityPack;
using HumbleChoiceScrapper.Responses;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HumbleChoiceScrapper.Services
{
    public class HumbleScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private static readonly SemaphoreSlim _gate = new(1, 1);
        private static DateTime _last = DateTime.MinValue;
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(10);

        private const string humble_url = "https://www.humblebundle.com/membership/{0}";

        public HumbleScraperService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<GameResponse<GameInfo>> ScrapeHumbleChoiceAsync(string month)
        {
            string cacheKey = $"humble_{month}";
            if (_cache.TryGetValue(cacheKey, out GameResponse<GameInfo> cacheResponse))
            {
                return cacheResponse;
            }

            // Cooldown
            await _gate.WaitAsync();
            try
            {
                var elapsed = DateTime.UtcNow - _last;
                if (elapsed < Cooldown) await Task.Delay(Cooldown - elapsed);
                _last = DateTime.UtcNow;
            }
            finally { _gate.Release(); }

            GameResponse<GameInfo> gameResponse = new GameResponse<GameInfo>();

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

            string message = string.Empty;

            games = FindTpkdsObjects(jsonResponse);            

            gameResponse = new GameResponse<GameInfo>(message, games);


            // ---- cache to avoid traffic ----
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            _cache.Set(cacheKey, gameResponse, cacheEntryOptions);


            return gameResponse;
        }




        static List<GameInfo> FindTpkdsObjects(JToken jsonToken)
        {            
            List<GameInfo> response = new List<GameInfo>();

            foreach (JToken title in jsonToken.SelectTokens("$..title"))
            {
                response.Add(new GameInfo() { Title = title.ToString() });
            }

            return response;
        }
    }

       

    public class GameInfo
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
