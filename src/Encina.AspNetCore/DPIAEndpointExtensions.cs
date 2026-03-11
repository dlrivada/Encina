using System.Diagnostics;

using Encina.Compliance.DPIA;

using LanguageExt;
using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.Model;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.AspNetCore;

/// <summary>
/// Extension methods for mapping DPIA (Data Protection Impact Assessment) management endpoints.
/// </summary>
/// <remarks>
/// <para>
/// These endpoints provide a REST API for managing DPIA assessments, enabling
/// Data Protection Officers (DPOs) and compliance officers to review, approve,
/// reject, and monitor assessments through an HTTP interface.
/// </para>
/// <para>
/// All endpoints are optional and must be explicitly mapped by the user.
/// Authorization is the caller's responsibility — apply authorization policies
/// via middleware or endpoint filters as needed.
/// </para>
/// <para>
/// Prerequisites: Call <c>AddEncinaDPIA()</c> in your DI configuration before mapping endpoints.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register DPIA services
/// builder.Services.AddEncinaDPIA(options =>
/// {
///     options.EnforcementMode = DPIAEnforcementMode.Block;
///     options.DPOEmail = "dpo@company.com";
/// });
///
/// var app = builder.Build();
///
/// // Map DPIA management endpoints (default prefix: /api/dpia)
/// app.MapDPIAEndpoints();
///
/// // Or with a custom prefix
/// app.MapDPIAEndpoints(prefix: "/compliance/dpia");
///
/// // With authorization
/// app.MapDPIAEndpoints().RequireAuthorization("DPOPolicy");
/// </code>
/// </example>
public static class DPIAEndpointExtensions
{
    /// <summary>
    /// Maps DPIA assessment management endpoints to the specified route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to add endpoints to.</param>
    /// <param name="prefix">
    /// The URL prefix for all DPIA endpoints. Defaults to <c>"/api/dpia"</c>.
    /// </param>
    /// <returns>
    /// A <see cref="RouteGroupBuilder"/> that can be used to further customize
    /// the endpoint group (e.g., adding authorization, rate limiting).
    /// </returns>
    /// <remarks>
    /// <para>
    /// The following endpoints are registered under the specified prefix:
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>Method &amp; Path</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><c>GET /assessments</c></term>
    /// <description>Lists all DPIA assessments.</description>
    /// </item>
    /// <item>
    /// <term><c>GET /assessments/{id}</c></term>
    /// <description>Gets a specific assessment by its unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><c>POST /assessments/{requestType}/assess</c></term>
    /// <description>Triggers a risk assessment for a request type using the assessment engine.</description>
    /// </item>
    /// <item>
    /// <term><c>POST /assessments/{id}/approve</c></term>
    /// <description>Approves an assessment (DPO action). Sets review period from options.</description>
    /// </item>
    /// <item>
    /// <term><c>POST /assessments/{id}/reject</c></term>
    /// <description>Rejects an assessment (DPO action). Accepts optional reason in body.</description>
    /// </item>
    /// <item>
    /// <term><c>GET /templates</c></term>
    /// <description>Lists all available DPIA assessment templates.</description>
    /// </item>
    /// <item>
    /// <term><c>GET /expired</c></term>
    /// <description>Lists expired assessments needing periodic review (Article 35(11)).</description>
    /// </item>
    /// </list>
    /// <para>
    /// All endpoints resolve <see cref="IDPIAStore"/> and <see cref="IDPIAAssessmentEngine"/>
    /// from the DI container. Ensure <c>AddEncinaDPIA()</c> has been called during service registration.
    /// </para>
    /// <para>
    /// Errors are returned as RFC 9457 Problem Details using the standard
    /// <see cref="ProblemDetailsExtensions.ToProblemDetails"/> mapping.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="endpoints"/> is <see langword="null"/>.
    /// </exception>
    public static RouteGroupBuilder MapDPIAEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/dpia")
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(prefix);

        group.MapGet("/assessments", HandleListAssessments);
        group.MapGet("/assessments/{id:guid}", HandleGetAssessment);
        group.MapPost("/assessments/{requestType}/assess", HandleAssessRequestType);
        group.MapPost("/assessments/{id:guid}/approve", HandleApproveAssessment);
        group.MapPost("/assessments/{id:guid}/reject", HandleRejectAssessment);
        group.MapGet("/templates", HandleListTemplates);
        group.MapGet("/expired", HandleListExpiredAssessments);

