using ECommerce.Domain.Audit;
using ECommerce.Domain.Pricing;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Commands;
using ECommerce.UseCases.Promotions.Ports;
using ECommerce.UseCases.Promotions.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Promotions.Handlers;

public sealed class UpdatePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<UpdatePromotionCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdatePromotionCommand, Result<PromotionResponse>>
{
    public async Task<Result<PromotionResponse>> Handle(UpdatePromotionCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PromotionResponse>();
        }

        var promotion = await promotions.GetByIdAsync(request.Id, cancellationToken);
        if (promotion is null)
        {
            return PromotionErrors.PromotionNotFound;
        }

        var result = promotion.Update(
            request.Name,
            request.Conditions.Select(condition => condition.ToDomain()).ToList(),
            request.Actions.Select(action => action.ToDomain()).ToList(),
            request.ToStacking(),
            request.EligibleCountries,
            request.EligibleCurrencies,
            timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.PromotionUpdated,
            "Promotion",
            promotion.Id.ToString(),
            After: new
            {
                promotion.Name,
                promotion.State
            }), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PromotionResponse>.Success(PromotionResponse.From(promotion));
    }
}
