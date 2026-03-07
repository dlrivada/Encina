using Encina.Messaging.Inbox;

using LanguageExt;

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
        var expiredResult = await _store.GetExpiredMessagesAsync(
            batchSize: 1,
            cancellationToken).ConfigureAwait(false);

        if (expiredResult.IsLeft)
        {
            var storeError = expiredResult.LeftToArray()[0];
            return HealthCheckResult.Unhealthy(
                $"Failed to query inbox store: {storeError.Message}",
                data: new Dictionary<string, object> { ["error"] = storeError.Message });
        }

        var expiredMessages = expiredResult.Match(Right: msgs => msgs, Left: _ => Enumerable.Empty<IInboxMessage>());

        var data = new Dictionary<string, object>
        {
            ["expired_sample"] = expiredMessages.Count()
        };

        return HealthCheckResult.Healthy("Inbox store is accessible and healthy", data);
    }
}
