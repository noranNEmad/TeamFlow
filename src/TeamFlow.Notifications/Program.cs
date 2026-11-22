using TeamFlow.Notifications.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Register RabbitMQ Event Consumer Background Service
builder.Services.AddHostedService<TaskEventConsumer>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Service = "TeamFlow Notifications Microservice",
    Status = "Healthy",
    Queue = "notifications.task-events",
    Timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run("http://localhost:5050");
