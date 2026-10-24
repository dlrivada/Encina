namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Factory methods and error codes for break-the-glass errors.
/// </summary>
/// <remarks>
/// <para>
/// Every message is a fixed string that carries no subject id, user id, justification or elevation
/// id. Authorization refusals use <c>encina.authorization.forbidden</c> with a <c>reason</c> detail,
/// so the HTTP, Lambda and Functions mappers answer 403 unchanged.
/// </para>
/// </remarks>
public static class BreakTheGlassErrors
{
    /// <summary>The request failed validation.</summary>
    public const string ValidationFailedCode = "break_glass.validation_failed";

    /// <summary>The scope is not declared.</summary>
    public const string ScopeUnknownCode = "break_glass.scope_unknown";

    /// <summary>An elevation for the same tenant, user, subject and scope is already active.</summary>
    public const string AlreadyActiveCode = "break_glass.already_active";

    /// <summary>The elevation does not exist.</summary>
    public const string NotFoundCode = "break_glass.not_found";

    /// <summary>The elevation is no longer active.</summary>
    public const string NotActiveCode = "break_glass.not_active";

    /// <summary>The elevation was already reviewed.</summary>
    public const string AlreadyReviewedCode = "break_glass.already_reviewed";

    /// <summary>The reviewer is the user who opened the elevation.</summary>
    public const string SelfReviewCode = "break_glass.self_review";

    /// <summary>The audit entry could not be written, so the operation was refused.</summary>
    public const string AuditFailedCode = "break_glass.audit_failed";

    /// <summary>The elevation store failed.</summary>
    public const string StoreFailedCode = "break_glass.store_failed";

    /// <summary>The operation needs a tenant and the request has none.</summary>
    public const string TenantRequiredCode = "break_glass.tenant_required";

    /// <summary>The <c>reason</c> detail of a refusal to open: the caller may not open this scope.</summary>
    public const string NotEligibleReason = "break_glass_not_eligible";

    /// <summary>The <c>reason</c> detail of a refusal to review: the caller may not review.</summary>
    public const string ReviewerNotEligibleReason = "break_glass_reviewer_not_eligible";

    private const string ReasonKey = "reason";

    /// <summary>Creates a validation error.</summary>
    /// <param name="reason">A fixed keyword naming the rule that failed (never caller input).</param>
    /// <returns>The error.</returns>
    public static EncinaError ValidationFailed(string reason) =>
        EncinaErrors.Create(
            ValidationFailedCode,
            "The break-the-glass request is not valid.",
            details: Reason(reason));

    /// <summary>Creates an unknown-scope error.</summary>
    /// <returns>The error.</returns>
    public static EncinaError ScopeUnknown() =>
        EncinaErrors.Create(ScopeUnknownCode, "The break-the-glass scope is not declared.");

    /// <summary>Creates an already-active error.</summary>
    /// <returns>The error.</returns>
    public static EncinaError AlreadyActive() =>
        EncinaErrors.Create(AlreadyActiveCode, "An elevation for this user, subject and scope is already active.");

    /// <summary>Creates a not-found error.</summary>
    /// <returns>The error.</returns>
    public static EncinaError NotFound() =>
        EncinaErrors.Create(NotFoundCode, "The break-the-glass elevation was not found.");

    /// <summary>Creates a not-active error.</summary>
    /// <returns>The error.</returns>
    public static EncinaError NotActive() =>
        EncinaErrors.Create(NotActiveCode, "The break-the-glass elevation is not active.");

    /// <summary>Creates an already-reviewed error.</summary>
    /// <returns>The error.</returns>
    public static EncinaError AlreadyReviewed() =>
        EncinaErrors.Create(AlreadyReviewedCode, "The break-the-glass elevation was already reviewed.");

    /// <summary>Creates a self-review error.</summary>
    /// <returns>The error.</returns>
    public static EncinaError SelfReview() =>
        EncinaErrors.Create(SelfReviewCode, "An elevation cannot be reviewed by the user who opened it.");

    /// <summary>Creates an audit-failed error (the operation fails closed).</summary>
    /// <returns>The error.</returns>
    public static EncinaError AuditFailed() =>
        EncinaErrors.Create(AuditFailedCode, "The break-the-glass audit entry could not be written.");

    /// <summary>Creates a store-failed error.</summary>
    /// <param name="exception">The provider exception, or <see langword="null"/>.</param>
    /// <returns>The error.</returns>
    public static EncinaError StoreFailed(Exception? exception = null) =>
        EncinaErrors.Create(StoreFailedCode, "The break-the-glass store failed.", exception);

    /// <summary>Creates a tenant-required error.</summary>
    /// <returns>The error.</returns>
    public static EncinaError TenantRequired() =>
        EncinaErrors.Create(TenantRequiredCode, "A tenant is required for break-the-glass operations.");

    /// <summary>Creates the refusal returned when the caller may not open the scope (403).</summary>
    /// <returns>An <c>encina.authorization.forbidden</c> error with <c>reason = break_glass_not_eligible</c>.</returns>
    public static EncinaError NotEligible() =>
        EncinaErrors.Forbidden(details: Reason(NotEligibleReason));

    /// <summary>Creates the refusal returned when the caller may not review (403).</summary>
    /// <returns>An <c>encina.authorization.forbidden</c> error with <c>reason = break_glass_reviewer_not_eligible</c>.</returns>
    public static EncinaError ReviewerNotEligible() =>
        EncinaErrors.Forbidden(details: Reason(ReviewerNotEligibleReason));

    private static Dictionary<string, object?> Reason(string reason) => new() { [ReasonKey] = reason };
}
