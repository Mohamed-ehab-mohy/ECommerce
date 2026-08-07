using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Common;

public static class ProblemResponse
{
    public static IActionResult Create(OperationError error)
    {
        var problem = new ProblemDetails
        {
            Status = error.StatusCode,
            Type = error.Type,
            Title = Title(error.StatusCode),
            Detail = error.Detail
        };

        problem.Extensions["code"] = error.Code;

        if (error.Permission is not null)
        {
            problem.Extensions["permission"] = error.Permission;
        }

        if (error.RetryAfterSeconds is { } retryAfter)
        {
            problem.Extensions["retryAfter"] = retryAfter;
        }

        if (error.Metadata is not null)
        {
            foreach (var (key, value) in error.Metadata)
            {
                problem.Extensions[key] = value;
            }
        }

        return new ObjectResult(problem)
        {
            StatusCode = error.StatusCode
        };
    }

    private static string Title(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        402 => "Payment Required",
        422 => "Validation Failed",
        409 => "Conflict",
        404 => "Not Found",
        401 => "Unauthorized",
        403 => "Forbidden",
        423 => "Locked",
        429 => "Too Many Requests",
        502 => "Bad Gateway",
        _ => "Internal Server Error"
    };
}
