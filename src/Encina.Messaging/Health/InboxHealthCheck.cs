using Encina.Messaging.Inbox;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check for the Inbox pattern.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>Inbox store is accessible</description></item>
/// <item><description>Can query for expired messages (basic connectivity)</description></item>
/// </list>
/// </para>
/// </remarks>
public class InboxHealthCheck : EncinaHealthCheck
{
    private readonly IInboxStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxHealthCheck"/> class.
    /// </summary>
    /// <param name="store">The inbox store to check.</param>
    public InboxHealthCheck(IInboxStore store)
        : base("encina-inbox", ["ready", "database", "messaging"])
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        // Try to query expired messages as a connectivity check
        var expiredMessages = await _store.GetExpiredMessagesAsync(
            batchSize: 1,
            cancellationToken).ConfigureAwait(false);

        var data = new Dictionary<string, object>
        {
            ["expired_sample"] = expiredMessages.Count()
        };

        return HealthCheckResult.Healthy("Inbox store is accessible and healthy", data);
    }
}
