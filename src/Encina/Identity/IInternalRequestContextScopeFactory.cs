using LanguageExt;

namespace Encina;

/// <summary>
/// The internal members of the scope factory, reached only by Encina packages through
/// <c>InternalsVisibleTo</c>. The same singleton implements <see cref="IRequestContextScopeFactory"/>.
/// </summary>
internal interface IInternalRequestContextScopeFactory
{
    /// <summary>
    /// Runs <paramref name="work"/> as the built-in service identity <paramref name="builtInName"/>
    /// (name prefix <c>encina.</c>, declared by an Encina package). Refused inside a user's or an
    /// inbound request, with no opt-out.
    /// </summary>
    Task<Either<EncinaError, T>> RunAsBuiltInAsync<T>(
        string builtInName,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The host-adapter twin of <see cref="IRequestContextScopeFactory.RunInboundAsync{T}"/> (request
    /// middleware, circuit handler): the same scope, logged at Debug (EventId 172) because it runs
    /// once per request or activity.
    /// </summary>
    Task<Either<EncinaError, T>> RunHostInboundAsync<T>(
        InboundRequestInfo request,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> under an anonymous marker scope (request middleware, circuit
    /// handler). Always permitted over any chain, because it only downgrades to the anonymous identity;
    /// the only refusals are the unsupported accessor and a cancelled token.
    /// </summary>
    /// <param name="marker">The marker: a connection, or a mask over the ambient context.</param>
    /// <param name="work">The work, which reads the anonymous identity.</param>
    /// <param name="cancellationToken">The cancellation token passed to <paramref name="work"/>.</param>
    /// <returns><c>Right</c> when <paramref name="work"/> ran; <c>Left</c> when the scope was refused.</returns>
    Task<Either<EncinaError, Unit>> RunAnonymousMarkerAsync(
        AnonymousMarker marker,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default);
}
