using Encina.Compliance.ProcessorAgreements.Notifications;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests that exercise notification record instantiation for ProcessorAgreements.
/// Creates instances of all 7 notification types and verifies properties are preserved.
/// </summary>
public sealed class ProcessorAgreementNotificationGuardTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    #region ProcessorRegisteredNotification

    [Fact]
    public void ProcessorRegisteredNotification_AllProperties_PreservesValues()
    {
        var notification = new ProcessorRegisteredNotification(
            ProcessorId: "proc-001",
            ProcessorName: "Stripe",
            OccurredAtUtc: Now);

        notification.ProcessorId.ShouldBe("proc-001");
        notification.ProcessorName.ShouldBe("Stripe");
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region DPASignedNotification

    [Fact]
    public void DPASignedNotification_AllProperties_PreservesValues()
    {
        var signed = Now.AddHours(-1);

        var notification = new DPASignedNotification(
            ProcessorId: "proc-002",
            DPAId: "dpa-002",
            ProcessorName: "AWS",
            SignedAtUtc: signed,
            OccurredAtUtc: Now);

        notification.ProcessorId.ShouldBe("proc-002");
        notification.DPAId.ShouldBe("dpa-002");
        notification.ProcessorName.ShouldBe("AWS");
        notification.SignedAtUtc.ShouldBe(signed);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region DPAExpiringNotification

    [Fact]
    public void DPAExpiringNotification_AllProperties_PreservesValues()
    {
        var expires = Now.AddDays(15);

        var notification = new DPAExpiringNotification(
            ProcessorId: "proc-003",
            DPAId: "dpa-003",
            ProcessorName: "Azure",
            ExpiresAtUtc: expires,
            DaysUntilExpiration: 15,
            OccurredAtUtc: Now);

        notification.ProcessorId.ShouldBe("proc-003");
        notification.DPAId.ShouldBe("dpa-003");
        notification.ProcessorName.ShouldBe("Azure");
        notification.ExpiresAtUtc.ShouldBe(expires);
        notification.DaysUntilExpiration.ShouldBe(15);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region DPAExpiredNotification

    [Fact]
    public void DPAExpiredNotification_AllProperties_PreservesValues()
    {
        var expired = Now.AddDays(-2);

        var notification = new DPAExpiredNotification(
            ProcessorId: "proc-004",
            DPAId: "dpa-004",
            ProcessorName: "GCP",
            ExpiredAtUtc: expired,
            OccurredAtUtc: Now);

        notification.ProcessorId.ShouldBe("proc-004");
        notification.DPAId.ShouldBe("dpa-004");
        notification.ProcessorName.ShouldBe("GCP");
        notification.ExpiredAtUtc.ShouldBe(expired);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region DPATerminatedNotification

    [Fact]
    public void DPATerminatedNotification_AllProperties_PreservesValues()
    {
        var notification = new DPATerminatedNotification(
            ProcessorId: "proc-005",
            DPAId: "dpa-005",
            ProcessorName: "Sendgrid",
            OccurredAtUtc: Now);

        notification.ProcessorId.ShouldBe("proc-005");
        notification.DPAId.ShouldBe("dpa-005");
        notification.ProcessorName.ShouldBe("Sendgrid");
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region SubProcessorAddedNotification

    [Fact]
    public void SubProcessorAddedNotification_AllProperties_PreservesValues()
    {
        var notification = new SubProcessorAddedNotification(
            ProcessorId: "proc-006",
            SubProcessorId: "sub-006",
            Depth: 1,
            OccurredAtUtc: Now);

        notification.ProcessorId.ShouldBe("proc-006");
        notification.SubProcessorId.ShouldBe("sub-006");
        notification.Depth.ShouldBe(1);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void SubProcessorAddedNotification_DeepHierarchy_PreservesDepth()
    {
        var notification = new SubProcessorAddedNotification(
            ProcessorId: "proc-007",
            SubProcessorId: "sub-007",
            Depth: 3,
            OccurredAtUtc: Now);

        notification.Depth.ShouldBe(3);
    }

    #endregion

    #region SubProcessorRemovedNotification

    [Fact]
    public void SubProcessorRemovedNotification_AllProperties_PreservesValues()
    {
        var notification = new SubProcessorRemovedNotification(
            ProcessorId: "proc-008",
            SubProcessorId: "sub-008",
            OccurredAtUtc: Now);

        notification.ProcessorId.ShouldBe("proc-008");
        notification.SubProcessorId.ShouldBe("sub-008");
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion
}
