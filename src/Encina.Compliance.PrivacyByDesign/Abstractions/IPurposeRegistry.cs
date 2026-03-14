using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Registry for managing <see cref="PurposeDefinition"/> instances, with support for
/// module-scoped and global purpose definitions.
/// </summary>
/// <remarks>
/// <para>
/// The purpose registry maintains a catalog of declared processing purposes. Each purpose
/// defines the legal basis for processing, the fields allowed under that purpose, and optional
/// expiration dates. The <see cref="IPrivacyByDesignValidator"/> uses this registry to validate
/// that request fields comply with the declared purpose.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(b), personal data shall be "collected for specified, explicit and
/// legitimate purposes." This registry provides the central source of truth for what those
/// purposes are and which fields they permit.
/// </para>
/// <para>
/// <b>Module-aware resolution:</b> When a <c>moduleId</c> is provided,
/// the registry first looks for a module-specific purpose definition. If none is found,
/// it falls back to the global scope (<c>moduleId = null</c>). This supports modular
/// monolith architectures where different modules may define the same purpose name
/// with different allowed fields.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Look up a purpose (module-specific first, then global fallback)
/// var purpose = await registry.GetPurposeAsync("Order Processing", "sales-module", ct);
///
/// // Look up a global purpose
/// var globalPurpose = await registry.GetPurposeAsync("Marketing Analytics", cancellationToken: ct);
///
/// // Register a new purpose
/// var definition = new PurposeDefinition
/// {
///     PurposeId = "order-processing",
///     Name = "Order Processing",
///     Description = "Processing personal data to fulfill customer orders",
///     LegalBasis = "Contract (Art. 6(1)(b))",
///     AllowedFields = ["Name", "ShippingAddress", "Email"],
///     CreatedAtUtc = DateTimeOffset.UtcNow
/// };
/// await registry.RegisterPurposeAsync(definition, ct);
/// </code>
/// </example>
public interface IPurposeRegistry
{
    /// <summary>
    /// Retrieves a purpose definition by name, with module-aware fallback resolution.
    /// </summary>
    /// <param name="purposeName">The name of the purpose to look up.</param>
    /// <param name="moduleId">
    /// The module identifier for module-scoped lookup, or <see langword="null"/> to search
    /// only the global scope. When provided, the registry first searches for a module-specific
    /// definition, then falls back to global scope if none is found.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Option{PurposeDefinition}"/> containing the purpose definition if found;
    /// <see cref="LanguageExt.Prelude.None"/> if no matching purpose exists;
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Resolution order:
    /// </para>
    /// <list type="number">
    /// <item><description>If <paramref name="moduleId"/> is provided, search for a purpose where
    /// <see cref="PurposeDefinition.Name"/> matches and <see cref="PurposeDefinition.ModuleId"/>
    /// equals <paramref name="moduleId"/>.</description></item>
    /// <item><description>If no module-specific match is found (or <paramref name="moduleId"/> is
    /// <see langword="null"/>), search for a purpose where <see cref="PurposeDefinition.Name"/>
    /// matches and <see cref="PurposeDefinition.ModuleId"/> is <see langword="null"/>.</description></item>
    /// </list>
    /// </remarks>
    ValueTask<Either<EncinaError, Option<PurposeDefinition>>> GetPurposeAsync(
        string purposeName,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all purpose definitions, optionally filtered by module scope.
    /// </summary>
    /// <param name="moduleId">
    /// The module identifier to filter by, or <see langword="null"/> to return
    /// module-specific purposes for the given module plus all global purposes.
    /// When <see langword="null"/>, only global purposes are returned.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of purpose definitions matching the scope criteria;
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no purposes
    /// are registered.
    /// </returns>
    /// <remarks>
    /// <para>
    /// When <paramref name="moduleId"/> is provided, the result includes both:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Purposes scoped to the specified module.</description></item>
    /// <item><description>Global purposes (where <see cref="PurposeDefinition.ModuleId"/> is <see langword="null"/>).</description></item>
    /// </list>
    /// <para>
    /// This supports the common pattern where modules inherit global purposes and
    /// optionally override them with module-specific definitions.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<PurposeDefinition>>> GetAllPurposesAsync(
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new purpose definition in the registry.
    /// </summary>
    /// <param name="purpose">The purpose definition to register.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the registration
    /// fails (e.g., duplicate purpose name within the same module scope).
    /// </returns>
    /// <remarks>
    /// <para>
    /// If a purpose with the same <see cref="PurposeDefinition.PurposeId"/> already exists,
    /// it is overwritten (upsert semantics). However, if a different purpose with the same
    /// <see cref="PurposeDefinition.Name"/> and <see cref="PurposeDefinition.ModuleId"/>
    /// combination exists, the operation fails to prevent ambiguous lookups.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RegisterPurposeAsync(
        PurposeDefinition purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a purpose definition from the registry by its unique identifier.
    /// </summary>
    /// <param name="purposeId">The unique identifier of the purpose to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the purpose
    /// was not found or the removal failed.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RemovePurposeAsync(
        string purposeId,
        CancellationToken cancellationToken = default);
}
