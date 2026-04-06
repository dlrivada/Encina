using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.BreachNotification;

/// <summary>
/// Property-based tests for <see cref="BreachNotificationOptions"/>.
/// </summary>
public class BreachNotificationOptionsPropertyTests
{
    [Property(MaxTest = 50)]
    public bool NotificationDeadlineHours_WhenPositive_ShouldBePreserved(PositiveInt hours)
    {
        var options = new BreachNotificationOptions { NotificationDeadlineHours = hours.Get };
        return options.NotificationDeadlineHours == hours.Get;
    }

    [Property(MaxTest = 50)]
    public bool EnableDeadlineMonitoring_ShouldBePreserved(bool enabled)
    {
        var options = new BreachNotificationOptions { EnableDeadlineMonitoring = enabled };
        return options.EnableDeadlineMonitoring == enabled;
    }
}
