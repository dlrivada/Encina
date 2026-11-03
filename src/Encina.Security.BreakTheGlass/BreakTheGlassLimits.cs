namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Size limits of the break-the-glass elevation, shared by the validators, every store, every schema and
/// every EF Core configuration so that all ten providers accept exactly the same values.
/// </summary>
/// <remarks>
/// Lengths are measured in characters. The service measures the subject, the scope and the justification of a request,
/// the revoke reason and the review note after trimming, and stores the trimmed value. It does not trim the user id or the
/// tenant id: they are measured as the identity and the request context supply them (a blank tenant id is "no tenant", not a value).
/// A value over its limit is refused with <c>break_glass.validation_failed</c> before any store call made by the service; the access gate
/// does not measure its inputs and denies an unknown or over-long value because no row matches it.
/// <para>
/// Keep justifications and review notes operational, for example "on call, unassigned patient, crisis"; do not write clinical
/// detail (data minimisation, GDPR Art. 5(1)(c) and Art. 9; decision D48).
/// </para>
/// </remarks>
public static class BreakTheGlassLimits
{
    /// <summary>Maximum length of <see cref="BreakTheGlassElevation.TenantId"/>.</summary>
    public const int TenantIdMaxLength = 128;

    /// <summary>Maximum length of <see cref="BreakTheGlassElevation.UserId"/>.</summary>
    public const int UserIdMaxLength = 256;

    /// <summary>Maximum length of <see cref="BreakTheGlassElevation.SubjectId"/>.</summary>
    public const int SubjectIdMaxLength = 256;

    /// <summary>Maximum length of <see cref="BreakTheGlassElevation.Scope"/>.</summary>
    public const int ScopeMaxLength = 256;

    /// <summary>Maximum length of <see cref="BreakTheGlassElevation.ReviewedByUserId"/>.</summary>
    public const int ReviewedByUserIdMaxLength = 256;

    /// <summary>Maximum length of <see cref="BreakTheGlassElevation.CloseReason"/> (a code or a short operational reason).</summary>
    public const int CloseReasonMaxLength = 256;

    /// <summary>Maximum length of <see cref="BreakTheGlassElevation.ReviewNote"/>.</summary>
    public const int ReviewNoteMaxLength = 2000;

    /// <summary>Maximum length of <see cref="BreakTheGlassElevation.Justification"/> (the same value as <see cref="BreakTheGlassOptions.MaxJustificationLength"/>).</summary>
    public const int JustificationMaxLength = BreakTheGlassOptions.MaxJustificationLength;
}
