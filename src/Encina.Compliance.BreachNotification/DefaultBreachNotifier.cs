using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Default no-op implementation of <see cref="IBreachNotifier"/> that logs notifications
/// without performing actual delivery.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides a safe default for development and testing environments
/// where actual notification delivery (email, API calls, etc.) is not configured.
/// All notifications are logged at <see cref="LogLevel.Warning"/> level to ensure
/// visibility, and successful <see cref="NotificationResult"/> instances are returned.
/// </para>
/// <para>
/// For production use, applications should register their own <see cref="IBreachNotifier"/>
/// implementation that handles the specifics of notification delivery to supervisory
/// authorities and data subjects as required by GDPR Articles 33 and 34.
/// </para>
/// </remarks>
public sealed class DefaultBreachNotifier : IBreachNotifier
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultBreachNotifier> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBreachNotifier"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider for notification timestamps.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultBreachNotifier(
        TimeProvider timeProvider,
        ILogger<DefaultBreachNotifier> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, NotificationResult>> NotifyAuthorityAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        _logger.LogWarning(
            "DefaultBreachNotifier: Authority notification for breach '{BreachId}' logged but NOT delivered. " +
            "Register a custom IBreachNotifier for production notification delivery.",
            breach.Id);

        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Sent,
            SentAtUtc = _timeProvider.GetUtcNow(),
            Recipient = "supervisory-authority (no-op)",
            BreachId = breach.Id
        };

        return ValueTask.FromResult<Either<EncinaError, NotificationResult>>(
            Right<EncinaError, NotificationResult>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, NotificationResult>> NotifyDataSubjectsAsync(
        BreachRecord breach,
        IEnumerable<string> subjectIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);
        ArgumentNullException.ThrowIfNull(subjectIds);

        var subjectCount = subjectIds.Count();

        _logger.LogWarning(
            "DefaultBreachNotifier: Data subject notification for breach '{BreachId}' ({SubjectCount} subjects) " +
            "logged but NOT delivered. Register a custom IBreachNotifier for production notification delivery.",
            breach.Id, subjectCount);

        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Sent,
            SentAtUtc = _timeProvider.GetUtcNow(),
            Recipient = $"data-subjects ({subjectCount} subjects, no-op)",
            BreachId = breach.Id
        };

        return ValueTask.FromResult<Either<EncinaError, NotificationResult>>(
            Right<EncinaError, NotificationResult>(result));
    }
}
