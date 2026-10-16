using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
/// <b>Fail closed:</b> when the masker or redactor throws, the factory throws
/// <see cref="OperationAuditMaskingException"/> and builds no entry, so an unmasked payload or hash never
/// reaches a store. Serialization failures propagate as well; nothing is swallowed.
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
    public OperationAuditEntry Create<TRequest>(
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
    public OperationAuditEntry Create<TRequest, TResponse>(
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

        // Get sensitive fields from attribute
        var sensitiveFields = auditableAttribute?.SensitiveFields;

        // Compute payload hash if enabled
        var payloadHash = ComputePayloadHashIfEnabled(request, auditableAttribute, sensitiveFields);

        // Serialize request payload if enabled
        var requestPayload = _options.IncludeRequestPayload
            ? SerializeAndRedactPayload(request, sensitiveFields)
            : null;

        // Serialize response payload if enabled and operation was successful
        var responsePayload = SerializeResponseIfEnabled(response, outcome, sensitiveFields);

        // Build metadata
        var metadata = BuildMetadata(context, auditableAttribute);

        return new OperationAuditEntry
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
            RequestPayloadHash = payloadHash,
            RequestPayload = requestPayload,
            ResponsePayload = responsePayload,
            Metadata = metadata
        };
    }

    private static (string EntityType, string Action) ResolveEntityTypeAndAction(
        Type requestType,
        AuditableAttribute? auditableAttribute)
    {
        var (conventionEntity, conventionAction) = RequestMetadataExtractor.ExtractFromTypeName(requestType);
        return (auditableAttribute?.EntityType ?? conventionEntity, auditableAttribute?.Action ?? conventionAction);
    }

    private string? ComputePayloadHashIfEnabled(
        object request,
        AuditableAttribute? auditableAttribute,
        string[]? sensitiveFields)
    {
        var includePayloadHash = auditableAttribute?.IncludePayload ?? _options.IncludePayloadHash;
        return includePayloadHash ? ComputePayloadHash(request, sensitiveFields) : null;
    }

    private string? SerializeResponseIfEnabled<TResponse>(
        TResponse? response,
        AuditOutcome outcome,
        string[]? sensitiveFields) =>
        _options.IncludeResponsePayload && outcome == AuditOutcome.Success && response is not null
            ? SerializeAndRedactPayload(response, sensitiveFields)
            : null;

    private string ComputePayloadHash(object request, string[]? additionalSensitiveFields)
    {
        // Apply PII masking before hashing; a masking failure fails the entry, it is never swallowed.
        var json = MaskToJson(request, additionalSensitiveFields);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string? SerializeAndRedactPayload(object payload, string[]? additionalSensitiveFields)
    {
        var json = MaskToJson(payload, additionalSensitiveFields);

        return Encoding.UTF8.GetByteCount(json) > _options.MaxPayloadSizeBytes ? null : json;
    }

    /// <summary>
    /// Returns the masked JSON of a payload. The default redactor masks the JSON form directly, so a sensitive
    /// property of any type (a decimal, a Guid, an enum) never has to round-trip through a typed object; any other
    /// masker masks the object before it is serialized, so a custom masker is never bypassed.
    /// </summary>
    private string MaskToJson(object payload, string[]? additionalSensitiveFields)
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
            throw new OperationAuditMaskingException(ex);
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
