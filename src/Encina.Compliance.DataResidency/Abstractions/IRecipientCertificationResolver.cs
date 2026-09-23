using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.DataResidency.Abstractions;

/// <summary>
/// Resolves whether the destination recipient of a residency check or cross-border transfer is
/// certified, or otherwise in scope, under the legal instrument backing a partial adequacy
/// decision (see <see cref="Region.RequiresRecipientCertification"/>) — for example, DPF
/// certification for a US recipient, or PIPEDA coverage for a Canadian recipient.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DataResidencyPipelineBehavior{TRequest,TResponse}"/> and
/// <c>Encina.Compliance.CrossBorderTransfer.Pipeline.TransferBlockingPipelineBehavior</c> cannot
/// know, on their own, whether a specific request's destination recipient meets a partial
/// adequacy decision's certification requirement. The application registers an implementation of
/// this interface (for example, backed by a DPF registry lookup or an internal vendor list) to
/// answer that question; both pipeline behaviors call it before treating a partial-adequacy
/// region as adequate.
/// </para>
/// <para>
/// When no application implementation is registered, the default
/// <c>NullRecipientCertificationResolver</c> is used and always answers <c>false</c> — fail
/// closed, consistent with <see cref="IAdequacyDecisionProvider"/> treating an unconfirmed
/// partial-adequacy region as not adequate.
/// </para>
/// </remarks>
public interface IRecipientCertificationResolver
{
    /// <summary>
    /// Determines whether the recipient at <paramref name="destination"/> is certified for the
    /// given <paramref name="dataCategory"/>.
    /// </summary>
    /// <param name="destination">The destination region of the transfer or residency check.</param>
    /// <param name="dataCategory">The data category being transferred or processed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the recipient is certified/in-scope for <paramref name="destination"/>'s
    /// partial adequacy decision; <c>false</c> otherwise, including when certification cannot be
    /// determined.
    /// </returns>
    ValueTask<bool> IsCertifiedAsync(
        Region destination,
        string dataCategory,
        CancellationToken cancellationToken = default);
}
