using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Infrastructure.Messaging;

public class RabbitMQEventBus : IEventBus, IAsyncDisposable
{
    private const string ExchangeName = "teamflow.events";
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RabbitMQEventBus(IConfiguration configuration, ILogger<RabbitMQEventBus> logger)
    {
        _logger = logger;
        var hostName = configuration.GetConnectionString("RabbitMQ")
            ?? configuration["RabbitMQ:Host"]
            ?? "localhost";

        _factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = configuration["RabbitMQ:User"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672
        };
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true }) return;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true }) return;

            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Connected to RabbitMQ on {HostName} with exchange '{ExchangeName}'", _factory.HostName, ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not connect to RabbitMQ broker on {HostName}. Messages will queue in Outbox until broker is available.", _factory.HostName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task PublishAsync(string eventType, string content, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (_channel is null)
        {
            throw new InvalidOperationException("RabbitMQ channel is unavailable.");
        }

        var routingKey = $"event.{eventType.ToLowerInvariant()}";
        var body = Encoding.UTF8.GetBytes(content);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Type = eventType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Published event {EventType} with routing key '{RoutingKey}' to RabbitMQ", eventType, routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _semaphore.Dispose();
    }
}
