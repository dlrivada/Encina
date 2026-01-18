using Encina.Dapper.Oracle.Scheduling;

namespace Encina.UnitTests.Dapper.Oracle.Scheduling;

public sealed class ScheduledMessageFactoryTests
{
    private readonly ScheduledMessageFactory _factory = new();

    [Fact]
    public void Create_ReturnsScheduledMessageWithCorrectProperties()
    {
        var id = Guid.NewGuid();
        var result = _factory.Create(id, "Type", "{}", DateTime.UtcNow.AddHours(1), DateTime.UtcNow, false, null);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
        result.ShouldBeOfType<ScheduledMessage>();
    }
}
