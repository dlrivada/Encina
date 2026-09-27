using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Marten.GDPR;

/// <summary>
/// Erasure strategy that implements crypto-shredding: deleting the subject's encryption keys
/// to render PII permanently unreadable.
/// </summary>
/// <remarks>
/// <para>
/// This strategy bridges the <c>Encina.Compliance.DataSubjectRights</c> erasure workflow with
/// the crypto-shredding infrastructure. Unlike <see cref="HardDeleteErasureStrategy"/> which
/// nullifies field values, this strategy deletes the per-subject encryption keys — the encrypted
/// ciphertext remains in the immutable event store but becomes permanently unreadable.
/// </para>
/// <para>
/// This approach satisfies GDPR Article 17 without modifying event history, making it ideal
/// for event-sourced systems where immutability of the event log is a core invariant.
/// </para>
/// <para>
/// The subject ID is extracted from <see cref="PersonalDataLocation.EntityId"/>, which is
/// expected to contain the data subject's unique identifier.
/// </para>
/// </remarks>
public sealed class CryptoShredErasureStrategy : IDataErasureStrategy
{
    private readonly ISubjectKeyProvider _subjectKeyProvider;
    private readonly ILogger<CryptoShredErasureStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoShredErasureStrategy"/> class.
    /// </summary>
    /// <param name="subjectKeyProvider">The provider managing per-subject encryption keys.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public CryptoShredErasureStrategy(
        ISubjectKeyProvider subjectKeyProvider,
        ILogger<CryptoShredErasureStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(subjectKeyProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _subjectKeyProvider = subjectKeyProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Extracts the subject ID from <see cref="PersonalDataLocation.EntityId"/> and calls
    /// <see cref="ISubjectKeyProvider.DeleteSubjectKeysAsync"/> to delete all encryption
    /// key versions for the subject. The encrypted PII remains in the event store but
    /// becomes permanently unreadable without the key material.
    /// </para>
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> EraseFieldAsync(
        PersonalDataLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var subjectId = location.EntityId;
        using var activity = CryptoShreddingDiagnostics.StartErasure();

        // The data subject's own identifier is never logged (#1429, following #1314); correlate
        // via the field name and entity type instead.
        _logger.LogDebug(
            "Crypto-shredding erasure for field '{FieldName}' on entity {EntityType}",
            location.FieldName,
            location.EntityType.Name);

        var result = await _subjectKeyProvider
            .DeleteSubjectKeysAsync(subjectId, cancellationToken)
            .ConfigureAwait(false);

        result.Match(
            _ => CryptoShreddingDiagnostics.RecordSuccess(activity),
            _ => CryptoShreddingDiagnostics.RecordFailed(activity, "Erasure failed"));

        return result.Map(_ => unit);
    }
}
