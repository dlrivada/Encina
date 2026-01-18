using Encina.ADO.Oracle.Sagas;

namespace Encina.UnitTests.ADO.Oracle.Sagas;

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
