
namespace ECommerce.Domain.Catalog;

public static class CategoryErrors
{
    public static readonly Error CategoryNotFound = new(
        "Category.CategoryNotFound",
        "The category was not found.",
        ErrorType.NotFound);

    public static readonly Error SlugAlreadyExists = new(
        "Category.SlugAlreadyExists",
        "A category with this slug already exists.",
        ErrorType.Conflict);

    public static readonly Error ParentNotFound = new(
        "Category.ParentNotFound",
        "The parent category was not found.",
        ErrorType.NotFound);

    public static readonly Error CycleDetected = new(
        "Category.CycleDetected",
        "The parent category would create a cycle in the category hierarchy.",
        ErrorType.BadRequest);

    public static readonly Error DepthLimitExceeded = new(
        "Category.DepthLimitExceeded",
        "The category hierarchy depth cannot exceed 5 levels.",
        ErrorType.BadRequest);
}
