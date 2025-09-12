namespace HumbleChoiceScrapper.Models
{
    public class GameInfo
    {
        public string Title { get; set; }
        public string Image { get; set; }
        public string BundleDate { get; set; }
        //Short game.
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public List<string>? Platforms { get; set; }
        public List<string>? Genres { get; set; }
        public string? Developer { get; set; }
        public UserRating? UserRating { get; set; }
    }

    public class UserRating
    {
        public decimal SteamPercent { get; set; }
        public string ReviewText { get; set; }
        public int SteamCount { get; set; }
    }
}
