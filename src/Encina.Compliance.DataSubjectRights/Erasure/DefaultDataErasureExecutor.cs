using System.Diagnostics;

using Encina.Compliance.DataSubjectRights.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Default implementation of <see cref="IDataErasureExecutor"/> that orchestrates the erasure workflow.
/// </summary>
/// <remarks>
/// <para>
/// The erasure workflow is:
/// <list type="number">
/// <item>Locate all personal data for the subject via <see cref="IPersonalDataLocator"/>.</item>
/// <item>Filter by the <see cref="ErasureScope"/> (categories and specific fields).</item>
/// <item>Separate erasable fields from retained fields (legal retention or non-erasable).</item>
/// <item>Apply <see cref="IDataErasureStrategy"/> to each erasable field.</item>
/// <item>Build a detailed <see cref="ErasureResult"/> including retention reasons.</item>
/// </list>
/// </para>
/// <para>
/// Fields with <see cref="PersonalDataLocation.HasLegalRetention"/> set to <c>true</c> are
/// unconditionally excluded from erasure and documented in the result, as required by
/// Article 17(3).
/// </para>
/// </remarks>
public sealed class DefaultDataErasureExecutor : IDataErasureExecutor
{
    private readonly IPersonalDataLocator _locator;
    private readonly IDataErasureStrategy _strategy;
    private readonly ILogger<DefaultDataErasureExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDataErasureExecutor"/> class.
    /// </summary>
    /// <param name="locator">The personal data locator for discovering data to erase.</param>
    /// <param name="strategy">The erasure strategy to apply to each field.</param>
    /// <param name="logger">Logger for structured erasure logging.</param>
    public DefaultDataErasureExecutor(
        IPersonalDataLocator locator,
        IDataErasureStrategy strategy,
        ILogger<DefaultDataErasureExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(logger);

        _locator = locator;
        _strategy = strategy;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ErasureResult>> EraseAsync(
        string subjectId,
        ErasureScope scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentNullException.ThrowIfNull(scope);

        using var activity = DataSubjectRightsDiagnostics.StartErasure(subjectId);
        var stopwatch = Stopwatch.StartNew();

        // Step 1: Locate all personal data
        var locateResult = await _locator.LocateAllDataAsync(subjectId, cancellationToken).ConfigureAwait(false);

        return await locateResult.MatchAsync(
            RightAsync: async locations =>
            {
                if (locations.Count == 0)
                {
                    _logger.ErasureNoDataFound(subjectId);

                    stopwatch.Stop();
                    DataSubjectRightsDiagnostics.RecordCompleted(activity);
                    DataSubjectRightsDiagnostics.ErasureDuration.Record(stopwatch.Elapsed.TotalMilliseconds);

                    return Right<EncinaError, ErasureResult>(new ErasureResult
                    {
                        FieldsErased = 0,
                        FieldsRetained = 0,
                        FieldsFailed = 0,
                        RetentionReasons = [],
                        Exemptions = []
                    });
                }

                // Step 2: Filter by scope
                var scopedLocations = ApplyScope(locations, scope);

                // Step 3: Separate erasable from retained
                var (erasable, retained) = PartitionFields(scopedLocations);

                // Step 4: Apply strategy to erasable fields
                var erased = 0;
                var failed = 0;

                foreach (var location in erasable)
                {
                    var eraseResult = await _strategy.EraseFieldAsync(location, cancellationToken)
                        .ConfigureAwait(false);

                    eraseResult.Match(
                        Right: _ =>
                        {
                            erased++;
                            _logger.ErasureFieldErased(
                                location.FieldName,
                                location.EntityType.Name,
                                location.EntityId);
                        },
                        Left: error =>
                        {
                            failed++;
                            _logger.ErasureFieldFailed(
                                location.FieldName,
                                location.EntityType.Name,
                                location.EntityId,
                                error.Message);
                        });
                }

                // Step 5: Build retention details
                var retentionReasons = retained
                    .Select(r => new RetentionDetail
                    {
                        FieldName = r.FieldName,
                        EntityType = r.EntityType,
                        Reason = r.HasLegalRetention
                            ? "Legal retention requirement (Article 17(3))"
                            : "Field is not erasable"
                    })
                    .ToList()
                    .AsReadOnly();

                // Collect exemptions from scope or auto-detect
                IReadOnlyList<ErasureExemption> exemptions = scope.ExemptionsToApply ?? [];
                if (retained.Count > 0 && exemptions.Count == 0)
                {
                    exemptions = retained.Any(r => r.HasLegalRetention)
                        ? [ErasureExemption.LegalObligation]
                        : [];
                }

                _logger.ErasureCompleted(subjectId, erased, retained.Count, failed);

                // Record metrics
                stopwatch.Stop();
                DataSubjectRightsDiagnostics.RecordCompleted(activity);
                DataSubjectRightsDiagnostics.ErasureDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
                DataSubjectRightsDiagnostics.ErasureFieldsErasedTotal.Add(erased);
                DataSubjectRightsDiagnostics.ErasureFieldsRetainedTotal.Add(retained.Count);

                return Right<EncinaError, ErasureResult>(new ErasureResult
                {
                    FieldsErased = erased,
                    FieldsRetained = retained.Count,
                    FieldsFailed = failed,
                    RetentionReasons = retentionReasons,
                    Exemptions = exemptions
                });
            },
            Left: error =>
            {
                _logger.ErasureFailed(subjectId, error.Message);

                stopwatch.Stop();
                DataSubjectRightsDiagnostics.RecordFailed(activity, error.Message);
                DataSubjectRightsDiagnostics.ErasureDuration.Record(stopwatch.Elapsed.TotalMilliseconds);

                return error;
            }).ConfigureAwait(false);
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<PersonalDataLocation> ApplyScope(
        IReadOnlyList<PersonalDataLocation> locations,
        ErasureScope scope)
    {
        var filtered = locations.AsEnumerable();

        if (scope.Categories is { Count: > 0 })
        {
            var categories = new System.Collections.Generic.HashSet<PersonalDataCategory>(scope.Categories);
            filtered = filtered.Where(l => categories.Contains(l.Category));
        }

        if (scope.SpecificFields is { Count: > 0 })
        {
            var fields = new System.Collections.Generic.HashSet<string>(scope.SpecificFields, StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(l => fields.Contains(l.FieldName));
        }

        return filtered.ToList().AsReadOnly();
    }

    private static (List<PersonalDataLocation> Erasable, List<PersonalDataLocation> Retained) PartitionFields(
        IReadOnlyList<PersonalDataLocation> locations)
    {
        var erasable = new List<PersonalDataLocation>();
        var retained = new List<PersonalDataLocation>();

        foreach (var location in locations)
        {
            // Legal retention ALWAYS takes priority — never erase retained fields
            if (location.HasLegalRetention || !location.IsErasable)
            {
                retained.Add(location);
            }
            else
            {
                erasable.Add(location);
            }
        }

        return (erasable, retained);
    }
}
