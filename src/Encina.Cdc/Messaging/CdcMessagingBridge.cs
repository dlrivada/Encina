using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Messaging;

/// <summary>
/// CDC event interceptor that publishes captured changes as
/// <see cref="CdcChangeNotification"/> via <see cref="IEncina.Publish{TNotification}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This bridge intercepts all successfully dispatched CDC events and converts them
/// into <see cref="CdcChangeNotification"/> instances published through Encina's
/// standard notification pipeline. Downstream handlers implementing
/// <c>INotificationHandler&lt;CdcChangeNotification&gt;</c> receive the events.
/// </para>
/// <para>
/// Configure via <see cref="CdcConfiguration.WithMessagingBridge"/> to enable
/// the bridge and set filtering/topic options.
/// </para>
/// </remarks>
internal sealed class CdcMessagingBridge : ICdcEventInterceptor
{
    private readonly IEncina _encina;
    private readonly CdcMessagingOptions _options;
    private readonly ILogger<CdcMessagingBridge> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcMessagingBridge"/> class.
    /// </summary>
    /// <param name="encina">The Encina coordinator for publishing notifications.</param>
    /// <param name="options">Messaging bridge options for filtering and topic configuration.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public CdcMessagingBridge(
        IEncina encina,
        CdcMessagingOptions options,
        ILogger<CdcMessagingBridge> logger)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> OnEventDispatchedAsync(
        ChangeEvent changeEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changeEvent);

        if (!_options.ShouldPublish(changeEvent.TableName, changeEvent.Operation))
        {
            CdcMessagingLog.ChangeEventFiltered(_logger, changeEvent.TableName, changeEvent.Operation);
            return Right(unit);
        }

        var notification = CdcChangeNotification.FromChangeEvent(changeEvent, _options.TopicPattern);

        CdcMessagingLog.PublishingChangeNotification(
            _logger, changeEvent.TableName, changeEvent.Operation, notification.TopicName);

        var result = await _encina.Publish(notification, cancellationToken).ConfigureAwait(false);

        if (result.IsRight)
        {
            CdcMessagingLog.PublishedChangeNotification(
                _logger, changeEvent.TableName, changeEvent.Operation);
        }

        return result;
    }
}
