using Encina.Dapper.MySQL.Sagas;

namespace Encina.UnitTests.Dapper.MySQL.Sagas;

public sealed class SagaStateFactoryTests
{
    private readonly SagaStateFactory _factory = new();

    [Fact]
    public void Create_ReturnsSagaStateWithCorrectProperties()
    {
        var sagaId = Guid.NewGuid();
        var result = _factory.Create(sagaId, "Saga", "{}", "Running", 0, DateTime.UtcNow);
        result.ShouldNotBeNull();
        result.SagaId.ShouldBe(sagaId);
        result.ShouldBeOfType<SagaState>();
    }
}
