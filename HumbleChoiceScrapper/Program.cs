using HumbleChoiceScrapper.Services;
using Google.Cloud.Firestore;
using Firebase.Database;
using HumbleChoiceScrapper.Services.Interface;

var builder = WebApplication.CreateBuilder(args);


// Configurar Firebase
string projectId = builder.Configuration["Firebase:ProjectId"];
builder.Services.AddSingleton<FirestoreDb>(provider =>
{
    // Para producción en Render, usa variables de entorno
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
        "/app/firebase-credentials.json");

    return FirestoreDb.Create(projectId);
});


builder.Services.AddSingleton<FirebaseClient>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();

    // Primero intenta la variable de entorno, luego el appsettings
    var firebaseUrl = Environment.GetEnvironmentVariable("FIREBASE_DATABASE_URL")
                     ?? config["Firebase:DatabaseUrl"];

    if (string.IsNullOrEmpty(firebaseUrl))
        throw new InvalidOperationException("Firebase Database URL not configured");

    return new FirebaseClient(firebaseUrl);
});


// Add services to the container.
// Register the HumbleScraperService with HttpClient
builder.Services.AddHttpClient<IHumbleScrapperService, HumbleScraperService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IFirebaseService, FirebaseService>();




builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ?? CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
        policy
            // En dev poné tus puertos reales de front (Vite/CRA/Next)
            .WithOrigins("http://localhost:5173",
            "http://localhost:3000", 
            "https://localhost:44376",
            "https://html-classic.itch.zone",
            "https://pproia.itch.io/humblebundlemonthlysorter")
            .AllowAnyHeader()
            .AllowAnyMethod()
    // SIN credentials si no usás cookies
    );
});


var app = builder.Build();
// Configuración dinámica de URLs
var port = Environment.GetEnvironmentVariable("PORT") ?? "5287";
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? $"http://0.0.0.0:{port}";

app.Urls.Add(urls);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    // CORS completamente abierto para desarrollo
    app.UseCors(policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
}
else
{
    app.UseCors("AllowWebApp");
}
app.UseAuthorization();
app.MapControllers();
app.Run();
