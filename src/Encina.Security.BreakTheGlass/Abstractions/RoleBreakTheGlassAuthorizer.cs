using LanguageExt;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// The default <see cref="IBreakTheGlassOpenAuthorizer"/>: roles and permissions configured per scope.
/// </summary>
/// <remarks>
/// <para>Fail-closed: a scope with no configured opener, an undeclared scope, an anonymous caller
/// and a service identity (an emergency is a human decision) are all refused.</para>
/// <para>A reviewer needs a role in <see cref="BreakTheGlassOptions.ReviewerRoles"/> and must not be
/// the user who opened the elevation.</para>
/// </remarks>
public sealed class RoleBreakTheGlassAuthorizer : IBreakTheGlassOpenAuthorizer
{
    private readonly IOptions<BreakTheGlassOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleBreakTheGlassAuthorizer"/> class.
    /// </summary>
    /// <param name="options">The break-the-glass options.</param>
    public RoleBreakTheGlassAuthorizer(IOptions<BreakTheGlassOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> AuthorizeOpenAsync(RequestIdentity identity, string scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(scope);

        var allowed = identity.Kind == IdentityKind.User
            && _options.Value.Scopes.TryGetValue(scope, out var scopeOptions)
            && (scopeOptions.OpenerRoles.Overlaps(identity.Roles) || scopeOptions.OpenerPermissions.Overlaps(identity.Permissions));

        return Result(allowed, BreakTheGlassErrors.NotEligible);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> AuthorizeReviewAsync(RequestIdentity identity, BreakTheGlassElevation elevation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(elevation);

        if (identity.Kind != IdentityKind.User || !_options.Value.ReviewerRoles.Overlaps(identity.Roles))
        {
            return Result(false, BreakTheGlassErrors.ReviewerNotEligible);
        }

        return Result(!string.Equals(identity.UserId, elevation.UserId, StringComparison.Ordinal), BreakTheGlassErrors.SelfReview);
    }

    private static ValueTask<Either<EncinaError, Unit>> Result(bool allowed, Func<EncinaError> refusal) =>
        ValueTask.FromResult<Either<EncinaError, Unit>>(allowed ? Right(unit) : Left(refusal()));
}
