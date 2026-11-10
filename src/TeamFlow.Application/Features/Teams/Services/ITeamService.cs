using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Teams.DTOs;

namespace TeamFlow.Application.Features.Teams.Services;

public interface ITeamService
{
    Task<Result<TeamDto>> CreateTeamAsync(CreateTeamRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<TeamDto>>> GetTeamsAsync(CancellationToken cancellationToken = default);
    Task<Result> AddMemberToTeamAsync(Guid teamId, AddTeamMemberRequest request, CancellationToken cancellationToken = default);
}
