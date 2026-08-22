using ECommerce.API.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Queries;
using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/roles")]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListRolesQuery(), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateRoleCommand(
            request.Name,
            request.Description,
            request.Permissions), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    [Authorize]
    [HttpPut("{roleId:guid}/permissions")]
    public async Task<IActionResult> AssignPermissions(
        Guid roleId,
        AssignRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AssignRolePermissionsCommand(roleId, request.Permissions),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string>? Permissions);

public sealed record AssignRolePermissionsRequest(IReadOnlyList<string> Permissions);
