using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.PrivacyByDesign.Diagnostics;
using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Default implementation of <see cref="IPrivacyByDesignValidator"/> that orchestrates
/// data minimization, purpose limitation, and default privacy checks.
/// </summary>
/// <remarks>
/// <para>
/// Coordinates the <see cref="IDataMinimizationAnalyzer"/> for field-level analysis and the
/// <see cref="IPurposeRegistry"/> for purpose lookup, producing an aggregate
/// <see cref="PrivacyValidationResult"/>.
/// </para>
/// <para>
/// Uses a static <see cref="ConcurrentDictionary{TKey, TValue}"/> to cache
/// <see cref="EnforceDataMinimizationAttribute"/> lookups per request type, ensuring zero
/// reflection overhead after the first invocation for each type.
/// </para>
/// <para>
/// Per GDPR Article 25(1), the controller shall implement appropriate technical and
/// organisational measures "designed to implement data-protection principles, such as
/// data minimisation, in an effective manner."
/// </para>
/// </remarks>
internal sealed class DefaultPrivacyByDesignValidator : IPrivacyByDesignValidator
{
    private static readonly ConcurrentDictionary<Type, EnforceDataMinimizationAttribute?> AttributeCache = new();

    private readonly IDataMinimizationAnalyzer _analyzer;
    private readonly IPurposeRegistry _purposeRegistry;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultPrivacyByDesignValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPrivacyByDesignValidator"/> class.
    /// </summary>
    /// <param name="analyzer">The data minimization analyzer for field-level inspection.</param>
    /// <param name="purposeRegistry">The purpose registry for purpose definition lookup.</param>
    /// <param name="timeProvider">Time provider for deterministic timestamps.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DefaultPrivacyByDesignValidator(
        IDataMinimizationAnalyzer analyzer,
        IPurposeRegistry purposeRegistry,
        TimeProvider timeProvider,
        ILogger<DefaultPrivacyByDesignValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(analyzer);
        ArgumentNullException.ThrowIfNull(purposeRegistry);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _analyzer = analyzer;
        _purposeRegistry = purposeRegistry;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PrivacyValidationResult>> ValidateAsync<TRequest>(
        TRequest request,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = typeof(TRequest);
        var requestTypeName = requestType.FullName ?? requestType.Name;

        try
        {
            var violations = new List<PrivacyViolation>();

            // Step 1: Data minimization analysis.
            var minimizationResult = await _analyzer.AnalyzeAsync(request, cancellationToken).ConfigureAwait(false);
            MinimizationReport? minimizationReport = null;

            minimizationResult.Match(
                Right: report =>
                {
                    minimizationReport = report;
                    foreach (var field in report.UnnecessaryFields)
                    {
                        if (field.HasValue)
                        {
                            violations.Add(new PrivacyViolation(
                                FieldName: field.FieldName,
                                ViolationType: PrivacyViolationType.DataMinimization,
                                Message: $"Field '{field.FieldName}' is not strictly necessary: {field.Reason}",
                                Severity: field.Severity));
                        }
                    }
                },
                Left: error =>
                {
                    _logger.PbDMinimizationAnalysisFailed(requestTypeName, error.Message);
                });

            // Step 2: Purpose limitation (if a purpose is declared).
            var attribute = AttributeCache.GetOrAdd(requestType, static type =>
                type.GetCustomAttribute<EnforceDataMinimizationAttribute>());

            PurposeValidationResult? purposeValidation = null;

            if (attribute?.Purpose is not null)
            {
                var purposeResult = await ValidatePurposeLimitationAsync(
                    request, attribute.Purpose, moduleId, cancellationToken).ConfigureAwait(false);

                purposeResult.Match(
                    Right: result =>
                    {
                        purposeValidation = result;
                        foreach (var field in result.ViolatingFields)
                        {
                            violations.Add(new PrivacyViolation(
                                FieldName: field,
                                ViolationType: PrivacyViolationType.PurposeLimitation,
                                Message: $"Field '{field}' is not allowed for purpose '{attribute.Purpose}'.",
                                Severity: MinimizationSeverity.Warning));
                        }
                    },
                    Left: error =>
                    {
                        _logger.PbDPurposeValidationFailed(requestTypeName, error.Message);
                    });
            }

            // Step 3: Default privacy checks.
            var defaultsResult = await _analyzer.InspectDefaultsAsync(request, cancellationToken).ConfigureAwait(false);

            defaultsResult.Match(
                Right: defaults =>
                {
                    foreach (var defaultInfo in defaults)
                    {
                        if (!defaultInfo.MatchesDefault)
                        {
                            violations.Add(new PrivacyViolation(
                                FieldName: defaultInfo.FieldName,
                                ViolationType: PrivacyViolationType.DefaultPrivacy,
                                Message: $"Field '{defaultInfo.FieldName}' deviates from privacy default "
                                    + $"(expected: {defaultInfo.DeclaredDefault ?? "null"}, actual: {defaultInfo.ActualValue ?? "null"}).",
                                Severity: MinimizationSeverity.Info));
                        }
                    }
                },
                Left: error =>
                {
                    _logger.PbDDefaultsInspectionFailed(requestTypeName, error.Message);
                });

            var result = new PrivacyValidationResult
            {
                RequestTypeName = requestTypeName,
                Violations = violations,
                MinimizationReport = minimizationReport,
                PurposeValidation = purposeValidation,
                ValidatedAtUtc = _timeProvider.GetUtcNow(),
                ModuleId = moduleId,
            };

            _logger.PbDValidationCompleted(requestTypeName, result.IsCompliant, violations.Count, moduleId);

            // Record purpose validation metrics when purpose was checked
            if (purposeValidation is not null)
            {
                PrivacyByDesignDiagnostics.PurposeValidationsTotal.Add(1,
                    new TagList { { PrivacyByDesignDiagnostics.TagRequestType, requestTypeName } });

                if (purposeValidation.ViolatingFields.Count > 0)
                {
                    PrivacyByDesignDiagnostics.PurposeViolationsTotal.Add(
                        purposeValidation.ViolatingFields.Count,
                        new TagList { { PrivacyByDesignDiagnostics.TagRequestType, requestTypeName } });
                }
            }

            return Right<EncinaError, PrivacyValidationResult>(result);
        }
        catch (Exception ex)
        {
            _logger.PbDValidationError(requestTypeName, ex);
            return Left<EncinaError, PrivacyValidationResult>(
                PrivacyByDesignErrors.StoreError("Validate", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, MinimizationReport>> AnalyzeMinimizationAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        return _analyzer.AnalyzeAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PurposeValidationResult>> ValidatePurposeLimitationAsync<TRequest>(
        TRequest request,
        string purpose,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(purpose);

        try
        {
            var requestType = typeof(TRequest);
            var cache = DefaultDataMinimizationAnalyzer.MetadataCache.GetOrAdd(
                requestType, static type => FieldMetadataCache.Build(type));

            // Look up the purpose definition from the registry (module-scoped → global fallback).
            var purposeResult = await _purposeRegistry.GetPurposeAsync(purpose, moduleId, cancellationToken)
                .ConfigureAwait(false);

            // Determine allowed fields: from PurposeDefinition if registered, otherwise from attributes.
            var allowedFieldsSet = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var violatingFields = new List<string>();

            var hasPurposeDefinition = false;

            purposeResult.Match(
                Right: option => option.Match(
                    Some: definition =>
                    {
                        hasPurposeDefinition = true;
                        foreach (var field in definition.AllowedFields)
                        {
                            allowedFieldsSet.Add(field);
                        }
                    },
                    None: () => { }),
                Left: _ => { });

            if (hasPurposeDefinition)
            {
                // Validate each property against the purpose definition's allowed fields.
                for (var i = 0; i < cache.Properties.Length; i++)
                {
                    var property = cache.Properties[i];
                    var value = property.GetValue(request);

                    // Only flag fields that have a non-default value and are not in allowed list.
                    if (value is not null && !allowedFieldsSet.Contains(property.Name))
                    {
                        violatingFields.Add(property.Name);
                    }
                }
            }
            else
            {
                // Fallback: use PurposeLimitation attributes on properties.
                for (var i = 0; i < cache.Properties.Length; i++)
                {
                    var purposeAttr = cache.PurposeLimitation[i];
                    if (purposeAttr is not null && purposeAttr.Purpose != purpose)
                    {
                        var property = cache.Properties[i];
                        var value = property.GetValue(request);

                        if (value is not null)
                        {
                            violatingFields.Add(property.Name);
                        }
                    }

                    if (purposeAttr is not null)
                    {
                        allowedFieldsSet.Add(cache.Properties[i].Name);
                    }
                }
            }

            var result = new PurposeValidationResult(
                DeclaredPurpose: purpose,
                AllowedFields: [.. allowedFieldsSet],
                ViolatingFields: violatingFields,
                IsValid: violatingFields.Count == 0);

            return Right<EncinaError, PurposeValidationResult>(result);
        }
        catch (Exception ex)
        {
            _logger.PbDPurposeLimitationError(typeof(TRequest).FullName ?? typeof(TRequest).Name, ex);
            return Left<EncinaError, PurposeValidationResult>(
                PrivacyByDesignErrors.StoreError("ValidatePurposeLimitation", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>> ValidateDefaultsAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        return _analyzer.InspectDefaultsAsync(request, cancellationToken);
    }
}
