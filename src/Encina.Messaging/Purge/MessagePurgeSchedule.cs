using Encina.Messaging.Inbox;

namespace Encina.Messaging.Purge;

/// <summary>
/// What one purge loop of the <see cref="MessagePurgeService"/> runs: how often and in which batch size.
/// </summary>
/// <param name="Interval">The pause between two purge cycles.</param>
/// <param name="BatchSize">The maximum number of messages one batch removes.</param>
internal readonly record struct PurgeEntry(TimeSpan Interval, int BatchSize);

/// <summary>
/// The purge loops the <see cref="MessagePurgeService"/> runs, one entry per messaging pattern, or
/// <see langword="null"/> when that pattern's automatic purge is off.
/// </summary>
/// <param name="InboxPurge">The inbox purge loop (<see cref="InboxOptions"/>).</param>
internal sealed record MessagePurgeSchedule(PurgeEntry? InboxPurge)
{
    /// <summary>
    /// Gets a value indicating whether at least one pattern has its automatic purge on.
    /// </summary>
    internal bool HasEntries => InboxPurge is not null;

    /// <summary>
    /// Builds the schedule from the inbox options, reading their current values.
    /// </summary>
    /// <param name="inboxOptions">The inbox options.</param>
    /// <returns>The schedule; the inbox entry is <see langword="null"/> when its purge is off.</returns>
    internal static MessagePurgeSchedule Create(InboxOptions inboxOptions)
    {
        ArgumentNullException.ThrowIfNull(inboxOptions);

        return new MessagePurgeSchedule(
            inboxOptions.EnableAutomaticPurge
                ? new PurgeEntry(inboxOptions.PurgeInterval, inboxOptions.PurgeBatchSize)
                : null);
    }
}
