using Marten;
using Marten.Services;

namespace Encina.Marten;

/// <summary>
/// Session listener that records the domain-event notifications of every successful commit, so
/// <see cref="EventPublishingPipelineBehavior{TRequest, TResponse}"/> can publish them after the
/// command: the aggregate repository saves the session inside the handler, which leaves nothing
/// pending by the time the behavior resumes.
/// </summary>
/// <remarks>
/// Deliberately not generic: one collector per session is shared by every command type, so exactly
/// one behavior instance (the one that attached it) publishes.
/// </remarks>
internal sealed class CommittedEventCollector : DocumentSessionListenerBase
{
    private readonly List<INotification> _events = [];

    /// <inheritdoc />
    public override Task AfterCommitAsync(IDocumentSession session, IChangeSet commit, CancellationToken token)
    {
        lock (_events)
        {
            _events.AddRange(commit.GetEvents().Select(e => e.Data).OfType<INotification>());
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the notifications recorded so far and forgets them, so each is published once.
    /// </summary>
    /// <returns>The notifications committed since the last drain, in commit order.</returns>
    public List<INotification> Drain()
    {
        lock (_events)
        {
            var drained = _events.ToList();
            _events.Clear();
            return drained;
        }
    }
}
