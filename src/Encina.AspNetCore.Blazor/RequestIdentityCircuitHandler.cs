using LanguageExt;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Encina.AspNetCore.Blazor;

/// <summary>
/// Binds the request identity of a Blazor Server circuit, one inbound scope per circuit activity
/// (a UI event, a JavaScript interop call), from the circuit's current authentication state.
/// </summary>
/// <remarks>
/// <para>
/// The circuit's connection runs under the anonymous connection marker of <c>UseEncinaContext()</c>,
/// so outside an activity code reads the anonymous identity. Each activity reads
/// <see cref="AuthenticationStateProvider.GetAuthenticationStateAsync"/> and runs inside an inbound
/// scope built from that principal, so a changed authentication state applies at the next activity.
/// </para>
/// <para>
/// When the scope is refused for a host reason (<c>ScopeConflict</c>: the circuit flow carries an inbound
/// or user context it should not, such as the first long poll of a misordered pipeline;
/// <c>UnsupportedAccessor</c>; the factory has logged the error code), the activity still runs, never
/// under the connection's identity: it runs inside an anonymous masking scope that keeps the refusals of
/// the context it hides.
/// </para>
/// <para>
/// Every other refusal fails closed (a mask-list, so a code added later refuses too): an authenticated
/// principal with no usable subject (<c>MissingSubjectClaim</c>, <c>MalformedSubjectClaim</c>,
/// <c>ReservedSubjectClaim</c>, <c>ConflictingSubjectClaims</c>) or with conflicting tenant claims
/// (<c>InboundTenantConflict</c>) never runs the activity as anonymous. The handler does not call the
/// next handler and throws an <see cref="InvalidOperationException"/> whose fixed text carries only the
/// error code (never a claim value or the error message); the factory has logged Warning 167. Blazor
/// reports an exception from an inbound activity handler as an unhandled circuit exception: it notifies
/// the client, which shows the error UI, and terminates the circuit. A null or unauthenticated principal
/// is not a refusal: its activities keep running anonymous.
/// </para>
/// <para>
/// Registered per circuit (scoped) by <c>AddEncinaBlazorAuthorization()</c>.
/// </para>
/// </remarks>
internal sealed class RequestIdentityCircuitHandler : CircuitHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IInternalRequestContextScopeFactory _scopes;
    private readonly string _correlationId = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestIdentityCircuitHandler"/> class.
    /// </summary>
    /// <param name="authenticationStateProvider">The circuit's authentication state.</param>
    /// <param name="scopes">The scope factory (the internal host-adapter members).</param>
    public RequestIdentityCircuitHandler(
        AuthenticationStateProvider authenticationStateProvider,
        IInternalRequestContextScopeFactory scopes)
    {
        ArgumentNullException.ThrowIfNull(authenticationStateProvider);
        ArgumentNullException.ThrowIfNull(scopes);

        _authenticationStateProvider = authenticationStateProvider;
        _scopes = scopes;
    }

    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return activity => RunActivityAsync(activity, next);
    }

    // No ConfigureAwait(false): the activity must keep running on the circuit's synchronization context.
    private async Task RunActivityAsync(CircuitInboundActivityContext activity, Func<CircuitInboundActivityContext, Task> next)
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(true);
        var info = InboundRequestInfo.ForCircuit(state.User, _correlationId);
        var result = await _scopes.RunHostInboundAsync(info, (_, _) => RunNextAsync(activity, next), CancellationToken.None).ConfigureAwait(true);
        if (result.IsRight)
        {
            return;
        }

        var code = result.Match(_ => string.Empty, static error => error.GetCode().IfNone(string.Empty));
        if (!IsHostRefusal(code))
        {
            throw Refusal(code);
        }

        var masked = await _scopes.RunAnonymousMarkerAsync(AnonymousMarker.Mask, _ => next(activity), CancellationToken.None).ConfigureAwait(true);
        if (masked.IsLeft)
        {
            // Only an unsupported accessor refuses a mask, and it already failed startup.
            throw new InvalidOperationException("The circuit activity could not run inside an identity scope; check the request identity registration.");
        }
    }

    // The mask-list: only the host refusals keep the mask-and-run. Any other code, including one the
    // factory adds later, refuses the activity (fail closed).
    private static bool IsHostRefusal(string code) =>
        code is RequestIdentityErrorCodes.ScopeConflict or RequestIdentityErrorCodes.UnsupportedAccessor;

    // Fixed text with the error code only: never a claim value, never EncinaError.Message.
    private static InvalidOperationException Refusal(string code) =>
        new($"The circuit activity was refused: the authenticated principal cannot open an identity scope ({code}).");

    private static async Task<Either<EncinaError, Unit>> RunNextAsync(CircuitInboundActivityContext activity, Func<CircuitInboundActivityContext, Task> next)
    {
        await next(activity).ConfigureAwait(true);
        return Unit.Default;
    }
}
