using System.Diagnostics;

using Encina.Compliance.DataSubjectRights.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Default implementation of <see cref="IDataPortabilityExporter"/> that coordinates
/// data location, filtering, and format-specific serialization.
/// </summary>
/// <remarks>
/// <para>
/// The export workflow is:
/// <list type="number">
/// <item>Locate all personal data for the subject via <see cref="IPersonalDataLocator"/>.</item>
/// <item>Filter to only portable fields (<see cref="PersonalDataLocation.IsPortable"/> = <c>true</c>).</item>
/// <item>Resolve the appropriate <see cref="IExportFormatWriter"/> for the requested format.</item>
/// <item>Delegate serialization to the writer and wrap in a <see cref="PortabilityResponse"/>.</item>
/// </list>
/// </para>
/// <para>
/// Per Article 20, only data processed by automated means based on consent or contract
/// is eligible for portability. The <see cref="PersonalDataAttribute.Portable"/> flag
/// controls which fields are included.
/// </para>
/// </remarks>
public sealed class DefaultDataPortabilityExporter : IDataPortabilityExporter
{
    private readonly IPersonalDataLocator _locator;
    private readonly Dictionary<ExportFormat, IExportFormatWriter> _writers;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultDataPortabilityExporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDataPortabilityExporter"/> class.
    /// </summary>
    /// <param name="locator">The personal data locator for discovering data to export.</param>
    /// <param name="writers">The available export format writers, keyed by format.</param>
    /// <param name="timeProvider">Time provider for response timestamps.</param>
    /// <param name="logger">Logger for structured export logging.</param>
    public DefaultDataPortabilityExporter(
        IPersonalDataLocator locator,
        IEnumerable<IExportFormatWriter> writers,
        TimeProvider timeProvider,
        ILogger<DefaultDataPortabilityExporter> logger)
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(writers);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _locator = locator;
        _writers = writers.ToDictionary(w => w.SupportedFormat);
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PortabilityResponse>> ExportAsync(
        string subjectId,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        _logger.PortabilityExportStarted(subjectId, format.ToString());
        using var activity = DataSubjectRightsDiagnostics.StartPortabilityExport(subjectId, format);
        var stopwatch = Stopwatch.StartNew();

        // Resolve the writer for the requested format
        if (!_writers.TryGetValue(format, out var writer))
        {
            _logger.ExportFormatNotSupported(format.ToString());

            stopwatch.Stop();
            DataSubjectRightsDiagnostics.RecordFailed(activity, "format_not_supported");
            DataSubjectRightsDiagnostics.PortabilityExportsTotal.Add(1,
                new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagFormat, format.ToString()),
                new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "failed"));
            DataSubjectRightsDiagnostics.PortabilityDuration.Record(stopwatch.Elapsed.TotalMilliseconds);

            return DSRErrors.FormatNotSupported(format);
        }

        // Locate all personal data
        var locateResult = await _locator.LocateAllDataAsync(subjectId, cancellationToken).ConfigureAwait(false);

        return await locateResult.MatchAsync(
            RightAsync: async locations =>
            {
                // Filter to portable fields only
                var portableData = locations
                    .Where(l => l.IsPortable)
                    .ToList()
                    .AsReadOnly();

                // Delegate to the format writer
                var writeResult = await writer.WriteAsync(portableData, cancellationToken).ConfigureAwait(false);

                return writeResult.Map(exportedData =>
                {
                    _logger.PortabilityExportCompleted(subjectId, format.ToString(), portableData.Count, locations.Count);

                    stopwatch.Stop();
                    DataSubjectRightsDiagnostics.RecordCompleted(activity);
                    DataSubjectRightsDiagnostics.PortabilityExportsTotal.Add(1,
                        new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagFormat, format.ToString()),
                        new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "completed"));
                    DataSubjectRightsDiagnostics.PortabilityDuration.Record(stopwatch.Elapsed.TotalMilliseconds);

                    return new PortabilityResponse
                    {
                        SubjectId = subjectId,
                        ExportedData = exportedData,
                        GeneratedAtUtc = _timeProvider.GetUtcNow()
                    };
                });
            },
            Left: error =>
            {
                _logger.PortabilityExportFailed(subjectId, format.ToString(), error.Message);

                stopwatch.Stop();
                DataSubjectRightsDiagnostics.RecordFailed(activity, error.Message);
                DataSubjectRightsDiagnostics.PortabilityExportsTotal.Add(1,
                    new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagFormat, format.ToString()),
                    new KeyValuePair<string, object?>(DataSubjectRightsDiagnostics.TagOutcome, "failed"));
                DataSubjectRightsDiagnostics.PortabilityDuration.Record(stopwatch.Elapsed.TotalMilliseconds);

                return error;
            }).ConfigureAwait(false);
    }
}
