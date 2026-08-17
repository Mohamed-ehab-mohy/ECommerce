using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record ExportPersonalDataCommand(Guid CustomerId) : IRequest<Result<PersonalDataExport>>;
