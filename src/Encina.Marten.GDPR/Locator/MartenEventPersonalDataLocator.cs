using System.Reflection;

using Encina.Compliance.DataSubjectRights;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Marten.GDPR;

/// <summary>
/// Locates personal data associated with a data subject across the Marten event store.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IPersonalDataLocator"/> by scanning the Marten event store for events
/// containing properties decorated with both <c>[CryptoShredded]</c> and <c>[PersonalData]</c>.
/// For each matching field, a <see cref="PersonalDataLocation"/> is returned with the field's
/// metadata and current value.
/// </para>
/// <para>
/// This locator queries all raw events from the store and filters in-memory by subject ID.
/// This is acceptable because GDPR data subject rights operations (access, erasure, portability)
/// are rare administrative operations, not hot-path queries.
/// </para>
/// <para>
/// Uses the static <c>CryptoShreddedPropertyCache</c> for efficient property discovery without
/// per-event reflection overhead.
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

        try
        {
            // The data subject's own identifier is never logged (#1429, following #1314).
            _logger.LogDebug("Locating personal data in Marten event store");

            // Query all raw events from the store
            var allEvents = await _session.Events
                .QueryAllRawEvents()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var locations = new List<PersonalDataLocation>();

            foreach (var eventData in allEvents)
            {
                if (eventData.Data is { } eventBody)
                {
                    locations.AddRange(LocateFieldsInEvent(eventBody, subjectId));
                }
            }

            _logger.LogDebug("Located {Count} personal data fields", locations.Count);

            return Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(locations);
        }
        catch (Exception ex)
        {
            // The data subject's own identifier is never logged (#1429, following #1314).
            _logger.LogError(ex, "Failed to locate personal data");

            return Left<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                CryptoShreddingErrors.KeyStoreError("LocateAllData", ex));
        }
    }

    /// <summary>
    /// Finds every crypto-shredded field on one event body that belongs to the given subject.
    /// </summary>
    /// <remarks>
    /// Internal (rather than private) and static so it can be unit-tested directly against plain
    /// CLR objects, without mocking Marten's query pipeline — this is where the leak fixed by
    /// #1429 lived (<c>EntityId</c> below is the subject id itself for this locator).
    /// </remarks>
    internal static IEnumerable<PersonalDataLocation> LocateFieldsInEvent(object eventBody, string subjectId)
    {
        var eventType = eventBody.GetType();
        var fields = CryptoShreddedPropertyCache.GetFields(eventType);

        foreach (var field in fields)
        {
            if (TryBuildLocation(eventBody, eventType, field, subjectId, out var location))
            {
                yield return location;
            }
        }
    }

    /// <summary>
    /// Builds a <see cref="PersonalDataLocation"/> for one field when it belongs to the given
    /// subject and carries a resolvable <see cref="PersonalDataAttribute"/>.
    /// </summary>
    internal static bool TryBuildLocation(
        object eventBody,
        Type eventType,
        CryptoShreddedFieldInfo field,
        string subjectId,
        out PersonalDataLocation location)
    {
        location = null!;

        // Read the subject ID from the event's subject ID property
        var subjectIdProp = eventType.GetProperty(
            field.SubjectIdProperty,
            BindingFlags.Public | BindingFlags.Instance);

        var eventSubjectId = subjectIdProp?.GetValue(eventBody) as string;
        if (!string.Equals(eventSubjectId, subjectId, StringComparison.Ordinal))
        {
            return false;
        }

        // Get the PersonalData attribute for category and flags
        var personalDataAttr = field.Property.GetCustomAttribute<PersonalDataAttribute>();
        if (personalDataAttr is null)
        {
            return false;
        }

        location = new PersonalDataLocation
        {
            EntityType = eventType,
            EntityId = subjectId,
            FieldName = field.Property.Name,
            Category = personalDataAttr.Category,
            IsErasable = personalDataAttr.Erasable,
            IsPortable = personalDataAttr.Portable,
            HasLegalRetention = personalDataAttr.LegalRetention,
            CurrentValue = field.GetValue(eventBody)
        };
        return true;
    }
}
