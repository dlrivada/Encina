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
/// The default implementation falls back to <see cref="IRequestContext.UserId"/> when no
/// custom extractor is registered.
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
    /// The data subject identifier, or <c>null</c> if the subject cannot be determined.
    /// </returns>
    string? ExtractSubjectId<TRequest>(TRequest request, IRequestContext context)
        where TRequest : notnull;
}
