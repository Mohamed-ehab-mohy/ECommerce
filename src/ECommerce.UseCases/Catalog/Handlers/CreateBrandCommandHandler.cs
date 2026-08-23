using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class CreateBrandCommandHandler(
    IBrandRepository brands,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateBrandCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreateBrandCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<Guid>();
        }

        var name = request.Name.Trim();
        if (await brands.GetByNameAsync(name, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(BrandErrors.NameAlreadyExists);
        }

        var brand = Brand.Create(
            name,
            request.Description?.Trim(),
            request.Website?.Trim(),
            timeProvider.GetUtcNow().UtcDateTime);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.BrandCreated,
            "Brand",
            brand.Id.ToString(),
            After: new { brand.Name, brand.Description, brand.Website }), cancellationToken);

        brands.Add(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(brand.Id);
    }
}
