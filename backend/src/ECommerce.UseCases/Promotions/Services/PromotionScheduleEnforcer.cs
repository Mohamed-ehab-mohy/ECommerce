using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Promotions.Services;

public sealed record PromotionScheduleEnforcementResult(int Activated, int Paused);

/// <summary>
/// Enforces promotion schedules on a clock: activates Draft campaigns whose window has started
/// and pauses Active campaigns whose window has ended. Manual pauses are never overridden (US-E-007).
/// </summary>
public sealed class PromotionScheduleEnforcer(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<PromotionScheduleEnforcer> logger)
{
    public async Task<PromotionScheduleEnforcementResult> EnforceAsync(CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var activated = 0;
        var dueForActivation = await promotions.GetDueForActivationAsync(utcNow, cancellationToken);
        foreach (var promotion in dueForActivation)
        {
            var result = promotion.Activate(utcNow);
            if (result.IsSuccess)
            {
                activated++;
            }
        }

        var paused = 0;
        var dueForPause = await promotions.GetDueForPauseAsync(utcNow, cancellationToken);
        foreach (var promotion in dueForPause)
        {
            var result = promotion.Pause(utcNow);
            if (result.IsSuccess)
            {
                paused++;
            }
        }

        if (activated > 0 || paused > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Promotion schedule enforced at {Timestamp:O}: activated {Activated}, paused {Paused}.",
                utcNow,
                activated,
                paused);
        }

        return new PromotionScheduleEnforcementResult(activated, paused);
    }
}
