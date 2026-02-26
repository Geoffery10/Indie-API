var builder = WebApplication.CreateBuilder(args);

// 1. Configure CORS to securely allow requests from your specific domain
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

// 2. Middleware pipeline
app.UseHttpsRedirection();
app.UseCors("AllowIndieFrontend");

// 3. Define Endpoints (We are writing the endpoint to pass our future test)
app.MapGet("/api/health", () => 
{
    return Results.Ok(new { Status = "Healthy", Version = "1.0", Service = "IndieAPI" });
});

app.Run();

// 4. Expose the Program class to the Test Project for WebApplicationFactory
public partial class Program { }