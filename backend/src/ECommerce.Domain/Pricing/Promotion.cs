using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Pricing;

public enum PromotionState
{
    Draft,
    Active,
    Paused,
    Ended
}

/// <summary>
/// Promotion campaign aggregate. Eligibility combines scope (countries/currencies),
/// schedule and conditions; only <see cref="PromotionState.Active"/> promotions within their schedule apply.
/// </summary>
public sealed class Promotion : BaseEntity<Guid>
{
    private readonly List<PromotionCondition> _conditions = [];
    private readonly List<DiscountRule> _actions = [];
    private readonly List<string> _eligibleCountries = [];
    private readonly List<string> _eligibleCurrencies = [];

    private Promotion()
    {
        Name = string.Empty;
        Stacking = StackingMatrix.BestOf;
    }

    public string Name { get; private set; }

    public PromotionState State { get; private set; }

    public DateTime? StartsAt { get; private set; }

    public DateTime? EndsAt { get; private set; }

    public StackingMatrix Stacking { get; private set; }

    public IReadOnlyCollection<PromotionCondition> Conditions => _conditions;

    public IReadOnlyCollection<DiscountRule> Actions => _actions;

    public IReadOnlyCollection<string> EligibleCountries => _eligibleCountries;

    public IReadOnlyCollection<string> EligibleCurrencies => _eligibleCurrencies;

    public static Result<Promotion> Create(
        string name,
        IReadOnlyList<PromotionCondition> conditions,
        IReadOnlyList<DiscountRule> actions,
        StackingMatrix stacking,
        IReadOnlyList<string> eligibleCountries,
        IReadOnlyList<string> eligibleCurrencies,
        DateTime? startsAt,
        DateTime? endsAt,
        DateTime utcNow)
    {
        var validation = ValidateShape(name, conditions, actions, startsAt, endsAt);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            State = PromotionState.Draft,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Stacking = stacking ?? StackingMatrix.BestOf,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        promotion._conditions.AddRange(conditions);
        promotion._actions.AddRange(actions);
        promotion._eligibleCountries.AddRange(Normalize(eligibleCountries));
        promotion._eligibleCurrencies.AddRange(Normalize(eligibleCurrencies));

        promotion.AddDomainEvent(new PromotionCreated(promotion.Id, promotion.Name));

        return promotion;
    }

    public Result Update(
        string name,
        IReadOnlyList<PromotionCondition> conditions,
        IReadOnlyList<DiscountRule> actions,
        StackingMatrix stacking,
        IReadOnlyList<string> eligibleCountries,
        IReadOnlyList<string> eligibleCurrencies,
        DateTime utcNow)
    {
        var validation = ValidateShape(name, conditions, actions, StartsAt, EndsAt);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        Name = name.Trim();
        Stacking = stacking ?? StackingMatrix.BestOf;
        UpdatedAt = utcNow;

        _conditions.Clear();
        _conditions.AddRange(conditions);
        _actions.Clear();
        _actions.AddRange(actions);
        _eligibleCountries.Clear();
        _eligibleCountries.AddRange(Normalize(eligibleCountries));
        _eligibleCurrencies.Clear();
        _eligibleCurrencies.AddRange(Normalize(eligibleCurrencies));

        return Result.Success();
    }

    public Result Activate(DateTime utcNow)
    {
        if (State == PromotionState.Ended)
        {
            return PromotionErrors.InvalidState;
        }

        State = PromotionState.Active;
        UpdatedAt = utcNow;

        AddDomainEvent(new PromotionActivated(Id));

        return Result.Success();
    }

    public Result Pause(DateTime utcNow)
    {
        if (State != PromotionState.Active)
        {
            return PromotionErrors.InvalidState;
        }

        State = PromotionState.Paused;
        UpdatedAt = utcNow;

        AddDomainEvent(new PromotionPaused(Id));

        return Result.Success();
    }

    public Result Schedule(DateTime? startsAt, DateTime? endsAt, DateTime utcNow)
    {
        if (startsAt is not null && endsAt is not null && startsAt > endsAt)
        {
            return PromotionErrors.InvalidSchedule;
        }

        StartsAt = startsAt;
        EndsAt = endsAt;
        UpdatedAt = utcNow;

        AddDomainEvent(new PromotionScheduled(Id, startsAt, endsAt));

        return Result.Success();
    }

    public void End(DateTime utcNow)
    {
        State = PromotionState.Ended;
        UpdatedAt = utcNow;
    }

    /// <summary>Deterministic eligibility: state, schedule, scope and conditions.</summary>
    public bool IsEligible(PricingContext context, DateTime utcNow) =>
        State != PromotionState.Active
            ? false
            : StartsAt is not null && utcNow < StartsAt.Value
                ? false
                : EndsAt is not null && utcNow > EndsAt.Value
                    ? false
                    : _eligibleCountries.Count > 0 && !_eligibleCountries.Contains(context.Country)
                        ? false
                        : _eligibleCurrencies.Count > 0 && !_eligibleCurrencies.Contains(context.Currency)
                            ? false
                            : _conditions.All(condition => PromotionConditionEvaluator.Matches(condition, context));

    /// <summary>Item targets for a product action: product/category/brand constrained lines, else all lines.</summary>
    public IReadOnlyList<PricingLine> TargetLines(PricingContext context)
    {
        var productIds = _conditions.OfType<ProductCondition>().SelectMany(c => c.ProductIds).ToHashSet();
        var categoryIds = _conditions.OfType<CategoryCondition>().SelectMany(c => c.CategoryIds).ToHashSet();
        var brandIds = _conditions.OfType<BrandCondition>().SelectMany(c => c.BrandIds).ToHashSet();

        return productIds.Count == 0 && categoryIds.Count == 0 && brandIds.Count == 0
            ? context.Lines
            : context.Lines
                .Where(line => productIds.Contains(line.ProductId)
                    || line.CategoryIds.Any(categoryIds.Contains)
                    || line.BrandIds.Any(brandIds.Contains))
                .ToList();
    }

    private static Result ValidateShape(
        string name,
        IReadOnlyList<PromotionCondition> conditions,
        IReadOnlyList<DiscountRule> actions,
        DateTime? startsAt,
        DateTime? endsAt)
    {
        _ = conditions;
        if (string.IsNullOrWhiteSpace(name))
        {
            return PromotionErrors.NameRequired;
        }

        if (actions.Count == 0)
        {
            return PromotionErrors.ActionsRequired;
        }

        if (startsAt is not null && endsAt is not null && startsAt > endsAt)
        {
            return PromotionErrors.InvalidSchedule;
        }

        foreach (var action in actions)
        {
            var validation = DiscountRule.Validate(action.Type, action.Basis, action.Value, action.Cap);
            if (validation.IsFailure)
            {
                return validation.Error;
            }
        }

        return Result.Success();
    }

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
}
