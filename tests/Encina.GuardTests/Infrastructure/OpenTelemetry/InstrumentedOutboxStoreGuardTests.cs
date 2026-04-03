using Encina.Messaging.Outbox;
using Encina.OpenTelemetry.MessagingStores;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="InstrumentedOutboxStore"/> to verify null parameter handling.
/// </summary>
public sealed class InstrumentedOutboxStoreGuardTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedOutboxStore(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_ValidInner_DoesNotThrow()
    {
        var inner = Substitute.For<IOutboxStore>();
        Should.NotThrow(() => new InstrumentedOutboxStore(inner));
    }
}
