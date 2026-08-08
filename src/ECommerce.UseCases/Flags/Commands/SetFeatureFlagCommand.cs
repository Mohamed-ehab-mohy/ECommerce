using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using MediatR;

namespace ECommerce.UseCases.Flags.Commands;

public sealed record SetFeatureFlagCommand(string Key, bool Enabled) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.PlatformFlagsWrite;
}
