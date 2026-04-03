using Encina.OpenTelemetry.Audit;
using Encina.Security.Audit;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="InstrumentedAuditStore"/> to verify null parameter handling.
/// </summary>
public sealed class InstrumentedAuditStoreGuardTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedAuditStore(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_ValidInner_DoesNotThrow()
    {
        var inner = Substitute.For<IAuditStore>();
        Should.NotThrow(() => new InstrumentedAuditStore(inner));
    }
}
