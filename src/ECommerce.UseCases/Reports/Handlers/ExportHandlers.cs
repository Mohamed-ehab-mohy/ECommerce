using System.Text.Json;
using ECommerce.Domain.Reporting;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reports.Commands;
using ECommerce.UseCases.Reports.Ports;
using ECommerce.UseCases.Reports.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Reports.Handlers;

/// <summary>Serialized report filters re-read by the async export job (T-DAT-017).</summary>
public sealed record ExportFilters(DateTime? From, DateTime? To, string? Granularity, string? Currency);

/// <summary>Persists an export job and enqueues generation (US-L-007, T-DAT-017).</summary>
public sealed class CreateExportCommandHandler(
    IExportJobRepository exports,
    IExportJobScheduler scheduler,
    IUnitOfWork unitOfWork,
    IValidator<CreateExportCommand> validator,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<CreateExportCommand, Result<ExportStartedResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<ExportStartedResponse>> Handle(
        CreateExportCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ExportStartedResponse>();
        }

        var filters = new ExportFilters(request.From, request.To, request.Granularity, request.Currency);
        var export = ExportJob.Create(
            request.ReportType.Trim().ToLowerInvariant(),
            JsonSerializer.Serialize(filters, JsonOptions),
            currentUser.UserId,
            timeProvider.GetUtcNow().UtcDateTime);

        exports.Add(export);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        scheduler.Enqueue(export.Id);

        return Result<ExportStartedResponse>.Success(
            new ExportStartedResponse(export.Id, export.Status.ToString()));
    }
}

/// <summary>Returns the export status; the controller streams the file when Completed (US-L-007).</summary>
public sealed class GetExportQueryHandler(
    IExportJobRepository exports,
    IValidator<GetExportQuery> validator) : IRequestHandler<GetExportQuery, Result<ExportStatusResponse>>
{
    public async Task<Result<ExportStatusResponse>> Handle(
        GetExportQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ExportStatusResponse>();
        }

        var export = await exports.GetByIdAsync(request.ExportId, cancellationToken);

        return export is null
            ? ExportErrors.ExportNotFound
            : new ExportStatusResponse(
                export.Id,
                export.ReportType,
                export.Status.ToString(),
                export.RowCount,
                export.FileKey,
                export.CreatedBy,
                export.CreatedAt,
                export.StartedAtUtc,
                export.CompletedAtUtc);
    }
}

public sealed class GetExportQueryValidator : AbstractValidator<GetExportQuery>
{
    public GetExportQueryValidator()
    {
        RuleFor(query => query.ExportId)
            .NotEmpty()
            .WithMessage("An export id is required.");
    }
}
