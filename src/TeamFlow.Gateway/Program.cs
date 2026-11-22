using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure YARP Reverse Proxy from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Configure CORS for React Client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 3. Centralized Rate Limiting Policy
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("GlobalLimiter", opt =>
    {
        opt.PermitLimit = 200;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok(new
{
    Gateway = "TeamFlow YARP API Gateway",
    Status = "Healthy",
    Version = "3.0-Distributed",
    Timestamp = DateTimeOffset.UtcNow
}));

// Route requests through YARP Reverse Proxy
app.MapReverseProxy();

app.Run("http://localhost:5000");
