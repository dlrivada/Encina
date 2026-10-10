using Encina.Messaging.Sagas;
using Shouldly;

namespace Encina.GuardTests.Messaging.Sagas;

/// <summary>
/// Guard clause tests for <see cref="SagaNotFoundContext"/>.
/// </summary>
public class SagaNotFoundContextGuardTests
{
    private sealed record Probe;

    [Fact]
    public void Constructor_NullSagaType_ThrowsArgumentNullException()
    {
        var act = () => new SagaNotFoundContext(Guid.NewGuid(), null!, typeof(Probe), "msg-1");

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("sagaType");
    }

    [Fact]
    public void Constructor_NullMessageType_ThrowsArgumentNullException()
    {
        var act = () => new SagaNotFoundContext(Guid.NewGuid(), "OrderSaga", null!, "msg-1");

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("messageType");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MoveToDeadLetterAsync_NullOrWhiteSpaceReason_ThrowsArgumentException(string? reason)
    {
        var context = new SagaNotFoundContext(Guid.NewGuid(), "OrderSaga", typeof(Probe), "msg-1");

        var act = async () => await context.MoveToDeadLetterAsync(reason!);

        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("reason");
    }
}
