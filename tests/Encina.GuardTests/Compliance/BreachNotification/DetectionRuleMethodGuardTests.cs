using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection.Rules;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for detection rule EvaluateAsync methods to verify null parameter handling.
/// </summary>
public class DetectionRuleMethodGuardTests
{
    private readonly IOptions<BreachNotificationOptions> _options =
        Options.Create(new BreachNotificationOptions());

    #region UnauthorizedAccessRule.EvaluateAsync

    [Fact]
    public async Task UnauthorizedAccessRule_EvaluateAsync_NullSecurityEvent_ThrowsArgumentNullException()
    {
        var rule = new UnauthorizedAccessRule(_options, NullLogger<UnauthorizedAccessRule>.Instance);

        var act = async () => await rule.EvaluateAsync(null!);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("securityEvent");
    }

    #endregion

    #region MassDataExfiltrationRule.EvaluateAsync

    [Fact]
    public async Task MassDataExfiltrationRule_EvaluateAsync_NullSecurityEvent_ThrowsArgumentNullException()
    {
        var rule = new MassDataExfiltrationRule(_options, NullLogger<MassDataExfiltrationRule>.Instance);

        var act = async () => await rule.EvaluateAsync(null!);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("securityEvent");
    }

    #endregion

    #region PrivilegeEscalationRule.EvaluateAsync

    [Fact]
    public async Task PrivilegeEscalationRule_EvaluateAsync_NullSecurityEvent_ThrowsArgumentNullException()
    {
        var rule = new PrivilegeEscalationRule(NullLogger<PrivilegeEscalationRule>.Instance);

        var act = async () => await rule.EvaluateAsync(null!);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("securityEvent");
    }

    #endregion

    #region AnomalousQueryPatternRule.EvaluateAsync

    [Fact]
    public async Task AnomalousQueryPatternRule_EvaluateAsync_NullSecurityEvent_ThrowsArgumentNullException()
    {
        var rule = new AnomalousQueryPatternRule(_options, NullLogger<AnomalousQueryPatternRule>.Instance);

        var act = async () => await rule.EvaluateAsync(null!);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("securityEvent");
    }

    #endregion
}
