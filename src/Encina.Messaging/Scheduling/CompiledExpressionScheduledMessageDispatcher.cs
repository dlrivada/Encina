using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Scheduling;

/// <summary>
/// Default <see cref="IScheduledMessageDispatcher"/> implementation that builds and caches
/// a compiled delegate per request <see cref="Type"/> via
/// <see cref="System.Linq.Expressions.Expression"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Performance</b>: After the first call per type, dispatch is a single virtual call
/// through the cached compiled delegate plus one <see cref="IEncina.Send{TResponse}"/>
/// or <see cref="IEncina.Publish{TNotification}"/> call. There is zero
/// <see cref="MethodInfo.Invoke"/>, zero <c>dynamic</c>, and zero boxing of arguments at
/// the dispatch site.
/// </para>
/// <para>
/// <b>Cache</b>: a static <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// request <see cref="Type"/>. Entries are never evicted — the number of distinct
/// scheduled request types is bounded by the application's message model. The cache is
/// shared across all instances (scoped or otherwise) of this dispatcher.
/// </para>
/// <para>
/// <b>AOT/Trimmer</b>: the type is annotated with
/// <see cref="DynamicallyAccessedMembersAttribute"/> so the IL trimmer preserves the
/// interfaces and methods needed by the expression builder. For full NativeAOT
/// compatibility without any runtime code generation, a source-generator-based
/// dispatcher can be provided in a follow-up package as a drop-in replacement.
/// </para>
/// <para>
/// <b>Failure signalling</b>: follows Railway Oriented Programming. Unknown request
/// shapes (neither <see cref="IRequest{TResponse}"/> nor <see cref="INotification"/>)
/// are cached as a constant <c>Left</c>-returning delegate so the failure path is also
/// reflection-free on subsequent calls.
/// </para>
/// </remarks>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
public sealed class CompiledExpressionScheduledMessageDispatcher : IScheduledMessageDispatcher
{
    /// <summary>
    /// Cached compiled delegates keyed by concrete request type.
    /// Shared across all instances — thread-safe by <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>> DelegateCache = new();

    private readonly IEncina _encina;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledExpressionScheduledMessageDispatcher"/> class.
    /// </summary>
    /// <param name="encina">
    /// The <see cref="IEncina"/> instance used to dispatch requests and notifications.
    /// Typically resolved from a DI scope so that scoped pipeline behaviors and handlers
    /// participate correctly.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encina"/> is <see langword="null"/>.
    /// </exception>
    public CompiledExpressionScheduledMessageDispatcher(IEncina encina)
    {
        ArgumentNullException.ThrowIfNull(encina);
        _encina = encina;
    }

    /// <summary>
    /// Gets the number of compiled delegates currently cached.
    /// </summary>
    /// <remarks>
    /// Exposed for unit tests that need to verify cache hit behavior.
    /// The cache is static and shared across all instances.
    /// </remarks>
    internal static int CacheCount => DelegateCache.Count;

    /// <summary>
    /// Clears the static delegate cache. Intended for test isolation only.
    /// </summary>
    internal static void ClearCache() => DelegateCache.Clear();

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> DispatchAsync(
        Type requestType,
        object request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(request);

        var dispatcher = DelegateCache.GetOrAdd(requestType, BuildDispatchDelegate);
        return dispatcher(_encina, request, cancellationToken);
    }

    /// <summary>
    /// Builds and compiles a dispatch delegate for the given request type.
    /// Called once per type, then cached.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2060:MakeGenericMethod",
        Justification = "The generic type arguments come from the user's application assembly and are preserved by the [DynamicallyAccessedMembers] annotation on this class.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "Expression.Lambda compilation is used for dispatch delegate caching. For full AOT, a source-generator-based dispatcher can replace this implementation.")]
    private static Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>> BuildDispatchDelegate(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type requestType)
    {
        // 1. Try INotification first (simpler — Publish already returns Either<EncinaError, Unit>)
        if (typeof(INotification).IsAssignableFrom(requestType))
        {
            return BuildPublishDelegate(requestType);
        }

        // 2. Try IRequest<TResponse>
        var requestInterface = FindRequestInterface(requestType);
        if (requestInterface is not null)
        {
            var responseType = requestInterface.GetGenericArguments()[0];
            return BuildSendDelegate(requestType, requestInterface, responseType);
        }

        // 3. Unknown shape — return a cached constant Left delegate
        var error = EncinaErrors.Create(
            SchedulingErrorCodes.UnknownRequestShape,
            $"Type {requestType.FullName} implements neither IRequest<TResponse> nor INotification.");
        Either<EncinaError, Unit> leftResult = error;
        return (_, _, _) => new ValueTask<Either<EncinaError, Unit>>(leftResult);
    }

