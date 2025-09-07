using HumbleChoiceScrapper.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Register the HumbleScraperService with HttpClient
builder.Services.AddHttpClient<HumbleScraperService>();
builder.Services.AddMemoryCache();

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
            .WithOrigins("http://localhost:5173", "http://localhost:3000", "https://tu-frontend.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
    // SIN credentials si no usás cookies
    );
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
