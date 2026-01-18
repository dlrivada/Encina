using Encina.Dapper.PostgreSQL.Outbox;

namespace Encina.UnitTests.Dapper.PostgreSQL.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxMessageFactory"/>.
/// </summary>
public sealed class OutboxMessageFactoryTests
{
    private readonly OutboxMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsOutboxMessageWithCorrectProperties()
    {
        var id = Guid.NewGuid();
        var result = _factory.Create(id, "Type", "{}", DateTime.UtcNow);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
        result.ShouldBeOfType<OutboxMessage>();
    }
}
