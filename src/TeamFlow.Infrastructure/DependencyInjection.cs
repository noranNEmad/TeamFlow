using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Infrastructure.BackgroundJobs;
using TeamFlow.Infrastructure.Caching;
using TeamFlow.Infrastructure.Messaging;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Infrastructure.Security;
using TeamFlow.Infrastructure.Services;

namespace TeamFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=TeamFlowDb;Username=postgres;Password=postgres";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            }));

        // Register IApplicationDbContext mapping to concrete ApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // HTTP Context Accessor
        services.AddHttpContextAccessor();

        // Infrastructure Security & Services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Redis Distributed Caching with fallback configuration
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "TeamFlow:";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // AI Copilot Assistant Service
        services.AddHttpClient<IAiAssistantService, AiAssistantService>();

        // RabbitMQ Event Bus & Outbox Background Processor
        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxProcessorBackgroundService>();

        return services;
    }
}
