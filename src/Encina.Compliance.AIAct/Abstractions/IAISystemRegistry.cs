using Encina.Compliance.AIAct.Model;

using LanguageExt;

namespace Encina.Compliance.AIAct.Abstractions;

/// <summary>
/// Registry for managing AI system registrations, maintaining an inventory of all
/// AI systems and their risk classifications within the organisation.
/// </summary>
/// <remarks>
/// <para>
/// Article 49 requires providers of high-risk AI systems to register them in the EU database
/// before placing them on the market or putting them into service. This interface provides
/// the programmatic contract for maintaining that inventory locally.
/// </para>
/// <para>
/// The default implementation (<c>InMemoryAISystemRegistry</c>) uses a thread-safe
/// <c>ConcurrentDictionary</c> and is populated at startup via <c>AIActOptions.RegisterAISystem()</c>
/// and attribute scanning (<c>[HighRiskAI]</c>). Users may replace it with a persistent
/// implementation via DI registration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register a system
/// var registration = new AISystemRegistration
/// {
///     SystemId = "cv-screener-v2",
///     Name = "CV Screening Assistant",
///     Category = AISystemCategory.EmploymentWorkersManagement,
///     RiskLevel = AIRiskLevel.HighRisk,
///     RegisteredAtUtc = DateTimeOffset.UtcNow
/// };
/// await registry.RegisterSystemAsync(registration, ct);
///
/// // Check if registered
/// if (registry.IsRegistered("cv-screener-v2"))
/// {
///     var result = await registry.GetSystemAsync("cv-screener-v2", ct);
/// }
/// </code>
/// </example>
public interface IAISystemRegistry
{
    /// <summary>
    /// Retrieves the registration details for a specific AI system.
    /// </summary>
    /// <param name="systemId">The unique identifier of the AI system.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="AISystemRegistration"/> for the system, or an <see cref="EncinaError"/>
    /// if the system is not registered.
    /// </returns>
    ValueTask<Either<EncinaError, AISystemRegistration>> GetSystemAsync(
        string systemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new AI system in the inventory.
    /// </summary>
    /// <param name="registration">The AI system registration to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the system
    /// is already registered or registration fails.
    /// </returns>
    /// <remarks>
    /// Registration is idempotent by system ID — attempting to register a system
    /// with an existing ID returns an error. Use <see cref="ReclassifyAsync"/> to
    /// update an existing system's risk level.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RegisterSystemAsync(
        AISystemRegistration registration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the risk level of a registered AI system and publishes an
    /// <see cref="Notifications.AISystemReclassifiedNotification"/>.
    /// </summary>
    /// <param name="systemId">The unique identifier of the AI system to reclassify.</param>
    /// <param name="newLevel">The new risk level to assign.</param>
    /// <param name="reason">Explanation of why the reclassification is being performed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the system
    /// is not registered or reclassification fails.
    /// </returns>
    /// <remarks>
    /// Article 6(3) acknowledges that AI systems may be reclassified based on changes
    /// to their intended purpose or deployment context. The reclassification publishes
    /// a domain notification for audit trail purposes.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ReclassifyAsync(
        string systemId,
        AIRiskLevel newLevel,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered AI systems with a specific risk level.
    /// </summary>
    /// <param name="level">The risk level to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of matching <see cref="AISystemRegistration"/> records,
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no
    /// systems match the specified level.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<AISystemRegistration>>> GetSystemsByRiskLevelAsync(
        AIRiskLevel level,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered AI systems in the inventory.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all <see cref="AISystemRegistration"/> records,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<AISystemRegistration>>> GetAllSystemsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an AI system with the specified identifier is registered.
    /// </summary>
    /// <param name="systemId">The unique identifier of the AI system.</param>
    /// <returns><c>true</c> if the system is registered; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// This is a synchronous, fast-path check used by the pipeline behavior to
    /// short-circuit when no AI system is associated with a request.
    /// </remarks>
    bool IsRegistered(string systemId);
}
