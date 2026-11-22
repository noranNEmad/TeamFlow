using TeamFlow.Domain.Common;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Events;

public record TaskCreatedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid OrganizationId,
    string Title,
    string CreatorName,
    DateTime OccurredOnUtc
) : IDomainEvent;

public record TaskStatusUpdatedEvent(
    Guid TaskId,
    Guid ProjectId,
    Guid OrganizationId,
    TaskItemStatus OldStatus,
    TaskItemStatus NewStatus,
    DateTime OccurredOnUtc
) : IDomainEvent;
