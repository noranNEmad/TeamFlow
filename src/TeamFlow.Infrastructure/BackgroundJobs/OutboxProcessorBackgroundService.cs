using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.BackgroundJobs;

public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    public OutboxProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Background Service is starting.");

        using var timer = new PeriodicTimer(_pollInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during Outbox processing cycle.");
            }
        }

        _logger.LogInformation("Outbox Processor Background Service has stopped.");
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventBus = scope.ServiceProvider.GetService<IEventBus>();

        if (eventBus is null)
        {
            return;
        }

        var pendingMessages = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending Outbox messages.", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            try
            {
                await eventBus.PublishAsync(message.Type, message.Content, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to publish Outbox message {Id} ({Type}) to RabbitMQ: {ErrorMessage}", message.Id, message.Type, ex.Message);
                message.Error = ex.Message;
                // Leave ProcessedOnUtc as null so it can be retried on next poll cycle
                break; // Break batch to maintain ordering if broker is unreachable
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
