using Encina.ADO.Oracle.Outbox;

namespace Encina.UnitTests.ADO.Oracle.Outbox;

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
