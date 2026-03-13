using System.Collections.Concurrent;

using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// In-memory implementation of <see cref="IProcessorAuditStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by processor ID,
/// with each value being a list of audit entries for that processor.
/// </para>
/// <para>
/// This implementation is not intended for production use. For production, use one of the
/// 13 database provider implementations (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
internal sealed class InMemoryProcessorAuditStore : IProcessorAuditStore
{
    private readonly ConcurrentDictionary<string, List<ProcessorAgreementAuditEntry>> _entries = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryProcessorAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryProcessorAuditStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InMemoryProcessorAuditStore(ILogger<InMemoryProcessorAuditStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ProcessorAgreementAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var list = _entries.GetOrAdd(entry.ProcessorId, _ => []);

        lock (list)
        {
            list.Add(entry);
        }

        _logger.AuditEntryRecorded(entry.ProcessorId, entry.Action, entry.PerformedByUserId ?? "Unknown");
        ProcessorAgreementDiagnostics.AuditEntryTotal.Add(1,
            new(ProcessorAgreementDiagnostics.TagProcessorId, entry.ProcessorId),
            new(ProcessorAgreementDiagnostics.TagOperation, entry.Action));

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>> GetAuditTrailAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        if (_entries.TryGetValue(processorId, out var list))
        {
            List<ProcessorAgreementAuditEntry> snapshot;
            lock (list)
            {
                snapshot = [.. list.OrderBy(e => e.OccurredAtUtc)];
            }

            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>>(snapshot);
        }

        List<ProcessorAgreementAuditEntry> empty = [];

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>>(empty);
    }

    /// <summary>
    /// Gets the total number of audit entries across all processors. Test helper.
    /// </summary>
    internal int Count => _entries.Values.Sum(l => { lock (l) { return l.Count; } });

    /// <summary>
    /// Removes all stored entries. Test helper method.
    /// </summary>
    internal void Clear() => _entries.Clear();
}
