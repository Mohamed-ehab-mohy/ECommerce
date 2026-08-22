using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateCategoryCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private const int MaxDepth = 5;

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<Guid>();
        }

        var slug = request.Slug.Trim().ToLowerInvariant();

        if (await categories.GetBySlugAsync(slug, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(CategoryErrors.SlugAlreadyExists);
        }

        var (parentId, level) = await ResolveParentAsync(request.ParentId, cancellationToken);
        if (level is null)
        {
            return Result<Guid>.Failure(CategoryErrors.ParentNotFound);
        }

        if (level > MaxDepth)
        {
            return Result<Guid>.Failure(CategoryErrors.DepthLimitExceeded);
        }

        var category = Category.Create(
            request.Name.Trim(),
            slug,
            parentId,
            request.SortOrder,
            level.Value,
            timeProvider.GetUtcNow().UtcDateTime);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.CategoryCreated,
            "Category",
            category.Id.ToString(),
            After: new
            {
                category.Name,
                category.Slug,
                category.ParentId,
                category.Level
            }), cancellationToken);

        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id);
    }

    private async Task<(Guid? ParentId, int? Level)> ResolveParentAsync(Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return (null, 1);
        }

        var parent = await categories.GetByIdAsync(parentId.Value, cancellationToken);
        return parent is null ? (parentId, null) : (parent.Id, parent.Level + 1);
    }
}
