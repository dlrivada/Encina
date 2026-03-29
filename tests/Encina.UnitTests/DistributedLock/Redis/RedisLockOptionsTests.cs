using Encina.DistributedLock;
using Encina.DistributedLock.Redis;

namespace Encina.UnitTests.DistributedLock.Redis;

/// <summary>
/// Unit tests for <see cref="RedisLockOptions"/> defaults and configuration.
/// </summary>
public class RedisLockOptionsTests
{
    [Fact]
    public void Database_DefaultValue_ShouldBeZero()
    {
        // Arrange & Act
        var options = new RedisLockOptions();

        // Assert
        options.Database.ShouldBe(0);
    }

    [Fact]
    public void Database_CanBeSet()
    {
        // Arrange
        var options = new RedisLockOptions();

        // Act
        options.Database = 5;

        // Assert
        options.Database.ShouldBe(5);
    }

    [Fact]
    public void KeyPrefix_DefaultValue_ShouldBeEmpty()
    {
        // Arrange & Act
        var options = new RedisLockOptions();

        // Assert
        options.KeyPrefix.ShouldBe(string.Empty);
    }

    [Fact]
    public void KeyPrefix_CanBeSet()
    {
        // Arrange
        var options = new RedisLockOptions();

        // Act
        options.KeyPrefix = "myapp";

        // Assert
        options.KeyPrefix.ShouldBe("myapp");
    }

    [Fact]
    public void DefaultExpiry_DefaultValue_ShouldBe30Seconds()
    {
        // Arrange & Act
        var options = new RedisLockOptions();

        // Assert
        options.DefaultExpiry.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void DefaultWait_DefaultValue_ShouldBe10Seconds()
    {
        // Arrange & Act
        var options = new RedisLockOptions();

        // Assert
        options.DefaultWait.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void DefaultRetry_DefaultValue_ShouldBe100Milliseconds()
    {
        // Arrange & Act
        var options = new RedisLockOptions();

        // Assert
        options.DefaultRetry.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void ProviderHealthCheck_DefaultValue_ShouldBeEnabled()
    {
        // Arrange & Act
        var options = new RedisLockOptions();

        // Assert
        options.ProviderHealthCheck.ShouldNotBeNull();
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void ProviderHealthCheck_Timeout_DefaultValue_ShouldBe5Seconds()
    {
        // Arrange & Act
        var options = new RedisLockOptions();

        // Assert
        options.ProviderHealthCheck.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void InheritsFromDistributedLockOptions()
    {
        // Arrange & Act
        var options = new RedisLockOptions();

        // Assert — it should be an instance of DistributedLockOptions
        options.ShouldBeAssignableTo<DistributedLockOptions>();
    }
}
