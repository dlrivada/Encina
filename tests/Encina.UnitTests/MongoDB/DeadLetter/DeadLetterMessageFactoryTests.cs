using Encina.Messaging.DeadLetter;
using Encina.MongoDB.DeadLetter;

namespace Encina.UnitTests.MongoDB.DeadLetter;

public sealed class DeadLetterMessageFactoryTests
{
    private readonly DeadLetterMessageFactory _factory = new();

    [Fact]
    public void Create_NullData_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => _factory.Create(null!));

        ex.ParamName.ShouldBe("data");
    }

    [Fact]
    public void Create_MapsEveryField()
    {
        var id = Guid.NewGuid();
        var failed = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var deadLettered = failed.AddMinutes(5);
        var expires = deadLettered.AddDays(30);
        var data = new DeadLetterData(
            id, "Orders.Create", "{\"a\":1}", "orders.failed", "Outbox", "src-1", 4,
            failed, deadLettered, expires, "corr-1", "System.Exception", "stack", "tenant-1");

        var result = _factory.Create(data);

        var message = result.ShouldBeOfType<DeadLetterMessage>();
        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("Orders.Create");
        message.RequestContent.ShouldBe("{\"a\":1}");
        message.ErrorCode.ShouldBe("orders.failed");
        message.SourcePattern.ShouldBe("Outbox");
        message.SourceMessageId.ShouldBe("src-1");
        message.TotalRetryAttempts.ShouldBe(4);
        message.FirstFailedAtUtc.ShouldBe(failed);
        message.DeadLetteredAtUtc.ShouldBe(deadLettered);
        message.ExpiresAtUtc.ShouldBe(expires);
        message.CorrelationId.ShouldBe("corr-1");
        message.ExceptionType.ShouldBe("System.Exception");
        message.ExceptionStackTrace.ShouldBe("stack");
        message.TenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public void Create_WithOptionalFieldsOmitted_LeavesThemNullAndMessageNotReplayed()
    {
        var now = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var data = new DeadLetterData(
            Guid.NewGuid(), "T", "{}", "code", "Inbox", "src-2", 0, now, now, null);

        var message = _factory.Create(data).ShouldBeOfType<DeadLetterMessage>();

        message.ExpiresAtUtc.ShouldBeNull();
        message.CorrelationId.ShouldBeNull();
        message.ExceptionType.ShouldBeNull();
        message.ExceptionStackTrace.ShouldBeNull();
        message.TenantId.ShouldBeNull();
        message.ReplayClaimedAtUtc.ShouldBeNull();
        message.ReplayedAtUtc.ShouldBeNull();
        message.IsReplayed.ShouldBeFalse();
    }
}
