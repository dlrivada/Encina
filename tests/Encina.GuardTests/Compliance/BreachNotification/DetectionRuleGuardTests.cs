using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection.Rules;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for breach detection rule constructors to verify null parameter handling.
/// </summary>
public class DetectionRuleGuardTests
{
    private readonly IOptions<BreachNotificationOptions> _options =
        Options.Create(new BreachNotificationOptions());

    #region UnauthorizedAccessRule

    [Fact]
    public void UnauthorizedAccessRule_NullOptions_Throws()
    {
        var act = () => new UnauthorizedAccessRule(null!, NullLogger<UnauthorizedAccessRule>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void UnauthorizedAccessRule_NullLogger_Throws()
    {
        var act = () => new UnauthorizedAccessRule(_options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region MassDataExfiltrationRule

    [Fact]
    public void MassDataExfiltrationRule_NullOptions_Throws()
    {
        var act = () => new MassDataExfiltrationRule(null!, NullLogger<MassDataExfiltrationRule>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void MassDataExfiltrationRule_NullLogger_Throws()
    {
        var act = () => new MassDataExfiltrationRule(_options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region PrivilegeEscalationRule

    [Fact]
    public void PrivilegeEscalationRule_NullLogger_Throws()
    {
        var act = () => new PrivilegeEscalationRule(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region AnomalousQueryPatternRule

    [Fact]
    public void AnomalousQueryPatternRule_NullOptions_Throws()
    {
        var act = () => new AnomalousQueryPatternRule(null!, NullLogger<AnomalousQueryPatternRule>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void AnomalousQueryPatternRule_NullLogger_Throws()
    {
        var act = () => new AnomalousQueryPatternRule(_options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
