using System.Collections.Concurrent;

using Encina.Compliance.DPIA.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DPIA;

/// <summary>
/// In-memory implementation of <see cref="IDPIAAuditStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by assessment ID,
/// with each value being a list of audit entries for that assessment.
/// </para>
/// <para>
/// This implementation is not intended for production use. For production, use one of the
/// 13 database provider implementations (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
internal sealed class InMemoryDPIAAuditStore : IDPIAAuditStore
{
    private readonly ConcurrentDictionary<Guid, List<DPIAAuditEntry>> _entries = new();
    private readonly ILogger<InMemoryDPIAAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDPIAAuditStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InMemoryDPIAAuditStore(ILogger<InMemoryDPIAAuditStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RecordAuditEntryAsync(
        DPIAAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var list = _entries.GetOrAdd(entry.AssessmentId, _ => []);

        lock (list)
        {
            list.Add(entry);
        }

        _logger.LogDebug(
            "DPIA audit entry '{Action}' recorded for assessment '{AssessmentId}'.",
            entry.Action, entry.AssessmentId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>> GetAuditTrailAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(assessmentId, out var list))
        {
            List<DPIAAuditEntry> snapshot;
            lock (list)
            {
                snapshot = [.. list.OrderBy(e => e.OccurredAtUtc)];
            }

            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>>(snapshot);
        }

        List<DPIAAuditEntry> empty = [];

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>>(empty);
    }

    /// <summary>
    /// Gets the total number of audit entries across all assessments. Test helper.
    /// </summary>
    internal int Count => _entries.Values.Sum(l => { lock (l) { return l.Count; } });

    /// <summary>
    /// Removes all stored entries. Test helper method.
    /// </summary>
    internal void Clear() => _entries.Clear();
}
