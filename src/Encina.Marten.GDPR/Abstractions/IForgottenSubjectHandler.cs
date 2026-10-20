namespace Encina.Marten.GDPR.Abstractions;

/// <summary>
/// Callback handler invoked when crypto-shredded data of a forgotten data subject is read.
/// </summary>
/// <remarks>
/// <para>
/// When the <c>CryptoShredderSerializer</c> deserializes a document (an event, a snapshot or a read model) and
/// finds fields of a subject whose keys have been deleted, it applies the anonymized placeholder to those fields
/// and invokes this handler once per subject per serializer call, at any depth of the document.
/// </para>
/// <para>
/// The default implementation (<c>DefaultForgottenSubjectHandler</c>) logs the occurrence at
/// <c>Information</c> level (event 8477) and performs no further action. An exception thrown by a handler is
/// logged (event 8475) and swallowed: the handler is a notification hook and never breaks a read. Custom
/// implementations can use this hook for:
/// </para>
/// <list type="bullet">
/// <item><description>Audit logging of forgotten data access attempts</description></item>
/// <item><description>Metrics collection for compliance dashboards</description></item>
/// <item><description>Custom projection handling (e.g., clearing cached data)</description></item>
/// <item><description>Alerting when forgotten subjects appear in active projections</description></item>
/// </list>
/// </remarks>
public interface IForgottenSubjectHandler
{
    /// <summary>
    /// Handles the encounter of crypto-shredded data belonging to a forgotten data subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the forgotten data subject. Never log it.</param>
    /// <param name="fieldPath">
    /// The path of the first field of that subject met in the call, in the format of
    /// <c>PersonalDataLocation.FieldName</c> (<c>Email</c>, <c>Contact.Email</c>, <c>Items[].Note</c>).
    /// </param>
    /// <param name="documentType">The root type being deserialized: an event, a snapshot envelope or a document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is called during deserialization on the hot path. Implementations should be lightweight and
    /// avoid blocking operations.
    /// </remarks>
    ValueTask HandleForgottenSubjectAsync(
        string subjectId,
        string fieldPath,
        Type documentType,
        CancellationToken cancellationToken = default);
}
