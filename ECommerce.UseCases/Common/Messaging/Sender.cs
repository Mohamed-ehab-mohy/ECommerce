using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UseCases.Common.Messaging;

internal sealed class Sender : ISender
{
    private readonly IServiceProvider _serviceProvider;

    public Sender(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

        return await handler.HandleAsync((dynamic)request, ct);
    }
}