    /// <summary>
    /// Builds a compiled delegate for <see cref="IEncina.Publish{TNotification}"/>.
    /// </summary>
    /// <remarks>
    /// Produces:
    /// <code>
    /// (IEncina encina, object req, CancellationToken ct) =>
    ///     encina.Publish&lt;TNotification&gt;((TNotification)req, ct)
    /// </code>
    /// </remarks>
    private static Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>> BuildPublishDelegate(Type notificationType)
    {
        // Parameters
        var encinaParam = Expression.Parameter(typeof(IEncina), "encina");
        var requestParam = Expression.Parameter(typeof(object), "req");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        // (TNotification)req
        var castRequest = Expression.Convert(requestParam, notificationType);

        // encina.Publish<TNotification>((TNotification)req, ct)
        var publishMethod = typeof(IEncina)
            .GetMethod(nameof(IEncina.Publish))!
            .MakeGenericMethod(notificationType);

        var callPublish = Expression.Call(encinaParam, publishMethod, castRequest, ctParam);

        // Compile: (encina, req, ct) => encina.Publish<T>((T)req, ct)
        var lambda = Expression.Lambda<Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>>(
            callPublish, encinaParam, requestParam, ctParam);

        return lambda.Compile();
    }

    /// <summary>
    /// Builds a compiled delegate for <see cref="IEncina.Send{TResponse}"/> with
    /// result mapping from <c>Either&lt;EncinaError, TResponse&gt;</c> to
    /// <c>Either&lt;EncinaError, Unit&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Produces:
    /// <code>
    /// (IEncina encina, object req, CancellationToken ct) =>
    ///     MapSendResultAsync&lt;TResponse&gt;(encina.Send&lt;TResponse&gt;((IRequest&lt;TResponse&gt;)req, ct))
    /// </code>
    /// </remarks>
    private static Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>> BuildSendDelegate(
        Type requestType, Type requestInterface, Type responseType)
    {
        // Parameters
        var encinaParam = Expression.Parameter(typeof(IEncina), "encina");
        var requestParam = Expression.Parameter(typeof(object), "req");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        // (IRequest<TResponse>)req
        var castRequest = Expression.Convert(requestParam, requestInterface);

        // encina.Send<TResponse>((IRequest<TResponse>)req, ct)
        var sendMethod = typeof(IEncina)
            .GetMethod(nameof(IEncina.Send))!
            .MakeGenericMethod(responseType);

        var callSend = Expression.Call(encinaParam, sendMethod, castRequest, ctParam);

        // MapSendResultAsync<TResponse>(encina.Send<TResponse>(...))
        var mapMethod = typeof(CompiledExpressionScheduledMessageDispatcher)
            .GetMethod(nameof(MapSendResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(responseType);

        var callMap = Expression.Call(mapMethod, callSend);

        // Compile: (encina, req, ct) => MapSendResultAsync<TResponse>(encina.Send<TResponse>((IRequest<TResponse>)req, ct))
        var lambda = Expression.Lambda<Func<IEncina, object, CancellationToken, ValueTask<Either<EncinaError, Unit>>>>(
            callMap, encinaParam, requestParam, ctParam);

        return lambda.Compile();
    }

    /// <summary>
    /// Finds the <c>IRequest&lt;TResponse&gt;</c> interface on the given type.
    /// </summary>
    private static Type? FindRequestInterface(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type requestType)
    {
        return System.Array.Find(
            requestType.GetInterfaces(),
            i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    /// <summary>
    /// Async helper that converts <c>Either&lt;EncinaError, TResponse&gt;</c> to
    /// <c>Either&lt;EncinaError, Unit&gt;</c>. Referenced from the compiled expression
    /// tree via <see cref="System.Reflection.MethodInfo"/>.
    /// </summary>
    /// <typeparam name="TResponse">The original response type from <see cref="IEncina.Send{TResponse}"/>.</typeparam>
    /// <param name="sendTask">The task returned by <see cref="IEncina.Send{TResponse}"/>.</param>
    /// <returns>
    /// <c>Right(Unit.Default)</c> when the send succeeded (discards the response value),
    /// or the original <c>Left(EncinaError)</c> when it failed.
    /// </returns>
    private static async ValueTask<Either<EncinaError, Unit>> MapSendResultAsync<TResponse>(
        ValueTask<Either<EncinaError, TResponse>> sendTask)
    {
        var result = await sendTask.ConfigureAwait(false);
        return result.Match<Either<EncinaError, Unit>>(
            Right: _ => Unit.Default,
            Left: err => err);
    }
}
