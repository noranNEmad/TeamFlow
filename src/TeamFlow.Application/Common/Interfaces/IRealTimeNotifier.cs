using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface IRealTimeNotifier
{
    Task NotifyTaskCreatedAsync(Guid projectId, TaskDto task, CancellationToken cancellationToken = default);
    Task NotifyTaskStatusUpdatedAsync(Guid projectId, Guid taskId, TaskItemStatus newStatus, CancellationToken cancellationToken = default);
    Task NotifyCommentAddedAsync(Guid projectId, Guid taskId, CommentDto comment, CancellationToken cancellationToken = default);
}

