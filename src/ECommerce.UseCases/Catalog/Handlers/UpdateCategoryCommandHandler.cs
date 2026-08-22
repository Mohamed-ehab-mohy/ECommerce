using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<UpdateCategoryCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdateCategoryCommand, Result>
{
    private const int MaxDepth = 5;

    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(CategoryErrors.CategoryNotFound);
        }

        var slug = request.Slug?.Trim().ToLowerInvariant();
        if (slug is not null && slug != category.Slug)
        {
            var existing = await categories.GetBySlugAsync(slug, cancellationToken);
            if (existing is not null && existing.Id != category.Id)
            {
                return Result.Failure(CategoryErrors.SlugAlreadyExists);
            }
        }

        if (request.Name is not null || slug is not null || request.SortOrder is not null)
        {
            category.UpdateDetails(
                request.Name?.Trim() ?? category.Name,
                slug ?? category.Slug,
                request.SortOrder ?? category.SortOrder,
                timeProvider.GetUtcNow().UtcDateTime);
        }

        if (request.ParentId is { } newParentId)
        {
            var all = await categories.ListAllAsync(cancellationToken);
            var parent = all.FirstOrDefault(item => item.Id == newParentId);
            if (parent is null)
            {
                return Result.Failure(CategoryErrors.ParentNotFound);
            }

            if (newParentId == category.Id || IsDescendant(all, newParentId, category.Id))
            {
                return Result.Failure(CategoryErrors.CycleDetected);
            }

            var newLevel = parent.Level + 1;
            if (newLevel > MaxDepth)
            {
                return Result.Failure(CategoryErrors.DepthLimitExceeded);
            }

            category.ChangeParent(parent.Id, newLevel, timeProvider.GetUtcNow().UtcDateTime);
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.CategoryUpdated,
            "Category",
            category.Id.ToString(),
            After: new
            {
                category.Name,
                category.Slug,
                category.ParentId,
                category.Level
            }), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static bool IsDescendant(
        IReadOnlyList<Category> all,
        Guid potentialAncestorId,
        Guid categoryId)
    {
        var current = all.FirstOrDefault(item => item.Id == potentialAncestorId);
        while (current is not null && current.ParentId is not null)
        {
            if (current.ParentId == categoryId)
            {
                return true;
            }

            current = all.FirstOrDefault(item => item.Id == current.ParentId);
        }

        return false;
    }
}
