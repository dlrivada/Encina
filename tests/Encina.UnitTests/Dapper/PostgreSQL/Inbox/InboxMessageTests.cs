using Encina.Dapper.PostgreSQL.Inbox;

namespace Encina.UnitTests.Dapper.PostgreSQL.Inbox;

/// <summary>
/// Unit tests for <see cref="InboxMessage"/>.
/// </summary>
public sealed class InboxMessageTests
{
    [Fact]
    public void IsProcessed_WhenProcessedAndNoError_ReturnsTrue()
    {
        var message = new InboxMessage { ProcessedAtUtc = DateTime.UtcNow, ErrorMessage = null };
        message.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public void IsProcessed_WhenNotProcessed_ReturnsFalse()
    {
        var message = new InboxMessage { ProcessedAtUtc = null };
        message.IsProcessed.ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresInPast_ReturnsTrue()
    {
        var message = new InboxMessage { ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10) };
        message.IsExpired().ShouldBeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresInFuture_ReturnsFalse()
    {
        var message = new InboxMessage { ExpiresAtUtc = DateTime.UtcNow.AddHours(1) };
        message.IsExpired().ShouldBeFalse();
    }
}
