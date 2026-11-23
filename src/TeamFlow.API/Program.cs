using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using TeamFlow.API.Hubs;
using TeamFlow.API.Services;
using TeamFlow.Application;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Infrastructure;
using TeamFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with Seq Sink and Console
var seqUrl = builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TeamFlow.API")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(seqUrl)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

// 1. Add Clean Architecture Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Add SignalR Real-Time Notifier
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealTimeNotifier, SignalRRealTimeNotifier>();

// 3. Add Controllers
builder.Services.AddControllers();

// 4. Configure CORS for React Frontend (Allow credentials for WebSockets & Cloud deployments)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 5. Configure JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? "TeamFlow_Super_Secret_Key_For_Development_Environment_Must_Be_Long_2026!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "TeamFlow";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "TeamFlowUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Allow SignalR to read token from query string during WebSocket handshake
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/teamflow"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 6. Enterprise Health Checks (Liveness and Readiness Probes)
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck("readiness", () => HealthCheckResult.Healthy("System is ready"), tags: ["ready"]);

var app = builder.Build();

// Auto-migrate Database in Development
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrations applied successfully.");
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning("Could not auto-migrate database on startup. Ensure PostgreSQL container is running: {Message}", ex.Message);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    Application = "TeamFlow API",
    Version = "2.0-SaaS",
    Status = "Healthy",
    Realtime = "SignalR Enabled",
    Timestamp = DateTimeOffset.UtcNow
}));

app.MapControllers();
app.MapHub<TeamFlowHub>("/hubs/teamflow");

// Map Health Checks for Kubernetes / Container Orchestrators
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

try
{
    Log.Information("Starting TeamFlow API with Serilog and Seq logging at {SeqUrl}...", seqUrl);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TeamFlow API terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}
