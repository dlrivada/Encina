using LanguageExt;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Central handler for processing Data Subject Rights requests under GDPR Articles 15-22.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides the primary entry point for handling all data subject rights:
/// access (Article 15), rectification (Article 16), erasure (Article 17), restriction (Article 18),
/// portability (Article 20), and objection (Article 21).
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// The handler coordinates between <see cref="IPersonalDataLocator"/>, <see cref="IDataErasureExecutor"/>,
/// <see cref="IDataPortabilityExporter"/>, and the store interfaces to fulfill each request type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Handle a data access request (Article 15)
/// var request = new AccessRequest("subject-123", IncludeProcessingActivities: true);
/// var result = await handler.HandleAccessAsync(request, cancellationToken);
///
/// result.Match(
///     Right: response => Console.WriteLine($"Found {response.Data.Count} data locations"),
///     Left: error => Console.WriteLine($"Access failed: {error.Message}"));
/// </code>
/// </example>
public interface IDataSubjectRightsHandler
{
    /// <summary>
    /// Handles a data access request (Article 15 - Right of access by the data subject).
    /// </summary>
    /// <param name="request">The access request containing the subject identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="AccessResponse"/> containing all personal data locations and optionally
    /// processing activities for the data subject, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// The data subject has the right to obtain confirmation as to whether personal data
    /// concerning them is being processed, and access to that data along with supplementary
    /// information about the processing.
    /// </remarks>
    ValueTask<Either<EncinaError, AccessResponse>> HandleAccessAsync(
        AccessRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a data rectification request (Article 16 - Right to rectification).
    /// </summary>
    /// <param name="request">The rectification request specifying the field and new value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the rectification
    /// could not be performed.
    /// </returns>
    /// <remarks>
    /// The data subject has the right to obtain the rectification of inaccurate personal
    /// data without undue delay. Per Article 19, the controller must communicate the
    /// rectification to each recipient to whom the data has been disclosed.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> HandleRectificationAsync(
        RectificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a data erasure request (Article 17 - Right to erasure / "Right to be forgotten").
    /// </summary>
    /// <param name="request">The erasure request specifying the reason and optional scope.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="ErasureResult"/> detailing which fields were erased, retained, or failed,
    /// or an <see cref="EncinaError"/> if the erasure could not be initiated.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The data subject has the right to obtain the erasure of personal data without undue delay
    /// where one of the grounds in Article 17(1) applies.
    /// </para>
    /// <para>
    /// Erasure may be refused when processing is necessary for exercising the right of freedom
    /// of expression, compliance with a legal obligation, public health, archiving, or legal
    /// claims (Article 17(3)).
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, ErasureResult>> HandleErasureAsync(
        ErasureRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a processing restriction request (Article 18 - Right to restriction of processing).
    /// </summary>
    /// <param name="request">The restriction request specifying the reason and optional scope.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the restriction
    /// could not be applied.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The data subject has the right to obtain restriction of processing where:
    /// the accuracy is contested, the processing is unlawful, the controller no longer needs
    /// the data, or the subject has objected to processing pending verification.
    /// </para>
    /// <para>
    /// While restriction is active, personal data may only be stored — not processed — except
    /// with consent, for legal claims, for protecting rights, or for important public interest
    /// (Article 18(2)).
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> HandleRestrictionAsync(
        RestrictionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a data portability request (Article 20 - Right to data portability).
    /// </summary>
    /// <param name="request">The portability request specifying the format and optional category filter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="PortabilityResponse"/> containing the exported data in the requested format,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// The data subject has the right to receive personal data in a structured, commonly used,
    /// and machine-readable format, and to transmit that data to another controller.
    /// This right applies only to data processed by automated means based on consent or contract.
    /// </remarks>
    ValueTask<Either<EncinaError, PortabilityResponse>> HandlePortabilityAsync(
        PortabilityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an objection to processing request (Article 21 - Right to object).
    /// </summary>
    /// <param name="request">The objection request specifying the processing purpose and reason.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success (objection accepted), or an <see cref="EncinaError"/>
    /// if the objection was rejected or could not be processed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The data subject has the right to object to processing based on legitimate interests
    /// or public task (Article 6(1)(e)/(f)). The controller must cease processing unless it
    /// demonstrates compelling legitimate grounds that override the subject's interests.
    /// </para>
    /// <para>
    /// For direct marketing, the data subject has an absolute right to object and processing
    /// must cease immediately (Article 21(2)-(3)).
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> HandleObjectionAsync(
        ObjectionRequest request,
        CancellationToken cancellationToken = default);
}
