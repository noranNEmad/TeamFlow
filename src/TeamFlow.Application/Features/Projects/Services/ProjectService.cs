using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Projects.Services;

public class ProjectService : IProjectService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService? _cacheService;

    public ProjectService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService? cacheService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<Result<ProjectDto>> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<ProjectDto>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context is missing."));
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Key))
        {
            return Result<ProjectDto>.Failure(Error.Validation("Project.InvalidInput", "Project name and key are required."));
        }

        var normalizedKey = request.Key.Trim().ToUpperInvariant();

        // 1. Enforce Key uniqueness per tenant
        var keyExists = await _context.Projects
            .AnyAsync(p => p.OrganizationId == orgId.Value && p.Key == normalizedKey, cancellationToken);

        if (keyExists)
        {
            return Result<ProjectDto>.Failure(Error.Conflict("Project.KeyExists", $"Project key '{normalizedKey}' is already in use."));
        }

        // 2. Validate Team if assigned
        string? teamName = null;
        if (request.TeamId.HasValue)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == request.TeamId.Value && t.OrganizationId == orgId.Value, cancellationToken);

            if (team is null)
            {
                return Result<ProjectDto>.Failure(Error.NotFound("Team.NotFound", "The specified team was not found."));
            }
            teamName = team.Name;
        }

        var project = new Project
        {
            OrganizationId = orgId.Value,
            TeamId = request.TeamId,
            Name = request.Name.Trim(),
            Key = normalizedKey,
            Description = request.Description?.Trim(),
            Status = ProjectStatus.Active
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        if (_cacheService != null)
        {
            await _cacheService.RemoveAsync($"tenant_{orgId.Value}_projects", cancellationToken);
        }

        return Result<ProjectDto>.Success(new ProjectDto(
            Id: project.Id,
            Name: project.Name,
            Key: project.Key,
            Description: project.Description,
            Status: project.Status,
            OrganizationId: project.OrganizationId,
            TeamId: project.TeamId,
            TeamName: teamName,
            TasksCount: 0,
            CreatedAt: project.CreatedAt
        ));
    }

    public async Task<Result<List<ProjectDto>>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<List<ProjectDto>>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context is missing."));
        }

        var cacheKey = $"tenant_{orgId.Value}_projects";
        if (_cacheService != null)
        {
            var cachedProjects = await _cacheService.GetAsync<List<ProjectDto>>(cacheKey, cancellationToken);
            if (cachedProjects != null)
            {
                return Result<List<ProjectDto>>.Success(cachedProjects);
            }
        }

        var projects = await _context.Projects
            .Where(p => p.OrganizationId == orgId.Value)
            .Include(p => p.Team)
            .Include(p => p.Tasks)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Key,
                p.Description,
                p.Status,
                p.OrganizationId,
                p.TeamId,
                p.Team != null ? p.Team.Name : null,
                p.Tasks.Count,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        if (_cacheService != null)
        {
            await _cacheService.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(10), cancellationToken);
        }

        return Result<List<ProjectDto>>.Success(projects);
    }

    public async Task<Result<ProjectDto>> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var orgId = _currentUserService.OrganizationId;
        if (!orgId.HasValue)
        {
            return Result<ProjectDto>.Failure(Error.Unauthorized("Auth.Unauthorized", "Organization context is missing."));
        }

        var project = await _context.Projects
            .Where(p => p.Id == projectId && p.OrganizationId == orgId.Value)
            .Include(p => p.Team)
            .Include(p => p.Tasks)
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Key,
                p.Description,
                p.Status,
                p.OrganizationId,
                p.TeamId,
                p.Team != null ? p.Team.Name : null,
                p.Tasks.Count,
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return Result<ProjectDto>.Failure(Error.NotFound("Project.NotFound", "Project was not found."));
        }

        return Result<ProjectDto>.Success(project);
    }
}

