using Encina.Testing.Time;

namespace Encina.GuardTests.Testing.Time;

public sealed class FakeTimeProviderGuardTests
{
    [Fact]
    public void CreateTimer_NullCallback_ThrowsArgumentNullException()
    {
        var provider = new FakeTimeProvider();

        Should.Throw<ArgumentNullException>(() =>
            provider.CreateTimer(null!, null, TimeSpan.FromSeconds(1), TimeSpan.Zero));
    }
}
