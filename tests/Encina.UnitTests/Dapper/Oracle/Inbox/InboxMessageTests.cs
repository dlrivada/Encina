using Encina.Dapper.Oracle.Inbox;

namespace Encina.UnitTests.Dapper.Oracle.Inbox;

public sealed class InboxMessageTests
{
    [Fact]
    public void IsProcessed_WhenProcessedAndNoError_ReturnsTrue()
    {
        var message = new InboxMessage { ProcessedAtUtc = DateTime.UtcNow, ErrorMessage = null };
        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresInPast_ReturnsTrue()
    {
        var message = new InboxMessage { ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10) };
        message.IsExpired().ShouldBeTrue();
    }
}
