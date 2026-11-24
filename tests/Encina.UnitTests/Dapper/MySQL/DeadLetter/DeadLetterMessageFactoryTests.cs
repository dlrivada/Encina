using Encina.Dapper.MySQL.DeadLetter;
using Encina.Messaging.DeadLetter;

namespace Encina.UnitTests.Dapper.MySQL.DeadLetter;

/// <summary>
/// Unit tests for the Dapper MySQL <see cref="DeadLetterMessageFactory"/> (#2879).
/// </summary>
public sealed class DeadLetterMessageFactoryTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private readonly DeadLetterMessageFactory _factory = new();

    [Fact]
    public void Create_MapsEveryDeadLetterDataFieldAndLeavesReplayStateEmpty()
    {
        var id = Guid.NewGuid();
        var data = new DeadLetterData(
            id, "T", "{}", "e", "outbox", "src", 4, Now.AddHours(-2), Now.AddHours(-1), Now.AddDays(7),
            CorrelationId: "corr", ExceptionType: "X", ExceptionStackTrace: "at X", TenantId: "tenant");

        var message = _factory.Create(data);

        message.ShouldBeOfType<DeadLetterMessage>();
        message.Id.ShouldBe(id);
        (message.RequestType, message.RequestContent, message.ErrorCode).ShouldBe(("T", "{}", "e"));
        (message.SourcePattern, message.SourceMessageId, message.TotalRetryAttempts).ShouldBe(("outbox", "src", 4));
        (message.CorrelationId, message.ExceptionType, message.ExceptionStackTrace).ShouldBe(("corr", "X", "at X"));
        message.TenantId.ShouldBe("tenant");
        message.FirstFailedAtUtc.ShouldBe(Now.AddHours(-2));
        message.DeadLetteredAtUtc.ShouldBe(Now.AddHours(-1));
        message.ExpiresAtUtc.ShouldBe(Now.AddDays(7));
        message.ReplayClaimedAtUtc.ShouldBeNull();
        message.ReplayedAtUtc.ShouldBeNull();
        message.ReplayResult.ShouldBeNull();
    }

    [Fact]
    public void Create_NullData_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => _factory.Create(null!));

        ex.ParamName.ShouldBe("data");
    }
}