        return group;
    }

    /// <summary>
    /// The HTTP header name for tenant identification in DPIA management endpoints.
    /// </summary>
    /// <remarks>
    /// When multi-tenancy is enabled, this header scopes DPIA assessments to a specific tenant.
    /// The value is optional — when absent, assessments are created without tenant scoping.
    /// </remarks>
    public const string TenantIdHeader = "X-Tenant-Id";

    /// <summary>
    /// The HTTP header name for module identification in DPIA management endpoints.
    /// </summary>
    /// <remarks>
    /// In modular monolith architectures, this header scopes DPIA assessments to a specific module.
    /// The value is optional — when absent, assessments are created without module scoping.
    /// </remarks>
    public const string ModuleIdHeader = "X-Module-Id";

    // ========================================================================
    // Endpoint handlers
    // ========================================================================

    /// <summary>
    /// Lists all DPIA assessments.
    /// </summary>
    private static async Task<IResult> HandleListAssessments(
        IDPIAStore store,
        HttpContext httpContext,
        ILogger<DPIAEndpointMarker> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DPIADiagnostics.StartEndpointExecution("list");
        var startedAt = Stopwatch.GetTimestamp();
        logger.EndpointRequestReceived("list", "GET", httpContext.Request.Path);

        var result = await store.GetAllAssessmentsAsync(cancellationToken);

        return result.Match(
            Right: assessments =>
            {
                RecordEndpointSuccess(activity, startedAt, "list", logger);
                return Results.Ok(assessments);
            },
            Left: error =>
            {
                RecordEndpointFailure(activity, startedAt, "list", StatusCodes.Status500InternalServerError, logger);
                return error.ToProblemDetails(httpContext);
            });
    }

    /// <summary>
    /// Gets a specific assessment by ID.
    /// </summary>
    private static async Task<IResult> HandleGetAssessment(
        Guid id,
        IDPIAStore store,
        HttpContext httpContext,
        ILogger<DPIAEndpointMarker> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DPIADiagnostics.StartEndpointExecution("get");
        var startedAt = Stopwatch.GetTimestamp();
        logger.EndpointRequestReceived("get", "GET", httpContext.Request.Path);

        var result = await store.GetAssessmentByIdAsync(id, cancellationToken);

        return result.Match(
            Right: assessmentOption =>
            {
                if (assessmentOption.IsSome)
                {
                    RecordEndpointSuccess(activity, startedAt, "get", logger);
                    return Results.Ok((DPIAAssessment)assessmentOption.Case!);
                }

                RecordEndpointFailure(activity, startedAt, "get", StatusCodes.Status404NotFound, logger);
                return AssessmentNotFound(id);
            },
            Left: error =>
            {
                RecordEndpointFailure(activity, startedAt, "get", StatusCodes.Status500InternalServerError, logger);
                return error.ToProblemDetails(httpContext);
            });
    }

    /// <summary>
    /// Triggers a risk assessment for a request type.
    /// </summary>
    private static async Task<IResult> HandleAssessRequestType(
        string requestType,
        AssessDPIARequest? request,
        IDPIAAssessmentEngine engine,
        IDPIAStore store,
        IDPIATemplateProvider templateProvider,
        IOptions<DPIAOptions> options,
        TimeProvider timeProvider,
        HttpContext httpContext,
        ILogger<DPIAEndpointMarker> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DPIADiagnostics.StartEndpointExecution("assess");
        var startedAt = Stopwatch.GetTimestamp();
        logger.EndpointRequestReceived("assess", "POST", httpContext.Request.Path);
        logger.EndpointAssessTriggered(Uri.UnescapeDataString(requestType));

        var decodedTypeName = Uri.UnescapeDataString(requestType);

        // Resolve the CLR type from loaded assemblies
        var resolvedType = ResolveType(decodedTypeName);
        if (resolvedType is null)
        {
            RecordEndpointFailure(activity, startedAt, "assess", StatusCodes.Status400BadRequest, logger);
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: $"Request type '{decodedTypeName}' could not be resolved. "
                    + "Ensure the assembly containing this type is loaded in the application.");
        }

        // Build assessment context
        DPIATemplate? template = null;
        if (request?.ProcessingType is not null)
        {
            var templateResult = await templateProvider
                .GetTemplateAsync(request.ProcessingType, cancellationToken);

            templateResult.Match(
                Right: t => template = t,
                Left: _ => { }); // Template not found is non-fatal
        }

        var context = new DPIAContext
        {
            RequestType = resolvedType,
            ProcessingType = request?.ProcessingType,
            DataCategories = request?.DataCategories ?? [],
            HighRiskTriggers = request?.HighRiskTriggers ?? [],
            Template = template,
        };

        // Run assessment engine (engine records its own audit trail)
        var assessResult = await engine.AssessAsync(context, cancellationToken);

        DPIAResult? dpiaResult = null;
        IResult? errorResponse = null;

        assessResult.Match(
            Right: r => dpiaResult = r,
            Left: error =>
            {
                RecordEndpointFailure(activity, startedAt, "assess", StatusCodes.Status500InternalServerError, logger);
                errorResponse = error.ToProblemDetails(httpContext);
            });

        if (errorResponse is not null)
        {
            return errorResponse;
        }

        // Resolve tenant/module context from HTTP headers
        var (tenantId, moduleId) = ResolveTenantModule(httpContext);

        // Look up existing assessment for this request type
        var nowUtc = timeProvider.GetUtcNow();
        DPIAAssessment? existing = null;

        var existingResult = await store.GetAssessmentAsync(decodedTypeName, cancellationToken);
        existingResult.Match(
            Right: assessmentOption => existing = assessmentOption.IsSome ? (DPIAAssessment?)assessmentOption.Case : null,
            Left: _ => { }); // Lookup failure is non-fatal; create new

        // Create or update assessment with results (including tenant/module context)
        var assessment = existing is not null
            ? existing with
            {
                Result = dpiaResult,
                Status = DPIAAssessmentStatus.InReview,
                RequestType = resolvedType,
                ProcessingType = request?.ProcessingType ?? existing.ProcessingType,
                TenantId = tenantId ?? existing.TenantId,
                ModuleId = moduleId ?? existing.ModuleId,
            }
            : new DPIAAssessment
            {
                Id = Guid.NewGuid(),
                RequestTypeName = decodedTypeName,
                RequestType = resolvedType,
                Status = DPIAAssessmentStatus.InReview,
                Result = dpiaResult,
                ProcessingType = request?.ProcessingType,
                CreatedAtUtc = nowUtc,
                TenantId = tenantId,
                ModuleId = moduleId,
            };

        // Persist
        var saveResult = await store.SaveAssessmentAsync(assessment, cancellationToken);

        return saveResult.Match(
            Right: _ =>
            {
                RecordEndpointSuccess(activity, startedAt, "assess", logger);
                return Results.Ok(assessment);
            },
            Left: error =>
            {
                RecordEndpointFailure(activity, startedAt, "assess", StatusCodes.Status500InternalServerError, logger);
                return error.ToProblemDetails(httpContext);
            });
    }

    /// <summary>
    /// Approves a DPIA assessment (DPO action).
    /// </summary>
    private static async Task<IResult> HandleApproveAssessment(
        Guid id,
        IDPIAStore store,
        IDPIAAuditStore auditStore,
        IOptions<DPIAOptions> options,
        TimeProvider timeProvider,
        HttpContext httpContext,
        ILogger<DPIAEndpointMarker> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DPIADiagnostics.StartEndpointExecution("approve");
        var startedAt = Stopwatch.GetTimestamp();
        logger.EndpointRequestReceived("approve", "POST", httpContext.Request.Path);
        logger.EndpointApproveTriggered(id);

        var getResult = await store.GetAssessmentByIdAsync(id, cancellationToken);

        DPIAAssessment? assessment = null;
        IResult? errorResponse = null;

        getResult.Match(
            Right: assessmentOption => assessment = assessmentOption.IsSome ? (DPIAAssessment?)assessmentOption.Case : null,
            Left: error =>
            {
                RecordEndpointFailure(activity, startedAt, "approve", StatusCodes.Status500InternalServerError, logger);
                errorResponse = error.ToProblemDetails(httpContext);
            });

        if (errorResponse is not null)
        {
            return errorResponse;
        }

        if (assessment is null)
        {
            RecordEndpointFailure(activity, startedAt, "approve", StatusCodes.Status404NotFound, logger);
            return AssessmentNotFound(id);
        }

        // Transition to approved with review period
        var nowUtc = timeProvider.GetUtcNow();
        var approved = assessment with
        {
            Status = DPIAAssessmentStatus.Approved,
            ApprovedAtUtc = nowUtc,
            NextReviewAtUtc = nowUtc + options.Value.DefaultReviewPeriod,
        };

        var saveResult = await store.SaveAssessmentAsync(approved, cancellationToken);

        if (saveResult.IsLeft)
        {
            RecordEndpointFailure(activity, startedAt, "approve", StatusCodes.Status500InternalServerError, logger);
            return ((EncinaError)saveResult).ToProblemDetails(httpContext);
        }

        // Record audit trail for the approval action
        if (options.Value.TrackAuditTrail)
        {
            var performedBy = httpContext.User.Identity?.Name ?? "DPO";
            await RecordAuditAsync(auditStore, id, "Approved", performedBy, nowUtc,
                $"Assessment approved. Next review at {approved.NextReviewAtUtc:O}.",
                assessment.TenantId, assessment.ModuleId, cancellationToken);
        }

        RecordEndpointSuccess(activity, startedAt, "approve", logger);
        return Results.Ok(approved);
    }

    /// <summary>
    /// Rejects a DPIA assessment (DPO action).
    /// </summary>
    private static async Task<IResult> HandleRejectAssessment(
        Guid id,
        RejectDPIARequest? request,
        IDPIAStore store,
        IDPIAAuditStore auditStore,
        IOptions<DPIAOptions> options,
        TimeProvider timeProvider,
        HttpContext httpContext,
        ILogger<DPIAEndpointMarker> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DPIADiagnostics.StartEndpointExecution("reject");
        var startedAt = Stopwatch.GetTimestamp();
        logger.EndpointRequestReceived("reject", "POST", httpContext.Request.Path);
        logger.EndpointRejectTriggered(id);

        var getResult = await store.GetAssessmentByIdAsync(id, cancellationToken);

        DPIAAssessment? assessment = null;
        IResult? errorResponse = null;

        getResult.Match(
            Right: assessmentOption => assessment = assessmentOption.IsSome ? (DPIAAssessment?)assessmentOption.Case : null,
            Left: error =>
            {
                RecordEndpointFailure(activity, startedAt, "reject", StatusCodes.Status500InternalServerError, logger);
                errorResponse = error.ToProblemDetails(httpContext);
            });

        if (errorResponse is not null)
        {
            return errorResponse;
        }

        if (assessment is null)
        {
            RecordEndpointFailure(activity, startedAt, "reject", StatusCodes.Status404NotFound, logger);
            return AssessmentNotFound(id);
        }

        // Transition to rejected
        var rejected = assessment with
        {
            Status = DPIAAssessmentStatus.Rejected,
            Reason = request?.Reason,
        };

        var saveResult = await store.SaveAssessmentAsync(rejected, cancellationToken);

        if (saveResult.IsLeft)
        {
            RecordEndpointFailure(activity, startedAt, "reject", StatusCodes.Status500InternalServerError, logger);
            return ((EncinaError)saveResult).ToProblemDetails(httpContext);
        }

        // Record audit trail for the rejection action
        if (options.Value.TrackAuditTrail)
        {
            var nowUtc = timeProvider.GetUtcNow();
            var performedBy = httpContext.User.Identity?.Name ?? "DPO";
            var details = string.IsNullOrWhiteSpace(request?.Reason)
                ? "Assessment rejected."
                : $"Assessment rejected. Reason: {request.Reason}";

            await RecordAuditAsync(auditStore, id, "Rejected", performedBy, nowUtc,
                details, assessment.TenantId, assessment.ModuleId, cancellationToken);
        }

        RecordEndpointSuccess(activity, startedAt, "reject", logger);
        return Results.Ok(rejected);
    }

    /// <summary>
    /// Lists all available DPIA templates.
    /// </summary>
    private static async Task<IResult> HandleListTemplates(
        IDPIATemplateProvider templateProvider,
        HttpContext httpContext,
        ILogger<DPIAEndpointMarker> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DPIADiagnostics.StartEndpointExecution("templates");
        var startedAt = Stopwatch.GetTimestamp();
        logger.EndpointRequestReceived("templates", "GET", httpContext.Request.Path);

        var result = await templateProvider.GetAllTemplatesAsync(cancellationToken);

        return result.Match(
            Right: templates =>
            {
                RecordEndpointSuccess(activity, startedAt, "templates", logger);
                return Results.Ok(templates);
            },
            Left: error =>
            {
                RecordEndpointFailure(activity, startedAt, "templates", StatusCodes.Status500InternalServerError, logger);
                return error.ToProblemDetails(httpContext);
            });
    }

    /// <summary>
    /// Lists expired assessments needing periodic review.
    /// </summary>
    private static async Task<IResult> HandleListExpiredAssessments(
        IDPIAStore store,
        TimeProvider timeProvider,
        HttpContext httpContext,
        ILogger<DPIAEndpointMarker> logger,
        CancellationToken cancellationToken)
    {
        using var activity = DPIADiagnostics.StartEndpointExecution("expired");
        var startedAt = Stopwatch.GetTimestamp();
        logger.EndpointRequestReceived("expired", "GET", httpContext.Request.Path);

        var nowUtc = timeProvider.GetUtcNow();
        var result = await store.GetExpiredAssessmentsAsync(nowUtc, cancellationToken);

        return result.Match(
            Right: expired =>
            {
                RecordEndpointSuccess(activity, startedAt, "expired", logger);
                return Results.Ok(expired);
            },
            Left: error =>
            {
                RecordEndpointFailure(activity, startedAt, "expired", StatusCodes.Status500InternalServerError, logger);
                return error.ToProblemDetails(httpContext);
            });
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    /// <summary>
    /// Creates a 404 Problem Details response for a missing assessment.
    /// </summary>
    private static IResult AssessmentNotFound(Guid id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: $"DPIA assessment with ID '{id}' was not found.");

    /// <summary>
    /// Resolves tenant and module identifiers from HTTP request headers.
    /// </summary>
    /// <remarks>
    /// Multi-tenancy and module isolation are soft dependencies. When the headers
    /// are absent, assessments are created without tenant/module scoping.
    /// </remarks>
    private static (string? TenantId, string? ModuleId) ResolveTenantModule(HttpContext httpContext)
    {
        var tenantId = httpContext.Request.Headers[TenantIdHeader].FirstOrDefault();
        var moduleId = httpContext.Request.Headers[ModuleIdHeader].FirstOrDefault();

        return (
            string.IsNullOrWhiteSpace(tenantId) ? null : tenantId,
            string.IsNullOrWhiteSpace(moduleId) ? null : moduleId);
    }

    /// <summary>
    /// Records an audit trail entry for a DPIA assessment state change.
    /// </summary>
    private static async ValueTask RecordAuditAsync(
        IDPIAAuditStore auditStore,
        Guid assessmentId,
        string action,
        string performedBy,
        DateTimeOffset occurredAtUtc,
        string details,
        string? tenantId,
        string? moduleId,
        CancellationToken cancellationToken)
    {
        var auditEntry = new DPIAAuditEntry
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            Action = action,
            PerformedBy = performedBy,
            OccurredAtUtc = occurredAtUtc,
            Details = details,
            TenantId = tenantId,
            ModuleId = moduleId,
        };

        await auditStore.RecordAuditEntryAsync(auditEntry, cancellationToken);
    }

    /// <summary>
    /// Records a successful endpoint execution with metrics, activity, and structured logging.
    /// </summary>
    private static void RecordEndpointSuccess(
        Activity? activity, long startedAt, string endpointName, ILogger logger)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        var tags = new TagList
        {
            { DPIADiagnostics.TagEndpoint, endpointName },
            { DPIADiagnostics.TagStatusCode, "200" },
        };

        DPIADiagnostics.EndpointRequestTotal.Add(1, tags);
        DPIADiagnostics.EndpointDuration.Record(elapsedMs, tags);
        DPIADiagnostics.RecordEndpointCompleted(activity, StatusCodes.Status200OK);
        logger.EndpointRequestCompleted(endpointName, StatusCodes.Status200OK, elapsedMs);
    }

    /// <summary>
    /// Records a failed endpoint execution with metrics, activity, and structured logging.
    /// </summary>
    private static void RecordEndpointFailure(
        Activity? activity, long startedAt, string endpointName, int statusCode, ILogger logger)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        var tags = new TagList
        {
            { DPIADiagnostics.TagEndpoint, endpointName },
            { DPIADiagnostics.TagStatusCode, statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture) },
        };

        DPIADiagnostics.EndpointRequestTotal.Add(1, tags);
        DPIADiagnostics.EndpointDuration.Record(elapsedMs, tags);
        DPIADiagnostics.RecordEndpointFailed(activity, statusCode, $"HTTP {statusCode}");
        logger.EndpointRequestFailed(endpointName, statusCode, elapsedMs);
    }

    /// <summary>
    /// Attempts to resolve a CLR <see cref="Type"/> from a fully-qualified type name
    /// by searching loaded assemblies.
    /// </summary>
    private static Type? ResolveType(string typeName)
    {
        // Try Type.GetType first (handles assembly-qualified names and mscorlib types)
        var type = Type.GetType(typeName);
        if (type is not null)
        {
            return type;
        }

        // Fall back to searching all loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (var i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(typeName);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }
}

