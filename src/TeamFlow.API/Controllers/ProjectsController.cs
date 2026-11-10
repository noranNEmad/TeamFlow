using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Application.Features.Projects.Services;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Application.Features.Tasks.Services;
using TeamFlow.Domain.Enums;

namespace TeamFlow.API.Controllers;

[Authorize]
public class ProjectsController : BaseApiController
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;

    public ProjectsController(IProjectService projectService, ITaskService taskService)
    {
        _projectService = projectService;
        _taskService = taskService;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await _projectService.CreateProjectAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _projectService.GetProjectsAsync(cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _projectService.GetProjectByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/tasks")]
    public async Task<ActionResult<List<TaskDto>>> GetTasks(
        Guid id,
        [FromQuery] TaskItemStatus? status,
        [FromQuery] TaskPriority? priority,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetTasksByProjectIdAsync(id, status, priority, cancellationToken);
        return HandleResult(result);
    }
}
