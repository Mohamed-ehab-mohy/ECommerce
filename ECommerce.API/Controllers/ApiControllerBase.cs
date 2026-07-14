using ECommerce.API.Models;
using ECommerce.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
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

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = result.Error.Type switch
            {
                ErrorType.Validation => "Validation Error",
                ErrorType.NotFound => "Resource Not Found",
                ErrorType.Unauthorized => "Unauthorized Access",
                ErrorType.Forbidden => "Forbidden Access",
                ErrorType.Conflict => "Conflict Error",
                _ => "Internal Server Error"
            },
            Type = $"https://example.com/errors/{result.Error.Code}",
            Detail = result.Error.Message,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        problemDetails.Extensions["errorCode"] = result.Error.Code;

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
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
