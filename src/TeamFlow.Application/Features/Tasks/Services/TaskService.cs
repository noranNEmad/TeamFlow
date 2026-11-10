using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Tasks.Services;

public class TaskService : ITaskService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TaskService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TaskDto>> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        var userId = _currentUserService.UserId;

        if (!orgId.HasValue || !userId.HasValue)
        {
            return Result<TaskDto>.Failure(Error.Unauthorized("Auth.Unauthorized", "User or organization context missing."));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<TaskDto>.Failure(Error.Validation("Task.InvalidTitle", "Task title is required."));
        }

        // 1. Verify Project belongs to current tenant organization
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.OrganizationId == orgId.Value, cancellationToken);

        if (project is null)
        {
            return Result<TaskDto>.Failure(Error.NotFound("Project.NotFound", "The target project was not found in your organization."));
        }

        // 2. Validate Assignee if provided
        string? assigneeName = null;
        if (request.AssigneeId.HasValue)
        {
            var isMember = await _context.OrganizationMembers
                .Include(om => om.User)
                .FirstOrDefaultAsync(om => om.OrganizationId == orgId.Value && om.UserId == request.AssigneeId.Value, cancellationToken);

            if (isMember is null)
            {
                return Result<TaskDto>.Failure(Error.NotFound("User.NotMember", "Assigned user is not a member of this organization."));
            }
            assigneeName = isMember.User.FullName;
        }

        var creator = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
        var creatorName = creator?.FullName ?? "Unknown";

        var task = new TaskItem
        {
            ProjectId = request.ProjectId,
            CreatorId = userId.Value,
            AssigneeId = request.AssigneeId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = request.Status,
            Priority = request.Priority,
            DueDate = request.DueDate
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<TaskDto>.Success(new TaskDto(
            Id: task.Id,
            ProjectId: task.ProjectId,
            Title: task.Title,
            Description: task.Description,
            Status: task.Status,
            Priority: task.Priority,
            AssigneeId: task.AssigneeId,
            AssigneeName: assigneeName,
            CreatorId: task.CreatorId,
            CreatorName: creatorName,
            DueDate: task.DueDate,
            CreatedAt: task.CreatedAt,
            CommentsCount: 0
        ));
    }

    public async Task<Result<List<TaskDto>>> GetTasksByProjectIdAsync(
        Guid projectId,
        TaskItemStatus? status = null,
        TaskPriority? priority = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<List<TaskDto>>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        // 1. Verify project exists and belongs to current tenant
        var projectExists = await _context.Projects
            .AnyAsync(p => p.Id == projectId && p.OrganizationId == orgId.Value, cancellationToken);

        if (!projectExists)
        {
            return Result<List<TaskDto>>.Failure(Error.NotFound("Project.NotFound", "Project was not found."));
        }

        // 2. Query tasks with optional filters
        var query = _context.Tasks
            .Where(t => t.ProjectId == projectId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        var tasks = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new TaskDto(
                t.Id,
                t.ProjectId,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.AssigneeId,
                t.Assignee != null ? t.Assignee.FullName : null,
                t.CreatorId,
                t.Creator.FullName,
                t.DueDate,
                t.CreatedAt,
                t.Comments.Count
            ))
            .ToListAsync(cancellationToken);

        return Result<List<TaskDto>>.Success(tasks);
    }

    public async Task<Result<TaskDto>> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<TaskDto>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.Project.OrganizationId == orgId.Value, cancellationToken);

        if (task is null)
        {
            return Result<TaskDto>.Failure(Error.NotFound("Task.NotFound", "Task was not found."));
        }

        return Result<TaskDto>.Success(new TaskDto(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssigneeId,
            task.Assignee?.FullName,
            task.CreatorId,
            task.Creator.FullName,
            task.DueDate,
            task.CreatedAt,
            task.Comments.Count
        ));
    }

    public async Task<Result<TaskDto>> UpdateTaskStatusAsync(Guid taskId, TaskItemStatus newStatus, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<TaskDto>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.Project.OrganizationId == orgId.Value, cancellationToken);

        if (task is null)
        {
            return Result<TaskDto>.Failure(Error.NotFound("Task.NotFound", "Task was not found."));
        }

        task.Status = newStatus;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<TaskDto>.Success(new TaskDto(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssigneeId,
            task.Assignee?.FullName,
            task.CreatorId,
            task.Creator.FullName,
            task.DueDate,
            task.CreatedAt,
            task.Comments.Count
        ));
    }

    public async Task<Result<TaskDto>> UpdateTaskDetailsAsync(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<TaskDto>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.Project.OrganizationId == orgId.Value, cancellationToken);

        if (task is null)
        {
            return Result<TaskDto>.Failure(Error.NotFound("Task.NotFound", "Task was not found."));
        }

        // Validate new assignee if changed
        if (request.AssigneeId.HasValue && request.AssigneeId != task.AssigneeId)
        {
            var isMember = await _context.OrganizationMembers
                .AnyAsync(om => om.OrganizationId == orgId.Value && om.UserId == request.AssigneeId.Value, cancellationToken);

            if (!isMember)
            {
                return Result<TaskDto>.Failure(Error.NotFound("User.NotMember", "Assigned user is not a member of this organization."));
            }
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.AssigneeId = request.AssigneeId;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetTaskByIdAsync(task.Id, cancellationToken);
    }

    public async Task<Result> DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        var task = await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.Project.OrganizationId == orgId.Value, cancellationToken);

        if (task is null)
        {
            return Result.Failure(Error.NotFound("Task.NotFound", "Task was not found."));
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CommentDto>> AddCommentAsync(Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        var userId = _currentUserService.UserId;

        if (!orgId.HasValue || !userId.HasValue)
        {
            return Result<CommentDto>.Failure(Error.Unauthorized("Auth.Unauthorized", "User context missing."));
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Result<CommentDto>.Failure(Error.Validation("Comment.Empty", "Comment content cannot be empty."));
        }

        var task = await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.Project.OrganizationId == orgId.Value, cancellationToken);

        if (task is null)
        {
            return Result<CommentDto>.Failure(Error.NotFound("Task.NotFound", "Task was not found."));
        }

        var author = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);

        var comment = new Comment
        {
            TaskItemId = taskId,
            AuthorId = userId.Value,
            Content = request.Content.Trim()
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CommentDto>.Success(new CommentDto(
            comment.Id,
            comment.TaskItemId,
            comment.AuthorId,
            author?.FullName ?? "Unknown",
            comment.Content,
            comment.CreatedAt
        ));
    }

    public async Task<Result<List<CommentDto>>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<List<CommentDto>>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        var taskExists = await _context.Tasks
            .Include(t => t.Project)
            .AnyAsync(t => t.Id == taskId && t.Project.OrganizationId == orgId.Value, cancellationToken);

        if (!taskExists)
        {
            return Result<List<CommentDto>>.Failure(Error.NotFound("Task.NotFound", "Task was not found."));
        }

        var comments = await _context.Comments
            .Where(c => c.TaskItemId == taskId)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id,
                c.TaskItemId,
                c.AuthorId,
                c.Author.FullName,
                c.Content,
                c.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<List<CommentDto>>.Success(comments);
    }
}

