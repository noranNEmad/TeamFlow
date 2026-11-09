using TeamFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register Clean Architecture layers
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    Application = "TeamFlow API",
    Version = "1.0-MVP",
    Environment = app.Environment.EnvironmentName,
    Status = "Healthy"
}));

app.Run();
