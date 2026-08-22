using ECommerce.Domain.Reporting;
using ECommerce.UseCases.Reports.Commands;

namespace ECommerce.UseCases.Reports.Handlers;

public sealed class CreateExportCommandValidator : AbstractValidator<CreateExportCommand>
{
    public CreateExportCommandValidator()
    {
        RuleFor(command => command.ReportType)
            .NotEmpty()
            .WithMessage("A report type is required.")
            .Must(ExportReportTypes.IsSupported)
            .WithMessage("Report type must be one of: sales, inventory, finance.");

        RuleFor(command => command.From)
            .Must((command, value) => value is null || command.To is null || value <= command.To.Value)
            .WithMessage("The start date cannot be after the end date.");

        RuleFor(command => command)
            .Must(command => command.From is null || command.To is null || command.To.Value - command.From.Value <= TimeSpan.FromDays(400))
            .WithMessage("An export cannot span more than 400 days.");

        RuleFor(command => command.Granularity)
            .Must(value => value is null or "day" or "week" or "month")
            .WithMessage("Granularity must be one of 'day', 'week' or 'month'.");

        RuleFor(command => command.Currency)
            .Must(value => value is null || value.Length is >= 3 and <= 8)
            .WithMessage("Currency must be a valid ISO code (3 to 8 characters).");
    }
}
