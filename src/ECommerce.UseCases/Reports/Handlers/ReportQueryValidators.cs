using ECommerce.UseCases.Reports.Queries;

namespace ECommerce.UseCases.Reports.Handlers;

internal static class ReportValidation
{
    public const int MaxReportDays = 400;
}

public sealed class SalesReportQueryValidator : AbstractValidator<SalesReportQuery>
{
    public SalesReportQueryValidator()
    {
        RuleFor(query => query.From)
            .Must((query, value) => value is null || query.To is null || value <= query.To.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(query => query.To)
            .Must((query, value) => value is null || query.From is null || value >= query.From.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null || query.To.Value - query.From.Value <= TimeSpan.FromDays(ReportValidation.MaxReportDays))
            .WithMessage($"A report cannot span more than {ReportValidation.MaxReportDays} days.");

        RuleFor(query => query.Granularity)
            .Must(value => value is null or "day" or "week" or "month")
            .WithMessage("Granularity must be one of 'day', 'week' or 'month'.");

        RuleFor(query => query.Currency)
            .Must(value => value is null || value.Length is >= 3 and <= 8)
            .WithMessage("Currency must be a valid ISO code (3 to 8 characters).");
    }
}

public sealed class FinanceReportQueryValidator : AbstractValidator<FinanceReportQuery>
{
    public FinanceReportQueryValidator()
    {
        RuleFor(query => query.From)
            .Must((query, value) => value is null || query.To is null || value <= query.To.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(query => query.To)
            .Must((query, value) => value is null || query.From is null || value >= query.From.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null || query.To.Value - query.From.Value <= TimeSpan.FromDays(ReportValidation.MaxReportDays))
            .WithMessage($"A report cannot span more than {ReportValidation.MaxReportDays} days.");
    }
}

public sealed class PromotionReportQueryValidator : AbstractValidator<PromotionReportQuery>
{
    public PromotionReportQueryValidator()
    {
        RuleFor(query => query.From)
            .Must((query, value) => value is null || query.To is null || value <= query.To.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(query => query.To)
            .Must((query, value) => value is null || query.From is null || value >= query.From.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null || query.To.Value - query.From.Value <= TimeSpan.FromDays(ReportValidation.MaxReportDays))
            .WithMessage($"A report cannot span more than {ReportValidation.MaxReportDays} days.");
    }
}

public sealed class FulfillmentReportQueryValidator : AbstractValidator<FulfillmentReportQuery>
{
    public FulfillmentReportQueryValidator()
    {
        RuleFor(query => query.From)
            .Must((query, value) => value is null || query.To is null || value <= query.To.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(query => query.To)
            .Must((query, value) => value is null || query.From is null || value >= query.From.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null || query.To.Value - query.From.Value <= TimeSpan.FromDays(ReportValidation.MaxReportDays))
            .WithMessage($"A report cannot span more than {ReportValidation.MaxReportDays} days.");
    }
}
