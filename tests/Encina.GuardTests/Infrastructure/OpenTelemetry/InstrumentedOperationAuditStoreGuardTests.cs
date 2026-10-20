using Encina.OpenTelemetry.Audit;
using Encina.Security.Audit;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="InstrumentedOperationAuditStore"/> to verify null parameter handling.
/// </summary>
public sealed class InstrumentedOperationAuditStoreGuardTests
{
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedOperationAuditStore(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_ValidInner_DoesNotThrow()
    {
        var inner = Substitute.For<IOperationAuditStore>();
        Should.NotThrow(() => new InstrumentedOperationAuditStore(inner));
    }
}
