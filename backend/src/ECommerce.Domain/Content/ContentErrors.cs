namespace ECommerce.Domain.Content;

public static class ContentErrors
{
    public static readonly Error BannerNotFound = new(
        "Content.BannerNotFound",
        "The banner was not found.",
        ErrorType.NotFound);

    public static readonly Error PageNotFound = new(
        "Content.PageNotFound",
        "The page was not found.",
        ErrorType.NotFound);

    public static readonly Error PageSlugAlreadyExists = new(
        "Content.PageSlugAlreadyExists",
        "A page with this slug already exists.",
        ErrorType.Conflict);

    public static readonly Error LayoutNotFound = new(
        "Content.LayoutNotFound",
        "The layout was not found.",
        ErrorType.NotFound);

    public static readonly Error LayoutSlugAlreadyExists = new(
        "Content.LayoutSlugAlreadyExists",
        "A layout with this slug already exists.",
        ErrorType.Conflict);
}
