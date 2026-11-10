using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Teams.DTOs;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Teams.Services;

public class TeamService : ITeamService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TeamService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TeamDto>> CreateTeamAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<TeamDto>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<TeamDto>.Failure(Error.Validation("Team.InvalidName", "Team name is required."));
        }

        var team = new Team
        {
            OrganizationId = orgId.Value,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<TeamDto>.Success(new TeamDto(
            team.Id,
            team.Name,
            team.Description,
            0,
            0
        ));
    }

    public async Task<Result<List<TeamDto>>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<List<TeamDto>>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        var teams = await _context.Teams
            .Where(t => t.OrganizationId == orgId.Value)
            .Select(t => new TeamDto(
                t.Id,
                t.Name,
                t.Description,
                t.Members.Count,
                t.Projects.Count
            ))
            .ToListAsync(cancellationToken);

        return Result<List<TeamDto>>.Success(teams);
    }

    public async Task<Result> AddMemberToTeamAsync(Guid teamId, AddTeamMemberRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context missing."));
        }

        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.OrganizationId == orgId.Value, cancellationToken);

        if (team is null)
        {
            return Result.Failure(Error.NotFound("Team.NotFound", "Team was not found."));
        }

        var isOrgMember = await _context.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == orgId.Value && om.UserId == request.UserId, cancellationToken);

        if (!isOrgMember)
        {
            return Result.Failure(Error.NotFound("User.NotMember", "User is not a member of this organization."));
        }

        var alreadyInTeam = await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == request.UserId, cancellationToken);

        if (alreadyInTeam)
        {
            return Result.Failure(Error.Conflict("Team.AlreadyMember", "User is already in this team."));
        }

        var member = new TeamMember
        {
            TeamId = teamId,
            UserId = request.UserId,
            Role = request.Role
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
