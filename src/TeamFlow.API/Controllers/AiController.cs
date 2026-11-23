using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.API.Controllers;

public record GenerateTaskBreakdownRequest(string Title, string? Context = null);
public record SummarizeDiscussionRequest(IReadOnlyList<string> Comments);

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiAssistantService _aiService;

    public AiController(IAiAssistantService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("task-breakdown")]
    public async Task<IActionResult> GenerateTaskBreakdown(
        [FromBody] GenerateTaskBreakdownRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { Message = "Task title is required." });
        }

        var result = await _aiService.GenerateTaskBreakdownAsync(request.Title, request.Context, cancellationToken);
        return Ok(result);
    }

    [HttpPost("summarize-discussion")]
    public async Task<IActionResult> SummarizeDiscussion(
        [FromBody] SummarizeDiscussionRequest request,
        CancellationToken cancellationToken)
    {
        var summary = await _aiService.SummarizeCommentsAsync(request.Comments ?? [], cancellationToken);
        return Ok(new { Summary = summary });
    }
}
