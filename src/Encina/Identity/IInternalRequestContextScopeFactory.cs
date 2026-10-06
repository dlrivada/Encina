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
}
