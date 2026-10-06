using System.Collections.Concurrent;
using System.Security.Claims;
using Encina.AspNetCore.Authorization;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore;

/// <summary>
/// Pipeline behavior that enforces authorization using ASP.NET Core's authorization system.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior checks for authorization attributes on the request type and enforces
/// authorization using ASP.NET Core's <see cref="IAuthorizationService"/>.
/// </para>
/// <para>
/// Supports:
/// <list type="bullet">
/// <item><description><b>Role-based authorization</b>: <c>[Authorize(Roles = "Admin")]</c></description></item>
/// <item><description><b>Policy-based authorization</b>: <c>[Authorize(Policy = "RequireElevation")]</c></description></item>
/// <item><description><b>Resource-based authorization</b>: <c>[ResourceAuthorize("PolicyName")]</c> — the request is passed as the resource</description></item>
/// <item><description><b>Multiple attributes</b>: All must pass (AND logic)</description></item>
/// <item><description><b>Allow anonymous</b>: <c>[AllowAnonymous]</c> bypasses all authorization</description></item>
/// <item><description><b>CQRS default policies</b>: Automatic policy application when <see cref="AuthorizationConfiguration.AutoApplyPolicies"/> is enabled</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Caller.</b> The behavior evaluates the request identity of the dispatch,
/// <see cref="IRequestContext.Identity"/>: its <see cref="RequestIdentity.Principal"/>, which holds
/// only the caller's authenticated identities. A request that needs authorization is denied with
/// <see cref="EncinaErrorCodes.AuthorizationUnauthenticated"/> when the identity is not authenticated,
/// including a token that the claim map turned into the anonymous identity (no subject, a reserved
/// <c>service:</c> subject). Over HTTP the identity is bound by <c>app.UseEncinaContext()</c>; in a
/// Blazor Server circuit by <c>AddEncinaBlazorAuthorization()</c>; in background work by the scope
/// factory (<see cref="IRequestContextScopeFactory"/>).
/// </para>
/// <para>
/// Logs and error details record the identity kind, never the user id.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Require authentication
/// [Authorize]
/// public record DeleteUserCommand(int UserId) : ICommand&lt;Unit&gt;;
///
/// // Require specific role
/// [Authorize(Roles = "Admin")]
/// public record BanUserCommand(int UserId) : ICommand&lt;Unit&gt;;
///
/// // Require custom policy
/// [Authorize(Policy = "RequireElevation")]
/// public record TransferMoneyCommand(decimal Amount) : ICommand&lt;Receipt&gt;;
///
/// // Resource-based authorization (request is the resource)
/// [ResourceAuthorize("CanEditOrder")]
/// public record UpdateOrderCommand(OrderId Id, string NewStatus) : ICommand&lt;Order&gt;;
///
/// // Multiple requirements (both must pass)
/// [Authorize(Roles = "Admin")]
/// [Authorize(Policy = "RequireApproval")]
/// public record DeleteAccountCommand(int AccountId) : ICommand&lt;Unit&gt;;
///
/// // Opt-out of authorization (public endpoint)
/// [AllowAnonymous]
/// public record GetPublicDataQuery : IQuery&lt;PublicData&gt;;
/// </code>
/// </example>
public sealed class AuthorizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "stage";
    private const string MetadataKeyIdentityKind = "identityKind";
    private const string MetadataStageAuthorization = "authorization";

    // Cache CQRS type checks and attribute lookups to avoid repeated reflection
    private static readonly ConcurrentDictionary<Type, bool> CommandTypeCache = new();
    private static readonly ConcurrentDictionary<Type, bool> AllowAnonymousCache = new();
    private static readonly ConcurrentDictionary<Type, List<AuthorizeAttribute>> AuthorizeAttributeCache = new();
    private static readonly ConcurrentDictionary<Type, ResourceAuthorizeAttribute?> ResourceAuthorizeCache = new();

    private static readonly Type CommandOpenGeneric = typeof(ICommand<>);

    // High-performance logging delegates. They record the identity kind, never the user id.
    // Event IDs: 200-201 (see EventIdRanges.AspNetCore)
    private static readonly Action<ILogger, string, string?, IdentityKind, Exception?> LogAuthorizationSucceeded =
        LoggerMessage.Define<string, string?, IdentityKind>(
            LogLevel.Debug,
            new EventId(200, "AuthorizationSucceeded"),
            "Authorization succeeded for {RequestType}. Policy: {Policy}, IdentityKind: {IdentityKind}");

    private static readonly Action<ILogger, string, string?, IdentityKind, string, Exception?> LogAuthorizationDenied =
        LoggerMessage.Define<string, string?, IdentityKind, string>(
            LogLevel.Warning,
            new EventId(201, "AuthorizationDenied"),
            "Authorization denied for {RequestType}. Policy: {Policy}, IdentityKind: {IdentityKind}, Reason: {Reason}");

    private readonly IAuthorizationService _authorizationService;
    private readonly AuthorizationConfiguration _configuration;
    private readonly ILogger<AuthorizationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="authorizationService">The ASP.NET Core authorization service.</param>
    /// <param name="options">CQRS-aware authorization configuration.</param>
    /// <param name="logger">Logger for structured authorization diagnostics.</param>
    public AuthorizationPipelineBehavior(
        IAuthorizationService authorizationService,
        IOptions<AuthorizationConfiguration> options,
        ILogger<AuthorizationPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _authorizationService = authorizationService;
        _configuration = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        var requirements = RequirementsOf(typeof(TRequest));
        if (requirements is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        var denial = await AuthorizeAsync(request, context, requirements).ConfigureAwait(false);
        return denial is { } error
            ? Left<EncinaError, TResponse>(error)
            : await nextStep().ConfigureAwait(false);
    }

    // The denial of the caller, or null when every requirement passes (logged 200).
    private async Task<EncinaError?> AuthorizeAsync(TRequest request, IRequestContext context, Requirements requirements)
    {
        // A non-conforming context whose Identity is null reads as anonymous and is denied.
        if (context.Identity is not { IsAuthenticated: true } identity)
        {
            return Unauthenticated(typeof(TRequest), context.Identity?.Kind ?? IdentityKind.Anonymous);
        }

        var denial = await EvaluateAsync(request, identity, requirements).ConfigureAwait(false);
        if (denial is null)
        {
            LogAuthorizationSucceeded(_logger, typeof(TRequest).FullName!, requirements.EffectivePolicy, identity.Kind, null);
        }

        return denial;
    }

    // The attributes and the CQRS default policy that apply; null when the request needs no authorization.
    private Requirements? RequirementsOf(Type requestType)
    {
        if (HasAllowAnonymous(requestType))
        {
            return null;
        }

        var authorizeAttributes = GetAuthorizeAttributes(requestType);
        var resourceAuthorizeAttribute = GetResourceAuthorizeAttribute(requestType);
        var explicitRequirements = authorizeAttributes.Count > 0 || resourceAuthorizeAttribute is not null;
        var autoAppliedPolicy = explicitRequirements ? null : AutoAppliedPolicy(requestType);

        return explicitRequirements || autoAppliedPolicy is not null
            ? new Requirements(authorizeAttributes, resourceAuthorizeAttribute, autoAppliedPolicy)
            : null;
    }

    // The CQRS default policy of a request without explicit attributes, when auto-apply is on.
    private string? AutoAppliedPolicy(Type requestType)
    {
        if (!_configuration.AutoApplyPolicies)
        {
            return null;
        }

        return IsCommand(requestType) ? _configuration.DefaultCommandPolicy : _configuration.DefaultQueryPolicy;
    }

    // Every requirement must pass (AND); the first failure is the denial.
    private async Task<EncinaError?> EvaluateAsync(TRequest request, RequestIdentity identity, Requirements requirements)
    {
        // An authenticated identity built without a principal (builders only) satisfies no policy or role.
        var user = identity.Principal ?? new ClaimsPrincipal(new ClaimsIdentity());
        var denial = await EvaluateAttributesAsync(request, user, identity.Kind, requirements.AuthorizeAttributes).ConfigureAwait(false);
        if (denial is not null)
        {
            return denial;
        }

        return await EvaluatePolicyAsync(request, user, identity.Kind, requirements.ResourceAuthorizeAttribute?.Policy, "resource_authorization").ConfigureAwait(false)
            ?? await EvaluatePolicyAsync(request, user, identity.Kind, requirements.AutoAppliedPolicy, "auto_applied_policy").ConfigureAwait(false);
    }

    // Each [Authorize]: its policy, then its roles.
    private async Task<EncinaError?> EvaluateAttributesAsync(TRequest request, ClaimsPrincipal user, IdentityKind identityKind, List<AuthorizeAttribute> attributes)
    {
        foreach (var attribute in attributes)
        {
            var denial = await EvaluatePolicyAsync(request, user, identityKind, attribute.Policy, "policy").ConfigureAwait(false)
                ?? EvaluateRoles(user, identityKind, attribute.Roles);
            if (denial is not null)
            {
                return denial;
            }
        }

        return null;
    }

    private async Task<EncinaError?> EvaluatePolicyAsync(TRequest request, ClaimsPrincipal user, IdentityKind identityKind, string? policy, string requirement)
    {
        if (string.IsNullOrWhiteSpace(policy))
        {
            return null;
        }

        // The request is the resource of every policy (resource-based authorization).
        var result = await _authorizationService.AuthorizeAsync(user, request, policy).ConfigureAwait(false);
        return result.Succeeded ? (EncinaError?)null : PolicyDenied(policy, requirement, identityKind, result);
    }

    private EncinaError? EvaluateRoles(ClaimsPrincipal user, IdentityKind identityKind, string? roles)
    {
        if (string.IsNullOrWhiteSpace(roles))
        {
            return null;
        }

        var requiredRoles = roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (requiredRoles.Any(user.IsInRole))
        {
            return null;
        }

        var requestType = typeof(TRequest);
        var reason = $"User does not have any of the required roles ({string.Join(", ", requiredRoles)}) for '{requestType.Name}'.";
        LogAuthorizationDenied(_logger, requestType.FullName!, null, identityKind, reason, null);
        return EncinaErrors.Create( // NOSONAR S6966
            code: EncinaErrorCodes.AuthorizationForbidden,
            message: reason,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAuthorization,
                ["requirement"] = "roles",
                ["requiredRoles"] = requiredRoles,
                [MetadataKeyIdentityKind] = identityKind.ToString()
            });
    }

    private EncinaError Unauthenticated(Type requestType, IdentityKind identityKind)
    {
        var reason = $"Request '{requestType.Name}' requires authentication.";
        LogAuthorizationDenied(_logger, requestType.FullName!, null, identityKind, reason, null);
        return EncinaErrors.Create( // NOSONAR S6966
            code: EncinaErrorCodes.AuthorizationUnauthenticated,
            message: reason,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAuthorization,
                ["requirement"] = "authenticated",
                [MetadataKeyIdentityKind] = identityKind.ToString()
            });
    }

    // crap-exempt: single-question switch — the error code and message of each policy requirement kind.
    private static (string Code, string Reason) DescribePolicyDenial(string requirement, string policy, string requestName) => requirement switch
    {
        "resource_authorization" => (EncinaErrorCodes.AuthorizationResourceDenied,
            $"Resource authorization denied. Policy '{policy}' was not satisfied for request '{requestName}'."),
        "auto_applied_policy" => (EncinaErrorCodes.AuthorizationPolicyFailed,
            $"User does not satisfy auto-applied default policy '{policy}' for '{requestName}'."),
        _ => (EncinaErrorCodes.AuthorizationPolicyFailed,
            $"User does not satisfy policy '{policy}' required by '{requestName}'.")
    };

    private EncinaError PolicyDenied(string policy, string requirement, IdentityKind identityKind, AuthorizationResult result)
    {
        var requestType = typeof(TRequest);
        var (code, reason) = DescribePolicyDenial(requirement, policy, requestType.Name);
        LogAuthorizationDenied(_logger, requestType.FullName!, policy, identityKind, reason, null);

        var details = new Dictionary<string, object?>
        {
            [MetadataKeyRequestType] = requestType.FullName,
            [MetadataKeyStage] = MetadataStageAuthorization,
            ["requirement"] = requirement,
            ["policy"] = policy,
            [MetadataKeyIdentityKind] = identityKind.ToString(),
            ["failureReasons"] = result.Failure?.FailureReasons
                .Select(static failure => failure.Message)
                .Where(static message => !string.IsNullOrEmpty(message))
                .ToList()
        };
        if (requirement == "auto_applied_policy")
        {
            details["isCommand"] = IsCommand(requestType);
        }

        return EncinaErrors.Create(code, reason, details: details); // NOSONAR S6966
    }

    /// <summary>
    /// Determines whether the specified type implements <see cref="ICommand{TResponse}"/>.
    /// If <c>false</c>, the type is treated as a query for CQRS default policy purposes.
    /// </summary>
    private static bool IsCommand(Type requestType)
    {
        return CommandTypeCache.GetOrAdd(requestType, static type =>
            type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == CommandOpenGeneric));
    }

    private static bool HasAllowAnonymous(Type requestType)
    {
        return AllowAnonymousCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0);
    }

    private static List<AuthorizeAttribute> GetAuthorizeAttributes(Type requestType)
    {
        return AuthorizeAttributeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .ToList());
    }

    private static ResourceAuthorizeAttribute? GetResourceAuthorizeAttribute(Type requestType)
    {
        return ResourceAuthorizeCache.GetOrAdd(requestType, static type =>
            type.GetCustomAttributes(typeof(ResourceAuthorizeAttribute), inherit: true)
                .Cast<ResourceAuthorizeAttribute>()
                .FirstOrDefault());
    }

    // The requirements of one request type: the explicit attributes and the CQRS default policy.
    private sealed record Requirements(
        List<AuthorizeAttribute> AuthorizeAttributes,
        ResourceAuthorizeAttribute? ResourceAuthorizeAttribute,
        string? AutoAppliedPolicy)
    {
        public string? EffectivePolicy =>
            ResourceAuthorizeAttribute?.Policy ?? AuthorizeAttributes.FirstOrDefault()?.Policy ?? AutoAppliedPolicy;
    }
}
