using System.Collections.Concurrent;
using System.Reflection;
using LanguageExt;

namespace Encina.Messaging;

/// <summary>
/// Sends a request whose type is only known at run time (a deserialized retry or dead letter)
/// through <see cref="IEncina.Send{TResponse}(IRequest{TResponse}, CancellationToken)"/>.
/// </summary>
/// <remarks>
/// <para>
/// Reflection is used once per request type, to close a typed helper over the response type; the
/// cached delegate then calls <see cref="IEncina.Send{TResponse}(IRequest{TResponse}, CancellationToken)"/>
/// directly, awaits its <see cref="ValueTask{TResult}"/> and keeps its <see cref="Either{L, R}"/>
/// outcome. Invoking <c>Send</c> through <see cref="MethodInfo.Invoke(object, object[])"/> instead
/// returns a boxed <see cref="ValueTask{TResult}"/>, which cannot be cast to <see cref="Task"/>, and
/// loses the outcome unless it is inspected by reflection.
/// </para>
/// </remarks>
internal static class RuntimeTypeRequestDispatcher
{
    private static readonly MethodInfo SendTypedMethod = typeof(RuntimeTypeRequestDispatcher)
        .GetMethod(nameof(SendTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>?> Invokers = new();

    /// <summary>
    /// Sends <paramref name="request"/> and returns its outcome with the response discarded.
    /// </summary>
    /// <param name="encina">The mediator.</param>
    /// <param name="request">The request; its runtime type must implement <see cref="IRequest{TResponse}"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right</c> when the request succeeded, the request's <c>Left</c> when it failed, or a
    /// <c>Left</c> when the runtime type does not implement <see cref="IRequest{TResponse}"/>.
    /// </returns>
    public static ValueTask<Either<EncinaError, Unit>> SendAsync(IEncina encina, object request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var invoker = Invokers.GetOrAdd(requestType, CreateInvoker);
        if (invoker is null)
        {
            return new ValueTask<Either<EncinaError, Unit>>(
                EncinaError.New($"Request type {requestType.Name} does not implement IRequest<TResponse>"));
        }

        return invoker(encina, request, cancellationToken);
    }

    private static Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>? CreateInvoker(Type requestType)
    {
        var requestInterface = Array.Find(
            requestType.GetInterfaces(),
            i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (requestInterface is null)
        {
            return null;
        }

        return SendTypedMethod
            .MakeGenericMethod(requestInterface.GetGenericArguments()[0])
            .CreateDelegate<Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>>();
    }

    private static async ValueTask<Either<EncinaError, Unit>> SendTypedAsync<TResponse>(
        IEncina encina,
        object request,
        CancellationToken cancellationToken)
    {
        var outcome = await encina.Send((IRequest<TResponse>)request, cancellationToken).ConfigureAwait(false);
        return outcome.Map(static _ => Unit.Default);
    }
}
