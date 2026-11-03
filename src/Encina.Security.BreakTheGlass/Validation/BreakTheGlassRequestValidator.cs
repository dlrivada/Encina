using LanguageExt;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Validates and normalises a <see cref="BreakTheGlassRequest"/> against the configured limits.
/// </summary>
/// <remarks>
/// Rules: subject id non-blank; scope declared; justification trimmed, at least
/// <see cref="BreakTheGlassOptions.MinimumJustificationLength"/> and at most
/// <see cref="BreakTheGlassOptions.MaxJustificationLength"/> characters; duration greater than zero
/// and at most the smaller of the global and the per-scope limit; the global limit itself at most 24 hours.
/// A failure names the broken rule with a fixed keyword in the <c>reason</c> detail, never the input.
/// </remarks>
public sealed class BreakTheGlassRequestValidator
{
    private readonly IOptions<BreakTheGlassOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreakTheGlassRequestValidator"/> class.
    /// </summary>
    /// <param name="options">The break-the-glass options.</param>
    public BreakTheGlassRequestValidator(IOptions<BreakTheGlassOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>
    /// Validates a request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The request with the subject, scope and justification trimmed, or the first broken rule.</returns>
    public Either<EncinaError, BreakTheGlassRequest> Validate(BreakTheGlassRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = _options.Value;

        var subjectId = Normalise(request.SubjectId);
        var scope = Normalise(request.Scope);
        var justification = Normalise(request.Justification);

        if (IdentifierViolation(subjectId, scope) is { } identifierError)
        {
            return Left(identifierError);
        }

        if (!options.Scopes.TryGetValue(scope, out var scopeOptions))
        {
            return Left(BreakTheGlassErrors.ScopeUnknown());
        }

        var violation = JustificationViolation(justification, options) ?? DurationViolation(request.Duration, scopeOptions, options);
        return violation is { } error
            ? Left(error)
            : Right(request with { SubjectId = subjectId, Scope = scope, Justification = justification });
    }

    // The subject must be present; the subject and the scope must fit their columns (checked before the scope lookup).
    private static EncinaError? IdentifierViolation(string subjectId, string scope)
    {
        if (subjectId.Length == 0)
        {
            return BreakTheGlassErrors.ValidationFailed("subject_required");
        }

        if (subjectId.Length > BreakTheGlassLimits.SubjectIdMaxLength)
        {
            return BreakTheGlassErrors.ValidationFailed("subject_too_long");
        }

        return scope.Length > BreakTheGlassLimits.ScopeMaxLength
            ? BreakTheGlassErrors.ValidationFailed("scope_too_long")
            : default(EncinaError?);
    }

    private static EncinaError? JustificationViolation(string justification, BreakTheGlassOptions options)
    {
        if (justification.Length < options.MinimumJustificationLength || justification.Length == 0)
        {
            return BreakTheGlassErrors.ValidationFailed("justification_too_short");
        }

        return justification.Length > BreakTheGlassLimits.JustificationMaxLength
            ? BreakTheGlassErrors.ValidationFailed("justification_too_long")
            : default(EncinaError?);
    }

    private static string Normalise(string? value) => value?.Trim() ?? string.Empty;

    private static EncinaError? DurationViolation(TimeSpan duration, BreakTheGlassScopeOptions scopeOptions, BreakTheGlassOptions options)
    {
        if (!IsValidGlobalLimit(options.MaxDuration))
        {
            return BreakTheGlassErrors.ValidationFailed("max_duration_invalid");
        }

        if (duration <= TimeSpan.Zero)
        {
            return BreakTheGlassErrors.ValidationFailed("duration_not_positive");
        }

        return duration > EffectiveLimit(scopeOptions, options)
            ? BreakTheGlassErrors.ValidationFailed("duration_too_long")
            : default(EncinaError?);
    }

    private static bool IsValidGlobalLimit(TimeSpan limit) =>
        limit > TimeSpan.Zero && limit <= BreakTheGlassOptions.MaxAllowedDuration;

    private static TimeSpan EffectiveLimit(BreakTheGlassScopeOptions scopeOptions, BreakTheGlassOptions options) =>
        scopeOptions.MaxDuration is { } scoped && scoped < options.MaxDuration ? scoped : options.MaxDuration;
}
