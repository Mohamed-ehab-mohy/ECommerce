using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/me")]
public sealed class ProfileController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProfileQuery(User.GetUserId()), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateProfileCommand(
            User.GetUserId(),
            request.DisplayName,
            request.Phone,
            request.Locale,
            request.Currency), cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.ToOperationError());
        }

        var profile = await sender.Send(new GetProfileQuery(User.GetUserId()), cancellationToken);
        return profile.IsFailure
            ? ToProblem(profile.ToOperationError())
            : Ok(profile.Value);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAddressesQuery(User.GetUserId()), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress(AddAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddAddressCommand(
            User.GetUserId(),
            request.Label,
            request.Street,
            request.City,
            request.Region,
            request.Country,
            request.PostalCode), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteAddressCommand(User.GetUserId(), addressId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [HttpPost("close")]
    public async Task<IActionResult> CloseAccount(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CloseAccountCommand(User.GetUserId()), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [HttpPost("erase")]
    public async Task<IActionResult> ErasePersonalData(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ErasePersonalDataCommand(User.GetUserId()), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}
