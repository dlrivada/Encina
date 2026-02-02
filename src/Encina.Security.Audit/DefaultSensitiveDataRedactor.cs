using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Encina.Security.Audit;

/// <summary>
/// Default implementation of <see cref="IPiiMasker"/> that redacts sensitive fields in JSON payloads.
/// </summary>
/// <remarks>
/// <para>
/// This redactor works by:
/// <list type="number">
/// <item>Serializing the request object to JSON</item>
/// <item>Traversing the JSON structure to find fields matching sensitive patterns</item>
/// <item>Replacing matched values with "[REDACTED]"</item>
/// <item>Deserializing back to the original type</item>
/// </list>
/// </para>
/// <para>
/// Field matching is case-insensitive and supports both:
/// <list type="bullet">
/// <item>Exact matches (e.g., "Password" matches "password", "PASSWORD")</item>
/// <item>Contains matches (e.g., "Password" matches "UserPassword", "PasswordHash")</item>
/// </list>
/// </para>
/// <para>
/// Default sensitive fields include: password, secret, token, key, apikey, api_key,
/// authorization, bearer, credential, ssn, socialSecurityNumber, creditCard, cvv, pin.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration with default options
/// services.AddSingleton&lt;IPiiMasker, DefaultSensitiveDataRedactor&gt;();
///
/// // Usage
/// var redactor = new DefaultSensitiveDataRedactor(Options.Create(new AuditOptions()));
/// var request = new CreateUserCommand { Email = "user@example.com", Password = "secret123" };
/// var masked = redactor.MaskForAudit(request);
/// // masked.Password == "[REDACTED]"
/// </code>
/// </example>
public sealed class DefaultSensitiveDataRedactor : IPiiMasker
{
    /// <summary>
    /// The string used to replace sensitive values.
    /// </summary>
    public const string RedactedValue = "[REDACTED]";

