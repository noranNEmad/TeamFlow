namespace TeamFlow.Domain.Common;

public interface ITenantScoped
{
    Guid OrganizationId { get; set; }
}

