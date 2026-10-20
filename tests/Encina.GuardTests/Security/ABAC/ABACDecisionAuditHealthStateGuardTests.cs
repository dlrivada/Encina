using Encina.Security.ABAC.DecisionAudit;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard tests for <see cref="ABACDecisionAuditHealthState"/> (#751 Phase 5).
/// </summary>
public sealed class ABACDecisionAuditHealthStateGuardTests
{
    [Fact]
    public void Constructor_NullTimeProvider_Throws() =>
        Should.Throw<ArgumentNullException>(() => new ABACDecisionAuditHealthState(null!))
            .ParamName.ShouldBe("timeProvider");
}
