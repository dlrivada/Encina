using Microsoft.Extensions.Options;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Validates <see cref="BreachNotificationOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that breach notification configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;BreachNotificationOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured breach compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="BreachNotificationOptions.EnforcementMode"/> is a defined enum value</description></item>
/// <item><description><see cref="BreachNotificationOptions.NotificationDeadlineHours"/> is positive</description></item>
/// <item><description><see cref="BreachNotificationOptions.AlertAtHoursRemaining"/> values are positive and less than the deadline</description></item>
/// <item><description><see cref="BreachNotificationOptions.DeadlineCheckInterval"/> is positive</description></item>
/// <item><description><see cref="BreachNotificationOptions.SubjectNotificationSeverityThreshold"/> is a defined enum value</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class BreachNotificationOptionsValidator : IValidateOptions<BreachNotificationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, BreachNotificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate EnforcementMode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"BreachNotificationOptions.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Block (0), Warn (1), or Disabled (2).");
        }

        // Validate NotificationDeadlineHours is positive
        if (options.NotificationDeadlineHours <= 0)
        {
            failures.Add(
                $"BreachNotificationOptions.NotificationDeadlineHours must be positive. "
                + $"Current value: {options.NotificationDeadlineHours}.");
        }

        // Validate AlertAtHoursRemaining values
        if (options.AlertAtHoursRemaining is { Length: > 0 })
        {
            foreach (var threshold in options.AlertAtHoursRemaining)
            {
                if (threshold <= 0)
                {
                    failures.Add(
                        $"BreachNotificationOptions.AlertAtHoursRemaining contains a non-positive value '{threshold}'. "
                        + "All alert thresholds must be positive.");
                    break;
                }

                if (threshold >= options.NotificationDeadlineHours)
                {
                    failures.Add(
                        $"BreachNotificationOptions.AlertAtHoursRemaining contains a value '{threshold}' "
                        + $"that is >= NotificationDeadlineHours ({options.NotificationDeadlineHours}). "
                        + "Alert thresholds must be less than the notification deadline.");
                    break;
                }
            }
        }

        // Validate DeadlineCheckInterval is positive
        if (options.DeadlineCheckInterval <= TimeSpan.Zero)
        {
            failures.Add(
                $"BreachNotificationOptions.DeadlineCheckInterval must be positive. "
                + $"Current value: {options.DeadlineCheckInterval}.");
        }

        // Validate SubjectNotificationSeverityThreshold is a defined enum value
        if (!Enum.IsDefined(options.SubjectNotificationSeverityThreshold))
        {
            failures.Add(
                $"BreachNotificationOptions.SubjectNotificationSeverityThreshold has an invalid value "
                + $"'{(int)options.SubjectNotificationSeverityThreshold}'. "
                + "Valid values are Low (0), Medium (1), High (2), or Critical (3).");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
