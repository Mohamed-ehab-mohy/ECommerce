using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Flags.Responses;

namespace ECommerce.UseCases.Flags.Queries;

public sealed record ListFeatureFlagsQuery : IRequest<Result<IReadOnlyList<FeatureFlagResponse>>>, IRequirePermission
{
    public string Permission => Permissions.PlatformFlagsRead;
}

public sealed record GetFeatureFlagQuery(string Key) : IRequest<Result<FeatureFlagResponse>>, IRequirePermission
{
    public string Permission => Permissions.PlatformFlagsRead;
}
