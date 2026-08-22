using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class UpdateBrandCommandHandler(
    IBrandRepository brands,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<UpdateBrandCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdateBrandCommand, Result>
{
    public async Task<Result> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var brand = await brands.GetByIdAsync(request.BrandId, cancellationToken);
        if (brand is null)
        {
            return Result.Failure(BrandErrors.BrandNotFound);
        }

        var name = request.Name?.Trim();
        if (name is not null && name != brand.Name)
        {
            var existing = await brands.GetByNameAsync(name, cancellationToken);
            if (existing is not null && existing.Id != brand.Id)
            {
                return Result.Failure(BrandErrors.NameAlreadyExists);
            }
        }

        var before = new { brand.Name, brand.Description, brand.Website };

        brand.UpdateDetails(
            name ?? brand.Name,
            request.Description is not null ? request.Description.Trim() : brand.Description,
            request.Website is not null ? request.Website.Trim() : brand.Website,
            timeProvider.GetUtcNow().UtcDateTime);

        var after = new { brand.Name, brand.Description, brand.Website };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.BrandUpdated,
            "Brand",
            brand.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
