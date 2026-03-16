namespace Encina.Compliance.LawfulBasis.Errors;

/// <summary>
/// Factory methods for lawful basis-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>lawfulbasis.{category}</c>.
/// All errors include structured metadata for observability and GDPR article references.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 5(2) — Accountability principle (audit trail of lawful basis determinations).
/// Article 6(1) — Lawfulness of processing (six lawful bases).
/// Article 6(1)(f) — Legitimate interests (requires LIA documentation).
/// </para>
/// </remarks>
public static class LawfulBasisErrors
{
    private const string MetadataKeyRegistrationId = "registrationId";
    private const string MetadataKeyLIAId = "liaId";
    private const string MetadataKeyStage = "lawful_basis";

    // --- Error codes ---

    /// <summary>Error code when a lawful basis registration is not found.</summary>
    public const string RegistrationNotFoundCode = "lawfulbasis.registration_not_found";

    /// <summary>Error code when a lawful basis registration is already revoked.</summary>
    public const string RegistrationAlreadyRevokedCode = "lawfulbasis.registration_already_revoked";

    /// <summary>Error code when a LIA is not found by its identifier.</summary>
    public const string LIANotFoundCode = "lawfulbasis.lia_not_found";

    /// <summary>Error code when a LIA is not found by its reference.</summary>
    public const string LIANotFoundByReferenceCode = "lawfulbasis.lia_not_found_by_reference";

    /// <summary>Error code when a LIA has already been decided (approved or rejected).</summary>
    public const string LIAAlreadyDecidedCode = "lawfulbasis.lia_already_decided";

    /// <summary>Error code for invalid aggregate state transitions.</summary>
    public const string InvalidStateTransitionCode = "lawfulbasis.invalid_state_transition";

    /// <summary>Error code when a store or repository operation fails.</summary>
    public const string StoreErrorCode = "lawfulbasis.store_error";

    // --- Registration errors ---

    /// <summary>
    /// Creates an error when a lawful basis registration is not found.
    /// </summary>
    /// <param name="id">The registration identifier that was not found.</param>
    /// <returns>An error indicating the registration was not found.</returns>
    public static EncinaError RegistrationNotFound(Guid id) =>
        EncinaErrors.Create(
            code: RegistrationNotFoundCode,
            message: $"Lawful basis registration '{id}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRegistrationId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a lawful basis registration is already revoked.
    /// </summary>
    /// <param name="id">The registration identifier.</param>
    /// <returns>An error indicating the registration is already revoked.</returns>
    public static EncinaError RegistrationAlreadyRevoked(Guid id) =>
        EncinaErrors.Create(
            code: RegistrationAlreadyRevokedCode,
            message: $"Lawful basis registration '{id}' has already been revoked.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRegistrationId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- LIA errors ---

    /// <summary>
    /// Creates an error when a LIA is not found by its identifier.
    /// </summary>
    /// <param name="id">The LIA identifier that was not found.</param>
    /// <returns>An error indicating the LIA was not found.</returns>
    public static EncinaError LIANotFound(Guid id) =>
        EncinaErrors.Create(
            code: LIANotFoundCode,
            message: $"Legitimate Interest Assessment '{id}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyLIAId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_6_1_f_lia"
            });

    /// <summary>
    /// Creates an error when a LIA is not found by its reference identifier.
    /// </summary>
    /// <param name="reference">The LIA reference that was not found.</param>
    /// <returns>An error indicating the LIA was not found for the given reference.</returns>
    public static EncinaError LIANotFoundByReference(string reference) =>
        EncinaErrors.Create(
            code: LIANotFoundByReferenceCode,
            message: $"Legitimate Interest Assessment with reference '{reference}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["liaReference"] = reference,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_6_1_f_lia"
            });

    /// <summary>
    /// Creates an error when a LIA has already been decided and cannot be approved or rejected again.
    /// </summary>
    /// <param name="id">The LIA identifier.</param>
    /// <returns>An error indicating the LIA has already been decided.</returns>
    public static EncinaError LIAAlreadyDecided(Guid id) =>
        EncinaErrors.Create(
            code: LIAAlreadyDecidedCode,
            message: $"Legitimate Interest Assessment '{id}' has already been decided (approved or rejected).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyLIAId] = id.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Generic errors ---

    /// <summary>
    /// Creates an error for an invalid aggregate state transition.
    /// </summary>
    /// <param name="operation">The attempted operation.</param>
    /// <param name="reason">Explanation of why the transition is not valid.</param>
    /// <returns>An error indicating the state transition is not valid.</returns>
    public static EncinaError InvalidStateTransition(string operation, string reason) =>
        EncinaErrors.Create(
            code: InvalidStateTransitionCode,
            message: $"Invalid state transition during '{operation}': {reason}",
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                ["reason"] = reason,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a store or repository operation fails.
    /// </summary>
    /// <param name="operation">The operation that failed (e.g., "RegisterAsync", "ApproveLIAAsync").</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An error wrapping the infrastructure failure.</returns>
    public static EncinaError StoreError(string operation, Exception exception) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Lawful basis store operation '{operation}' failed: {exception.Message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });
}
