using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Tasks.Services;

public interface ITaskService
{
    Task<Result<TaskDto>> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<TaskDto>>> GetTasksByProjectIdAsync(Guid projectId, TaskItemStatus? status = null, TaskPriority? priority = null, CancellationToken cancellationToken = default);
    Task<Result<TaskDto>> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<Result<TaskDto>> UpdateTaskStatusAsync(Guid taskId, TaskItemStatus newStatus, CancellationToken cancellationToken = default);
    Task<Result<TaskDto>> UpdateTaskDetailsAsync(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<Result<CommentDto>> AddCommentAsync(Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<CommentDto>>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken = default);
}

