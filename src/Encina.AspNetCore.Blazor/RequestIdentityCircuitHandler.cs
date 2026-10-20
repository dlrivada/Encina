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
/// When the scope is refused (the circuit flow carries an inbound or user context it should not,
/// such as the first long poll of a misordered pipeline; the factory has logged the error code), the
/// activity still runs, never under the connection's identity: it runs inside an anonymous masking
/// scope that keeps the refusals of the context it hides.
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

        var masked = await _scopes.RunAnonymousMarkerAsync(AnonymousMarker.Mask, _ => next(activity), CancellationToken.None).ConfigureAwait(true);
        if (masked.IsLeft)
        {
            // Only an unsupported accessor refuses a mask, and it already failed startup.
            throw new InvalidOperationException("The circuit activity could not run inside an identity scope; check the request identity registration.");
        }
    }

    private static async Task<Either<EncinaError, Unit>> RunNextAsync(CircuitInboundActivityContext activity, Func<CircuitInboundActivityContext, Task> next)
    {
        await next(activity).ConfigureAwait(true);
        return Unit.Default;
    }
}
