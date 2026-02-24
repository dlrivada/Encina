using LanguageExt;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Bridge interface for consent status verification in the lawful basis pipeline.
/// </summary>
/// <remarks>
/// <para>
/// When the lawful basis for a request is <see cref="LawfulBasis.Consent"/>, the
/// <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> uses this provider
/// to verify that the data subject has given valid consent for the required processing purposes.
/// </para>
/// <para>
/// This interface is intentionally minimal and lives in <c>Encina.Compliance.GDPR</c> to avoid
/// a hard dependency on <c>Encina.Compliance.Consent</c>. The Consent package provides a
/// <c>ConsentStatusProviderAdapter</c> that bridges this interface to its rich consent management
/// infrastructure.
/// </para>
/// <para>
/// Registration of this provider is <b>optional</b>. If no <see cref="IConsentStatusProvider"/>
/// is registered and a request declares <see cref="LawfulBasis.Consent"/>, the behavior will
/// apply enforcement mode rules (block or warn depending on configuration).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple implementation for testing
/// public class TestConsentProvider : IConsentStatusProvider
/// {
///     public ValueTask&lt;Either&lt;EncinaError, ConsentCheckResult&gt;&gt; CheckConsentAsync(
///         string subjectId,
///         IReadOnlyList&lt;string&gt; purposes,
///         CancellationToken ct)
///     {
///         // All purposes are consented
///         return ValueTask.FromResult(
///             Either&lt;EncinaError, ConsentCheckResult&gt;.Right(
///                 new ConsentCheckResult(true, Array.Empty&lt;string&gt;())));
///     }
/// }
/// </code>
/// </example>
public interface IConsentStatusProvider
{
    /// <summary>
    /// Checks whether a data subject has valid consent for the specified processing purposes.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purposes">The processing purposes to check consent for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ConsentCheckResult"/> indicating whether valid consent exists and listing
    /// any missing purposes, or an <see cref="EncinaError"/> if the consent check failed.
    /// </returns>
    ValueTask<Either<EncinaError, ConsentCheckResult>> CheckConsentAsync(
        string subjectId,
        IReadOnlyList<string> purposes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a consent status check for a data subject and set of processing purposes.
/// </summary>
/// <param name="HasValidConsent">
/// <c>true</c> if the data subject has valid (active, non-expired) consent for all requested purposes;
/// <c>false</c> if consent is missing for one or more purposes.
/// </param>
/// <param name="MissingPurposes">
/// The processing purposes for which consent is missing or invalid.
/// Empty when <paramref name="HasValidConsent"/> is <c>true</c>.
/// </param>
public sealed record ConsentCheckResult(
    bool HasValidConsent,
    IReadOnlyList<string> MissingPurposes);
