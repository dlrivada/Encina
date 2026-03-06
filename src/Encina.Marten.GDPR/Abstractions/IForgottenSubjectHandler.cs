namespace Encina.Marten.GDPR.Abstractions;

/// <summary>
/// Callback handler invoked when a crypto-shredded property is encountered for a forgotten
/// data subject during event deserialization.
/// </summary>
/// <remarks>
/// <para>
/// When the <c>CryptoShredderSerializer</c> deserializes an event and discovers that the
/// data subject's encryption keys have been deleted (i.e., the subject has been forgotten),
/// it invokes this handler for each affected property before substituting a placeholder value.
/// </para>
/// <para>
/// The default implementation (<c>DefaultForgottenSubjectHandler</c>) logs the occurrence
/// at <c>Information</c> level and performs no further action. Custom implementations can
/// use this hook for:
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
    /// Handles the encounter of a crypto-shredded property belonging to a forgotten data subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the forgotten data subject.</param>
    /// <param name="propertyName">The name of the crypto-shredded property being accessed.</param>
    /// <param name="eventType">The type of the event containing the forgotten data.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is called during event deserialization on the hot path. Implementations
    /// should be lightweight and avoid blocking operations. Consider using fire-and-forget
    /// patterns for expensive operations like database writes.
    /// </remarks>
    ValueTask HandleForgottenSubjectAsync(
        string subjectId,
        string propertyName,
        Type eventType,
        CancellationToken cancellationToken = default);
}
