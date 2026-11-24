using Encina.Dapper.SqlServer.DeadLetter;
using Encina.Messaging.DeadLetter;

namespace Encina.UnitTests.Dapper.SqlServer.DeadLetter;

/// <summary>
/// Unit tests for the Dapper SQL Server <see cref="DeadLetterMessageFactory"/> (#2879).
/// </summary>
public sealed class DeadLetterMessageFactoryTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private readonly DeadLetterMessageFactory _factory = new();

    [Fact]
    public void Create_MapsEveryDeadLetterDataField()
    {
        var id = Guid.NewGuid();
        var data = new DeadLetterData(
            id,
            "Orders.PlaceOrder",
            "{}",
            "orders.failed",
            "outbox",
            "src-1",
            4,
            Now.AddHours(-2),
            Now.AddHours(-1),
            Now.AddDays(7),
            CorrelationId: "corr-1",
            ExceptionType: "System.InvalidOperationException",
            ExceptionStackTrace: "at X",
            TenantId: "tenant-1");

        var message = _factory.Create(data);

        message.ShouldBeOfType<DeadLetterMessage>();
        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("Orders.PlaceOrder");
        message.RequestContent.ShouldBe("{}");
        message.ErrorCode.ShouldBe("orders.failed");
        message.SourcePattern.ShouldBe("outbox");
        message.SourceMessageId.ShouldBe("src-1");
        message.TotalRetryAttempts.ShouldBe(4);
        message.FirstFailedAtUtc.ShouldBe(Now.AddHours(-2));
        message.DeadLetteredAtUtc.ShouldBe(Now.AddHours(-1));
        message.ExpiresAtUtc.ShouldBe(Now.AddDays(7));
        message.CorrelationId.ShouldBe("corr-1");
        message.ExceptionType.ShouldBe("System.InvalidOperationException");
        message.ExceptionStackTrace.ShouldBe("at X");
        message.TenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public void Create_NewMessageHasNoReplayState()
    {
        var data = new DeadLetterData(Guid.NewGuid(), "T", "{}", "e", "outbox", "s", 1, Now, Now, null);

        var message = _factory.Create(data);

        message.ReplayClaimedAtUtc.ShouldBeNull();
        message.ReplayedAtUtc.ShouldBeNull();
        message.ReplayResult.ShouldBeNull();
        message.ExpiresAtUtc.ShouldBeNull();
        message.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_NullData_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => _factory.Create(null!));

        ex.ParamName.ShouldBe("data");
    }
}
