using System.Linq.Expressions;

namespace ECommerce.Domain.Specifications;

public abstract class Specification<T> : ISpecification<T> where T : class
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<(Expression<Func<T, object>> KeySelector, bool Descending)> _orderExpressions = [];

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;
    public IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderExpressions => _orderExpressions;
    public Expression<Func<T, object>>? GroupBy { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool IsAsNoTracking { get; private set; }
    public bool IsAsSplitQuery { get; private set; }
    public bool IgnoreQueryFilters { get; private set; }

    protected Specification<T> Where(Expression<Func<T, bool>> predicate)
    {
        Criteria = predicate;
        return this;
    }

    protected Specification<T> And(Expression<Func<T, bool>> predicate)
    {
        if (Criteria is null)
        {
            Criteria = predicate;
        }
        else
        {
            var parameter = Expression.Parameter(typeof(T));
            var combined = Expression.AndAlso(
                Expression.Invoke(Criteria, parameter),
                Expression.Invoke(predicate, parameter));
            Criteria = Expression.Lambda<Func<T, bool>>(combined, parameter);
        }

        return this;
    }

    protected Specification<T> Or(Expression<Func<T, bool>> predicate)
    {
        if (Criteria is null)
        {
            Criteria = predicate;
        }
        else
        {
            var parameter = Expression.Parameter(typeof(T));
            var combined = Expression.OrElse(
                Expression.Invoke(Criteria, parameter),
                Expression.Invoke(predicate, parameter));
            Criteria = Expression.Lambda<Func<T, bool>>(combined, parameter);
        }

        return this;
    }

    protected Specification<T> Include(Expression<Func<T, object>> include)
    {
        _includes.Add(include);
        return this;
    }

    protected Specification<T> OrderBy(Expression<Func<T, object>> keySelector)
    {
        _orderExpressions.Add((keySelector, false));
        return this;
    }

    protected Specification<T> OrderByDescending(Expression<Func<T, object>> keySelector)
    {
        _orderExpressions.Add((keySelector, true));
        return this;
    }

    protected Specification<T> ThenBy(Expression<Func<T, object>> keySelector)
    {
        if (_orderExpressions.Count > 0)
            _orderExpressions.Add((keySelector, false));

        return this;
    }

    protected Specification<T> ThenByDescending(Expression<Func<T, object>> keySelector)
    {
        if (_orderExpressions.Count > 0)
            _orderExpressions.Add((keySelector, true));

        return this;
    }

    protected Specification<T> ApplyGroupBy(Expression<Func<T, object>> keySelector)
    {
        GroupBy = keySelector;
        return this;
    }

    protected Specification<T> ApplySkip(int count)
    {
        Skip = count;
        IsPagingEnabled = true;
        return this;
    }

    protected Specification<T> ApplyTake(int count)
    {
        Take = count;
        IsPagingEnabled = true;
        return this;
    }

    protected Specification<T> ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
        return this;
    }

    protected Specification<T> AsNoTracking()
    {
        IsAsNoTracking = true;
        return this;
    }

    protected Specification<T> AsSplitQuery()
    {
        IsAsSplitQuery = true;
        return this;
    }

    protected Specification<T> DisableQueryFilters()
    {
        IgnoreQueryFilters = true;
        return this;
    }

    public virtual bool IsSatisfiedBy(T entity)
    {
        if (Criteria is null) return true;

        var predicate = Criteria.Compile();
        return predicate(entity);
    }
}
