using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Encina.Caching;

/// <summary>
/// Default implementation of <see cref="ICacheKeyGenerator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Generates cache keys using the format:
/// <c>{prefix}:{tenant}:{user}:{request-type}:{property-hash}</c>
/// </para>
/// <para>
/// The property hash is a SHA256 hash of the serialized request properties,
/// ensuring that different request instances with the same values produce identical keys.
/// </para>
/// </remarks>
public sealed class DefaultCacheKeyGenerator : ICacheKeyGenerator
{
    private readonly CachingOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCacheKeyGenerator"/> class.
    /// </summary>
    /// <param name="options">The caching options.</param>
    public DefaultCacheKeyGenerator(IOptions<CachingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc/>
    public string GenerateKey<TRequest, TResponse>(TRequest request, IRequestContext context)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var cacheAttribute = typeof(TRequest).GetCustomAttribute<CacheAttribute>();
        if (cacheAttribute?.KeyTemplate is not null)
        {
            return GenerateFromTemplate(cacheAttribute.KeyTemplate, request, context, cacheAttribute);
        }

        return GenerateDefaultKey(request, context, cacheAttribute);
    }

    /// <inheritdoc/>
    public string GeneratePattern<TRequest>(IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var parts = new List<string> { _options.KeyPrefix };

        if (!string.IsNullOrEmpty(context.TenantId))
        {
            parts.Add($"t:{context.TenantId}");
        }
        else
        {
            parts.Add("t:*");
        }

        parts.Add(typeof(TRequest).Name);
        parts.Add("*");

        return string.Join(":", parts);
    }

    /// <inheritdoc/>
    public string GeneratePatternFromTemplate<TRequest>(string keyTemplate, TRequest request, IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(keyTemplate);
        ArgumentNullException.ThrowIfNull(context);

        var result = new StringBuilder();
        var tenantPrefix = !string.IsNullOrEmpty(context.TenantId)
            ? $"{_options.KeyPrefix}:t:{context.TenantId}:"
            : $"{_options.KeyPrefix}:";

        result.Append(tenantPrefix);

        var pattern = keyTemplate;
        if (request is not null)
        {
            pattern = SubstitutePlaceholders(keyTemplate, request);
        }

        result.Append(pattern);
        return result.ToString();
    }

    private string GenerateDefaultKey<TRequest>(
        TRequest request,
        IRequestContext context,
        CacheAttribute? cacheAttribute)
    {
        var parts = new List<string> { _options.KeyPrefix };

        // Tenant isolation (default: true)
        var varyByTenant = cacheAttribute?.VaryByTenant ?? true;
        if (varyByTenant && !string.IsNullOrEmpty(context.TenantId))
        {
            parts.Add($"t:{context.TenantId}");
        }

        // User isolation (default: false)
        var varyByUser = cacheAttribute?.VaryByUser ?? false;
        if (varyByUser && !string.IsNullOrEmpty(context.UserId))
        {
            parts.Add($"u:{context.UserId}");
        }

        // Request type
        parts.Add(typeof(TRequest).Name);

        // Request hash
        var requestHash = ComputeRequestHash(request);
        parts.Add(requestHash);

        return string.Join(":", parts);
    }

    private string GenerateFromTemplate<TRequest>(
        string keyTemplate,
        TRequest request,
        IRequestContext context,
        CacheAttribute cacheAttribute)
    {
        var result = new StringBuilder();

        // Prefix
        result.Append(_options.KeyPrefix);
        result.Append(':');

        // Tenant
        if (cacheAttribute.VaryByTenant && !string.IsNullOrEmpty(context.TenantId))
        {
            result.Append("t:");
            result.Append(context.TenantId);
            result.Append(':');
        }

        // User
        if (cacheAttribute.VaryByUser && !string.IsNullOrEmpty(context.UserId))
        {
            result.Append("u:");
            result.Append(context.UserId);
            result.Append(':');
        }

        // Template with substituted values
        var substituted = SubstitutePlaceholders(keyTemplate, request);
        result.Append(substituted);

        return result.ToString();
    }

    private static string SubstitutePlaceholders<TRequest>(string keyTemplate, TRequest request)
    {
        if (request is null)
        {
            return keyTemplate;
        }

        var result = keyTemplate;
        var type = typeof(TRequest);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var placeholder = $"{{{property.Name}}}";
            if (result.Contains(placeholder, StringComparison.Ordinal))
            {
                var value = property.GetValue(request);
                var stringValue = value switch
                {
                    null => "null",
                    Guid g => g.ToString("N"),
                    DateTime dt => dt.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture),
                    DateTimeOffset dto => dto.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture),
                    _ => value.ToString() ?? "null"
                };
                result = result.Replace(placeholder, stringValue, StringComparison.Ordinal);
            }
        }

        return result;
    }

    private static string ComputeRequestHash<TRequest>(TRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16]; // Use first 16 chars of hash
    }
}
