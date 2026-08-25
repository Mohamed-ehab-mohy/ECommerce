using ECommerce.Shared.Primitives;
using MediatR;
using System;

namespace ECommerce.UseCases.Tenants.Commands;

public sealed record ChangeSubscriptionPlanCommand(Guid PlanId) : IRequest<Result<Guid>>;
