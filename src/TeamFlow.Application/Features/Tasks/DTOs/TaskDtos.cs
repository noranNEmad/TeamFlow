using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Tasks.DTOs;

public record CreateTaskRequest(
    Guid ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueDate
);

public record UpdateTaskStatusRequest(
    TaskItemStatus Status
);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTimeOffset? DueDate
);

public record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid CreatorId,
    string CreatorName,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    int CommentsCount
);

public record CreateCommentRequest(
    string Content
);

public record CommentDto(
    Guid Id,
    Guid TaskItemId,
    Guid AuthorId,
    string AuthorName,
    string Content,
    DateTimeOffset CreatedAt
);
