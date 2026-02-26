using System.Net.Http.Headers;
using IndieAPI.Api.Endpoints;
using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION & SERVICES ---
var haBaseUrl = builder.Configuration["HomeAssistant:BaseUrl"];
var haToken = builder.Configuration["HomeAssistant:Token"];

builder.Services.AddHttpClient<IHomeAssistantService, HomeAssistantService>(client =>
{
    if (!string.IsNullOrEmpty(haBaseUrl)) client.BaseAddress = new Uri(haBaseUrl);
    if (!string.IsNullOrEmpty(haToken)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowIndieFrontend", policy =>
    {
        policy.WithOrigins("https://indie.geoffery10.com", "http://localhost:5500", "http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- 2. MIDDLEWARE ---
app.UseHttpsRedirection();
app.UseCors("AllowIndieFrontend");

// --- 3. ENDPOINTS ---
app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy" }));

// Register the Bible endpoints cleanly
app.MapBibleEndpoints();

app.Run();