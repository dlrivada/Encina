using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.MessagingStores;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="InstrumentedScheduledMessageStore"/> to verify null parameter handling.
/// </summary>
public sealed class InstrumentedScheduledMessageStoreGuardTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedScheduledMessageStore(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_ValidInner_DoesNotThrow()
    {
        var inner = Substitute.For<IScheduledMessageStore>();
        Should.NotThrow(() => new InstrumentedScheduledMessageStore(inner));
    }
}
