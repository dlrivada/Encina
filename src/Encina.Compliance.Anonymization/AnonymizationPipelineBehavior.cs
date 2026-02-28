using System.Diagnostics;
using System.Reflection;

using Encina.Compliance.Anonymization.Diagnostics;
using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Pipeline behavior that automatically applies anonymization, pseudonymization, and tokenization
/// transformations to response properties decorated with <see cref="AnonymizeAttribute"/>,
/// <see cref="PseudonymizeAttribute"/>, or <see cref="TokenizeAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type whose properties may be decorated with anonymization attributes.</typeparam>
/// <remarks>
/// <para>
/// Per GDPR Article 25 (Data Protection by Design and by Default), this behavior intercepts
/// responses after the handler has processed the request and applies configured transformations
/// to sensitive fields. The handler works with real data; transformation occurs on the way out.
/// </para>
/// <para>
/// The behavior scans <typeparamref name="TResponse"/> properties for:
/// <list type="bullet">
/// <item><see cref="AnonymizeAttribute"/>: Delegates to <see cref="IAnonymizationTechnique"/> strategies (irreversible)</item>
/// <item><see cref="PseudonymizeAttribute"/>: Delegates to <see cref="IPseudonymizer"/> (reversible/deterministic)</item>
/// <item><see cref="TokenizeAttribute"/>: Delegates to <see cref="ITokenizer"/> (stateful token mapping)</item>
/// </list>
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Each closed generic type resolves its attribute info exactly once
/// via a <c>static readonly</c> field. This ensures zero reflection overhead on subsequent calls
/// for the same <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/> pair.
/// </para>
/// <para>
/// The enforcement mode is controlled by <see cref="AnonymizationOptions.EnforcementMode"/>:
/// <see cref="AnonymizationEnforcementMode.Block"/> returns an error if any transformation fails,
/// <see cref="AnonymizationEnforcementMode.Warn"/> logs a warning but returns the untransformed field,
/// <see cref="AnonymizationEnforcementMode.Disabled"/> skips all transformations entirely.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Response type with declarative anonymization
/// public record GetCustomerResponse
/// {
///     public Guid Id { get; init; }
///
///     [Anonymize(Technique = AnonymizationTechnique.DataMasking)]
///     public string FullName { get; init; } = string.Empty;
///
///     [Pseudonymize(KeyId = "customer-emails")]
///     public string Email { get; init; } = string.Empty;
///
///     [Tokenize(Format = TokenFormat.FormatPreserving)]
///     public string CreditCard { get; init; } = string.Empty;
/// }
///
/// // The pipeline behavior automatically transforms decorated fields
/// // before the response reaches the caller.
/// </code>
/// </example>
public sealed class AnonymizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Static per-generic-type attribute info. Each closed generic type (e.g.,
    /// <c>AnonymizationPipelineBehavior&lt;GetCustomerQuery, GetCustomerResponse&gt;</c>)
    /// resolves its own attribute info exactly once via the CLR's static field guarantee.
    /// </summary>
    private static readonly AnonymizationAttributeInfo? CachedAttributeInfo = ResolveAttributeInfo();

    private readonly Dictionary<AnonymizationTechnique, IAnonymizationTechnique> _techniques;
    private readonly IPseudonymizer _pseudonymizer;
    private readonly ITokenizer _tokenizer;
    private readonly IKeyProvider _keyProvider;
    private readonly AnonymizationOptions _options;
    private readonly ILogger<AnonymizationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymizationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="techniques">The registered anonymization technique implementations.</param>
    /// <param name="pseudonymizer">The pseudonymizer for applying reversible/deterministic transformations.</param>
    /// <param name="tokenizer">The tokenizer for applying stateful token mappings.</param>
    /// <param name="keyProvider">Provider for cryptographic key material.</param>
    /// <param name="options">Anonymization configuration options controlling enforcement mode.</param>
    /// <param name="logger">Logger for structured anonymization pipeline logging.</param>
    public AnonymizationPipelineBehavior(
        IEnumerable<IAnonymizationTechnique> techniques,
        IPseudonymizer pseudonymizer,
        ITokenizer tokenizer,
        IKeyProvider keyProvider,
        IOptions<AnonymizationOptions> options,
        ILogger<AnonymizationPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(techniques);
        ArgumentNullException.ThrowIfNull(pseudonymizer);
        ArgumentNullException.ThrowIfNull(tokenizer);
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _techniques = techniques.ToDictionary(t => t.Technique, t => t);
        _pseudonymizer = pseudonymizer;
        _tokenizer = tokenizer;
        _keyProvider = keyProvider;
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

        var requestTypeName = typeof(TRequest).Name;
        var responseTypeName = typeof(TResponse).Name;

        // Step 1: Disabled mode — no-op, no logging, no metrics
        if (_options.EnforcementMode == AnonymizationEnforcementMode.Disabled)
        {
            _logger.AnonymizationPipelineDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        var attrInfo = CachedAttributeInfo;

        // Step 2: No anonymization attributes on response type — skip entirely
        if (attrInfo is null)
        {
            _logger.AnonymizationPipelineNoAttributes(requestTypeName, responseTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 3: Execute the handler to get the response
        var result = await nextStep().ConfigureAwait(false);

        // Step 4: If the handler returned an error, pass it through
        if (result.IsLeft)
        {
            return result;
        }

        // Step 5: Apply transformations to the response
        using var activity = AnonymizationDiagnostics.StartPipelineExecution(requestTypeName, responseTypeName);

        _logger.AnonymizationPipelineStarted(requestTypeName, responseTypeName, attrInfo.Fields.Length);

        var response = (TResponse)result;

        try
        {
            var fieldsTransformed = 0;

            foreach (var field in attrInfo.Fields)
            {
                var transformResult = await ApplyFieldTransformationAsync(
                    response!, field, responseTypeName, activity, cancellationToken).ConfigureAwait(false);

                if (transformResult.IsLeft)
                {
                    // Block mode — return error immediately
                    AnonymizationDiagnostics.PipelineExecutionsTotal.Add(1,
                        new KeyValuePair<string, object?>(AnonymizationDiagnostics.TagOutcome, "blocked"));
                    return Left<EncinaError, TResponse>((EncinaError)transformResult);
                }

                // transformResult.Right is true if field was transformed, false if skipped (warn mode failure)
                if ((bool)transformResult)
                {
                    fieldsTransformed++;
                }
            }

            _logger.AnonymizationPipelineCompleted(requestTypeName, responseTypeName, fieldsTransformed);
            AnonymizationDiagnostics.RecordCompleted(activity, fieldsTransformed);
            AnonymizationDiagnostics.PipelineExecutionsTotal.Add(1,
                new KeyValuePair<string, object?>(AnonymizationDiagnostics.TagOutcome, "completed"));

            return Right<EncinaError, TResponse>(response!);
        }
        catch (Exception ex)
        {
            _logger.AnonymizationPipelineError(requestTypeName, responseTypeName, ex.Message);
            AnonymizationDiagnostics.RecordFailed(activity, ex.Message);
            AnonymizationDiagnostics.PipelineExecutionsTotal.Add(1,
                new KeyValuePair<string, object?>(AnonymizationDiagnostics.TagOutcome, "error"));

            return Left<EncinaError, TResponse>(
                AnonymizationErrors.AnonymizationFailed("(pipeline)", ex.Message, ex));
        }
    }

    // ================================================================
    // Field transformation
    // ================================================================

    private async ValueTask<Either<EncinaError, bool>> ApplyFieldTransformationAsync(
        TResponse response,
        FieldTransformationInfo field,
        string responseTypeName,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var value = field.Property.GetValue(response);
        if (value is null)
        {
            return Right<EncinaError, bool>(false);
        }

        Either<EncinaError, object?> transformResult;

        switch (field.TransformationType)
        {
            case TransformationType.Anonymize:
                transformResult = await ApplyAnonymizationAsync(
                    value, field, cancellationToken).ConfigureAwait(false);
                break;

            case TransformationType.Pseudonymize:
                transformResult = await ApplyPseudonymizationAsync(
                    value, field, cancellationToken).ConfigureAwait(false);
                break;

            case TransformationType.Tokenize:
                transformResult = await ApplyTokenizationAsync(
                    value, field, cancellationToken).ConfigureAwait(false);
                break;

            default:
                return Right<EncinaError, bool>(false);
        }

        return transformResult.Match(
            Right: transformed =>
            {
                field.Property.SetValue(response, transformed);
                RecordFieldSuccess(field, responseTypeName);
                return Right<EncinaError, bool>(true);
            },
            Left: error => ApplyEnforcement(activity, error, field.Property.Name, responseTypeName));
    }

    private async ValueTask<Either<EncinaError, object?>> ApplyAnonymizationAsync(
        object value,
        FieldTransformationInfo field,
        CancellationToken cancellationToken)
    {
        var technique = field.AnonymizeTechnique!.Value;

        if (!_techniques.TryGetValue(technique, out var techniqueImpl))
        {
            return Left<EncinaError, object?>(
                AnonymizationErrors.TechniqueNotRegistered(technique));
        }

        if (!techniqueImpl.CanApply(field.Property.PropertyType))
        {
            return Left<EncinaError, object?>(
                AnonymizationErrors.TechniqueNotApplicable(
                    technique, field.Property.Name, field.Property.PropertyType));
        }

        var parameters = BuildAnonymizationParameters(field);

        return await techniqueImpl.ApplyAsync(
            value, field.Property.PropertyType, parameters, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, object?>> ApplyPseudonymizationAsync(
        object value,
        FieldTransformationInfo field,
        CancellationToken cancellationToken)
    {
        if (value is not string stringValue)
        {
            return Left<EncinaError, object?>(
                AnonymizationErrors.PseudonymizationFailed(
                    $"Pseudonymization requires a string value for field '{field.Property.Name}', but got '{value.GetType().Name}'."));
        }

        // Resolve key ID — from attribute or fall back to active key
        var keyId = field.PseudonymizeKeyId;
        if (string.IsNullOrWhiteSpace(keyId))
        {
            var activeKeyResult = await _keyProvider.GetActiveKeyIdAsync(cancellationToken)
                .ConfigureAwait(false);

            var keyIdResolved = activeKeyResult.Match(
                Right: id => id,
                Left: _ => (string?)null);

            if (keyIdResolved is null)
            {
                return Left<EncinaError, object?>(
                    AnonymizationErrors.PseudonymizationFailed(
                        $"No key ID specified and active key resolution failed for field '{field.Property.Name}'."));
            }

            keyId = keyIdResolved;
        }

        var pseudonymResult = await _pseudonymizer.PseudonymizeValueAsync(
            stringValue, keyId, field.PseudonymizeAlgorithm!.Value, cancellationToken).ConfigureAwait(false);

        return pseudonymResult.Match<Either<EncinaError, object?>>(
            Right: pseudonym => Right<EncinaError, object?>(pseudonym),
            Left: error => Left<EncinaError, object?>(error));
    }

    private async ValueTask<Either<EncinaError, object?>> ApplyTokenizationAsync(
        object value,
        FieldTransformationInfo field,
        CancellationToken cancellationToken)
    {
        if (value is not string stringValue)
        {
            return Left<EncinaError, object?>(
                AnonymizationErrors.TokenizationFailed(
                    $"Tokenization requires a string value for field '{field.Property.Name}', but got '{value.GetType().Name}'."));
        }

        var options = new TokenizationOptions
        {
            Format = field.TokenizeFormat!.Value,
            Prefix = field.TokenizePrefix
        };

        var tokenResult = await _tokenizer.TokenizeAsync(
            stringValue, options, cancellationToken).ConfigureAwait(false);

        return tokenResult.Match<Either<EncinaError, object?>>(
            Right: token => Right<EncinaError, object?>(token),
            Left: error => Left<EncinaError, object?>(error));
    }

    // ================================================================
    // Enforcement
    // ================================================================

    private Either<EncinaError, bool> ApplyEnforcement(
        Activity? activity, EncinaError error, string fieldName, string responseTypeName)
    {
        if (_options.EnforcementMode == AnonymizationEnforcementMode.Block)
        {
            _logger.TransformationBlocked(fieldName, responseTypeName, error.Message);
            AnonymizationDiagnostics.RecordBlocked(activity, fieldName);
            AnonymizationDiagnostics.FieldTransformationsTotal.Add(1,
                new KeyValuePair<string, object?>(AnonymizationDiagnostics.TagOutcome, "blocked"));
            return Left<EncinaError, bool>(error);
        }

        // Warn mode — log warning but allow response with untransformed field
        _logger.TransformationWarned(fieldName, responseTypeName, error.Message);
        AnonymizationDiagnostics.RecordWarned(activity, fieldName);
        AnonymizationDiagnostics.FieldTransformationsTotal.Add(1,
            new KeyValuePair<string, object?>(AnonymizationDiagnostics.TagOutcome, "warned"));
        return Right<EncinaError, bool>(false);
    }

    // ================================================================
    // Helpers
    // ================================================================

    private void RecordFieldSuccess(FieldTransformationInfo field, string responseTypeName)
    {
        var transformType = field.TransformationType.ToString();

        switch (field.TransformationType)
        {
            case TransformationType.Anonymize:
                _logger.FieldAnonymized(field.Property.Name, field.AnonymizeTechnique!.Value.ToString(), responseTypeName);
                break;

            case TransformationType.Pseudonymize:
                _logger.FieldPseudonymized(field.Property.Name, field.PseudonymizeAlgorithm!.Value.ToString(), responseTypeName);
                break;

            case TransformationType.Tokenize:
                _logger.FieldTokenized(field.Property.Name, field.TokenizeFormat!.Value.ToString(), responseTypeName);
                break;
        }

        AnonymizationDiagnostics.FieldTransformationsTotal.Add(1,
            new KeyValuePair<string, object?>(AnonymizationDiagnostics.TagOutcome, "completed"),
            new KeyValuePair<string, object?>(AnonymizationDiagnostics.TagTransformationType, transformType));
    }

    private static Dictionary<string, object>? BuildAnonymizationParameters(FieldTransformationInfo field)
    {
        Dictionary<string, object>? parameters = null;

        if (field.AnonymizeGranularity.HasValue)
        {
            parameters ??= [];
            parameters["Granularity"] = field.AnonymizeGranularity.Value;
        }

        if (field.AnonymizePattern is not null)
        {
            parameters ??= [];
            parameters["Pattern"] = field.AnonymizePattern;
        }

        if (field.AnonymizeNoiseRange.HasValue)
        {
            parameters ??= [];
            parameters["NoiseRange"] = field.AnonymizeNoiseRange.Value;
        }

        return parameters;
    }

    // ================================================================
    // Attribute resolution
    // ================================================================

    private static AnonymizationAttributeInfo? ResolveAttributeInfo()
    {
        var responseType = typeof(TResponse);
        var fields = new List<FieldTransformationInfo>();

        foreach (var property in responseType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            // Check for [Anonymize]
            var anonymizeAttr = property.GetCustomAttribute<AnonymizeAttribute>();
            if (anonymizeAttr is not null)
            {
                fields.Add(new FieldTransformationInfo(
                    Property: property,
                    TransformationType: TransformationType.Anonymize,
                    AnonymizeTechnique: anonymizeAttr.Technique,
                    AnonymizeGranularity: anonymizeAttr.Granularity,
                    AnonymizePattern: anonymizeAttr.Pattern,
                    AnonymizeNoiseRange: anonymizeAttr.NoiseRange,
                    PseudonymizeKeyId: null,
                    PseudonymizeAlgorithm: null,
                    TokenizeFormat: null,
                    TokenizePrefix: null));
                continue; // First attribute wins — no stacking
            }

            // Check for [Pseudonymize]
            var pseudonymizeAttr = property.GetCustomAttribute<PseudonymizeAttribute>();
            if (pseudonymizeAttr is not null)
            {
                fields.Add(new FieldTransformationInfo(
                    Property: property,
                    TransformationType: TransformationType.Pseudonymize,
                    AnonymizeTechnique: null,
                    AnonymizeGranularity: null,
                    AnonymizePattern: null,
                    AnonymizeNoiseRange: null,
                    PseudonymizeKeyId: pseudonymizeAttr.KeyId,
                    PseudonymizeAlgorithm: pseudonymizeAttr.Algorithm,
                    TokenizeFormat: null,
                    TokenizePrefix: null));
                continue;
            }

            // Check for [Tokenize]
            var tokenizeAttr = property.GetCustomAttribute<TokenizeAttribute>();
            if (tokenizeAttr is not null)
            {
                fields.Add(new FieldTransformationInfo(
                    Property: property,
                    TransformationType: TransformationType.Tokenize,
                    AnonymizeTechnique: null,
                    AnonymizeGranularity: null,
                    AnonymizePattern: null,
                    AnonymizeNoiseRange: null,
                    PseudonymizeKeyId: null,
                    PseudonymizeAlgorithm: null,
                    TokenizeFormat: tokenizeAttr.Format,
                    TokenizePrefix: tokenizeAttr.Prefix));
            }
        }

        // No decorated fields at all — null signals "skip entirely"
        if (fields.Count == 0)
        {
            return null;
        }

        return new AnonymizationAttributeInfo([.. fields]);
    }

    // ================================================================
    // Nested types
    // ================================================================

    /// <summary>
    /// Cached attribute information for a response type's anonymization-related decorations.
    /// </summary>
    /// <param name="Fields">
    /// The list of fields that have anonymization attributes and need transformation.
    /// </param>
    private sealed record AnonymizationAttributeInfo(FieldTransformationInfo[] Fields);

    /// <summary>
    /// Metadata for a single field's transformation configuration.
    /// </summary>
    /// <param name="Property">The property to transform.</param>
    /// <param name="TransformationType">The type of transformation to apply.</param>
    /// <param name="AnonymizeTechnique">The anonymization technique (for <see cref="TransformationType.Anonymize"/>).</param>
    /// <param name="AnonymizeGranularity">Optional granularity for generalization.</param>
    /// <param name="AnonymizePattern">Optional masking pattern.</param>
    /// <param name="AnonymizeNoiseRange">Optional noise range for perturbation.</param>
    /// <param name="PseudonymizeKeyId">The key ID for pseudonymization.</param>
    /// <param name="PseudonymizeAlgorithm">The pseudonymization algorithm.</param>
    /// <param name="TokenizeFormat">The token format.</param>
    /// <param name="TokenizePrefix">Optional token prefix.</param>
    private sealed record FieldTransformationInfo(
        PropertyInfo Property,
        TransformationType TransformationType,
        AnonymizationTechnique? AnonymizeTechnique,
        int? AnonymizeGranularity,
        string? AnonymizePattern,
        double? AnonymizeNoiseRange,
        string? PseudonymizeKeyId,
        PseudonymizationAlgorithm? PseudonymizeAlgorithm,
        TokenFormat? TokenizeFormat,
        string? TokenizePrefix);

    /// <summary>
    /// The type of transformation to apply to a field.
    /// </summary>
    private enum TransformationType
    {
        Anonymize,
        Pseudonymize,
        Tokenize
    }
}
