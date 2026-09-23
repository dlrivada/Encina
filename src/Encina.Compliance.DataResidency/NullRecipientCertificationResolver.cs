using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Default <see cref="IRecipientCertificationResolver"/> registered when the application does not
/// provide its own. Always answers <c>false</c> — fail closed.
/// </summary>
/// <remarks>
/// Register a custom <see cref="IRecipientCertificationResolver"/> implementation before calling
/// <c>AddEncinaDataResidency</c> or <c>AddEncinaCrossBorderTransfer</c> to confirm recipient
/// certification (for example, against a DPF or PIPEDA registry); this default otherwise leaves
/// every partial-adequacy region (see <see cref="Region.RequiresRecipientCertification"/>)
/// treated as not adequate.
/// </remarks>
public sealed class NullRecipientCertificationResolver : IRecipientCertificationResolver
{
    /// <inheritdoc />
    public ValueTask<bool> IsCertifiedAsync(
        Region destination,
        string dataCategory,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
}
