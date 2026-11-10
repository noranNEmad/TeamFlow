using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Teams.DTOs;

public record CreateTeamRequest(
    string Name,
    string? Description
);

public record TeamDto(
    Guid Id,
    string Name,
    string? Description,
    int MembersCount,
    int ProjectsCount
);

public record AddTeamMemberRequest(
    Guid UserId,
    TeamRole Role
);

