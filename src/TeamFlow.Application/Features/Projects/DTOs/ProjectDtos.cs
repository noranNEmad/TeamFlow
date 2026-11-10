using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Projects.DTOs;

public record CreateProjectRequest(
    string Name,
    string Key,
    string? Description,
    Guid? TeamId
);

public record ProjectDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    ProjectStatus Status,
    Guid OrganizationId,
    Guid? TeamId,
    string? TeamName,
    int TasksCount,
    DateTimeOffset CreatedAt
);

