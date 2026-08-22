using ECommerce.Domain.Audit;
using ECommerce.Domain.Pricing;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Commands;
using ECommerce.UseCases.Promotions.Ports;
using ECommerce.UseCases.Promotions.Responses;

namespace ECommerce.UseCases.Promotions.Handlers;

public sealed class ActivatePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IAuditLogWriter auditLogWriter) : IRequestHandler<ActivatePromotionCommand, Result<PromotionResponse>>
{
    public async Task<Result<PromotionResponse>> Handle(ActivatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(request.Id, cancellationToken);
        if (promotion is null)
        {
            return PromotionErrors.PromotionNotFound;
        }

        var result = promotion.Activate(timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.PromotionActivated,
            "Promotion",
            promotion.Id.ToString()), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PromotionResponse>.Success(PromotionResponse.From(promotion));
    }
}

public sealed class PausePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IAuditLogWriter auditLogWriter) : IRequestHandler<PausePromotionCommand, Result<PromotionResponse>>
{
    public async Task<Result<PromotionResponse>> Handle(PausePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(request.Id, cancellationToken);
        if (promotion is null)
        {
            return PromotionErrors.PromotionNotFound;
        }

        var result = promotion.Pause(timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.PromotionPaused,
            "Promotion",
            promotion.Id.ToString()), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PromotionResponse>.Success(PromotionResponse.From(promotion));
    }
}

public sealed class SchedulePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IAuditLogWriter auditLogWriter) : IRequestHandler<SchedulePromotionCommand, Result<PromotionResponse>>
{
    public async Task<Result<PromotionResponse>> Handle(SchedulePromotionCommand request, CancellationToken cancellationToken)
    {
        if (request.StartsAt is not null && request.EndsAt is not null && request.StartsAt > request.EndsAt)
        {
            return PromotionErrors.InvalidSchedule;
        }

        var promotion = await promotions.GetByIdAsync(request.Id, cancellationToken);
        if (promotion is null)
        {
            return PromotionErrors.PromotionNotFound;
        }

        var result = promotion.Schedule(request.StartsAt, request.EndsAt, timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.PromotionScheduled,
            "Promotion",
            promotion.Id.ToString(),
            After: new
            {
                request.StartsAt,
                request.EndsAt
            }), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PromotionResponse>.Success(PromotionResponse.From(promotion));
    }
}
