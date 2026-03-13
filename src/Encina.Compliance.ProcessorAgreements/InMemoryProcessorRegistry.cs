using System.Collections.Concurrent;

using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// In-memory implementation of <see cref="IProcessorRegistry"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by <see cref="Processor.Id"/>
/// for thread-safe access. Supports full hierarchy validation: when registering a sub-processor,
/// the parent must exist and the depth must equal <c>parent.Depth + 1</c>, bounded by
/// <see cref="MaxSubProcessorDepth"/>.
/// </para>
/// <para>
/// This implementation is not intended for production use. For production, use one of the
/// 13 database provider implementations (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
internal sealed class InMemoryProcessorRegistry : IProcessorRegistry
{
    /// <summary>
    /// Default maximum depth for sub-processor chains.
    /// </summary>
    internal const int DefaultMaxSubProcessorDepth = 5;

    private readonly ConcurrentDictionary<string, Processor> _processors = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryProcessorRegistry> _logger;

    /// <summary>
    /// Gets or sets the maximum allowed sub-processor depth.
    /// </summary>
    internal int MaxSubProcessorDepth { get; set; } = DefaultMaxSubProcessorDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryProcessorRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InMemoryProcessorRegistry(ILogger<InMemoryProcessorRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of processors currently registered.
    /// </summary>
    internal int Count => _processors.Count;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RegisterProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        if (_processors.ContainsKey(processor.Id))
        {
            _logger.ProcessorRegistrationFailed(processor.Id, "already_exists");
            ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
                new(ProcessorAgreementDiagnostics.TagOperation, "Register"),
                new(ProcessorAgreementDiagnostics.TagOutcome, "failed"));
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ProcessorAgreementErrors.AlreadyExists(processor.Id));
        }

        // Validate depth constraint (DC 5).
        if (processor.Depth > MaxSubProcessorDepth)
        {
            _logger.SubProcessorDepthExceeded(processor.Id, processor.Depth, MaxSubProcessorDepth);
            ProcessorAgreementDiagnostics.SubProcessorDepthExceededTotal.Add(1,
                new KeyValuePair<string, object?>(ProcessorAgreementDiagnostics.TagProcessorId, processor.Id));
            ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
                new(ProcessorAgreementDiagnostics.TagOperation, "Register"),
                new(ProcessorAgreementDiagnostics.TagOutcome, "failed"));
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ProcessorAgreementErrors.SubProcessorDepthExceeded(
                    processor.Id, processor.Depth, MaxSubProcessorDepth));
        }

        // Validate parent exists and depth is consistent.
        if (processor.ParentProcessorId is not null)
        {
            if (!_processors.TryGetValue(processor.ParentProcessorId, out var parent))
            {
                _logger.ProcessorRegistrationFailed(processor.Id, "parent_not_found");
                ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
                    new(ProcessorAgreementDiagnostics.TagOperation, "Register"),
                    new(ProcessorAgreementDiagnostics.TagOutcome, "failed"));
                return ValueTask.FromResult<Either<EncinaError, Unit>>(
                    ProcessorAgreementErrors.NotFound(processor.ParentProcessorId));
            }

            if (processor.Depth != parent.Depth + 1)
            {
                _logger.SubProcessorDepthInconsistent(
                    processor.Id, processor.ParentProcessorId, parent.Depth + 1, processor.Depth);
                ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
                    new(ProcessorAgreementDiagnostics.TagOperation, "Register"),
                    new(ProcessorAgreementDiagnostics.TagOutcome, "failed"));
                return ValueTask.FromResult<Either<EncinaError, Unit>>(
                    ProcessorAgreementErrors.ValidationFailed(
                        processor.Id,
                        $"Depth must be {parent.Depth + 1} (parent depth + 1), but was {processor.Depth}."));
            }
        }

        _processors[processor.Id] = processor;

        // Log sub-processor registration separately (DC 5).
        if (processor.ParentProcessorId is not null)
        {
            _logger.SubProcessorRegistered(processor.Id, processor.ParentProcessorId, processor.Depth);
        }
        else
        {
            _logger.ProcessorRegistered(processor.Id, processor.Name, processor.Depth);
        }

        ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
            new(ProcessorAgreementDiagnostics.TagOperation, "Register"),
            new(ProcessorAgreementDiagnostics.TagOutcome, "completed"));
        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<Processor>>> GetProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        var option = _processors.TryGetValue(processorId, out var processor)
            ? Some(processor)
            : Option<Processor>.None;

        return ValueTask.FromResult(Right<EncinaError, Option<Processor>>(option));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default)
    {
        var all = _processors.Values.ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<Processor>>>(all);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdateProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        if (!_processors.ContainsKey(processor.Id))
        {
            _logger.ProcessorUpdateFailed(processor.Id, "not_found");
            ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
                new(ProcessorAgreementDiagnostics.TagOperation, "Update"),
                new(ProcessorAgreementDiagnostics.TagOutcome, "failed"));
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ProcessorAgreementErrors.NotFound(processor.Id));
        }

        _processors[processor.Id] = processor;

        _logger.ProcessorUpdated(processor.Id);
        ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
            new(ProcessorAgreementDiagnostics.TagOperation, "Update"),
            new(ProcessorAgreementDiagnostics.TagOutcome, "completed"));

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        if (!_processors.TryRemove(processorId, out _))
        {
            _logger.ProcessorRemovalFailed(processorId, "not_found");
            ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
                new(ProcessorAgreementDiagnostics.TagOperation, "Remove"),
                new(ProcessorAgreementDiagnostics.TagOutcome, "failed"));
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ProcessorAgreementErrors.NotFound(processorId));
        }

        _logger.ProcessorRemoved(processorId);
        ProcessorAgreementDiagnostics.RegistryOperationTotal.Add(1,
            new(ProcessorAgreementDiagnostics.TagOperation, "Remove"),
            new(ProcessorAgreementDiagnostics.TagOutcome, "completed"));

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetSubProcessorsAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        var children = _processors.Values
            .Where(p => p.ParentProcessorId == processorId)
            .ToList();

        _logger.SubProcessorsRetrieved(processorId, children.Count);

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<Processor>>>(children);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetFullSubProcessorChainAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        // BFS traversal bounded by MaxSubProcessorDepth.
        var result = new List<Processor>();
        var queue = new Queue<string>();
        queue.Enqueue(processorId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = _processors.Values
                .Where(p => p.ParentProcessorId == currentId && p.Depth <= MaxSubProcessorDepth);

            foreach (var child in children)
            {
                result.Add(child);
                queue.Enqueue(child.Id);
            }
        }

        _logger.SubProcessorChainResolved(processorId, result.Count);

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<Processor>>>(result);
    }

    /// <summary>
    /// Returns all stored processors. Test helper method.
    /// </summary>
    internal IReadOnlyList<Processor> GetAllRecords() => _processors.Values.ToList();

    /// <summary>
    /// Removes all stored processors. Test helper method.
    /// </summary>
    internal void Clear() => _processors.Clear();
}
