using Encina.Messaging.Scheduling;
using Shouldly;

namespace Encina.GuardTests.Messaging.Scheduling;

/// <summary>
/// Guard clause tests for <see cref="CompiledExpressionScheduledMessageDispatcher"/> constructor.
/// </summary>
public sealed class CompiledExpressionScheduledMessageDispatcherGuardTests
{
    [Fact]
    public void Constructor_NullEncina_ThrowsArgumentNullException()
    {
        var act = () => new CompiledExpressionScheduledMessageDispatcher(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("encina");
    }

    [Fact]
    public void Constructor_ValidEncina_Succeeds()
    {
        var encina = Substitute.For<IEncina>();
        var act = () => new CompiledExpressionScheduledMessageDispatcher(encina);
        Should.NotThrow(act);
    }
}
