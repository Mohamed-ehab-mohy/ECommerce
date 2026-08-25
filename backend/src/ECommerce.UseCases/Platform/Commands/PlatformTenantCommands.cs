using ECommerce.Shared.Primitives;
using MediatR;
using System;

namespace ECommerce.UseCases.Platform.Commands;

public sealed record SuspendTenantCommand(Guid TenantId) : IRequest<Result>;
public sealed record ActivateTenantCommand(Guid TenantId) : IRequest<Result>;
