using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUser currentUser,
        ICorrelationIdProvider correlationIdProvider)
    {
        _logger = logger;
        _currentUser = currentUser;
        _correlationIdProvider = correlationIdProvider;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = _correlationIdProvider.CorrelationId;
        var userId = _currentUser.UserId;

        _logger.LogInformation(
            "Handling {RequestName} (CorrelationId: {CorrelationId}, UserId: {UserId})",
            requestName, correlationId, userId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms (CorrelationId: {CorrelationId}, UserId: {UserId})",
                requestName, stopwatch.ElapsedMilliseconds, correlationId, userId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Request {RequestName} failed after {ElapsedMilliseconds} ms (CorrelationId: {CorrelationId}, UserId: {UserId})",
                requestName, stopwatch.ElapsedMilliseconds, correlationId, userId);

            throw;
        }
    }
}
