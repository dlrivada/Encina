using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Options;

namespace Encina.Security.Audit;

/// <summary>
/// Default implementation of <see cref="IOperationAuditEntryFactory"/> that creates audit entries
/// using naming conventions, attributes, and request context.
/// </summary>
/// <remarks>
/// <para>
/// This factory:
/// <list type="bullet">
/// <item>Extracts entity type and action from <see cref="AuditableAttribute"/> or type name conventions</item>
/// <item>Extracts entity ID from request properties</item>
/// <item>Computes SHA-256 hash of the request payload (after PII masking)</item>
/// <item>Serializes and redacts request/response payloads (when enabled)</item>
/// <item>Captures operation timing (start, completion, duration)</item>
/// <item>Populates context information from <see cref="IRequestContext"/></item>
/// </list>
/// </para>
/// <para>
/// <b>Fail closed:</b> when the masker, the redactor or the serializer fails, the factory returns a Left with
/// <see cref="OperationAuditErrors.MaskingFailedCode"/> (carrying only the exception type, never its message)
/// and builds no entry, so an unmasked payload or hash never reaches a store.
/// </para>
/// </remarks>
public sealed class DefaultOperationAuditEntryFactory : IOperationAuditEntryFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IPiiMasker _piiMasker;
    private readonly DefaultSensitiveDataRedactor? _redactor;
    private readonly IOptions<OperationAuditOptions> _optionsAccessor;
    private readonly OperationAuditOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultOperationAuditEntryFactory"/> class.
    /// </summary>
    /// <param name="piiMasker">The PII masker for sanitizing request payloads.</param>
    /// <param name="options">The audit options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="piiMasker"/> or <paramref name="options"/> is null.
    /// </exception>
    public DefaultOperationAuditEntryFactory(IPiiMasker piiMasker, IOptions<OperationAuditOptions> options)
    {
        ArgumentNullException.ThrowIfNull(piiMasker);
        ArgumentNullException.ThrowIfNull(options);

        _piiMasker = piiMasker;
        _optionsAccessor = options;
        _options = options.Value;

        // If the piiMasker is a DefaultSensitiveDataRedactor, use it for JSON redaction
        _redactor = piiMasker as DefaultSensitiveDataRedactor;
    }

    /// <inheritdoc/>
    public Either<EncinaError, OperationAuditEntry> Create<TRequest>(
        TRequest request,
        IRequestContext context,
        AuditOutcome outcome,
        string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // Use context timestamp for both start and complete (legacy behavior)
        var timestamp = context.Timestamp;
        return Create<TRequest, object>(request, default, context, outcome, errorMessage, timestamp, timestamp);
    }

    /// <inheritdoc/>
    public Either<EncinaError, OperationAuditEntry> Create<TRequest, TResponse>(
        TRequest request,
        TResponse? response,
        IRequestContext context,
        AuditOutcome outcome,
        string? errorMessage,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var requestType = typeof(TRequest);
        var auditableAttribute = requestType.GetCustomAttribute<AuditableAttribute>();

        // Extract entity type and action
        var (entityType, action) = ResolveEntityTypeAndAction(requestType, auditableAttribute);

        // Extract entity ID
        var entityId = RequestMetadataExtractor.TryExtractEntityId(request);

        // Mask and serialize the payloads; a masking failure is a Left and no entry is built
        var payloads = BuildPayloads(request!, response, outcome, auditableAttribute, requestType.Name);

        // Build metadata
        var metadata = BuildMetadata(context, auditableAttribute);

        return payloads.Map(p => new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = context.CorrelationId,
            UserId = context.UserId,
            TenantId = context.TenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = outcome,
            ErrorMessage = errorMessage,
            TimestampUtc = completedAtUtc.UtcDateTime,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            IpAddress = GetIpAddress(context),
            UserAgent = GetUserAgent(context),
            RequestPayloadHash = p.Hash.IfNoneUnsafe((string?)null),
            RequestPayload = p.Request.IfNoneUnsafe((string?)null),
            ResponsePayload = p.Response.IfNoneUnsafe((string?)null),
            Metadata = metadata
        });
    }

    private readonly record struct Payloads(Option<string> Hash, Option<string> Request, Option<string> Response);

    private static (string EntityType, string Action) ResolveEntityTypeAndAction(
        Type requestType,
        AuditableAttribute? auditableAttribute)
    {
        var (conventionEntity, conventionAction) = RequestMetadataExtractor.ExtractFromTypeName(requestType);
        return (auditableAttribute?.EntityType ?? conventionEntity, auditableAttribute?.Action ?? conventionAction);
    }

    /// <summary>
    /// Masks the request once (the hash and the request payload derive from the same masked JSON) and the
    /// response when it is captured. Left carries <see cref="OperationAuditErrors.MaskingFailedCode"/>.
    /// </summary>
    private Either<EncinaError, Payloads> BuildPayloads(
        object request,
        object? response,
        AuditOutcome outcome,
        AuditableAttribute? auditableAttribute,
        string requestTypeName)
    {
        var sensitiveFields = auditableAttribute?.SensitiveFields;
        var includeHash = IncludeHash(auditableAttribute);
        var includeRequest = _options.IncludeRequestPayload;
        var captureResponse = _options.IncludeResponsePayload && outcome == AuditOutcome.Success;

        return from maskedRequest in MaskIf(includeHash || includeRequest, request, sensitiveFields, requestTypeName)
               from maskedResponse in MaskIf(captureResponse, response, sensitiveFields, requestTypeName)
               select ToPayloads(includeHash, includeRequest, maskedRequest, maskedResponse);
    }

    private bool IncludeHash(AuditableAttribute? auditableAttribute) =>
        auditableAttribute?.IncludePayload ?? _options.IncludePayloadHash;

    private Payloads ToPayloads(
        bool includeHash,
        bool includeRequest,
        Option<string> maskedRequest,
        Option<string> maskedResponse) =>
        new(
            includeHash ? maskedRequest.Map(Sha256Hex) : Option<string>.None,
            includeRequest ? maskedRequest.Bind(WithinLimit) : Option<string>.None,
            maskedResponse.Bind(WithinLimit));

    private static string Sha256Hex(string json) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();

    private Option<string> WithinLimit(string json) =>
        Encoding.UTF8.GetByteCount(json) > _options.MaxPayloadSizeBytes ? Option<string>.None : json;

    private Either<EncinaError, Option<string>> MaskIf(
        bool enabled,
        object? payload,
        string[]? additionalSensitiveFields,
        string requestTypeName) =>
        enabled && payload is not null
            ? MaskToJson(payload, additionalSensitiveFields, requestTypeName).Map(Option<string>.Some)
            : Option<string>.None;

    /// <summary>
    /// Masks a payload to JSON. The default redactor masks the JSON form directly, so a sensitive property of any
    /// type (a decimal, a Guid, an enum) never has to round-trip through a typed object; any other masker masks the
    /// object before it is serialized, so a custom masker is never bypassed. Maskers and the serializer signal
    /// failure by throwing (the <see cref="IPiiMasker"/> contract); that is caught here, at the boundary, and only
    /// the exception type reaches the error.
    /// </summary>
    private Either<EncinaError, string> MaskToJson(
        object payload,
        string[]? additionalSensitiveFields,
        string requestTypeName)
    {
        try
        {
            if (_redactor is not null)
            {
                var raw = JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions);
                return _redactor.RedactJsonString(raw, additionalSensitiveFields);
            }

            var masked = _piiMasker.MaskForAudit(payload);
            return JsonSerializer.Serialize(masked, masked.GetType(), JsonOptions);
        }
        catch (Exception ex)
        {
            return OperationAuditErrors.MaskingFailed(requestTypeName, ex.GetType().Name);
        }
    }

    private static Dictionary<string, object?> BuildMetadata(
        IRequestContext context,
        AuditableAttribute? attribute)
    {
        var metadata = new Dictionary<string, object?>();

        // Add sensitivity level if specified
        if (!string.IsNullOrEmpty(attribute?.SensitivityLevel))
        {
            metadata["SensitivityLevel"] = attribute.SensitivityLevel;
        }

        // Copy relevant context metadata
        foreach (var kvp in context.Metadata)
        {
            // Skip internal audit keys (they're already handled)
            if (kvp.Key.StartsWith("Encina.Audit.", StringComparison.Ordinal))
            {
                continue;
            }

            metadata[kvp.Key] = kvp.Value;
        }

        return metadata;
    }

    private static string? GetIpAddress(IRequestContext context)
    {
        const string key = "Encina.Audit.IpAddress";
        return context.Metadata.TryGetValue(key, out var value) ? value as string : null;
    }

    private static string? GetUserAgent(IRequestContext context)
    {
        const string key = "Encina.Audit.UserAgent";
        return context.Metadata.TryGetValue(key, out var value) ? value as string : null;
    }
}
