using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DSRRequestAggregate"/> state transition guards.
/// Each method validates preconditions on the aggregate status before proceeding.
/// </summary>
public class DSRRequestAggregateStateGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private static DSRRequestAggregate CreateReceived() =>
        DSRRequestAggregate.Submit(Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now);

    private static DSRRequestAggregate CreateVerified()
    {
        var agg = CreateReceived();
        agg.Verify("verifier", Now.AddHours(1));
        return agg;
    }

    private static DSRRequestAggregate CreateInProgress()
    {
        var agg = CreateVerified();
        agg.StartProcessing("processor", Now.AddHours(2));
        return agg;
    }

    private static DSRRequestAggregate CreateCompleted()
    {
        var agg = CreateInProgress();
        agg.Complete(Now.AddHours(3));
        return agg;
    }

    #region Verify State Guards

    [Fact]
    public void Verify_FromVerifiedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateVerified();
        var act = () => agg.Verify("another", Now.AddHours(2));
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Verify_FromCompletedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateCompleted();
        var act = () => agg.Verify("verifier", Now.AddHours(4));
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region StartProcessing State Guards

    [Fact]
    public void StartProcessing_FromReceivedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateReceived();
        var act = () => agg.StartProcessing("processor", Now.AddHours(1));
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void StartProcessing_FromInProgressStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateInProgress();
        var act = () => agg.StartProcessing("processor", Now.AddHours(3));
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void StartProcessing_FromCompletedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateCompleted();
        var act = () => agg.StartProcessing("processor", Now.AddHours(4));
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Complete State Guards

    [Fact]
    public void Complete_FromReceivedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateReceived();
        var act = () => agg.Complete(Now.AddHours(1));
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Complete_FromVerifiedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateVerified();
        var act = () => agg.Complete(Now.AddHours(2));
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Complete_FromCompletedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateCompleted();
        var act = () => agg.Complete(Now.AddHours(4));
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Deny State Guards

    [Fact]
    public void Deny_FromCompletedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateCompleted();
        var act = () => agg.Deny("reason", Now.AddHours(4));
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Extend State Guards

    [Fact]
    public void Extend_FromCompletedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateCompleted();
        var act = () => agg.Extend("complexity", Now.AddHours(4));
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Extend_FromExtendedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateReceived();
        agg.Extend("complex case", Now.AddDays(1));
        var act = () => agg.Extend("more complex", Now.AddDays(2));
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region Expire State Guards

    [Fact]
    public void Expire_FromCompletedStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateCompleted();
        var act = () => agg.Expire(Now.AddDays(31));
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_FromExpiredStatus_ThrowsInvalidOperationException()
    {
        var agg = CreateReceived();
        agg.Expire(Now.AddDays(31));
        var act = () => agg.Expire(Now.AddDays(32));
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion
}
