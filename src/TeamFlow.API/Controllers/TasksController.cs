using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Application.Features.Tasks.Services;

namespace TeamFlow.API.Controllers;

[Authorize]
public class TasksController : BaseApiController
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateTaskAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetTaskByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TaskDto>> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateTaskStatusAsync(id, request.Status, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskDto>> UpdateDetails(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateTaskDetailsAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.DeleteTaskAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }
        return BadRequest(new { error = result.Error.Code, message = result.Error.Description });
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(Guid id, [FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await _taskService.AddCommentAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetComments(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetCommentsAsync(id, cancellationToken);
        return HandleResult(result);
    }
}
