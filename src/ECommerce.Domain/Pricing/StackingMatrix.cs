namespace ECommerce.Domain.Pricing;

/// <summary>
/// Stacking policy for a promotion (US-E-004). <c>AllowStack = false</c> means best-of when multiple
/// promotions target the same discount bucket; otherwise the promotion stacks with others, optionally
/// restricted to <c>AllowStackWith</c>.
/// </summary>
public sealed record StackingMatrix(bool AllowStack, IReadOnlyList<Guid> AllowStackWith)
{
    public static readonly StackingMatrix BestOf = new(false, []);

    public bool CanStackWith(Guid promotionId) =>
        AllowStack && (AllowStackWith.Count == 0 || AllowStackWith.Contains(promotionId));
}
