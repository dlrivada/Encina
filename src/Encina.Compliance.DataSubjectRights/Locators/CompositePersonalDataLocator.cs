using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Aggregates results from multiple <see cref="IPersonalDataLocator"/> implementations.
/// </summary>
/// <remarks>
/// <para>
/// When multiple data locators are registered (e.g., one per database, one for file storage,
/// one for external services), this composite locator combines their results into a single
/// unified response.
/// </para>
/// <para>
/// If any individual locator fails, the error is logged and the locator is skipped —
/// results from other locators are still returned. This ensures partial availability
/// when some data stores are temporarily unreachable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var locators = new IPersonalDataLocator[]
/// {
///     new EfCorePersonalDataLocator(dbContext),
///     new BlobStoragePersonalDataLocator(blobClient)
/// };
///
/// var composite = new CompositePersonalDataLocator(locators, logger);
/// var result = await composite.LocateAllDataAsync("subject-123");
/// </code>
/// </example>
public sealed class CompositePersonalDataLocator : IPersonalDataLocator
{
    private readonly IReadOnlyList<IPersonalDataLocator> _locators;
    private readonly ILogger<CompositePersonalDataLocator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositePersonalDataLocator"/> class.
    /// </summary>
    /// <param name="locators">The collection of locators to aggregate results from.</param>
    /// <param name="logger">Logger for structured logging of locator operations.</param>
    public CompositePersonalDataLocator(
        IEnumerable<IPersonalDataLocator> locators,
        ILogger<CompositePersonalDataLocator> logger)
    {
        ArgumentNullException.ThrowIfNull(locators);
        ArgumentNullException.ThrowIfNull(logger);

        _locators = locators.ToList().AsReadOnly();
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>> LocateAllDataAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        var allLocations = new List<PersonalDataLocation>();
        var failedLocators = 0;

        foreach (var locator in _locators)
        {
            var result = await locator.LocateAllDataAsync(subjectId, cancellationToken).ConfigureAwait(false);

            result.Match(
                Right: locations =>
                {
                    allLocations.AddRange(locations);
                    _logger.LogDebug(
                        "Locator {LocatorType} found {Count} personal data locations for subject '{SubjectId}'",
                        locator.GetType().Name,
                        locations.Count,
                        subjectId);
                },
                Left: error =>
                {
                    failedLocators++;
                    _logger.LogWarning(
                        "Locator {LocatorType} failed for subject '{SubjectId}': {ErrorMessage}",
                        locator.GetType().Name,
                        subjectId,
                        error.Message);
                });
        }

        if (failedLocators > 0 && allLocations.Count == 0)
        {
            return DSRErrors.LocatorFailed(subjectId,
                $"All {failedLocators} personal data locator(s) failed. No data could be located.");
        }

        IReadOnlyList<PersonalDataLocation> result2 = allLocations.AsReadOnly();
        return Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(result2);
    }
}
