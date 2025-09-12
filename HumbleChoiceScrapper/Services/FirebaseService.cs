using Firebase.Database;
using Firebase.Database.Query;
using HumbleChoiceScrapper.Models;
using Newtonsoft.Json;


// Interfaz del servicio Firebase
public interface IFirebaseService
{
    Task<T> GetAsync<T>(string path);
    Task<string> PostAsync<T>(string path, T data);
    Task PutAsync<T>(string path, T data);
    Task DeleteAsync(string path);
    Task<List<GameInfo>> GetAllGamesAsync();
    Task<string> AddGameAsync(GameInfo game);
    Task UpdateGameAsync(string gameId, GameInfo game);
    Task DeleteGameAsync(string gameId);
}

// Implementación del servicio
public class FirebaseService : IFirebaseService
{
    private readonly FirebaseClient _firebaseClient;

    public FirebaseService(FirebaseClient firebaseClient)
    {
        _firebaseClient = firebaseClient;
    }

    // Métodos genéricos
    public async Task<T> GetAsync<T>(string path)
    {
        return await _firebaseClient
            .Child(path)
            .OnceSingleAsync<T>();
    }

    public async Task<string> PostAsync<T>(string path, T data)
    {
        var json = JsonConvert.SerializeObject(data);
        var result = await _firebaseClient
            .Child(path)
            .PostAsync(json);
        return result.Key;
    }

    public async Task PutAsync<T>(string path, T data)
    {
        await _firebaseClient
            .Child(path)
            .PutAsync(data);
    }

    public async Task DeleteAsync(string path)
    {
        await _firebaseClient
            .Child(path)
            .DeleteAsync();
    }

    // Métodos específicos para GameInfo
    public async Task<List<GameInfo>> GetAllGamesAsync()
    {
        try
        {
            // Firebase devuelve un Dictionary<string, GameInfo> donde la key es el ID
            var gamesDict = await _firebaseClient
                .Child("games")
                .OnceSingleAsync<Dictionary<string, GameInfo>>();

            if (gamesDict == null || !gamesDict.Any())
                return new List<GameInfo>();

            // Convertir el Dictionary a List
            return gamesDict.Values.ToList();
        }
        catch (Exception)
        {
            return new List<GameInfo>();
        }
    }

    public async Task<string> AddGameAsync(GameInfo game)
    {
        var json = JsonConvert.SerializeObject(game);
        var result = await _firebaseClient
            .Child("games")
            .PostAsync(json);
        return result.Key;
    }

    public async Task UpdateGameAsync(string gameId, GameInfo game)
    {
        await _firebaseClient
            .Child("games")
            .Child(gameId)
            .PutAsync(game);
    }

    public async Task DeleteGameAsync(string gameId)
    {
        await _firebaseClient
            .Child("games")
            .Child(gameId)
            .DeleteAsync();
    }
}