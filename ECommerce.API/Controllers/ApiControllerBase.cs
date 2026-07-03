using ECommerce.API.Models;
using ECommerce.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiControllerBase : ControllerBase
{
    protected IActionResult Problem(Result result)
    {
        var statusCode = result.Error!.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = result.Error.Type switch
        {
            ErrorType.Validation => "Validation Error",
            ErrorType.NotFound => "Resource Not Found",
            ErrorType.Unauthorized => "Unauthorized Access",
            ErrorType.Forbidden => "Forbidden Access",
            ErrorType.Conflict => "Conflict Error",
            _ => "Internal Server Error"
        };

        var problem = new Dictionary<string, object?>
        {
            { "type", $"https://example.com/errors/{result.Error.Code}" },
            { "title", title },
            { "status", statusCode },
            { "traceId", HttpContext.TraceIdentifier }
        };

        if (result.Error.Type == ErrorType.Validation)
        {
            problem["errors"] = new Dictionary<string, string[]>
            {
                { result.Error.Code, new[] { result.Error.Message } }
            };
        }
        else
        {
            problem["detail"] = result.Error.Message;
        }

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    protected OkObjectResult Success<T>(T data, string? message = null, PaginationMetadata? pagination = null)
    {
        var response = ApiResponse<T>.Ok(data, HttpContext.TraceIdentifier, message, pagination);
        return new OkObjectResult(response);
    }

    protected IActionResult FromResult<T>(Result<T> result, string? message = null, PaginationMetadata? pagination = null)
    {
        if (result.IsFailure)
        {
            return Problem(result);
        }

        return Success(result.Data!, message, pagination);
    }
}
