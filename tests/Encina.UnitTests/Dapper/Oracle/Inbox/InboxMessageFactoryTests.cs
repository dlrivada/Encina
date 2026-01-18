using Encina.Dapper.Oracle.Inbox;

namespace Encina.UnitTests.Dapper.Oracle.Inbox;

public sealed class InboxMessageFactoryTests
{
    private readonly InboxMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsInboxMessageWithCorrectProperties()
    {
        var result = _factory.Create("msg-1", "Type", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), null);
        result.ShouldNotBeNull();
        result.ShouldBeOfType<InboxMessage>();
    }
}
