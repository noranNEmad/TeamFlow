using TeamFlow.Domain.Common;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class TaskItem : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public Guid CreatorId { get; set; }
    public User Creator { get; set; } = null!;

    public DateTimeOffset? DueDate { get; set; }
}
