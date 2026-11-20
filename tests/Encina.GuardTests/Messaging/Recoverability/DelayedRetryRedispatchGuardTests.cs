using Encina.Messaging.Recoverability;
using Shouldly;

namespace Encina.GuardTests.Messaging.Recoverability;

/// <summary>
/// Guard clause tests for <see cref="DelayedRetryRedispatch"/>.
/// </summary>
public class DelayedRetryRedispatchGuardTests
{
    [Fact]
    public void Begin_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => { _ = DelayedRetryRedispatch.Begin(null!); };

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("request");
    }

    [Fact]
    public void Begin_ValidRequest_BindsMarkerUntilDisposed()
    {
        var request = new object();

        using (var scope = DelayedRetryRedispatch.Begin(request))
        {
            DelayedRetryRedispatch.For(request).ShouldBeSameAs(scope.Marker);
        }

        DelayedRetryRedispatch.For(request).ShouldBeNull();
    }
}
