using ECommerce.Shared.Primitives;
using MediatR;
using System;

namespace ECommerce.UseCases.Tenants.Commands;

public sealed record SetCustomDomainCommand(string CustomDomain) : IRequest<Result<Guid>>;
