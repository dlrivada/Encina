using System.Collections.Concurrent;

using Encina.Compliance.DPIA.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DPIA;

/// <summary>
/// In-memory implementation of <see cref="IDPIAStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for thread-safe access with dual indexing:
/// a primary index by <see cref="DPIAAssessment.RequestTypeName"/> (string) and a secondary
/// index by <see cref="DPIAAssessment.Id"/> (Guid).
/// </para>
/// <para>
/// This implementation is not intended for production use. For production, use one of the
/// 13 database provider implementations (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
internal sealed class InMemoryDPIAStore : IDPIAStore
{
    private readonly ConcurrentDictionary<string, DPIAAssessment> _byRequestType = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, DPIAAssessment> _byId = new();
    private readonly ILogger<InMemoryDPIAStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDPIAStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public InMemoryDPIAStore(ILogger<InMemoryDPIAStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of assessments currently stored.
    /// </summary>
    internal int Count => _byId.Count;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> SaveAssessmentAsync(
        DPIAAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        _byRequestType[assessment.RequestTypeName] = assessment;
        _byId[assessment.Id] = assessment;

        _logger.LogDebug("DPIA assessment '{AssessmentId}' saved for request type '{RequestTypeName}'.",
            assessment.Id, assessment.RequestTypeName);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestTypeName);

        var option = _byRequestType.TryGetValue(requestTypeName, out var assessment)
            ? Some(assessment)
            : Option<DPIAAssessment>.None;

        return ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(option));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentByIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var option = _byId.TryGetValue(assessmentId, out var assessment)
            ? Some(assessment)
            : Option<DPIAAssessment>.None;

        return ValueTask.FromResult(Right<EncinaError, Option<DPIAAssessment>>(option));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetExpiredAssessmentsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var expired = _byId.Values
            .Where(a => a.Status == DPIAAssessmentStatus.Approved
                && a.NextReviewAtUtc is not null
                && a.NextReviewAtUtc <= nowUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAAssessment>>>(expired);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetAllAssessmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var all = _byId.Values.ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAAssessment>>>(all);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> DeleteAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        if (!_byId.TryRemove(assessmentId, out var removed))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                DPIAErrors.StoreError("DeleteAssessment", $"Assessment '{assessmentId}' not found."));
        }

        _byRequestType.TryRemove(removed.RequestTypeName, out _);

        _logger.LogDebug("DPIA assessment '{AssessmentId}' deleted.", assessmentId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <summary>
    /// Returns all stored assessments. Test helper method.
    /// </summary>
    internal IReadOnlyList<DPIAAssessment> GetAllRecords() => _byId.Values.ToList();

    /// <summary>
    /// Removes all stored assessments. Test helper method.
    /// </summary>
    internal void Clear()
    {
        _byRequestType.Clear();
        _byId.Clear();
    }
}
