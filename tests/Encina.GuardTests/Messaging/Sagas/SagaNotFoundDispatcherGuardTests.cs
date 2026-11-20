using Encina.Messaging.Sagas;
using Shouldly;

namespace Encina.GuardTests.Messaging.Sagas;

/// <summary>
/// Guard clause tests for <see cref="SagaNotFoundDispatcher"/>.
/// </summary>
public class SagaNotFoundDispatcherGuardTests
{
    private sealed record Probe;

    private static SagaNotFoundDispatcher CreateSut() => new(
        Substitute.For<IServiceProvider>(),
        NullLogger<SagaNotFoundDispatcher>.Instance);

    private static SagaNotFoundContext CreateContext() =>
        new(Guid.NewGuid(), "OrderSaga", typeof(Probe), "msg-1");

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new SagaNotFoundDispatcher(null!, NullLogger<SagaNotFoundDispatcher>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SagaNotFoundDispatcher(Substitute.For<IServiceProvider>(), null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullOptionalParameters_CreatesInstance()
    {
        var sut = new SagaNotFoundDispatcher(
            Substitute.For<IServiceProvider>(),
            NullLogger<SagaNotFoundDispatcher>.Instance,
            deadLetterCapture: null,
            timeProvider: null);

        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task DispatchAsync_NullMessage_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.DispatchAsync<Probe>(null!, CreateContext());

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("message");
    }

    [Fact]
    public async Task DispatchAsync_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.DispatchAsync(new Probe(), null!);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }
}
