using System.Collections.Concurrent;

using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// In-memory implementation of <see cref="IDPAStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="DataProcessingAgreement.Id"/> for thread-safe access.
/// </para>
/// <para>
/// This implementation is not intended for production use. For production, use one of the
/// 13 database provider implementations (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
internal sealed class InMemoryDPAStore : IDPAStore
{
    private readonly ConcurrentDictionary<string, DataProcessingAgreement> _agreements = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryDPAStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDPAStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InMemoryDPAStore(ILogger<InMemoryDPAStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of agreements currently stored.
    /// </summary>
    internal int Count => _agreements.Count;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> AddAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        if (!_agreements.TryAdd(agreement.Id, agreement))
        {
            _logger.DPAAdditionFailed(agreement.Id, "already_exists");
            ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1,
                new(ProcessorAgreementDiagnostics.TagOperation, "Add"),
                new(ProcessorAgreementDiagnostics.TagOutcome, "failed"));
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ProcessorAgreementErrors.StoreError("AddDPA", $"Agreement '{agreement.Id}' already exists."));
        }

        _logger.DPAAdded(agreement.Id, agreement.ProcessorId, agreement.Status.ToString());
        ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1,
            new(ProcessorAgreementDiagnostics.TagOperation, "Add"),
            new(ProcessorAgreementDiagnostics.TagOutcome, "completed"));

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
        string dpaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dpaId);

        var option = _agreements.TryGetValue(dpaId, out var agreement)
            ? Some(agreement)
            : Option<DataProcessingAgreement>.None;

        return ValueTask.FromResult(Right<EncinaError, Option<DataProcessingAgreement>>(option));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        var agreements = _agreements.Values
            .Where(a => a.ProcessorId == processorId)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>>(agreements);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        var active = _agreements.Values
            .FirstOrDefault(a => a.ProcessorId == processorId && a.Status == DPAStatus.Active);

        var option = active is not null
            ? Some(active)
            : Option<DataProcessingAgreement>.None;

        _logger.ActiveDPARetrieved(processorId, active is not null);

        return ValueTask.FromResult(Right<EncinaError, Option<DataProcessingAgreement>>(option));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        if (!_agreements.ContainsKey(agreement.Id))
        {
            _logger.DPAUpdateFailed(agreement.Id, "not_found");
            ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1,
                new(ProcessorAgreementDiagnostics.TagOperation, "Update"),
                new(ProcessorAgreementDiagnostics.TagOutcome, "failed"));
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                ProcessorAgreementErrors.DPANotFound(agreement.Id));
        }

        _agreements[agreement.Id] = agreement;

        _logger.DPAUpdated(agreement.Id, agreement.Status.ToString());
        ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1,
            new(ProcessorAgreementDiagnostics.TagOperation, "Update"),
            new(ProcessorAgreementDiagnostics.TagOutcome, "completed"));

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByStatusAsync(
        DPAStatus status,
        CancellationToken cancellationToken = default)
    {
        var agreements = _agreements.Values
            .Where(a => a.Status == status)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>>(agreements);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetExpiringAsync(
        DateTimeOffset threshold,
        CancellationToken cancellationToken = default)
    {
        var expiring = _agreements.Values
            .Where(a => a.Status == DPAStatus.Active
                && a.ExpiresAtUtc is not null
                && a.ExpiresAtUtc <= threshold)
            .ToList();

        _logger.ExpiringDPAsRetrieved(expiring.Count, threshold);

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>>(expiring);
    }

    /// <summary>
    /// Returns all stored agreements. Test helper method.
    /// </summary>
    internal IReadOnlyList<DataProcessingAgreement> GetAllRecords() => _agreements.Values.ToList();

    /// <summary>
    /// Removes all stored agreements. Test helper method.
    /// </summary>
    internal void Clear() => _agreements.Clear();
}
