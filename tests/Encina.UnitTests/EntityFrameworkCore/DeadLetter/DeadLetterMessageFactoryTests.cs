using Encina.EntityFrameworkCore.DeadLetter;
using Encina.Messaging.DeadLetter;

namespace Encina.UnitTests.EntityFrameworkCore.DeadLetter;

/// <summary>
/// Unit tests for <see cref="DeadLetterMessageFactory"/>. The factory's argument check is covered by
/// <c>EntityFrameworkDeadLetterStoreGuardTests</c> and the factory guard tests in <c>Encina.GuardTests</c>.
/// </summary>
public sealed class DeadLetterMessageFactoryTests
{
    private static readonly DateTime Instant = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_FullData_MapsEveryField()
    {
        var id = Guid.NewGuid();
        var data = new DeadLetterData(
            Id: id,
            RequestType: "Orders.PlaceOrder",
            RequestContent: "{\"id\":1}",
            ErrorCode: "orders.failed",
            SourcePattern: "Inbox",
            SourceMessageId: "src-1",
            TotalRetryAttempts: 4,
            FirstFailedAtUtc: Instant.AddHours(-1),
            DeadLetteredAtUtc: Instant,
            ExpiresAtUtc: Instant.AddDays(3),
            CorrelationId: "corr-1",
            ExceptionType: "System.TimeoutException",
            ExceptionStackTrace: "at Bar",
            TenantId: "tenant-1");

        var message = new DeadLetterMessageFactory().Create(data).ShouldBeOfType<DeadLetterMessage>();

        message.Id.ShouldBe(id);
        message.RequestType.ShouldBe("Orders.PlaceOrder");
        message.RequestContent.ShouldBe("{\"id\":1}");
        message.ErrorCode.ShouldBe("orders.failed");
        message.SourcePattern.ShouldBe("Inbox");
        message.SourceMessageId.ShouldBe("src-1");
        message.TotalRetryAttempts.ShouldBe(4);
        message.FirstFailedAtUtc.ShouldBe(Instant.AddHours(-1));
        message.DeadLetteredAtUtc.ShouldBe(Instant);
        message.ExpiresAtUtc.ShouldBe(Instant.AddDays(3));
        message.CorrelationId.ShouldBe("corr-1");
        message.ExceptionType.ShouldBe("System.TimeoutException");
        message.ExceptionStackTrace.ShouldBe("at Bar");
        message.TenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public void Create_MinimalData_LeavesOptionalFieldsAndReplayStateEmpty()
    {
        var data = new DeadLetterData(
            Guid.NewGuid(), "T", "{}", "e", "Outbox", "s", 0, Instant, Instant, null);

        var message = new DeadLetterMessageFactory().Create(data).ShouldBeOfType<DeadLetterMessage>();

        message.CorrelationId.ShouldBeNull();
        message.ExceptionType.ShouldBeNull();
        message.ExceptionStackTrace.ShouldBeNull();
        message.TenantId.ShouldBeNull();
        message.ExpiresAtUtc.ShouldBeNull();
        message.ReplayClaimedAtUtc.ShouldBeNull();
        message.ReplayedAtUtc.ShouldBeNull();
        message.ReplayResult.ShouldBeNull();
        message.IsReplayed.ShouldBeFalse();
    }

    [Fact]
    public void Create_CalledTwice_ReturnsDistinctInstances()
    {
        var factory = new DeadLetterMessageFactory();
        var data = new DeadLetterData(
            Guid.NewGuid(), "T", "{}", "e", "Outbox", "s", 0, Instant, Instant, null);

        factory.Create(data).ShouldNotBeSameAs(factory.Create(data));
    }
}
