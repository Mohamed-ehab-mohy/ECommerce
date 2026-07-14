using System.Linq.Expressions;

namespace ECommerce.Domain.Specifications;

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
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;
    private readonly CompositeType _type;

    public CompositeSpecification(Specification<T> left, Specification<T> right, CompositeType type)
    {
        _left = left;
        _right = right;
        _type = type;

        if (left.Criteria is not null && right.Criteria is not null)
        {
            var combined = _type == CompositeType.And
                ? left.Criteria.And(right.Criteria)
                : left.Criteria.Or(right.Criteria);

            ApplyCriteria(combined);
        }
        else if (left.Criteria is not null)
        {
            ApplyCriteria(left.Criteria);
        }
        else if (right.Criteria is not null)
        {
            ApplyCriteria(right.Criteria);
        }

        foreach (var include in left.Includes)
            ApplyInclude(include);

        foreach (var include in right.Includes)
            ApplyInclude(include);

        if (left.OrderBy is not null)
            ApplyOrderBy(left.OrderBy);

        if (left.OrderByDescending is not null)
            ApplyOrderByDescending(left.OrderByDescending);

        if (left.IsPagingEnabled)
            ApplyPaging(left.Skip!.Value, left.Take!.Value);
    }
}

internal sealed class NotSpecification<T> : Specification<T> where T : class
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;

        if (specification.Criteria is not null)
        {
            ApplyCriteria(specification.Criteria.Not());
        }
    }
}

internal static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));

        var combined = Expression.AndAlso(
            Expression.Invoke(left, parameter),
            Expression.Invoke(right, parameter));

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));

        var combined = Expression.OrElse(
            Expression.Invoke(left, parameter),
            Expression.Invoke(right, parameter));

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
    {
        var parameter = Expression.Parameter(typeof(T));

        var body = Expression.Not(Expression.Invoke(expression, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
