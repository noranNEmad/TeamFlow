using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Projects.DTOs;

namespace TeamFlow.Application.Features.Projects.Services;

public interface IProjectService
{
    Task<Result<ProjectDto>> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<ProjectDto>>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<Result<ProjectDto>> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}

