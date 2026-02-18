using LanguageExt;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Registry for managing GDPR Records of Processing Activities (RoPA) as required by Article 30.
/// </summary>
/// <remarks>
/// <para>
/// Article 30 of the GDPR requires controllers and processors to maintain records of processing
/// activities under their responsibility. This interface provides CRUD operations for managing
/// those records programmatically.
/// </para>
/// <para>
/// Implementations may store activities in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store.
/// </para>
/// <para><b>Key Article 30 Requirements:</b></para>
/// <list type="bullet">
/// <item>Name and contact details of the controller</item>
/// <item>Purposes of the processing</item>
/// <item>Description of categories of data subjects and personal data</item>
/// <item>Categories of recipients</item>
/// <item>Transfers to third countries (with safeguards)</item>
/// <item>Envisaged time limits for erasure (retention periods)</item>
/// <item>Description of technical and organizational security measures</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Register a processing activity
/// var activity = new ProcessingActivity
/// {
///     Id = Guid.NewGuid(),
///     Name = "Order Processing",
///     Purpose = "Fulfill customer orders",
///     LawfulBasis = LawfulBasis.Contract,
///     CategoriesOfDataSubjects = ["Customers"],
///     CategoriesOfPersonalData = ["Name", "Email", "Address"],
///     Recipients = ["Shipping Provider"],
///     RetentionPeriod = TimeSpan.FromDays(2555), // 7 years
///     SecurityMeasures = "Encryption at rest and in transit",
///     RequestType = typeof(CreateOrderCommand),
///     CreatedAtUtc = DateTimeOffset.UtcNow,
///     LastUpdatedAtUtc = DateTimeOffset.UtcNow
/// };
///
/// await registry.RegisterActivityAsync(activity, cancellationToken);
/// </code>
/// </example>
public interface IProcessingActivityRegistry
{
    /// <summary>
    /// Registers a new processing activity in the RoPA.
    /// </summary>
    /// <param name="activity">The processing activity to register.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the activity
    /// could not be registered (e.g., duplicate request type).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RegisterActivityAsync(
        ProcessingActivity activity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered processing activities.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all processing activities, or an <see cref="EncinaError"/>
    /// if the registry could not be queried.
    /// </returns>
    /// <remarks>
    /// This method is typically used for RoPA export and compliance auditing.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ProcessingActivity>>> GetAllActivitiesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a processing activity linked to a specific Encina request type.
    /// </summary>
    /// <param name="requestType">The request type to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="Option{ProcessingActivity}"/> containing the matching activity if found,
    /// or <see cref="Option{ProcessingActivity}.None"/> if no activity is registered for the given type,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Used by the GDPR compliance pipeline behavior to verify that a request has a
    /// registered processing activity before processing personal data.
    /// </remarks>
    ValueTask<Either<EncinaError, Option<ProcessingActivity>>> GetActivityByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing processing activity in the RoPA.
    /// </summary>
    /// <param name="activity">The updated processing activity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the activity
    /// was not found or could not be updated.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> UpdateActivityAsync(
        ProcessingActivity activity,
        CancellationToken cancellationToken = default);
}
