using Encina.Messaging.Inbox;
using Encina.OpenTelemetry.MessagingStores;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="InstrumentedInboxStore"/> to verify null parameter handling.
/// </summary>
public sealed class InstrumentedInboxStoreGuardTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedInboxStore(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_ValidInner_DoesNotThrow()
    {
        var inner = Substitute.For<IInboxStore>();
        Should.NotThrow(() => new InstrumentedInboxStore(inner));
    }
}
