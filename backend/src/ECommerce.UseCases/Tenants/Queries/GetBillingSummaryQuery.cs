using ECommerce.Shared.Primitives;
using MediatR;
using System;

namespace ECommerce.UseCases.Tenants.Queries;

public sealed record GetBillingSummaryQuery() : IRequest<Result<BillingSummaryResponse>>;