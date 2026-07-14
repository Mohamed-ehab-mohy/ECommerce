using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Specifications;

public interface ISpecificationEvaluator
{
    IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> spec) where T : BaseEntity;
}
