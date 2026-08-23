using MediatR;

namespace ECommerce.UseCases.Common;

public interface ICacheableQuery<TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    
    TimeSpan? Expiration { get; }
}
