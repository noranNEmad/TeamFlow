using TeamFlow.Domain.Common;

namespace TeamFlow.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // e.g. "acme-corp" for unique subdomain/routing
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

