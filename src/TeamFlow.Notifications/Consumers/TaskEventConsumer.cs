using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TeamFlow.Notifications.Consumers;

public class TaskEventConsumer : BackgroundService
{
    private const string ExchangeName = "teamflow.events";
    private const string QueueName = "notifications.task-events";
    private readonly ConnectionFactory _factory;
    private readonly ILogger<TaskEventConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public TaskEventConsumer(IConfiguration configuration, ILogger<TaskEventConsumer> logger)
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Event Consumer starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await _factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // 1. Ensure Topic Exchange exists
                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // 2. Declare durable Queue for Notification service
                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                // 3. Bind queue to all task events
                await _channel.QueueBindAsync(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: "event.#",
                    cancellationToken: stoppingToken);

                // 4. Prefetch count = 10 (Fair dispatch)
                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var routingKey = ea.RoutingKey;

                        _logger.LogInformation("📨 [NOTIFICATION SERVICE RECEIVED] RoutingKey: '{RoutingKey}' | Message: {Message}", routingKey, message);

                        // Simulate sending email/push notification
                        using var doc = JsonDocument.Parse(message);
                        var root = doc.RootElement;
                        var title = root.TryGetProperty("Title", out var titleProp) ? titleProp.GetString() : "Task Update";

                        _logger.LogInformation("🔔 [ALERT SENT] Notification processed successfully for '{Title}'", title);

                        // Acknowledge message delivery
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event from RabbitMQ queue.");
                        // Negative acknowledge (requeue = false or dead-letter)
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("✅ Notification Consumer is actively listening on queue '{QueueName}'", QueueName);

                // Keep service alive while connected
                while (!stoppingToken.IsCancellationRequested && _channel.IsOpen)
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("RabbitMQ connection lost in Notification Consumer: {Message}. Reconnecting in 5s...", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}
