namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Extracts the data subject identifier from a request and context for DSR processing restriction checks.
/// </summary>
/// <remarks>
/// <para>
/// When a request is decorated with <see cref="RestrictProcessingAttribute"/> and does not
/// specify a <see cref="RestrictProcessingAttribute.SubjectIdProperty"/>, the pipeline behavior
/// uses this interface to determine the data subject whose restriction status should be checked.
/// </para>
/// <para>
/// The default implementation, <see cref="DefaultDataSubjectIdExtractor"/>, reads a <c>SubjectId</c>
/// or <c>UserId</c> property of the request (or the property named by
/// <see cref="RestrictProcessingAttribute.SubjectIdProperty"/>, which must exist) and falls back to
/// <see cref="IRequestContext.UserId"/> only when no property is configured and the request has no
/// <c>SubjectId</c> or <c>UserId</c> property.
/// </para>
/// <para>
/// When the resolved subject is missing (<c>null</c> or empty) for a request decorated with
/// <see cref="RestrictProcessingAttribute"/>, the processing restriction behavior fails closed
/// unless <see cref="DataSubjectRightsOptions.FailClosedOnMissingSubjectId"/> is <c>false</c>.
/// </para>
/// <para>
/// This follows the same pattern as <c>ILawfulBasisSubjectIdExtractor</c> in the
/// <c>Encina.Compliance.GDPR</c> module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Extract subject ID from a custom request property
/// public class OrderSubjectIdExtractor : IDataSubjectIdExtractor
/// {
///     public string? ExtractSubjectId&lt;TRequest&gt;(TRequest request, IRequestContext context)
///         where TRequest : notnull
///     {
///         if (request is IHasCustomerId customer)
///             return customer.CustomerId;
///
///         return context.UserId;
///     }
/// }
/// </code>
/// </example>
public interface IDataSubjectIdExtractor
{
    /// <summary>
    /// Extracts the data subject identifier from the request or context.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request being processed.</param>
    /// <param name="context">The pipeline request context.</param>
    /// <returns>
    /// The data subject identifier, or <c>null</c> (or an empty string) when the request carries no
    /// subject — for example a subject-id property whose value is <c>null</c> or
    /// <see cref="Guid.Empty"/>. A missing subject is a normal outcome, not an error.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The request is misconfigured: its subject-id property has a type that cannot be converted to a
    /// stable identifier, or <see cref="RestrictProcessingAttribute.SubjectIdProperty"/> names a property
    /// that does not exist. <see cref="DefaultDataSubjectIdExtractor"/> throws in this case instead of
    /// returning <c>null</c>, so a configuration error is never mistaken for a missing subject or
    /// silently replaced by the authenticated caller. Custom implementations should do the same.
    /// </exception>
    string? ExtractSubjectId<TRequest>(TRequest request, IRequestContext context)
        where TRequest : notnull;
}
