using Encina.MongoDB;

namespace Encina.UnitTests.MongoDB;

public sealed class MongoDbCollectionNamesTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var names = new MongoDbCollectionNames();

        names.Outbox.ShouldBe("outbox_messages");
        names.Inbox.ShouldBe("inbox_messages");
        names.Sagas.ShouldBe("saga_states");
        names.ScheduledMessages.ShouldBe("scheduled_messages");
        names.AuditLogs.ShouldBe("audit_logs");
    }

    [Fact]
    public void Properties_CanBeCustomized()
    {
        var names = new MongoDbCollectionNames
        {
            Outbox = "custom_outbox",
            Inbox = "custom_inbox",
            Sagas = "custom_sagas",
            ScheduledMessages = "custom_scheduled",
            AuditLogs = "custom_audit_logs"
        };

        names.Outbox.ShouldBe("custom_outbox");
        names.Inbox.ShouldBe("custom_inbox");
        names.Sagas.ShouldBe("custom_sagas");
        names.ScheduledMessages.ShouldBe("custom_scheduled");
        names.AuditLogs.ShouldBe("custom_audit_logs");
    }
}
