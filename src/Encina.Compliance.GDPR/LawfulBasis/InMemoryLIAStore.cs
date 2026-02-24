using System.Collections.Concurrent;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR;

/// <summary>
/// In-memory implementation of <see cref="ILIAStore"/> for development, testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// LIA records are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="LIARecord.Id"/>, ensuring thread-safe concurrent access.
/// </para>
/// <para>
/// Storing a record with an existing <see cref="LIARecord.Id"/> replaces the previous record
/// (upsert semantics), allowing LIA updates as assessments are reviewed and approved.
/// </para>
/// <para>
/// For production systems requiring durable storage, consider a database-backed implementation
/// of <see cref="ILIAStore"/>.
/// </para>
/// </remarks>
public sealed class InMemoryLIAStore : ILIAStore
{
    private readonly ConcurrentDictionary<string, LIARecord> _records = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> StoreAsync(
        LIARecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        _records.AddOrUpdate(record.Id, record, (_, _) => record);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<LIARecord>>> GetByReferenceAsync(
        string liaReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(liaReference);

        Option<LIARecord> result = _records.TryGetValue(liaReference, out var record)
            ? Some(record)
            : None;

        return ValueTask.FromResult<Either<EncinaError, Option<LIARecord>>>(result);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<LIARecord>>> GetPendingReviewAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LIARecord> result = _records.Values
            .Where(r => r.Outcome == LIAOutcome.RequiresReview)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<LIARecord>>>(Right(result));
    }
}
