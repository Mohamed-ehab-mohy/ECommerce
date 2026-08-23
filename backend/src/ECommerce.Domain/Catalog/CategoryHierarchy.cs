namespace ECommerce.Domain.Catalog;

public sealed class CategoryHierarchy
{
    public Guid AncestorId { get; private set; }

    public Guid DescendantId { get; private set; }

    public int Depth { get; private set; }

    public static CategoryHierarchy Create(Guid ancestorId, Guid descendantId, int depth)
    {
        return new CategoryHierarchy
        {
            AncestorId = ancestorId,
            DescendantId = descendantId,
            Depth = depth
        };
    }
}
