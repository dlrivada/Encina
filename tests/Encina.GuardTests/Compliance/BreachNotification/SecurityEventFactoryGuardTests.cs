using Encina.Compliance.BreachNotification.Detection;
using Encina.Compliance.BreachNotification.Model;

using NSubstitute;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="SecurityEventFactory"/> Create method null parameter handling.
/// </summary>
public sealed class SecurityEventFactoryGuardTests
{
    private sealed record TestRequest(string Data);

    [Fact]
    public void Create_NullRequest_ThrowsArgumentNullException()
    {
        var context = Substitute.For<IRequestContext>();

        var act = () => SecurityEventFactory.Create<TestRequest>(
            null!, SecurityEventType.UnauthorizedAccess, "source", context, TimeProvider.System);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("request");
    }

    [Fact]
    public void Create_NullContext_ThrowsArgumentNullException()
    {
        var request = new TestRequest("data");

        var act = () => SecurityEventFactory.Create(
            request, SecurityEventType.UnauthorizedAccess, "source", null!, TimeProvider.System);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
    }

    [Fact]
    public void Create_NullTimeProvider_ThrowsArgumentNullException()
    {
        var request = new TestRequest("data");
        var context = Substitute.For<IRequestContext>();

        var act = () => SecurityEventFactory.Create(
            request, SecurityEventType.UnauthorizedAccess, "source", context, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }
}
