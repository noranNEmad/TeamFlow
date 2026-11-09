using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class OrganizationMember
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
