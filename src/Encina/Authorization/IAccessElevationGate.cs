using LanguageExt;

namespace Encina;

/// <summary>
/// Lets an authorization gate offer a definite denial to an emergency-access mechanism (for
/// example break-the-glass) before the denial is returned to the caller.
/// </summary>
/// <remarks>
/// <para>
/// <b>Contract.</b> A gate calls <see cref="TryElevateAsync{TRequest}"/> only when it is about to
/// return a <i>definite</i> denial (see <see cref="AccessElevationDenials.IsOverridable"/>); it never
/// offers an unauthenticated, indeterminate, error or missing-context outcome, and it never calls
/// the method on the allow path.
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Option{A}.None"/> keeps the denial.</description></item>
/// <item><description><c>Left</c> replaces the denial (for example when the audit of the elevated access failed: the gate fails closed).</description></item>
/// <item><description><c>Some</c> lets the request continue under the returned <see cref="AccessElevation"/>.</description></item>
/// </list>
/// <para>
/// The default implementation registered by <c>AddEncina</c> always answers <c>None</c>, so an
/// application that does not use an elevation package pays one interface call on a path that already
/// denies.
/// </para>
/// </remarks>
public interface IAccessElevationGate
{
    /// <summary>
    /// Offers a definite denial to the elevation mechanism.
    /// </summary>
    /// <typeparam name="TRequest">The request type that was denied.</typeparam>
    /// <param name="request">The denied request.</param>
    /// <param name="context">The request context (identity, tenant, correlation id).</param>
    /// <param name="denial">The definite denial the gate is about to return.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(Some(elevation))</c> to let the request continue, <c>Right(None)</c> to keep the
    /// denial, or <c>Left</c> to replace the denial with another error.
    /// </returns>
    ValueTask<Either<EncinaError, Option<AccessElevation>>> TryElevateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        EncinaError denial,
        CancellationToken cancellationToken)
        where TRequest : notnull;
}
