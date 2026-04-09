#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Notifications;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for cross-border transfer notification event records
/// verifying correct instantiation of all notification types.
/// </summary>
public class CrossBorderTransferNotificationGuardTests
{
    private static readonly Guid TestId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    #region TransferExpiringNotification

    [Fact]
    public void TransferExpiringNotification_CanBeInstantiated()
    {
        var expiresAt = Now.AddDays(15);
        var notification = new TransferExpiringNotification(
            TestId, "DE", "US", "personal-data", expiresAt, 15, Now);

        notification.TransferId.ShouldBe(TestId);
        notification.SourceCountryCode.ShouldBe("DE");
        notification.DestinationCountryCode.ShouldBe("US");
        notification.DataCategory.ShouldBe("personal-data");
        notification.ExpiresAtUtc.ShouldBe(expiresAt);
        notification.DaysUntilExpiration.ShouldBe(15);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void TransferExpiringNotification_ZeroDays_CanBeInstantiated()
    {
        var notification = new TransferExpiringNotification(
            TestId, "DE", "US", "personal-data", Now, 0, Now);

        notification.DaysUntilExpiration.ShouldBe(0);
    }

    #endregion

    #region TransferExpiredNotification

    [Fact]
    public void TransferExpiredNotification_CanBeInstantiated()
    {
        var expiredAt = Now.AddDays(-1);
        var notification = new TransferExpiredNotification(
            TestId, "DE", "US", "personal-data", expiredAt, Now);

        notification.TransferId.ShouldBe(TestId);
        notification.SourceCountryCode.ShouldBe("DE");
        notification.DestinationCountryCode.ShouldBe("US");
        notification.DataCategory.ShouldBe("personal-data");
        notification.ExpiredAtUtc.ShouldBe(expiredAt);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region TIAExpiringNotification

    [Fact]
    public void TIAExpiringNotification_CanBeInstantiated()
    {
        var expiresAt = Now.AddDays(20);
        var notification = new TIAExpiringNotification(
            TestId, "DE", "US", "personal-data", expiresAt, 20, Now);

        notification.TIAId.ShouldBe(TestId);
        notification.SourceCountryCode.ShouldBe("DE");
        notification.DestinationCountryCode.ShouldBe("US");
        notification.DataCategory.ShouldBe("personal-data");
        notification.ExpiresAtUtc.ShouldBe(expiresAt);
        notification.DaysUntilExpiration.ShouldBe(20);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region TIAExpiredNotification

    [Fact]
    public void TIAExpiredNotification_CanBeInstantiated()
    {
        var expiredAt = Now.AddDays(-5);
        var notification = new TIAExpiredNotification(
            TestId, "DE", "US", "personal-data", expiredAt, Now);

        notification.TIAId.ShouldBe(TestId);
        notification.SourceCountryCode.ShouldBe("DE");
        notification.DestinationCountryCode.ShouldBe("US");
        notification.DataCategory.ShouldBe("personal-data");
        notification.ExpiredAtUtc.ShouldBe(expiredAt);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region SCCAgreementExpiringNotification

    [Fact]
    public void SCCAgreementExpiringNotification_CanBeInstantiated()
    {
        var expiresAt = Now.AddDays(10);
        var notification = new SCCAgreementExpiringNotification(
            TestId, "proc-1", "ControllerToProcessor", expiresAt, 10, Now);

        notification.AgreementId.ShouldBe(TestId);
        notification.ProcessorId.ShouldBe("proc-1");
        notification.Module.ShouldBe("ControllerToProcessor");
        notification.ExpiresAtUtc.ShouldBe(expiresAt);
        notification.DaysUntilExpiration.ShouldBe(10);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region SCCAgreementExpiredNotification

    [Fact]
    public void SCCAgreementExpiredNotification_CanBeInstantiated()
    {
        var expiredAt = Now.AddDays(-3);
        var notification = new SCCAgreementExpiredNotification(
            TestId, "proc-1", "ControllerToProcessor", expiredAt, Now);

        notification.AgreementId.ShouldBe(TestId);
        notification.ProcessorId.ShouldBe("proc-1");
        notification.Module.ShouldBe("ControllerToProcessor");
        notification.ExpiredAtUtc.ShouldBe(expiredAt);
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion
}
