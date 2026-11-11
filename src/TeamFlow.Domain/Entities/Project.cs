using TeamFlow.Domain.Common;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class Project : BaseEntity, ITenantScoped
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; // e.g., "TF", "CORE"
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    // Navigation properties
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

