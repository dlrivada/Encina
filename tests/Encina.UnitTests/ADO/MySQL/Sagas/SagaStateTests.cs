using Encina.ADO.MySQL.Sagas;

namespace Encina.UnitTests.ADO.MySQL.Sagas;

public sealed class SagaStateTests
{
    [Fact]
    public void Properties_SetAndGetCorrectly()
    {
        var sagaId = Guid.NewGuid();

        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "InProgress"
        };

        saga.SagaId.ShouldBe(sagaId);
        saga.SagaType.ShouldBe("OrderSaga");
    }
}
