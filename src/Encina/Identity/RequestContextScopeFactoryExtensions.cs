using System.Security.Claims;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Overloads of <see cref="IRequestContextScopeFactory"/> for work that returns a plain
/// <see cref="Task"/>; the result is <see cref="Unit"/> on success or the refusal.
/// </summary>
/// <remarks>
/// The scope, its refusals and its logging are those of the underlying member. An exception thrown
/// by <c>work</c> propagates after the scope has ended.
/// </remarks>
/// <example>
/// <code>
/// var result = await scopes.RunAsServiceAsync(
///     "nightly-cleanup",
///     async (context, ct) => await cleaner.RunAsync(ct),
///     cancellationToken: ct);
/// </code>
/// </example>
public static class RequestContextScopeFactoryExtensions
{
    /// <summary>
    /// Runs <paramref name="work"/> as a declared service identity
    /// (see <see cref="IRequestContextScopeFactory.RunAsServiceAsync{T}"/>).
    /// </summary>
    /// <param name="factory">The scope factory.</param>
    /// <param name="serviceId">The declared service name.</param>
    /// <param name="work">The unit of work.</param>
    /// <param name="options">The tenant and the inbound opt-in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Unit"/> when <paramref name="work"/> completed, or the refusal.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static Task<Either<EncinaError, Unit>> RunAsServiceAsync(
        this IRequestContextScopeFactory factory,
        string serviceId,
        Func<IRequestContext, CancellationToken, Task> work,
        IdentityScopeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory.RunAsServiceAsync(serviceId, ToUnit(work), options, cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="work"/> as the identity mapped from <paramref name="principal"/>
    /// (see <see cref="IRequestContextScopeFactory.RunAsPrincipalAsync{T}"/>).
    /// </summary>
    /// <param name="factory">The scope factory.</param>
    /// <param name="principal">The principal to run as.</param>
    /// <param name="work">The unit of work.</param>
    /// <param name="options">The tenant and the inbound opt-in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Unit"/> when <paramref name="work"/> completed, or the refusal.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static Task<Either<EncinaError, Unit>> RunAsPrincipalAsync(
        this IRequestContextScopeFactory factory,
        ClaimsPrincipal principal,
        Func<IRequestContext, CancellationToken, Task> work,
        IdentityScopeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory.RunAsPrincipalAsync(principal, ToUnit(work), options, cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="work"/> as one inbound unit of work
    /// (see <see cref="IRequestContextScopeFactory.RunInboundAsync{T}"/>).
    /// </summary>
    /// <param name="factory">The scope factory.</param>
    /// <param name="request">What the entry point read from its caller.</param>
    /// <param name="work">The unit of work.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Unit"/> when <paramref name="work"/> completed, or the refusal.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static Task<Either<EncinaError, Unit>> RunInboundAsync(
        this IRequestContextScopeFactory factory,
        InboundRequestInfo request,
        Func<IRequestContext, CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory.RunInboundAsync(request, ToUnit(work), cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="work"/> as the restored originator of a deferred message
    /// (see <see cref="IRequestContextScopeFactory.RunRestoredAsync{T}"/>).
    /// </summary>
    /// <param name="factory">The scope factory.</param>
    /// <param name="persisted">The identity stored with the message.</param>
    /// <param name="source">Where the dispatcher read the message from.</param>
    /// <param name="work">The unit of work.</param>
    /// <param name="configuredTenantId">For external sources only: the tenant from trusted configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Unit"/> when <paramref name="work"/> completed, or the refusal.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static Task<Either<EncinaError, Unit>> RunRestoredAsync(
        this IRequestContextScopeFactory factory,
        PersistedRequestIdentity persisted,
        PersistedIdentitySource source,
        Func<IRequestContext, CancellationToken, Task> work,
        string? configuredTenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory.RunRestoredAsync(persisted, source, ToUnit(work), configuredTenantId, cancellationToken);
    }

    private static Func<IRequestContext, CancellationToken, Task<Either<EncinaError, Unit>>> ToUnit(
        Func<IRequestContext, CancellationToken, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        return async (context, cancellationToken) =>
        {
            await work(context, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, Unit>(unit);
        };
    }
}
