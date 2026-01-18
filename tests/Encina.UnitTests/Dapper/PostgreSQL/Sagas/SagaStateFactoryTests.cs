using Encina.Dapper.PostgreSQL.Sagas;

namespace Encina.UnitTests.Dapper.PostgreSQL.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaStateFactory"/>.
/// </summary>
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
