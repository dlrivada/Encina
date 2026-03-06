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
            _logger.LogDebug(
                "Locating personal data for subject {SubjectId} in Marten event store",
                subjectId);

            // Query all raw events from the store
            var allEvents = await _session.Events
                .QueryAllRawEvents()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var locations = new List<PersonalDataLocation>();

            foreach (var eventData in allEvents)
            {
                var eventBody = eventData.Data;
                if (eventBody is null)
                {
                    continue;
                }

                var eventType = eventBody.GetType();
                var fields = CryptoShreddedPropertyCache.GetFields(eventType);

                if (fields.Length == 0)
                {
                    continue;
                }

                foreach (var field in fields)
                {
                    // Read the subject ID from the event's subject ID property
                    var subjectIdProp = eventType.GetProperty(
                        field.SubjectIdProperty,
                        BindingFlags.Public | BindingFlags.Instance);

                    if (subjectIdProp is null)
                    {
                        continue;
                    }

                    var eventSubjectId = subjectIdProp.GetValue(eventBody) as string;
                    if (!string.Equals(eventSubjectId, subjectId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // Get the PersonalData attribute for category and flags
                    var personalDataAttr = field.Property.GetCustomAttribute<PersonalDataAttribute>();
                    if (personalDataAttr is null)
                    {
                        continue;
                    }

                    var currentValue = field.GetValue(eventBody);

                    locations.Add(new PersonalDataLocation
                    {
                        EntityType = eventType,
                        EntityId = subjectId,
                        FieldName = field.Property.Name,
                        Category = personalDataAttr.Category,
                        IsErasable = personalDataAttr.Erasable,
                        IsPortable = personalDataAttr.Portable,
                        HasLegalRetention = personalDataAttr.LegalRetention,
                        CurrentValue = currentValue
                    });
                }
            }

            _logger.LogDebug(
                "Located {Count} personal data fields for subject {SubjectId}",
                locations.Count,
                subjectId);

            return Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to locate personal data for subject {SubjectId}",
                subjectId);

            return Left<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                CryptoShreddingErrors.KeyStoreError("LocateAllData", ex));
        }
    }
}
