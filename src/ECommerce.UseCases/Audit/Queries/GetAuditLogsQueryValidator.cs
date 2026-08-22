
namespace ECommerce.UseCases.Audit.Queries;

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Action)
            .MaximumLength(100)
            .When(x => x.Action is not null);

        RuleFor(x => x.EntityType)
            .MaximumLength(100)
            .When(x => x.EntityType is not null);

        RuleFor(x => x)
            .Must(query => query.From is null || query.To is null || query.From <= query.To)
            .WithMessage("'From' must be on or before 'To'.");
    }
}
