using System.Net.Http.Headers;
using IndieAPI.Api.Endpoints;
using IndieAPI.Api.Interfaces;
using IndieAPI.Api.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION & SERVICES ---
var haBaseUrl = builder.Configuration["HomeAssistant:BaseUrl"];
var haToken = builder.Configuration["HomeAssistant:Token"];

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// --- Immich Service ---
builder.Services.AddHttpClient<IImmichService, ImmichService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Immich:BaseUrl"] ?? "http://localhost");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["Immich:ApiKey"]);
});

// --- Home Assistant Service ---
builder.Services.AddHttpClient<IHomeAssistantService, HomeAssistantService>(client =>
{
    if (!string.IsNullOrEmpty(haBaseUrl)) client.BaseAddress = new Uri(haBaseUrl);
    if (!string.IsNullOrEmpty(haToken)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", haToken);
});

builder.Services.AddSingleton<IProjectService, ProjectService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowIndieFrontend", policy =>
    {
        policy.WithOrigins(
                "https://indie.geoffery10.com", 
                "http://localhost:5500", 
                "http://127.0.0.1:5500"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- 2. MIDDLEWARE ---
app.UseForwardedHeaders();
app.UseCors("AllowIndieFrontend");

// --- 3. ENDPOINTS ---
app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy" }));
app.MapBibleEndpoints();
app.MapArtEndpoints();
app.MapProjectEndpoints();

app.Run();