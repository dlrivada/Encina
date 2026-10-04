using System.Text.Json;

using Encina.Compliance.DataSubjectRights;
using Encina.Diagnostics;
using Encina.Marten.GDPR.Diagnostics;

using LanguageExt;

using Marten;
using Marten.Exceptions;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Marten.GDPR;

/// <summary>
/// Locates crypto-shredded personal data of a data subject across the Marten event store, at any depth of each
/// event.
/// </summary>
/// <remarks>
/// <para>
/// The locator reads every raw event of the store while a subject filter is active, so only the requested
/// subject's fields are decrypted and no key of another subject is fetched: an unreadable event of another
/// subject never blocks this subject's request. It then walks each event over its System.Text.Json contract and
/// returns one <see cref="PersonalDataLocation"/> per non-null field whose own subject-id sibling equals the
/// requested subject.
/// </para>
/// <para>
/// <see cref="PersonalDataLocation.FieldName"/> is the field path: the bare property name at the top level,
/// <c>Contact.Email</c> for a nested object, <c>Items[].Note</c> for collection elements and <c>Notes{}.Text</c> for
/// dictionary values. A path never contains an index or a dictionary key.
/// </para>
/// <para>
/// Fails closed: a store whose serializer is not the crypto-shredding serializer returns
/// <c>crypto.serializer_unsupported</c>, and a failed read of the requested subject's data returns
/// <c>crypto.key_store_error</c> instead of a partial inventory.
/// </para>
/// </remarks>
public sealed class MartenEventPersonalDataLocator : IPersonalDataLocator
{
    private readonly IDocumentSession _session;
    private readonly ILogger<MartenEventPersonalDataLocator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenEventPersonalDataLocator"/> class.
    /// </summary>
    /// <param name="session">The Marten document session for event store queries.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public MartenEventPersonalDataLocator(
        IDocumentSession session,
        ILogger<MartenEventPersonalDataLocator> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>> LocateAllDataAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        // The data subject's own identifier is never logged (#1429, following #1314).
        _logger.PersonalDataLocateStarted();

        var storeSerializer = _session.DocumentStore.Options.Serializer();
        if (storeSerializer is not CryptoShredderSerializer serializer)
        {
            return CryptoShreddingErrors.SerializerUnsupported(storeSerializer.GetType());
        }

        try
        {
            var locations = await LocateAsync(serializer, subjectId, cancellationToken).ConfigureAwait(false);
            _logger.PersonalDataLocateCompleted(locations.Count);
            return Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(locations);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.PersonalDataLocateFailed(ex.ForLogging(), FailingEventSequence(ex));
            return CryptoShreddingErrors.KeyStoreError("LocateAllData", ex);
        }
    }

    private async Task<List<PersonalDataLocation>> LocateAsync(
        CryptoShredderSerializer serializer, string subjectId, CancellationToken cancellationToken)
    {
        using var filter = CryptoShreddingCallScope.FilterToSubject(subjectId);
        var events = await _session.Events.QueryAllRawEvents().ToListAsync(cancellationToken).ConfigureAwait(false);

        var locations = new List<PersonalDataLocation>();
        foreach (var @event in events)
        {
            if (@event.Data is { } body)
            {
                locations.AddRange(LocateFieldsInEvent(body, subjectId, serializer.WalkOptions!, serializer.Registry));
            }
        }

        return locations;
    }

    /// <summary>
    /// Finds every crypto-shredded field of one event body, at any depth, that belongs to the given subject, and
    /// records each location for the erasure router.
    /// </summary>
    internal static IEnumerable<PersonalDataLocation> LocateFieldsInEvent(
        object eventBody, string subjectId, JsonSerializerOptions options, CryptoShreddingTypePlanRegistry registry)
    {
        foreach (var occurrence in CryptoShreddedGraphWalker.Walk(eventBody, options, registry))
        {
            if (BelongsTo(occurrence, subjectId) && occurrence.Field.Getter(occurrence.Owner) is { } value)
            {
                yield return BuildLocation(eventBody.GetType(), subjectId, occurrence, value);
            }
        }
    }

    private static bool BelongsTo(CryptoShreddedOccurrence occurrence, string subjectId)
    {
        try
        {
            return string.Equals(occurrence.Field.ResolveSubjectId(occurrence.Owner), subjectId, StringComparison.Ordinal);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static PersonalDataLocation BuildLocation(Type eventType, string subjectId, CryptoShreddedOccurrence occurrence, object value)
    {
        var personalData = occurrence.Field.PersonalData;
        var location = new PersonalDataLocation
        {
            EntityType = eventType,
            EntityId = subjectId,
            FieldName = occurrence.Path,
            Category = personalData.Category,
            IsErasable = personalData.Erasable,
            IsPortable = personalData.Portable,
            HasLegalRetention = personalData.LegalRetention,
            CurrentValue = value
        };

        CryptoShredRoutingErasureStrategy.MarkMartenLocation(location);
        return location;
    }

    // Marten wraps a failed event read with its sequence number, which identifies the event without a stream id
    // (an application may key streams by subject).
    private static long? FailingEventSequence(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is EventDeserializationFailureException failure)
            {
                return failure.Sequence;
            }
        }

        return null;
    }
}
