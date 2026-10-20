using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DSRRequestAggregate"/> to verify null and invalid parameter handling.
/// </summary>
public class DSRRequestAggregateGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region Submit Guard Tests

    [Fact]
    public void Submit_NullSubjectId_ThrowsArgumentException()
    {
        var act = () => DSRRequestAggregate.Submit(
            Guid.NewGuid(), null!, DataSubjectRight.Access, Now);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public void Submit_EmptySubjectId_ThrowsArgumentException()
    {
        var act = () => DSRRequestAggregate.Submit(
            Guid.NewGuid(), "", DataSubjectRight.Access, Now);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Submit_WhitespaceSubjectId_ThrowsArgumentException()
    {
        var act = () => DSRRequestAggregate.Submit(
            Guid.NewGuid(), "   ", DataSubjectRight.Access, Now);

        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Verify Guard Tests

    [Fact]
    public void Verify_NullVerifiedBy_ThrowsArgumentException()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

        var act = () => aggregate.Verify(null!, Now.AddHours(1));

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("verifiedBy");
    }

    [Fact]
    public void Verify_EmptyVerifiedBy_ThrowsArgumentException()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

        var act = () => aggregate.Verify("", Now.AddHours(1));

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Verify_WhitespaceVerifiedBy_ThrowsArgumentException()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

        var act = () => aggregate.Verify("   ", Now.AddHours(1));

        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Deny Guard Tests

    [Fact]
    public void Deny_NullRejectionExplanation_ThrowsArgumentException()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

        var act = () => aggregate.Deny(DSRRejectionReason.Other, null!, Now.AddHours(1));

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("rejectionExplanation");
    }

    [Fact]
    public void Deny_EmptyRejectionExplanation_ThrowsArgumentException()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

        var act = () => aggregate.Deny(DSRRejectionReason.Other, "", Now.AddHours(1));

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Deny_UndefinedRejectionReason_ThrowsArgumentOutOfRangeException()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

        var act = () => aggregate.Deny((DSRRejectionReason)0, "explanation", Now.AddHours(1));

        var ex = Should.Throw<ArgumentOutOfRangeException>(act);
        ex.ParamName.ShouldBe("rejectionReason");
    }

    #endregion

    #region Extend Guard Tests

    [Fact]
    public void Extend_NullExtensionReason_ThrowsArgumentException()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

        var act = () => aggregate.Extend(null!, Now.AddDays(20));

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("extensionReason");
    }

    [Fact]
    public void Extend_EmptyExtensionReason_ThrowsArgumentException()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

        var act = () => aggregate.Extend("", Now.AddDays(20));

        Should.Throw<ArgumentException>(act);
    }

    #endregion
}
