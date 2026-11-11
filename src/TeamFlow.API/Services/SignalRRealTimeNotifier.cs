using Microsoft.AspNetCore.SignalR;
using TeamFlow.API.Hubs;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Enums;

namespace TeamFlow.API.Services;

public class SignalRRealTimeNotifier : IRealTimeNotifier
{
    private readonly IHubContext<TeamFlowHub> _hubContext;

    public SignalRRealTimeNotifier(IHubContext<TeamFlowHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyTaskCreatedAsync(Guid projectId, TaskDto task, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"project_{projectId}")
            .SendAsync("TaskCreated", task, cancellationToken);
    }

    public async Task NotifyTaskStatusUpdatedAsync(Guid projectId, Guid taskId, TaskItemStatus newStatus, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"project_{projectId}")
            .SendAsync("TaskStatusUpdated", new { taskId, status = (int)newStatus }, cancellationToken);
    }

    public async Task NotifyCommentAddedAsync(Guid projectId, Guid taskId, CommentDto comment, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"project_{projectId}")
            .SendAsync("CommentAdded", new { taskId, comment }, cancellationToken);
    }
}

