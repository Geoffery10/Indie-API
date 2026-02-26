using System.Net.Http.Headers;
using IndieAPI.Api;

var builder = WebApplication.CreateBuilder(args);

// --- SECRETS & HTTP CLIENT CONFIGURATION ---
var haBaseUrl = builder.Configuration["HomeAssistant:BaseUrl"];
var haToken = builder.Configuration["HomeAssistant:Token"];

// Register the HttpClient for our Home Assistant Service
builder.Services.AddHttpClient<IHomeAssistantService, HomeAssistantService>(client =>
{
    if (!string.IsNullOrEmpty(haBaseUrl))
    {
        client.BaseAddress = new Uri(haBaseUrl);
    }
    if (!string.IsNullOrEmpty(haToken))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);
    }
});

// Setup CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowIndieFrontend", policy =>
    {
        policy.WithOrigins("https://indie.geoffery10.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowIndieFrontend");

// --- ENDPOINTS ---
app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Version = "1.0", Service = "IndieAPI" }));

// New Daily Verse Endpoint!
app.MapGet("/api/bible-daily-verse", async (IHomeAssistantService haService) => 
{
    var verse = await haService.GetDailyVerseAsync();
    
    // Return an anonymous object so it automatically formats as JSON for your frontend
    return Results.Ok(new { Verse = verse });
});

app.Run();

public partial class Program { }