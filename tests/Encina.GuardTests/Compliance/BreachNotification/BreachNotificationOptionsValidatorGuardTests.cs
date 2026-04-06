using Encina.Compliance.BreachNotification;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="BreachNotificationOptionsValidator"/> Validate method null parameter handling.
/// </summary>
public sealed class BreachNotificationOptionsValidatorGuardTests
{
    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var validator = new BreachNotificationOptionsValidator();

        var act = () => validator.Validate(null, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }
}
