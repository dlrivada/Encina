using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Encina.Security.Audit;

/// <summary>
/// Default implementation of <see cref="IAuditEntryFactory"/> that creates audit entries
/// using naming conventions, attributes, and request context.
/// </summary>
/// <remarks>
/// <para>
/// This factory:
/// <list type="bullet">
/// <item>Extracts entity type and action from <see cref="AuditableAttribute"/> or type name conventions</item>
/// <item>Extracts entity ID from request properties</item>
/// <item>Computes SHA-256 hash of the request payload (after PII masking)</item>
/// <item>Populates context information from <see cref="IRequestContext"/></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DefaultAuditEntryFactory : IAuditEntryFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IPiiMasker _piiMasker;
    private readonly AuditOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAuditEntryFactory"/> class.
    /// </summary>
    /// <param name="piiMasker">The PII masker for sanitizing request payloads.</param>
    /// <param name="options">The audit options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="piiMasker"/> or <paramref name="options"/> is null.
    /// </exception>
    public DefaultAuditEntryFactory(IPiiMasker piiMasker, IOptions<AuditOptions> options)
    {
        ArgumentNullException.ThrowIfNull(piiMasker);
        ArgumentNullException.ThrowIfNull(options);

        _piiMasker = piiMasker;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public AuditEntry Create<TRequest>(
        TRequest request,
        IRequestContext context,
        AuditOutcome outcome,
        string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var requestType = typeof(TRequest);
        var auditableAttribute = requestType.GetCustomAttribute<AuditableAttribute>();

        // Extract entity type and action
        var (conventionEntity, conventionAction) = RequestMetadataExtractor.ExtractFromTypeName(requestType);
        var entityType = auditableAttribute?.EntityType ?? conventionEntity;
        var action = auditableAttribute?.Action ?? conventionAction;

        // Extract entity ID
        var entityId = RequestMetadataExtractor.TryExtractEntityId(request);

        // Compute payload hash if enabled
        var includePayload = auditableAttribute?.IncludePayload ?? _options.IncludePayloadHash;
        var payloadHash = includePayload ? ComputePayloadHash(request) : null;

        // Build metadata
        var metadata = BuildMetadata(context, auditableAttribute);

        return new AuditEntry
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
            TimestampUtc = context.Timestamp.UtcDateTime,
            IpAddress = GetIpAddress(context),
            UserAgent = GetUserAgent(context),
            RequestPayloadHash = payloadHash,
            Metadata = metadata
        };
    }

    private string? ComputePayloadHash(object request)
    {
        try
        {
            // Apply PII masking before hashing
            var maskedRequest = _piiMasker.MaskForAudit(request);

            // Serialize to JSON
            var json = JsonSerializer.Serialize(maskedRequest, maskedRequest.GetType(), JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            // Compute SHA-256 hash
            var hash = SHA256.HashData(bytes);

            // Convert to hex string
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch
        {
            // If serialization fails, return null rather than failing the audit
            return null;
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
