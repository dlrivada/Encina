using Encina.ADO.PostgreSQL.DeadLetter;
using Encina.Messaging.DeadLetter;

namespace Encina.UnitTests.ADO.PostgreSQL.DeadLetter;

public sealed class DeadLetterMessageFactoryTests
{
    private static readonly DateTime Instant = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly DeadLetterMessageFactory _factory = new();

    [Fact]
    public void Create_CopiesEveryDataField()
    {
        var data = new DeadLetterData(
            Id: Guid.NewGuid(),
            RequestType: "OrderCommand",
            RequestContent: "{\"orderId\":1}",
            ErrorCode: "order.failed",
            SourcePattern: "Outbox",
            SourceMessageId: "src-1",
            TotalRetryAttempts: 4,
            FirstFailedAtUtc: Instant.AddHours(-2),
            DeadLetteredAtUtc: Instant.AddHours(-1),
            ExpiresAtUtc: Instant.AddDays(7),
            CorrelationId: "corr-1",
            ExceptionType: "System.TimeoutException",
            ExceptionStackTrace: "at Handler",
            TenantId: "tenant-1");

        var message = _factory.Create(data);

        message.ShouldBeOfType<DeadLetterMessage>();
        message.Id.ShouldBe(data.Id);
        message.RequestType.ShouldBe(data.RequestType);
        message.RequestContent.ShouldBe(data.RequestContent);
        message.ErrorCode.ShouldBe(data.ErrorCode);
        message.SourcePattern.ShouldBe(data.SourcePattern);
        message.SourceMessageId.ShouldBe(data.SourceMessageId);
        message.TotalRetryAttempts.ShouldBe(data.TotalRetryAttempts);
        message.FirstFailedAtUtc.ShouldBe(data.FirstFailedAtUtc);
        message.DeadLetteredAtUtc.ShouldBe(data.DeadLetteredAtUtc);
        message.ExpiresAtUtc.ShouldBe(data.ExpiresAtUtc);
        message.CorrelationId.ShouldBe(data.CorrelationId);
        message.ExceptionType.ShouldBe(data.ExceptionType);
        message.ExceptionStackTrace.ShouldBe(data.ExceptionStackTrace);
        message.TenantId.ShouldBe(data.TenantId);
    }

    [Fact]
    public void Create_StartsWithoutAReplayState()
    {
        var message = _factory.Create(new DeadLetterData(
            Guid.NewGuid(), "T", "{}", "e", "Outbox", "src", 0, Instant, Instant, null));

        message.ReplayClaimedAtUtc.ShouldBeNull();
        message.ReplayedAtUtc.ShouldBeNull();
        message.ReplayResult.ShouldBeNull();
        message.IsReplayed.ShouldBeFalse();
        message.ExpiresAtUtc.ShouldBeNull();
        message.TenantId.ShouldBeNull();
    }
}
