using ECommerce.Domain.Audit;
using ECommerce.Domain.Flags;
using ECommerce.Shared.Audit;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Flags.Commands;
using ECommerce.UseCases.Flags.Ports;

namespace ECommerce.UseCases.Flags.Handlers;

public sealed class SetFeatureFlagCommandHandler(
    IFeatureFlagRepository repository,
    IAuditLogWriter auditLogWriter,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<SetFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(SetFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var flag = await repository.GetByKeyAsync(request.Key, cancellationToken);
        if (flag is null)
        {
            flag = FeatureFlag.Create(request.Key, string.Empty, request.Enabled, utcNow);
            repository.Add(flag);
        }
        else
        {
            flag.SetEnabled(request.Enabled, utcNow);
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.FeatureFlagChanged,
            "FeatureFlag",
            request.Key,
            After: new { request.Enabled }), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