    /// <summary>
    /// Default set of field names considered sensitive.
    /// </summary>
    /// <remarks>
    /// These patterns cover common authentication, authorization, and PII fields.
    /// Field matching is case-insensitive.
    /// </remarks>
    public static readonly IReadOnlySet<string> DefaultSensitiveFieldPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "token",
        "key",
        "apikey",
        "api_key",
        "authorization",
        "bearer",
        "credential",
        "ssn",
        "socialsecuritynumber",
        "social_security_number",
        "creditcard",
        "credit_card",
        "cardnumber",
        "card_number",
        "cvv",
        "cvc",
        "pin",
        "accesstoken",
        "access_token",
        "refreshtoken",
        "refresh_token",
        "privatekey",
        "private_key",
        "connectionstring",
        "connection_string"
    };

    private readonly HashSet<string> _sensitivePatterns;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSensitiveDataRedactor"/> class.
    /// </summary>
    /// <param name="options">The audit options containing global sensitive field configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    public DefaultSensitiveDataRedactor(IOptions<AuditOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Combine default patterns with any additional patterns from options
        _sensitivePatterns = new HashSet<string>(DefaultSensitiveFieldPatterns, StringComparer.OrdinalIgnoreCase);

        var globalFields = options.Value.GlobalSensitiveFields;
        if (globalFields is not null)
        {
            foreach (var field in globalFields)
            {
                _sensitivePatterns.Add(field);
            }
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public T MaskForAudit<T>(T request) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(request);

        return MaskForAudit(request, additionalSensitiveFields: null);
    }

    /// <summary>
    /// Masks PII in a request object with additional sensitive fields specific to the request.
    /// </summary>
    /// <typeparam name="T">The type of the request object.</typeparam>
    /// <param name="request">The request object potentially containing PII.</param>
    /// <param name="additionalSensitiveFields">
    /// Additional field names to redact, typically from <see cref="AuditableAttribute.SensitiveFields"/>.
    /// </param>
    /// <returns>A sanitized copy of the request with PII masked.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <c>null</c>.</exception>
    public T MaskForAudit<T>(T request, IEnumerable<string>? additionalSensitiveFields) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // Build the effective set of sensitive patterns
            var effectivePatterns = additionalSensitiveFields is null
                ? _sensitivePatterns
                : BuildEffectivePatterns(additionalSensitiveFields);

            // Serialize to JSON
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var node = JsonNode.Parse(json);

            if (node is null)
            {
                return request;
            }

            // Redact sensitive fields
            RedactNode(node, effectivePatterns);

            // Deserialize back
            var redacted = node.Deserialize<T>(_jsonOptions);
            return redacted ?? request;
        }
        catch (JsonException)
        {
            // If serialization/deserialization fails, return original
            // This ensures we don't break the pipeline for non-serializable types
            return request;
        }
    }

    /// <inheritdoc />
    public object MaskForAudit(object request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MaskForAudit(request, additionalSensitiveFields: null);
    }

    /// <summary>
    /// Masks PII in a request object with additional sensitive fields specific to the request.
    /// </summary>
    /// <param name="request">The request object potentially containing PII.</param>
    /// <param name="additionalSensitiveFields">
    /// Additional field names to redact, typically from <see cref="AuditableAttribute.SensitiveFields"/>.
    /// </param>
    /// <returns>A sanitized copy of the request with PII masked.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <c>null</c>.</exception>
    public object MaskForAudit(object request, IEnumerable<string>? additionalSensitiveFields)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var type = request.GetType();

            // Build the effective set of sensitive patterns
            var effectivePatterns = additionalSensitiveFields is null
                ? _sensitivePatterns
                : BuildEffectivePatterns(additionalSensitiveFields);

            // Serialize to JSON
            var json = JsonSerializer.Serialize(request, type, _jsonOptions);
            var node = JsonNode.Parse(json);

            if (node is null)
            {
                return request;
            }

            // Redact sensitive fields
            RedactNode(node, effectivePatterns);

            // Deserialize back
            var redacted = node.Deserialize(type, _jsonOptions);
            return redacted ?? request;
        }
        catch (JsonException)
        {
            // If serialization/deserialization fails, return original
            return request;
        }
    }

    /// <summary>
    /// Redacts sensitive fields in a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to redact.</param>
    /// <param name="additionalSensitiveFields">Additional field names to redact.</param>
    /// <returns>The redacted JSON string, or the original if parsing fails.</returns>
    public string RedactJsonString(string json, IEnumerable<string>? additionalSensitiveFields = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var effectivePatterns = additionalSensitiveFields is null
                ? _sensitivePatterns
                : BuildEffectivePatterns(additionalSensitiveFields);

            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            RedactNode(node, effectivePatterns);
            return node.ToJsonString(_jsonOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private HashSet<string> BuildEffectivePatterns(IEnumerable<string> additionalFields)
    {
        var effective = new HashSet<string>(_sensitivePatterns, StringComparer.OrdinalIgnoreCase);
        foreach (var field in additionalFields)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                effective.Add(field);
            }
        }

        return effective;
    }

    private void RedactNode(JsonNode node, HashSet<string> patterns)
    {
        switch (node)
        {
            case JsonObject obj:
                RedactObject(obj, patterns);
                break;
            case JsonArray array:
                RedactArray(array, patterns);
                break;
        }
    }

    private void RedactObject(JsonObject obj, HashSet<string> patterns)
    {
        // Collect keys to modify (can't modify during enumeration)
        var keysToRedact = new List<string>();
        var nodesToRecurse = new List<(string Key, JsonNode Node)>();

        foreach (var property in obj)
        {
            var key = property.Key;
            var value = property.Value;

            if (IsSensitiveField(key, patterns))
            {
                keysToRedact.Add(key);
            }
            else if (value is not null)
            {
                nodesToRecurse.Add((key, value));
            }
        }

        // Redact sensitive fields
        foreach (var key in keysToRedact)
        {
            obj[key] = RedactedValue;
        }

        // Recurse into nested objects/arrays
        foreach (var (_, childNode) in nodesToRecurse)
        {
            RedactNode(childNode, patterns);
        }
    }

    private void RedactArray(JsonArray array, HashSet<string> patterns)
    {
        foreach (var item in array)
        {
            if (item is not null)
            {
                RedactNode(item, patterns);
            }
        }
    }

    private static bool IsSensitiveField(string fieldName, HashSet<string> patterns)
    {
        // Check exact match first (most common case)
        if (patterns.Contains(fieldName))
        {
            return true;
        }

        // Check if field name contains any sensitive pattern
        foreach (var pattern in patterns)
        {
            if (fieldName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
