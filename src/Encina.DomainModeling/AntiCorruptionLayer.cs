using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Represents a translation error in the Anti-Corruption Layer.
/// </summary>
/// <param name="ErrorCode">The error code identifying the translation failure.</param>
/// <param name="ErrorMessage">The human-readable error message.</param>
/// <param name="ExternalSystemId">Optional identifier of the external system.</param>
public sealed record TranslationError(
    string ErrorCode,
    string ErrorMessage,
    string? ExternalSystemId = null)
{
    /// <summary>
    /// Creates a translation error for an unsupported external type.
    /// </summary>
    /// <param name="typeName">The name of the unsupported type.</param>
    /// <param name="systemId">Optional external system identifier.</param>
    /// <returns>A new translation error.</returns>
    public static TranslationError UnsupportedType(string typeName, string? systemId = null)
        => new("ACL_UNSUPPORTED_TYPE", $"Unsupported external type: {typeName}", systemId);

    /// <summary>
    /// Creates a translation error for missing required data.
    /// </summary>
    /// <param name="fieldName">The name of the missing field.</param>
    /// <param name="systemId">Optional external system identifier.</param>
    /// <returns>A new translation error.</returns>
    public static TranslationError MissingRequiredField(string fieldName, string? systemId = null)
        => new("ACL_MISSING_FIELD", $"Missing required field: {fieldName}", systemId);

    /// <summary>
    /// Creates a translation error for invalid data format.
    /// </summary>
    /// <param name="fieldName">The name of the field with invalid format.</param>
    /// <param name="systemId">Optional external system identifier.</param>
    /// <returns>A new translation error.</returns>
    public static TranslationError InvalidFormat(string fieldName, string? systemId = null)
        => new("ACL_INVALID_FORMAT", $"Invalid format for field: {fieldName}", systemId);
}

/// <summary>
/// Interface for Anti-Corruption Layer that translates between external and internal domain models.
/// </summary>
/// <typeparam name="TExternal">The external (foreign) model type.</typeparam>
/// <typeparam name="TInternal">The internal domain model type.</typeparam>
/// <remarks>
/// The Anti-Corruption Layer (ACL) is a DDD pattern by Eric Evans that protects
/// the domain model from external system concepts. It provides a translation
/// boundary between bounded contexts.
///
/// Use ACL when:
/// <list type="bullet">
/// <item><description>Integrating with legacy systems</description></item>
/// <item><description>Consuming external APIs with different domain concepts</description></item>
/// <item><description>Processing webhooks from third-party services</description></item>
/// <item><description>Communicating between bounded contexts</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class StripePaymentACL : IAntiCorruptionLayer&lt;StripeWebhook, PaymentReceived&gt;
/// {
///     public Either&lt;TranslationError, PaymentReceived&gt; TranslateToInternal(StripeWebhook external)
///     {
///         if (external.Type != "payment_intent.succeeded")
///             return TranslationError.UnsupportedType(external.Type, "Stripe");
///
///         return new PaymentReceived(
///             PaymentId: Guid.Parse(external.Data.Metadata["order_id"]),
///             Amount: Money.FromCents(external.Data.Amount, external.Data.Currency),
///             ReceivedAt: DateTimeOffset.FromUnixTimeSeconds(external.Created));
///     }
///
///     public Either&lt;TranslationError, StripeWebhook&gt; TranslateToExternal(PaymentReceived internal)
///         =&gt; TranslationError.UnsupportedType("PaymentReceived", "Stripe");
/// }
/// </code>
/// </example>
public interface IAntiCorruptionLayer<TExternal, TInternal>
{
    /// <summary>
    /// Translates an external model to an internal domain model.
    /// </summary>
    /// <param name="external">The external model to translate.</param>
    /// <returns>Either a translation error or the internal domain model.</returns>
    Either<TranslationError, TInternal> TranslateToInternal(TExternal external);

    /// <summary>
    /// Translates an internal domain model to an external format.
    /// </summary>
    /// <param name="internalModel">The internal model to translate.</param>
    /// <returns>Either a translation error or the external model.</returns>
    Either<TranslationError, TExternal> TranslateToExternal(TInternal internalModel);
}

/// <summary>
/// Async variant of the Anti-Corruption Layer for complex translations.
/// </summary>
/// <typeparam name="TExternal">The external (foreign) model type.</typeparam>
/// <typeparam name="TInternal">The internal domain model type.</typeparam>
/// <remarks>
/// Use this interface when translation requires:
/// <list type="bullet">
/// <item><description>Looking up reference data from a database</description></item>
/// <item><description>Calling external services for enrichment</description></item>
/// <item><description>Complex async mapping logic</description></item>
/// </list>
/// </remarks>
public interface IAsyncAntiCorruptionLayer<TExternal, TInternal>
{
    /// <summary>
    /// Asynchronously translates an external model to an internal domain model.
    /// </summary>
    /// <param name="external">The external model to translate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a translation error or the internal domain model.</returns>
    ValueTask<Either<TranslationError, TInternal>> TranslateToInternalAsync(
        TExternal external,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously translates an internal domain model to an external format.
    /// </summary>
    /// <param name="internalModel">The internal model to translate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a translation error or the external model.</returns>
    ValueTask<Either<TranslationError, TExternal>> TranslateToExternalAsync(
        TInternal internalModel,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for Anti-Corruption Layer implementations.
/// </summary>
/// <typeparam name="TExternal">The external (foreign) model type.</typeparam>
/// <typeparam name="TInternal">The internal domain model type.</typeparam>
/// <remarks>
/// Provides protected helper methods for common translation patterns.
/// </remarks>
public abstract class AntiCorruptionLayerBase<TExternal, TInternal>
    : IAntiCorruptionLayer<TExternal, TInternal>
{
    /// <summary>
    /// Gets the external system identifier for error reporting.
    /// </summary>
    protected virtual string? ExternalSystemId => null;

    /// <inheritdoc />
    public abstract Either<TranslationError, TInternal> TranslateToInternal(TExternal external);

    /// <inheritdoc />
    public abstract Either<TranslationError, TExternal> TranslateToExternal(TInternal internalModel);

    /// <summary>
    /// Creates a translation error with the system ID.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A new translation error.</returns>
    protected TranslationError Error(string errorCode, string message)
        => new(errorCode, message, ExternalSystemId);

    /// <summary>
    /// Creates an unsupported type error.
    /// </summary>
    /// <param name="typeName">The unsupported type name.</param>
    /// <returns>A translation error for unsupported type.</returns>
    protected TranslationError UnsupportedType(string typeName)
        => TranslationError.UnsupportedType(typeName, ExternalSystemId);

    /// <summary>
    /// Creates a missing field error.
    /// </summary>
    /// <param name="fieldName">The missing field name.</param>
    /// <returns>A translation error for missing field.</returns>
    protected TranslationError MissingField(string fieldName)
        => TranslationError.MissingRequiredField(fieldName, ExternalSystemId);

    /// <summary>
    /// Creates an invalid format error.
    /// </summary>
    /// <param name="fieldName">The field with invalid format.</param>
    /// <returns>A translation error for invalid format.</returns>
    protected TranslationError InvalidFormat(string fieldName)
        => TranslationError.InvalidFormat(fieldName, ExternalSystemId);
}
