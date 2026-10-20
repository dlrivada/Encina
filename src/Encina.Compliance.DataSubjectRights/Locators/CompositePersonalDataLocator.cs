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
/// Fails closed: if any individual locator fails, the failure is logged and the whole request returns
/// <c>Left</c>. A partial inventory would make an access, portability or erasure request look complete while
/// the personal data of an unreachable store is left out (AGENTS.md: compliance gates fail closed).
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
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<IPersonalDataLocator> _locators;
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

    /// <summary>Gets the aggregated locators (used by startup checks that a locator takes part in requests).</summary>
    internal IReadOnlyList<IPersonalDataLocator> Locators => _locators;

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

            // The data subject's own identifier is never logged (#1429, following #1314);
            // correlate via the locator type instead.
            result.Match(
                Right: locations =>
                {
                    allLocations.AddRange(locations);
                    _logger.LogDebug(
                        "Locator {LocatorType} found {Count} personal data locations",
                        locator.GetType().Name,
                        locations.Count);
                },
                Left: error =>
                {
                    failedLocators++;
                    _logger.LogWarning(
                        "Locator {LocatorType} failed: {ErrorCode}",
                        locator.GetType().Name,
                        error.GetCode().IfNone("encina.unknown"));
                });
        }

        if (failedLocators > 0)
        {
            return DSRErrors.LocatorFailed(subjectId,
                $"{failedLocators} of {_locators.Count} personal data locator(s) failed; the inventory would be incomplete.");
        }

        IReadOnlyList<PersonalDataLocation> result2 = allLocations.AsReadOnly();
        return Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(result2);
    }
}
