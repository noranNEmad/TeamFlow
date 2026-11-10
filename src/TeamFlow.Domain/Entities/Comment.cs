using TeamFlow.Domain.Common;

namespace TeamFlow.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
}

