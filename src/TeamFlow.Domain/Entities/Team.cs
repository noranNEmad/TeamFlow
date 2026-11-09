using TeamFlow.Domain.Common;

namespace TeamFlow.Domain.Entities;

public class Team : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

