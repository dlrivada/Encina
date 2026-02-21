using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Encina.Security.Audit;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Diagnostics;
using Encina.Security.PII.Internal;
using Encina.Security.PII.Strategies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.PII;

/// <summary>
/// Default implementation of <see cref="IPIIMasker"/> and <see cref="IPiiMasker"/>
/// that masks PII using configurable strategies per <see cref="PIIType"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the primary PII masking orchestration for the Encina framework.
/// It delegates masking to <see cref="IMaskingStrategy"/> implementations registered
/// for each <see cref="PIIType"/>, and integrates with the <c>Encina.Security.Audit</c>
/// package by implementing <see cref="IPiiMasker"/>.
/// </para>
/// <para>
/// Property metadata is discovered via reflection on first access per type and cached
/// in <see cref="PIIPropertyScanner"/> using a <c>ConcurrentDictionary</c> for
/// thread-safe, lock-free lookups on subsequent calls.
/// </para>
/// <para>
/// The <see cref="MaskObject{T}"/> method uses JSON serialization to create a deep copy
/// of the object, then applies masking to decorated properties. This ensures the original
/// object is never modified.
/// </para>
/// <para>
/// When <see cref="PIIOptions.EnableTracing"/> is enabled, OpenTelemetry activities are
/// created for each masking operation via <see cref="PIIDiagnostics.ActivitySource"/>.
/// When <see cref="PIIOptions.EnableMetrics"/> is enabled, counters and histograms are
/// recorded via <see cref="PIIDiagnostics.Meter"/>.
/// </para>
/// </remarks>
public sealed class PIIMasker : IPIIMasker, IPiiMasker
{
    private readonly Dictionary<PIIType, IMaskingStrategy> _strategies;
    private readonly PIIOptions _options;
    private readonly ILogger<PIIMasker> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PIIMasker"/> class.
    /// </summary>
    /// <param name="options">The PII configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">
    /// The service provider for resolving custom <see cref="IMaskingStrategy"/> implementations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public PIIMasker(
        IOptions<PIIOptions> options,
        ILogger<PIIMasker> logger,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _options = options.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Build strategy map: custom overrides take precedence over defaults
        _strategies = BuildStrategyMap(serviceProvider);
    }

    /// <inheritdoc />
    public string Mask(string value, PIIType type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var strategy = GetStrategy(type);
        var maskingOptions = BuildMaskingOptions(type, _options.DefaultMode);

        return strategy.Apply(value, maskingOptions);
    }

    /// <inheritdoc />
    public string Mask(string value, string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            var maskingOptions = BuildMaskingOptions(PIIType.Custom, _options.DefaultMode);
            return Regex.Replace(value, pattern, match =>
                new string(maskingOptions.MaskCharacter, match.Length));
        }
        catch (RegexParseException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern for PII masking: {Pattern}", pattern);
            return value;
        }
    }

    /// <inheritdoc />
    public T MaskObject<T>(T obj) where T : class
    {
        ArgumentNullException.ThrowIfNull(obj);

        var typeName = typeof(T).Name;
        var properties = PIIPropertyScanner.GetProperties(obj.GetType());

        // Start tracing activity (only if listeners are registered and tracing is enabled)
        Activity? activity = null;
        if (_options.EnableTracing)
        {
            activity = PIIDiagnostics.StartMaskObject(typeName);
            activity?.SetTag(PIIDiagnostics.TagPropertyCount, properties.Length);
        }

        // Structured logging
        PIILogMessages.PIIMaskingStarted(_logger, typeName, properties.Length);

        var stopwatch = _options.EnableMetrics || _options.EnableTracing
            ? Stopwatch.StartNew()
            : null;

        try
        {
            var result = MaskObjectInternal(obj, logContextOnly: false, out var maskedCount);

            stopwatch?.Stop();
            var elapsedMs = stopwatch?.Elapsed.TotalMilliseconds ?? 0;

            // Record success tracing
            PIIDiagnostics.RecordSuccess(activity, maskedCount);
            activity?.Dispose();

            // Structured log completion
            PIILogMessages.PIIMaskingCompleted(_logger, typeName, maskedCount, elapsedMs);

            // Record metrics
            if (_options.EnableMetrics)
            {
                PIIDiagnostics.RecordOperationMetrics(
                    typeName,
                    _options.DefaultMode.ToString(),
                    success: true,
                    maskedCount,
                    elapsedMs);
            }

            return result;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            stopwatch?.Stop();
            var elapsedMs = stopwatch?.Elapsed.TotalMilliseconds ?? 0;

            // Record failure tracing
            PIIDiagnostics.RecordFailure(activity, ex);
            activity?.Dispose();

            // Structured log failure
            PIILogMessages.PIIMaskingFailed(_logger, typeName, ex.Message);

            // Record error metrics
            if (_options.EnableMetrics)
            {
                PIIDiagnostics.RecordOperationMetrics(
                    typeName,
                    _options.DefaultMode.ToString(),
                    success: false,
                    maskedCount: 0,
                    elapsedMs);

                PIIDiagnostics.RecordErrorMetric(ex.GetType().Name);
            }

            throw;
        }
    }

    /// <inheritdoc cref="IPiiMasker.MaskForAudit{T}(T)" />
    public T MaskForAudit<T>(T request) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.MaskInAuditTrails)
        {
            return request;
        }

        // For value types, we cannot mask properties — return as-is
        if (!typeof(T).IsClass)
        {
            return request;
        }

        try
        {
            return MaskObjectInternal(request, logContextOnly: false, out _);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "Failed to mask PII for audit on type {TypeName}", typeof(T).Name);
            return request;
        }
    }

    /// <inheritdoc cref="IPiiMasker.MaskForAudit(object)" />
    public object MaskForAudit(object request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.MaskInAuditTrails)
        {
            return request;
        }

        try
        {
            return MaskObjectViaJson(request, request.GetType(), logContextOnly: false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "Failed to mask PII for audit on type {TypeName}", request.GetType().Name);
            return request;
        }
    }

    private T MaskObjectInternal<T>(T obj, bool logContextOnly, out int maskedCount)
    {
        var type = obj!.GetType();
        var properties = PIIPropertyScanner.GetProperties(type);

        if (properties.Length == 0)
        {
            // No PII properties found - check sensitive field patterns
            maskedCount = 0;
            return MaskViaSensitiveFieldPatterns(obj, type);
        }

        return MaskViaPropertyMetadata(obj, type, properties, logContextOnly, out maskedCount);
    }

    private T MaskViaPropertyMetadata<T>(
        T obj,
        Type type,
        PropertyMaskingMetadata[] properties,
        bool logContextOnly,
        out int maskedCount)
    {
        maskedCount = 0;

        try
        {
            // Serialize to JSON for deep copy
            var json = JsonSerializer.Serialize(obj, type, _jsonOptions);
            var node = JsonNode.Parse(json);

            if (node is not JsonObject jsonObj)
            {
                return obj;
            }

            foreach (var prop in properties)
            {
                // Skip log-only properties when not in log context
                if (prop.LogOnly && !logContextOnly)
                {
                    continue;
                }

                var propertyName = _jsonOptions.PropertyNamingPolicy?.ConvertName(prop.Property.Name)
                    ?? prop.Property.Name;

                if (jsonObj[propertyName] is not JsonValue jsonValue)
                {
                    continue;
                }

                var originalValue = jsonValue.ToString();
                if (string.IsNullOrEmpty(originalValue))
                {
                    continue;
                }

                var maskedValue = MaskPropertyValue(originalValue, prop);
                jsonObj[propertyName] = maskedValue;
                maskedCount++;

                // Trace-level log per property
                if (_options.EnableTracing)
                {
                    PIILogMessages.StrategyApplied(
                        _logger,
                        prop.Property.Name,
                        prop.Type.ToString(),
                        GetStrategy(prop.Type).GetType().Name);
                }
            }

            // Also apply sensitive field pattern matching
            MaskSensitiveFieldsInNode(jsonObj);

            var result = node.Deserialize<T>(_jsonOptions);
            return result ?? obj;
        }
        catch (JsonException ex)
        {
            PIILogMessages.SerializationFailed(_logger, ex, type.Name);
            return obj;
        }
    }

    private T MaskViaSensitiveFieldPatterns<T>(T obj, Type type)
    {
        if (_options.SensitiveFieldPatterns.Count == 0)
        {
            return obj;
        }

        try
        {
            var json = JsonSerializer.Serialize(obj, type, _jsonOptions);
            var node = JsonNode.Parse(json);

            if (node is not JsonObject jsonObj)
            {
                return obj;
            }

            var modified = MaskSensitiveFieldsInNode(jsonObj);
            if (!modified)
            {
                return obj;
            }

            var result = node.Deserialize<T>(_jsonOptions);
            return result ?? obj;
        }
        catch (JsonException)
        {
            return obj;
        }
    }

    private object MaskObjectViaJson(object obj, Type type, bool logContextOnly)
    {
        var properties = PIIPropertyScanner.GetProperties(type);

        try
        {
            var json = JsonSerializer.Serialize(obj, type, _jsonOptions);
            var node = JsonNode.Parse(json);

            if (node is not JsonObject jsonObj)
            {
                return obj;
            }

            // Apply attribute-based masking
            foreach (var prop in properties)
            {
                if (prop.LogOnly && !logContextOnly)
                {
                    continue;
                }

                var propertyName = _jsonOptions.PropertyNamingPolicy?.ConvertName(prop.Property.Name)
                    ?? prop.Property.Name;

                if (jsonObj[propertyName] is not JsonValue jsonValue)
                {
                    continue;
                }

                var originalValue = jsonValue.ToString();
                if (string.IsNullOrEmpty(originalValue))
                {
                    continue;
                }

                var maskedValue = MaskPropertyValue(originalValue, prop);
                jsonObj[propertyName] = maskedValue;
            }

            // Apply sensitive field pattern matching
            MaskSensitiveFieldsInNode(jsonObj);

            var result = node.Deserialize(type, _jsonOptions);
            return result ?? obj;
        }
        catch (JsonException)
        {
            return obj;
        }
    }

    private string MaskPropertyValue(string value, PropertyMaskingMetadata metadata)
    {
        // Custom replacement takes priority
        if (metadata.Replacement is not null)
        {
            return metadata.Replacement;
        }

        // Custom pattern masking
        if (metadata.Pattern is not null)
        {
            try
            {
                var maskingOptions = BuildMaskingOptions(metadata.Type, metadata.Mode);
                return Regex.Replace(value, metadata.Pattern, match =>
                    new string(maskingOptions.MaskCharacter, match.Length));
            }
            catch (RegexParseException)
            {
                return value;
            }
        }

        // Strategy-based masking
        var strategy = GetStrategy(metadata.Type);
        var options = BuildMaskingOptions(metadata.Type, metadata.Mode);
        return strategy.Apply(value, options);
    }

    private bool MaskSensitiveFieldsInNode(JsonObject obj)
    {
        var modified = false;
        var keysToRedact = new List<string>();
        var nodesToRecurse = new List<(string Key, JsonNode Node)>();

        foreach (var property in obj)
        {
            if (IsSensitiveField(property.Key))
            {
                keysToRedact.Add(property.Key);
            }
            else if (property.Value is not null)
            {
                nodesToRecurse.Add((property.Key, property.Value));
            }
        }

        foreach (var key in keysToRedact)
        {
            obj[key] = "[REDACTED]";
            modified = true;
        }

        foreach (var (_, childNode) in nodesToRecurse)
        {
            switch (childNode)
            {
                case JsonObject childObj:
                    if (MaskSensitiveFieldsInNode(childObj))
                    {
                        modified = true;
                    }

                    break;
                case JsonArray childArray:
                    if (MaskSensitiveFieldsInArray(childArray))
                    {
                        modified = true;
                    }

                    break;
            }
        }

        return modified;
    }

    private bool MaskSensitiveFieldsInArray(JsonArray array)
    {
        var modified = false;

        foreach (var item in array)
        {
            if (item is JsonObject childObj && MaskSensitiveFieldsInNode(childObj))
            {
                modified = true;
            }
        }

        return modified;
    }

    private bool IsSensitiveField(string fieldName)
    {
        foreach (var pattern in _options.SensitiveFieldPatterns)
        {
            if (fieldName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private IMaskingStrategy GetStrategy(PIIType type)
    {
        if (_strategies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }

        // Log strategy not found
        PIILogMessages.StrategyNotFound(_logger, type.ToString());

        // Fallback: use Full masking for unknown types
        return _strategies[PIIType.Custom];
    }

    private Dictionary<PIIType, IMaskingStrategy> BuildStrategyMap(IServiceProvider serviceProvider)
    {
        var strategies = new Dictionary<PIIType, IMaskingStrategy>
        {
            [PIIType.Email] = new EmailMaskingStrategy(),
            [PIIType.Phone] = new PhoneMaskingStrategy(),
            [PIIType.CreditCard] = new CreditCardMaskingStrategy(),
            [PIIType.SSN] = new SSNMaskingStrategy(),
            [PIIType.Name] = new NameMaskingStrategy(),
            [PIIType.Address] = new AddressMaskingStrategy(),
            [PIIType.DateOfBirth] = new DateOfBirthMaskingStrategy(),
            [PIIType.IPAddress] = new IPAddressMaskingStrategy(),
            [PIIType.Custom] = new FullMaskingStrategy()
        };

        // Override with custom strategies from options
        foreach (var (piiType, strategyType) in _options.CustomStrategies)
        {
            var customStrategy = serviceProvider.GetService(strategyType) as IMaskingStrategy;
            if (customStrategy is not null)
            {
                strategies[piiType] = customStrategy;
            }
            else
            {
                _logger.LogWarning(
                    "Custom masking strategy {StrategyType} for {PIIType} could not be resolved from DI",
                    strategyType.Name,
                    piiType);
            }
        }

        return strategies;
    }

    private static MaskingOptions BuildMaskingOptions(PIIType type, MaskingMode mode)
    {
        return type switch
        {
            PIIType.Email => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = false,
                VisibleCharactersStart = 1,
                VisibleCharactersEnd = 0
            },
            PIIType.Phone => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = true,
                VisibleCharactersStart = 0,
                VisibleCharactersEnd = 4
            },
            PIIType.CreditCard => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = true,
                VisibleCharactersStart = 0,
                VisibleCharactersEnd = 4
            },
            PIIType.SSN => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = true,
                VisibleCharactersStart = 0,
                VisibleCharactersEnd = 4
            },
            PIIType.Name => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = true,
                VisibleCharactersStart = 1,
                VisibleCharactersEnd = 0
            },
            PIIType.Address => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = true,
                VisibleCharactersStart = 0,
                VisibleCharactersEnd = 0
            },
            PIIType.DateOfBirth => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = true,
                VisibleCharactersStart = 0,
                VisibleCharactersEnd = 0
            },
            PIIType.IPAddress => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = false,
                VisibleCharactersStart = 0,
                VisibleCharactersEnd = 0
            },
            _ => new MaskingOptions
            {
                Mode = mode,
                MaskCharacter = '*',
                PreserveLength = true,
                VisibleCharactersStart = 0,
                VisibleCharactersEnd = 0
            }
        };
    }
}
