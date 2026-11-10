using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Code switch
        {
            var code when code.Contains("NotFound", StringComparison.OrdinalIgnoreCase) =>
                NotFound(new { error = result.Error.Code, message = result.Error.Description }),

            var code when code.Contains("Conflict", StringComparison.OrdinalIgnoreCase) || code.Contains("Exists", StringComparison.OrdinalIgnoreCase) =>
                Conflict(new { error = result.Error.Code, message = result.Error.Description }),

            var code when code.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) || code.Contains("Credentials", StringComparison.OrdinalIgnoreCase) =>
                Unauthorized(new { error = result.Error.Code, message = result.Error.Description }),

            var code when code.Contains("Validation", StringComparison.OrdinalIgnoreCase) || code.Contains("Invalid", StringComparison.OrdinalIgnoreCase) =>
                BadRequest(new { error = result.Error.Code, message = result.Error.Description }),

            _ => BadRequest(new { error = result.Error.Code, message = result.Error.Description })
        };
    }
}
