using System.Collections.Concurrent;
using System.Reflection;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Common;

internal static class AuthorizationResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> Builders = new();

    public static TResponse Forbidden<TResponse>(Error error)
    {
        var builder = Builders.GetOrAdd(typeof(TResponse), BuildBuilder);
        return (TResponse)builder(error);
    }

    private static Func<Error, object> BuildBuilder(Type responseType)
    {
        if (responseType == typeof(Result))
        {
            return error => Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failure = responseType.GetMethod(
                    nameof(Result<object>.Failure),
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: [typeof(Error)],
                    modifiers: null)
                ?? throw new InvalidOperationException($"Result<>.Failure was not found for {responseType}.");

            return error => failure.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"Authorization requires a Result response; '{responseType}' is not supported.");
    }
}
