using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HumbleChoiceScrapper.Responses
{
    public class GameResponse<T>
    {
        public string Message { get; set; }
        public IEnumerable<T> Data { get; set; }

        public GameResponse()
        {
            Message = string.Empty;
            Data = null;
        }

        public GameResponse(string message, IEnumerable<T> data)
        {
            Message = message;
            Data = data;
        }
    }
}
