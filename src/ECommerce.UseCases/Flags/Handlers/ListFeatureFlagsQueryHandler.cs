using ECommerce.Domain.Flags;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Flags.Ports;
using ECommerce.UseCases.Flags.Queries;
using ECommerce.UseCases.Flags.Responses;

namespace ECommerce.UseCases.Flags.Handlers;

public sealed class ListFeatureFlagsQueryHandler(
    IFeatureFlagRepository repository) : IRequestHandler<ListFeatureFlagsQuery, Result<IReadOnlyList<FeatureFlagResponse>>>
{
    public async Task<Result<IReadOnlyList<FeatureFlagResponse>>> Handle(
        ListFeatureFlagsQuery request,
        CancellationToken cancellationToken)
    {
        var flags = await repository.ListAsync(cancellationToken);

        var response = flags
            .Select(ToResponse)
            .ToList();

        return Result<IReadOnlyList<FeatureFlagResponse>>.Success(response);
    }

    private static FeatureFlagResponse ToResponse(FeatureFlag flag) =>
        new(flag.Key, flag.Description, flag.Enabled);
}

public sealed class GetFeatureFlagQueryHandler(
    IFeatureFlagRepository repository) : IRequestHandler<GetFeatureFlagQuery, Result<FeatureFlagResponse>>
{
    public async Task<Result<FeatureFlagResponse>> Handle(GetFeatureFlagQuery request, CancellationToken cancellationToken)
    {
        var flag = await repository.GetByKeyAsync(request.Key, cancellationToken);
        return flag is null
            ? Result<FeatureFlagResponse>.Failure(new Error(
                "Flags.NotFound",
                $"The feature flag '{request.Key}' was not found.",
                ErrorType.NotFound))
            : Result<FeatureFlagResponse>.Success(new FeatureFlagResponse(flag.Key, flag.Description, flag.Enabled));
    }
}
