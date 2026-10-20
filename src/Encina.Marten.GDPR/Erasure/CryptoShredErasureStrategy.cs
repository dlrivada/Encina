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
/// Unlike <see cref="HardDeleteErasureStrategy"/>, which nullifies field values, this strategy deletes the
/// per-subject encryption keys: the ciphertext remains in the immutable event store but becomes permanently
/// unreadable. This satisfies GDPR Article 17 without modifying event history.
/// </para>
/// <para>
/// <b>Erasure is subject-wide.</b> The subject id is taken from <see cref="PersonalDataLocation.EntityId"/> and
/// every key of that subject is deleted, whatever <see cref="PersonalDataLocation.FieldName"/> says, so one
/// location shreds every crypto-shredded field of the subject (#1144 tracks field-scoped keys).
/// </para>
/// <para>
/// <b>Idempotent.</b> Erasing a subject that is already forgotten succeeds (event 8480): the data subject rights
/// executor calls the strategy once per location, and several locations share one subject key.
/// </para>
/// <para>
/// <c>AddEncinaMartenGdpr</c> registers it behind an internal routing strategy, so locations of the Marten
/// locator always reach it, whatever other <see cref="IDataErasureStrategy"/> the application registers first.
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
    /// Calls <see cref="ISubjectKeyProvider.DeleteSubjectKeysAsync"/> for the subject in
    /// <see cref="PersonalDataLocation.EntityId"/>; a subject that is already forgotten is a success.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> EraseFieldAsync(
        PersonalDataLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        using var activity = CryptoShreddingDiagnostics.StartErasure();

        // The data subject's own identifier is never logged (#1429, following #1314).
        _logger.ErasureRequested(location.EntityType.Name, location.FieldName);

        var result = await _subjectKeyProvider
            .DeleteSubjectKeysAsync(location.EntityId, cancellationToken)
            .ConfigureAwait(false);

        return result.Match(
            Right: _ => Succeeded(activity),
            Left: error => Failed(activity, location, error));
    }

    private static Either<EncinaError, Unit> Succeeded(System.Diagnostics.Activity? activity)
    {
        CryptoShreddingDiagnostics.RecordSuccess(activity);
        return Right<EncinaError, Unit>(unit);
    }

    private Either<EncinaError, Unit> Failed(System.Diagnostics.Activity? activity, PersonalDataLocation location, EncinaError error)
    {
        var code = CryptoShreddingEngine.ErrorCode(error, CryptoShreddingErrors.KeyStoreErrorCode);
        if (code == CryptoShreddingErrors.SubjectForgottenCode)
        {
            _logger.ErasureSubjectAlreadyForgotten(location.EntityType.Name, location.FieldName);
            return Succeeded(activity);
        }

        CryptoShreddingDiagnostics.RecordFailed(activity, code);
        return Left<EncinaError, Unit>(error);
    }
}
