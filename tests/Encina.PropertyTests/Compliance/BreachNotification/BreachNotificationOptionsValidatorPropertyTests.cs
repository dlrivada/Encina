using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Compliance.BreachNotification;

/// <summary>
/// Property-based tests for <see cref="BreachNotificationOptionsValidator"/> verifying
/// that validation invariants hold across random inputs.
/// </summary>
public class BreachNotificationOptionsValidatorPropertyTests
{
    private readonly BreachNotificationOptionsValidator _validator = new();

    [Property(MaxTest = 50)]
    public bool ValidOptions_AlwaysSucceeds(PositiveInt deadlineHours)
    {
        var options = new BreachNotificationOptions
        {
            EnforcementMode = BreachDetectionEnforcementMode.Block,
            NotificationDeadlineHours = deadlineHours.Get,
            DeadlineCheckInterval = TimeSpan.FromMinutes(15),
            SubjectNotificationSeverityThreshold = BreachSeverity.High,
            AlertAtHoursRemaining = deadlineHours.Get > 1
                ? [deadlineHours.Get - 1]
                : []
        };

        var result = _validator.Validate(null, options);
        return result.Succeeded;
    }

    [Property(MaxTest = 50)]
    public bool NegativeDeadlineHours_AlwaysFails(NegativeInt deadlineHours)
    {
        var options = new BreachNotificationOptions
        {
            NotificationDeadlineHours = deadlineHours.Get
        };

        var result = _validator.Validate(null, options);
        return result.Failed;
    }

    [Property(MaxTest = 50)]
    public bool ZeroDeadlineCheckInterval_AlwaysFails(PositiveInt deadlineHours)
    {
        var options = new BreachNotificationOptions
        {
            NotificationDeadlineHours = deadlineHours.Get,
            DeadlineCheckInterval = TimeSpan.Zero
        };

        var result = _validator.Validate(null, options);
        return result.Failed;
    }
}
