using Encina.Messaging.Sagas;
using Encina.OpenTelemetry.MessagingStores;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="InstrumentedSagaStore"/> to verify null parameter handling.
/// </summary>
public sealed class InstrumentedSagaStoreGuardTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedSagaStore(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_ValidInner_DoesNotThrow()
    {
        var inner = Substitute.For<ISagaStore>();
        Should.NotThrow(() => new InstrumentedSagaStore(inner));
    }
}
