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

public sealed class CreatePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreatePromotionCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreatePromotionCommand, Result<PromotionResponse>>
{
    public async Task<Result<PromotionResponse>> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PromotionResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var created = Promotion.Create(
            request.Name,
            request.Conditions.Select(condition => condition.ToDomain()).ToList(),
            request.Actions.Select(action => action.ToDomain()).ToList(),
            request.ToStacking(),
            request.EligibleCountries,
            request.EligibleCurrencies,
            request.StartsAt,
            request.EndsAt,
            utcNow);
        if (created.IsFailure)
        {
            return created.Error;
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.PromotionCreated,
            "Promotion",
            created.Value.Id.ToString(),
            After: new
            {
                created.Value.Name,
                created.Value.State,
                created.Value.StartsAt,
                created.Value.EndsAt
            }), cancellationToken);

        promotions.Add(created.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PromotionResponse>.Success(PromotionResponse.From(created.Value));
    }
}
