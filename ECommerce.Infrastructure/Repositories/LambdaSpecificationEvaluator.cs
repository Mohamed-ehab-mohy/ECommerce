using ECommerce.Domain.Entities;
using ECommerce.Domain.Specifications;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class LambdaSpecificationEvaluator : ISpecificationEvaluator
{
    public IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> spec) where T : BaseEntity
    {
        var query = inputQuery;

        if (spec.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderExpressions.Count > 0)
        {
            IOrderedQueryable<T>? ordered = null;

            for (var i = 0; i < spec.OrderExpressions.Count; i++)
            {
                var (keySelector, descending) = spec.OrderExpressions[i];

                ordered = i switch
                {
                    0 when descending => query.OrderByDescending(keySelector),
                    0 => query.OrderBy(keySelector),
                    _ when descending => ordered!.ThenByDescending(keySelector),
                    _ => ordered!.ThenBy(keySelector)
                };
            }

            if (ordered is not null)
                query = ordered;
        }

        if (spec.IsPagingEnabled)
        {
            if (spec.Skip.HasValue)
                query = query.Skip(spec.Skip.Value);
            if (spec.Take.HasValue)
                query = query.Take(spec.Take.Value);
        }

        if (spec.IsAsSplitQuery)
            query = query.AsSplitQuery();

        if (spec.IsAsNoTracking)
            query = query.AsNoTracking();

        return query;
    }
}
