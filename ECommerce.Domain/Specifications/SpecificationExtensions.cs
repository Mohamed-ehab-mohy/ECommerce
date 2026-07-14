using System.Linq.Expressions;

namespace ECommerce.Domain.Specifications;

public static class Specification
{
    public static Specification<T> Create<T>(Expression<Func<T, bool>>? predicate = null) where T : class
    {
        return new InlineSpecification<T>(predicate);
    }
}

internal sealed class InlineSpecification<T> : Specification<T> where T : class
{
    public InlineSpecification(Expression<Func<T, bool>>? predicate)
    {
        if (predicate is not null)
            Where(predicate);
    }
}

public static class SpecificationExtensions
{
    public static Specification<T> And<T>(this Specification<T> left, Specification<T> right) where T : class
    {
        return new CompositeSpecification<T>(left, right, CompositeType.And);
    }

    public static Specification<T> Or<T>(this Specification<T> left, Specification<T> right) where T : class
    {
        return new CompositeSpecification<T>(left, right, CompositeType.Or);
    }

    public static Specification<T> Not<T>(this Specification<T> spec) where T : class
    {
        return new NotSpecification<T>(spec);
    }
}

internal enum CompositeType
{
    And,
    Or
}

internal sealed class CompositeSpecification<T> : Specification<T> where T : class
{
    public CompositeSpecification(Specification<T> left, Specification<T> right, CompositeType type)
    {
        if (left.Criteria is not null && right.Criteria is not null)
        {
            var parameter = Expression.Parameter(typeof(T));
            var combined = type == CompositeType.And
                ? Expression.AndAlso(
                    Expression.Invoke(left.Criteria, parameter),
                    Expression.Invoke(right.Criteria, parameter))
                : Expression.OrElse(
                    Expression.Invoke(left.Criteria, parameter),
                    Expression.Invoke(right.Criteria, parameter));

            Where(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }
        else if (left.Criteria is not null)
        {
            Where(left.Criteria);
        }
        else if (right.Criteria is not null)
        {
            Where(right.Criteria);
        }

        foreach (var include in left.Includes)
            Include(include);
        foreach (var include in right.Includes)
            Include(include);

        foreach (var order in left.OrderExpressions)
        {
            if (order.Descending)
                OrderByDescending(order.KeySelector);
            else
                OrderBy(order.KeySelector);
        }

        foreach (var order in right.OrderExpressions)
        {
            if (order.Descending)
                OrderByDescending(order.KeySelector);
            else
                OrderBy(order.KeySelector);
        }

        if (left.IsPagingEnabled)
        {
            ApplySkip(left.Skip ?? 0);
            ApplyTake(left.Take ?? 0);
        }
    }
}

internal sealed class NotSpecification<T> : Specification<T> where T : class
{
    public NotSpecification(Specification<T> spec)
    {
        if (spec.Criteria is not null)
        {
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Not(Expression.Invoke(spec.Criteria, parameter));
            Where(Expression.Lambda<Func<T, bool>>(body, parameter));
        }
    }
}
