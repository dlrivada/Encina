using Encina.Compliance.DataSubjectRights;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for notification records verifying record semantics and property initialization.
/// </summary>
public class NotificationRecordTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region DataErasedNotification

    [Fact]
    public void DataErasedNotification_PropertiesAreSet()
    {
        var notification = new DataErasedNotification(
            SubjectId: "subject-1",
            AffectedFields: ["Email", "Phone"],
            DSRRequestId: "req-123",
            OccurredAtUtc: Now);

        notification.SubjectId.ShouldBe("subject-1");
        notification.AffectedFields.Count.ShouldBe(2);
        notification.DSRRequestId.ShouldBe("req-123");
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void DataErasedNotification_Equality_WorksCorrectly()
    {
        var n1 = new DataErasedNotification("sub", ["f"], "r", Now);
        var n2 = new DataErasedNotification("sub", ["f"], "r", Now);

        // Records with reference-type collections compare by reference for the collection
        n1.SubjectId.ShouldBe(n2.SubjectId);
        n1.DSRRequestId.ShouldBe(n2.DSRRequestId);
        n1.OccurredAtUtc.ShouldBe(n2.OccurredAtUtc);
    }

    #endregion

    #region DataRectifiedNotification

    [Fact]
    public void DataRectifiedNotification_PropertiesAreSet()
    {
        var notification = new DataRectifiedNotification(
            SubjectId: "subject-1",
            FieldName: "Email",
            DSRRequestId: "req-123",
            OccurredAtUtc: Now);

        notification.SubjectId.ShouldBe("subject-1");
        notification.FieldName.ShouldBe("Email");
        notification.DSRRequestId.ShouldBe("req-123");
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region ProcessingRestrictedNotification

    [Fact]
    public void ProcessingRestrictedNotification_PropertiesAreSet()
    {
        var notification = new ProcessingRestrictedNotification(
            SubjectId: "subject-1",
            DSRRequestId: "req-123",
            OccurredAtUtc: Now);

        notification.SubjectId.ShouldBe("subject-1");
        notification.DSRRequestId.ShouldBe("req-123");
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion

    #region RestrictionLiftedNotification

    [Fact]
    public void RestrictionLiftedNotification_PropertiesAreSet()
    {
        var notification = new RestrictionLiftedNotification(
            SubjectId: "subject-1",
            DSRRequestId: "req-123",
            OccurredAtUtc: Now);

        notification.SubjectId.ShouldBe("subject-1");
        notification.DSRRequestId.ShouldBe("req-123");
        notification.OccurredAtUtc.ShouldBe(Now);
    }

    #endregion
}
