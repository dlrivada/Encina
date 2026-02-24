namespace Encina.Compliance.GDPR;

/// <summary>
/// Extracts the data subject identifier from a request and context for consent-based lawful basis validation.
/// </summary>
/// <remarks>
/// <para>
/// When the lawful basis is <see cref="LawfulBasis.Consent"/>, the pipeline behavior needs to
/// identify the data subject whose consent should be verified. This interface provides a pluggable
/// mechanism for extracting the subject identifier from the request or pipeline context.
/// </para>
/// <para>
/// The default implementation falls back to <see cref="IRequestContext.UserId"/> when no
/// custom extractor is registered.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Extract subject ID from a custom request property
/// public class OrderSubjectIdExtractor : ILawfulBasisSubjectIdExtractor
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
public interface ILawfulBasisSubjectIdExtractor
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

/// <summary>
/// Default implementation of <see cref="ILawfulBasisSubjectIdExtractor"/> that returns
/// <see cref="IRequestContext.UserId"/>.
/// </summary>
public sealed class DefaultLawfulBasisSubjectIdExtractor : ILawfulBasisSubjectIdExtractor
{
    /// <inheritdoc />
    public string? ExtractSubjectId<TRequest>(TRequest request, IRequestContext context)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.UserId;
    }
}
