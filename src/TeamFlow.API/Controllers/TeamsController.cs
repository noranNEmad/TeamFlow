using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Features.Teams.DTOs;
using TeamFlow.Application.Features.Teams.Services;

namespace TeamFlow.API.Controllers;

[Authorize]
public class TeamsController : BaseApiController
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create([FromBody] CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await _teamService.CreateTeamAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<TeamDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _teamService.GetTeamsAsync(cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddTeamMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await _teamService.AddMemberToTeamAsync(id, request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new { message = "Member added to team successfully." });
        }
        return BadRequest(new { error = result.Error.Code, message = result.Error.Description });
    }
}

