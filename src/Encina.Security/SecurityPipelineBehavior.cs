using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Security.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security;

/// <summary>
/// Pipeline behavior that enforces security requirements declared via security attributes.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior inspects the request type for <see cref="SecurityAttribute"/> subclasses
/// and evaluates them in order:
/// <list type="number">
/// <item><description><see cref="AllowAnonymousAttribute"/> — bypasses all checks</description></item>
/// <item><description><see cref="DenyAnonymousAttribute"/> — authentication gate</description></item>
/// <item><description><see cref="RequireRoleAttribute"/> — OR role check</description></item>
/// <item><description><see cref="RequireAllRolesAttribute"/> — AND role check</description></item>
/// <item><description><see cref="RequirePermissionAttribute"/> — permission check via <see cref="IPermissionEvaluator"/></description></item>
/// <item><description><see cref="RequireClaimAttribute"/> — claim check against principal</description></item>
/// <item><description><see cref="RequireOwnershipAttribute"/> — resource ownership via <see cref="IResourceOwnershipEvaluator"/></description></item>
/// </list>
/// </para>
/// <para>
/// The behavior short-circuits on the first failed check and returns an appropriate
/// <see cref="EncinaError"/> via <see cref="SecurityErrors"/>.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Security</c> ActivitySource,
/// metrics via <c>Encina.Security</c> Meter, and structured log messages via <see cref="ILogger"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Request with multiple security attributes
/// [DenyAnonymous]
/// [RequirePermission("orders:read")]
/// [RequireOwnership("OwnerId")]
/// public sealed record GetOrderQuery(Guid OrderId, string OwnerId) : IQuery&lt;OrderDto&gt;;
/// </code>
/// </example>
public sealed class SecurityPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISecurityContextAccessor _securityContextAccessor;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IResourceOwnershipEvaluator _ownershipEvaluator;
    private readonly SecurityOptions _options;
    private readonly ILogger<SecurityPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="securityContextAccessor">Accessor to get the current security context.</param>
    /// <param name="permissionEvaluator">Evaluator for permission checks.</param>
    /// <param name="ownershipEvaluator">Evaluator for resource ownership checks.</param>
    /// <param name="options">Security configuration options.</param>
    /// <param name="logger">Logger for structured security logging.</param>
    public SecurityPipelineBehavior(
        ISecurityContextAccessor securityContextAccessor,
        IPermissionEvaluator permissionEvaluator,
        IResourceOwnershipEvaluator ownershipEvaluator,
        IOptions<SecurityOptions> options,
        ILogger<SecurityPipelineBehavior<TRequest, TResponse>> logger)
    {
        _securityContextAccessor = securityContextAccessor;
        _permissionEvaluator = permissionEvaluator;
        _ownershipEvaluator = ownershipEvaluator;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.Name;
        var startedAt = Stopwatch.GetTimestamp();

        var securityAttributes = DiscoverAttributes(requestType, requestTypeName);

        // [AllowAnonymous], or no attributes and no authentication required by default: proceed
        if (securityAttributes is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Start tracing and logging
        using var activity = SecurityDiagnostics.StartAuthorize(requestTypeName);
        _logger.AuthorizationStarted(requestTypeName, securityAttributes.Count);

        var denial = await AuthorizeAsync(securityAttributes, request, requestType, activity, startedAt, cancellationToken).ConfigureAwait(false);

        if (denial.HasValue)
        {
            return Left<EncinaError, TResponse>(denial.Value); // NOSONAR S6966
        }

        return await nextStep().ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the security attributes to evaluate, ordered by <c>Order</c>, or <c>null</c> when the request
    /// bypasses the gate (<c>[AllowAnonymous]</c>, or no attributes and no default authentication requirement).
    /// </summary>
    private List<SecurityAttribute>? DiscoverAttributes(Type requestType, string requestTypeName)
    {
        // Check for [AllowAnonymous] first - bypasses all security checks
        if (requestType.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0)
        {
            _logger.AllowAnonymousBypass(requestTypeName);
            return null;
        }

        var securityAttributes = requestType
            .GetCustomAttributes(typeof(SecurityAttribute), inherit: true)
            .Cast<SecurityAttribute>()
            .OrderBy(a => a.Order)
            .ThenBy(a => GetAttributeTypePriority(a))
            .ToList();

        return securityAttributes.Count == 0 && !_options.RequireAuthenticatedByDefault ? null : securityAttributes;
    }

    /// <summary>
    /// Evaluates the request against the security context, records the outcome and returns the denial error,
    /// or <c>null</c> when access is allowed.
    /// </summary>
    private async ValueTask<EncinaError?> AuthorizeAsync(
        List<SecurityAttribute> securityAttributes,
        TRequest request,
        Type requestType,
        Activity? activity,
        long startedAt,
        CancellationToken cancellationToken)
    {
        var requestTypeName = requestType.Name;
        var securityContext = _securityContextAccessor.SecurityContext;

        if (securityContext is null)
        {
            var missing = DenyMissingContext(securityAttributes, requestType, activity, startedAt);
            if (missing.HasValue)
            {
                return missing;
            }

            // Treat as anonymous
            securityContext = SecurityContext.Anonymous;
        }

        SecurityDiagnostics.SetUserId(activity, securityContext.UserId);

        // If requiring auth by default and no attributes, enforce authentication
        if (securityAttributes.Count == 0)
        {
            return AuthorizeByDefault(securityContext, requestType, activity, startedAt);
        }

        // Evaluate each security attribute in order
        var denial = await EvaluateAttributesAsync(securityAttributes, securityContext, request, requestType, activity, cancellationToken).ConfigureAwait(false);

        if (denial is { } denied)
        {
            RecordDenied(activity, startedAt, requestTypeName, GetDenialCode(denied.Attribute), securityContext.UserId);
            return denied.Error;
        }

        RecordAllowed(activity, startedAt, requestTypeName, securityContext.UserId);
        return null;
    }

    private EncinaError? DenyMissingContext(
        List<SecurityAttribute> securityAttributes,
        Type requestType,
        Activity? activity,
        long startedAt)
    {
        _logger.MissingSecurityContext(requestType.Name);

        if (!_options.ThrowOnMissingSecurityContext || (securityAttributes.Count == 0 && !_options.RequireAuthenticatedByDefault))
        {
            return null;
        }

        RecordDenied(activity, startedAt, requestType.Name, EncinaErrorCodes.AuthorizationUnauthenticated, null);
        return SecurityErrors.MissingContext(requestType);
    }

    private EncinaError? AuthorizeByDefault(
        ISecurityContext securityContext,
        Type requestType,
        Activity? activity,
        long startedAt)
    {
        if (!securityContext.IsAuthenticated)
        {
            RecordDenied(activity, startedAt, requestType.Name, EncinaErrorCodes.AuthorizationUnauthenticated, securityContext.UserId);
            return SecurityErrors.Unauthenticated(requestType);
        }

        RecordAllowed(activity, startedAt, requestType.Name, securityContext.UserId);
        return null;
    }

    private async ValueTask<(EncinaError Error, SecurityAttribute Attribute)?> EvaluateAttributesAsync(
        List<SecurityAttribute> securityAttributes,
        ISecurityContext securityContext,
        TRequest request,
        Type requestType,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        foreach (var attribute in securityAttributes)
        {
            var result = await EvaluateAttributeAsync(attribute, securityContext, request, requestType, cancellationToken).ConfigureAwait(false);

            SecurityDiagnostics.RecordAttributeEvaluated(activity, attribute.GetType().Name);

            if (result.HasValue)
            {
                return (result.Value, attribute);
            }
        }

        return null;
    }

    // crap-exempt: single-question switch — dispatches one security attribute to its evaluator
    private ValueTask<EncinaError?> EvaluateAttributeAsync(
        SecurityAttribute attribute,
        ISecurityContext securityContext,
        TRequest request,
        Type requestType,
        CancellationToken cancellationToken) => attribute switch
        {
            DenyAnonymousAttribute => new ValueTask<EncinaError?>(EvaluateDenyAnonymous(securityContext, requestType)),
            RequireRoleAttribute requireRole => new ValueTask<EncinaError?>(EvaluateRequireRole(securityContext, requireRole, requestType)),
            RequireAllRolesAttribute requireAllRoles => new ValueTask<EncinaError?>(EvaluateRequireAllRoles(securityContext, requireAllRoles, requestType)),
            RequirePermissionAttribute requirePermission => EvaluateRequirePermissionAsync(securityContext, requirePermission, requestType, cancellationToken),
            RequireClaimAttribute requireClaim => new ValueTask<EncinaError?>(EvaluateRequireClaim(securityContext, requireClaim, requestType)),
            RequireOwnershipAttribute requireOwnership => EvaluateRequireOwnershipAsync(securityContext, requireOwnership, request, requestType, cancellationToken),
            _ => new ValueTask<EncinaError?>((EncinaError?)null)
        };

    private void RecordAllowed(Activity? activity, long startedAt, string requestTypeName, string? userId)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { SecurityDiagnostics.TagRequestType, requestTypeName }
        };

        SecurityDiagnostics.AuthorizationTotal.Add(1, tags);
        SecurityDiagnostics.AuthorizationAllowed.Add(1, tags);
        SecurityDiagnostics.AuthorizationDuration.Record(elapsed.TotalMilliseconds, tags);
        SecurityDiagnostics.RecordAllowed(activity);
        _logger.AuthorizationAllowed(userId, requestTypeName);
    }

    private void RecordDenied(Activity? activity, long startedAt, string requestTypeName, string denialReason, string? userId)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var tags = new TagList
        {
            { SecurityDiagnostics.TagRequestType, requestTypeName },
            { SecurityDiagnostics.TagDenialReason, denialReason }
        };

        SecurityDiagnostics.AuthorizationTotal.Add(1, tags);
        SecurityDiagnostics.AuthorizationDenied.Add(1, tags);
        SecurityDiagnostics.AuthorizationDuration.Record(elapsed.TotalMilliseconds, tags);
        SecurityDiagnostics.RecordDenied(activity, denialReason);
        _logger.AuthorizationDenied(userId, requestTypeName, denialReason);
    }

    private static EncinaError? EvaluateDenyAnonymous(ISecurityContext securityContext, Type requestType)
    {
        if (!securityContext.IsAuthenticated)
        {
            return SecurityErrors.Unauthenticated(requestType);
        }

        return null;
    }

    private static EncinaError? EvaluateRequireRole(
        ISecurityContext securityContext,
        RequireRoleAttribute attribute,
        Type requestType)
    {
        if (attribute.Roles.Length == 0)
        {
            return null;
        }

        var hasAnyRole = attribute.Roles.Any(role => securityContext.Roles.Contains(role));
        if (!hasAnyRole)
        {
            return SecurityErrors.InsufficientRoles(requestType, attribute.Roles, securityContext.UserId);
        }

        return null;
    }

    private static EncinaError? EvaluateRequireAllRoles(
        ISecurityContext securityContext,
        RequireAllRolesAttribute attribute,
        Type requestType)
    {
        if (attribute.Roles.Length == 0)
        {
            return null;
        }

        var hasAllRoles = attribute.Roles.All(role => securityContext.Roles.Contains(role));
        if (!hasAllRoles)
        {
            return SecurityErrors.InsufficientRoles(requestType, attribute.Roles, securityContext.UserId, requireAll: true);
        }

        return null;
    }

    private async ValueTask<EncinaError?> EvaluateRequirePermissionAsync(
        ISecurityContext securityContext,
        RequirePermissionAttribute attribute,
        Type requestType,
        CancellationToken cancellationToken)
    {
        if (attribute.Permissions.Length == 0)
        {
            return null;
        }

        bool hasPermission;
        if (attribute.RequireAll)
        {
            hasPermission = await _permissionEvaluator
                .HasAllPermissionsAsync(securityContext, attribute.Permissions, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            hasPermission = await _permissionEvaluator
                .HasAnyPermissionAsync(securityContext, attribute.Permissions, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!hasPermission)
        {
            return SecurityErrors.PermissionDenied(requestType, attribute.Permissions, securityContext.UserId, attribute.RequireAll);
        }

        return null;
    }

    private static EncinaError? EvaluateRequireClaim(
        ISecurityContext securityContext,
        RequireClaimAttribute attribute,
        Type requestType)
    {
        if (securityContext.User is null)
        {
            return SecurityErrors.ClaimMissing(requestType, attribute.ClaimType, attribute.ClaimValue, securityContext.UserId);
        }

        if (attribute.ClaimValue is null)
        {
            // Check claim type existence only
            var hasClaim = securityContext.User.HasClaim(c => c.Type == attribute.ClaimType);
            if (!hasClaim)
            {
                return SecurityErrors.ClaimMissing(requestType, attribute.ClaimType, null, securityContext.UserId);
            }
        }
        else
        {
            // Check claim type with exact value
            var hasClaimWithValue = securityContext.User.HasClaim(attribute.ClaimType, attribute.ClaimValue);
            if (!hasClaimWithValue)
            {
                return SecurityErrors.ClaimMissing(requestType, attribute.ClaimType, attribute.ClaimValue, securityContext.UserId);
            }
        }

        return null;
    }

    private async ValueTask<EncinaError?> EvaluateRequireOwnershipAsync(
        ISecurityContext securityContext,
        RequireOwnershipAttribute attribute,
        TRequest request,
        Type requestType,
        CancellationToken cancellationToken)
    {
        var isOwner = await _ownershipEvaluator
            .IsOwnerAsync(securityContext, request, attribute.OwnerProperty, cancellationToken)
            .ConfigureAwait(false);

        if (!isOwner)
        {
            return SecurityErrors.NotOwner(requestType, attribute.OwnerProperty, securityContext.UserId);
        }

        return null;
    }

    /// <summary>
    /// Maps a security attribute to its corresponding <see cref="EncinaErrorCodes"/> authorization code.
    /// </summary>
    // crap-exempt: single-question switch — maps one security attribute to its denial code
    private static string GetDenialCode(SecurityAttribute attribute) => attribute switch
    {
        DenyAnonymousAttribute => EncinaErrorCodes.AuthorizationUnauthenticated,
        RequireRoleAttribute => EncinaErrorCodes.AuthorizationInsufficientRoles,
        RequireAllRolesAttribute => EncinaErrorCodes.AuthorizationInsufficientRoles,
        RequirePermissionAttribute => EncinaErrorCodes.AuthorizationPermissionDenied,
        RequireClaimAttribute => EncinaErrorCodes.AuthorizationClaimMissing,
        RequireOwnershipAttribute => EncinaErrorCodes.AuthorizationNotOwner,
        _ => EncinaErrorCodes.AuthorizationForbidden
    };

    /// <summary>
    /// Returns a priority value for attribute types to enforce consistent evaluation order
    /// when multiple attributes share the same <see cref="SecurityAttribute.Order"/> value.
    /// </summary>
    private static int GetAttributeTypePriority(SecurityAttribute attribute) => attribute switch
    {
        DenyAnonymousAttribute => 0,
        RequireRoleAttribute => 1,
        RequireAllRolesAttribute => 2,
        RequirePermissionAttribute => 3,
        RequireClaimAttribute => 4,
        RequireOwnershipAttribute => 5,
        _ => 99
    };
}
