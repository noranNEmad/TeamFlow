namespace TeamFlow.Application.Common.Interfaces;

public interface IEventBus
{
    Task PublishAsync(string eventType, string content, CancellationToken cancellationToken = default);
}