// ========================================================================
// Request DTOs
// ========================================================================

/// <summary>
/// Request body for triggering a DPIA risk assessment via the
/// <c>POST /assessments/{requestType}/assess</c> endpoint.
/// </summary>
/// <remarks>
/// <para>
/// All properties are optional. When omitted, the assessment engine evaluates
/// the request type using only its default risk criteria.
/// </para>
/// <para>
/// Providing <see cref="ProcessingType"/> enables template-based assessment,
/// while <see cref="DataCategories"/> and <see cref="HighRiskTriggers"/> allow
/// explicit risk signal injection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // JSON body example
/// {
///     "processingType": "AutomatedDecisionMaking",
///     "dataCategories": ["BiometricData", "HealthData"],
///     "highRiskTriggers": ["BiometricData", "AutomatedDecisionMaking"]
/// }
/// </code>
/// </example>
public sealed record AssessDPIARequest
{
    /// <summary>
    /// Gets the type of processing being assessed (e.g., "AutomatedDecisionMaking",
    /// "SystematicProfiling", "LargeScaleProcessing").
    /// </summary>
    /// <remarks>
    /// When provided, the endpoint attempts to match an appropriate
    /// <see cref="DPIATemplate"/> via <see cref="IDPIATemplateProvider"/>.
    /// </remarks>
    public string? ProcessingType { get; init; }

