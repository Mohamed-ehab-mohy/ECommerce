using ECommerce.Domain.Audit;
using ECommerce.Domain.Pricing;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Coupons.Commands;
using ECommerce.UseCases.Coupons.Responses;
using ECommerce.UseCases.Promotions.Ports;

namespace ECommerce.UseCases.Coupons.Handlers;

public sealed class CreateCouponCommandHandler(
    ICouponRepository coupons,
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateCouponCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreateCouponCommand, Result<CouponResponse>>
{
    public async Task<Result<CouponResponse>> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CouponResponse>();
        }

        if (await promotions.GetByIdAsync(request.PromotionId, cancellationToken) is null)
        {
            return PromotionErrors.PromotionNotFound;
        }

        var created = Coupon.Create(
            request.Code,
            request.PromotionId,
            request.TotalUses,
            request.PerCustomerLimit,
            request.StartsAt,
            request.EndsAt,
            timeProvider.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
        {
            return created.Error;
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.CouponCreated,
            "Coupon",
            created.Value.Id.ToString(),
            After: new
            {
                created.Value.Code,
                created.Value.PromotionId,
                created.Value.TotalUses,
                created.Value.PerCustomerLimit
            }), cancellationToken);

        coupons.Add(created.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CouponResponse>.Success(CouponResponse.From(created.Value));
    }
}
