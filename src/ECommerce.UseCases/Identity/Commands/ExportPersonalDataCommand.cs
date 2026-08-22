using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record ExportPersonalDataCommand(Guid CustomerId) : IRequest<Result<PersonalDataExport>>;
