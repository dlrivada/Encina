using System.Diagnostics;
using Encina.Messaging.DeadLetter;
using LanguageExt;

namespace Encina.OpenTelemetry.MessagingStores;

/// <summary>
/// Decorator that adds OpenTelemetry distributed tracing to any <see cref="IDeadLetterStore"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps the inner store and creates <see cref="Activity"/> spans for add, query, count, replay claim and mark,
/// and the delete operations. All activity creation is guarded by <see cref="ActivitySource.HasListeners()"/>
/// for zero cost when no trace collector is configured.
/// </para>
/// <para>
/// A span carries the source pattern, the dead letter id (a generated GUID, not a subject identifier), a result
/// count and, on failure, the <c>encina.error_code</c> only. It never carries the tenant id, the request payload,
/// the text of an <see cref="EncinaError"/> or an exception message (SPEC-002 REQ-062).
/// </para>
/// <para>
/// The activity source name <c>"Encina.Messaging.DeadLetter"</c> is registered with the OpenTelemetry tracer by
/// <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
internal sealed class InstrumentedDeadLetterStore : IDeadLetterStore
{
    private static readonly ActivitySource Source = new("Encina.Messaging.DeadLetter", "1.0");

    private readonly IDeadLetterStore _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedDeadLetterStore"/> class.
    /// </summary>
    /// <param name="inner">The inner dead letter store to decorate.</param>
    public InstrumentedDeadLetterStore(IDeadLetterStore inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var activity = Start("encina.dlq.add");
        activity?.SetTag("dlq.source_pattern", message.SourcePattern);
        activity?.SetTag("dlq.message_id", message.Id.ToString());
        var result = await _inner.AddAsync(message, cancellationToken).ConfigureAwait(false);
        return Finish(activity, result);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
        => _inner.GetAsync(messageId, cancellationToken);

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IDeadLetterMessage>>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        bool newestFirst = false,
        CancellationToken cancellationToken = default)
    {
        using var activity = Start("encina.dlq.query");
        activity?.SetTag("dlq.source_pattern", filter?.SourcePattern);
        var result = await _inner.GetMessagesAsync(filter, skip, take, newestFirst, cancellationToken).ConfigureAwait(false);
        return Finish(activity, result, messages => messages.Count());
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetCountAsync(
        DeadLetterFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = Start("encina.dlq.count");
        activity?.SetTag("dlq.source_pattern", filter?.SourcePattern);
        var result = await _inner.GetCountAsync(filter, cancellationToken).ConfigureAwait(false);
        return Finish(activity, result, count => count);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> TryClaimForReplayAsync(
        Guid messageId,
        DateTime claimExpiredBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        using var activity = Start("encina.dlq.replay_claim");
        activity?.SetTag("dlq.message_id", messageId.ToString());
        var result = await _inner.TryClaimForReplayAsync(messageId, claimExpiredBeforeUtc, cancellationToken).ConfigureAwait(false);
        return Finish(activity, result);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> MarkAsReplayedAsync(
        Guid messageId,
        string replayResult,
        CancellationToken cancellationToken = default)
    {
        using var activity = Start("encina.dlq.replay_mark");
        activity?.SetTag("dlq.message_id", messageId.ToString());
        var result = await _inner.MarkAsReplayedAsync(messageId, replayResult, cancellationToken).ConfigureAwait(false);
        return Finish(activity, result);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        using var activity = Start("encina.dlq.delete");
        activity?.SetTag("dlq.message_id", messageId.ToString());
        var result = await _inner.DeleteAsync(messageId, cancellationToken).ConfigureAwait(false);
        return Finish(activity, result);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using var activity = Start("encina.dlq.delete_many");
        activity?.SetTag("dlq.source_pattern", filter.SourcePattern);
        var result = await _inner.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);
        return Finish(activity, result, count => count);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        using var activity = Start("encina.dlq.delete_expired");
        var result = await _inner.DeleteExpiredAsync(cancellationToken).ConfigureAwait(false);
        return Finish(activity, result, count => count);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _inner.SaveChangesAsync(cancellationToken);

    private static Activity? Start(string name)
        => Source.HasListeners() ? Source.StartActivity(name, ActivityKind.Internal) : null;

    private static Either<EncinaError, T> Finish<T>(Activity? activity, Either<EncinaError, T> result, Func<T, int>? count = null)
    {
        if (activity is null)
        {
            return result;
        }

        result.IfRight(value =>
        {
            if (count is not null)
            {
                activity.SetTag("dlq.count", count(value));
            }

            activity.SetStatus(ActivityStatusCode.Ok);
        });
        result.IfLeft(error =>
        {
            // The code only: EncinaError.Message can carry personal data (#1788).
            activity.SetTag("encina.error_code", error.GetCode().IfNone("encina.unknown"));
            activity.SetStatus(ActivityStatusCode.Error);
        });
        return result;
    }
}
