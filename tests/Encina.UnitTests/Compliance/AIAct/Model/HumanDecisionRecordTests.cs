using Encina.Compliance.AIAct.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.AIAct.Model;

/// <summary>
/// Unit tests for <see cref="HumanDecisionRecord"/>.
/// </summary>
public class HumanDecisionRecordTests
{
    [Fact]
    public void OptionalProperties_ShouldDefaultToNull()
    {
        var record = new HumanDecisionRecord
        {
            DecisionId = Guid.NewGuid(),
            SystemId = "sys-1",
            ReviewerId = "reviewer-1",
            ReviewedAtUtc = DateTimeOffset.UtcNow,
            Decision = "approved",
            Rationale = "All checks passed"
        };

        record.RequestTypeName.Should().BeNull();
        record.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void RecordEquality_SameValues_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;

        var a = new HumanDecisionRecord
        {
            DecisionId = id,
            SystemId = "sys-1",
            ReviewerId = "reviewer-1",
            ReviewedAtUtc = time,
            Decision = "approved",
            Rationale = "OK"
        };
        var b = new HumanDecisionRecord
        {
            DecisionId = id,
            SystemId = "sys-1",
            ReviewerId = "reviewer-1",
            ReviewedAtUtc = time,
            Decision = "approved",
            Rationale = "OK"
        };

        a.Should().Be(b);
    }
}
