using System.Linq.Expressions;

namespace ECommerce.Domain.Specifications;

public interface ISpecification<T> where T : class
{
    Expression<Func<T, bool>>? Criteria { get; }
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }
    IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderExpressions { get; }
    Expression<Func<T, object>>? GroupBy { get; }
    int? Skip { get; }
    int? Take { get; }
    bool IsPagingEnabled { get; }
    bool IsAsNoTracking { get; }
    bool IsAsSplitQuery { get; }
    bool IgnoreQueryFilters { get; }

    bool IsSatisfiedBy(T entity);
}