    /// <summary>
    /// Gets the categories of personal data involved in the processing.
    /// </summary>
    /// <remarks>
    /// Used by risk criteria evaluators to determine if special categories
    /// (Article 9) or criminal conviction data (Article 10) are being processed.
    /// </remarks>
    public IReadOnlyList<string> DataCategories { get; init; } = [];

    /// <summary>
    /// Gets the high-risk triggers applicable to this processing operation.
    /// </summary>
    /// <remarks>
    /// See <see cref="HighRiskTriggers"/> for standard trigger constants
    /// (e.g., <c>"BiometricData"</c>, <c>"AutomatedDecisionMaking"</c>).
    /// </remarks>
    public IReadOnlyList<string> HighRiskTriggers { get; init; } = [];
}

/// <summary>
/// Request body for rejecting a DPIA assessment via the
/// <c>POST /assessments/{id}/reject</c> endpoint.
/// </summary>
/// <remarks>
/// All properties are optional. Providing a <see cref="Reason"/> is recommended
/// for audit trail completeness and GDPR accountability (Article 5(2)).
/// </remarks>
/// <example>
/// <code>
/// // JSON body example
/// {
///     "reason": "Insufficient mitigation measures for biometric data processing."
/// }
/// </code>
/// </example>
public sealed record RejectDPIARequest
{
    /// <summary>
    /// Gets the reason for rejecting the assessment.
    /// </summary>
    /// <remarks>
    /// Recorded in the assessment for audit trail purposes.
    /// </remarks>
    public string? Reason { get; init; }
}

/// <summary>
/// Marker class used as the logger category for DPIA endpoint handlers.
/// </summary>
/// <remarks>
/// Minimal API handlers are static methods and cannot use generic logger injection
/// without a category type. This marker provides a consistent category name
/// (<c>Encina.AspNetCore.DPIAEndpointMarker</c>) for all DPIA endpoint log messages.
/// </remarks>
internal sealed class DPIAEndpointMarker;
