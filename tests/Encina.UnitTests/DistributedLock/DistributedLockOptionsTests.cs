using Encina.DistributedLock;

namespace Encina.UnitTests.DistributedLock;

public sealed class DistributedLockOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new DistributedLockOptions();

        options.KeyPrefix.ShouldBeEmpty();
        options.DefaultExpiry.ShouldBe(TimeSpan.FromSeconds(30));
        options.DefaultWait.ShouldBe(TimeSpan.FromSeconds(10));
        options.DefaultRetry.ShouldBe(TimeSpan.FromMilliseconds(100));
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new DistributedLockOptions
        {
            KeyPrefix = "myapp",
            DefaultExpiry = TimeSpan.FromMinutes(1),
            DefaultWait = TimeSpan.FromSeconds(30),
            DefaultRetry = TimeSpan.FromMilliseconds(200)
        };

        options.KeyPrefix.ShouldBe("myapp");
        options.DefaultExpiry.ShouldBe(TimeSpan.FromMinutes(1));
        options.DefaultWait.ShouldBe(TimeSpan.FromSeconds(30));
        options.DefaultRetry.ShouldBe(TimeSpan.FromMilliseconds(200));
    }
}
