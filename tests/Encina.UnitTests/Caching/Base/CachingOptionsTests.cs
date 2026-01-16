using Encina.Caching;
namespace Encina.UnitTests.Caching.Base;

/// <summary>
/// Unit tests for <see cref="CachingOptions"/> and <see cref="CacheSerializerOptions"/>.
/// </summary>
public class CachingOptionsTests
{
    #region CachingOptions Default Values Tests

    [Fact]
    public void CachingOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new CachingOptions();

        // Assert
        options.EnableQueryCaching.ShouldBeTrue();
        options.EnableCacheInvalidation.ShouldBeTrue();
        options.EnableDistributedIdempotency.ShouldBeTrue();
        options.EnableDistributedLocks.ShouldBeTrue();
        options.EnablePubSubInvalidation.ShouldBeTrue();
        options.DefaultDuration.ShouldBe(TimeSpan.FromMinutes(5));
        options.DefaultPriority.ShouldBe(CachePriority.Normal);
        options.KeyPrefix.ShouldBe("sm");
        options.InvalidationChannel.ShouldBe("sm:cache:invalidate");
        options.IdempotencyKeyPrefix.ShouldBe("sm:idempotency");
        options.IdempotencyTtl.ShouldBe(TimeSpan.FromHours(24));
        options.LockKeyPrefix.ShouldBe("sm:lock");
        options.DefaultLockExpiry.ShouldBe(TimeSpan.FromSeconds(30));
        options.DefaultLockWait.ShouldBe(TimeSpan.FromSeconds(10));
        options.DefaultLockRetry.ShouldBe(TimeSpan.FromMilliseconds(200));
        options.ThrowOnCacheErrors.ShouldBeFalse();
        options.SerializerOptions.ShouldNotBeNull();
    }

    [Fact]
    public void CachingOptions_CanSetAllBooleanProperties()
    {
        // Arrange & Act
        var options = new CachingOptions
        {
            EnableQueryCaching = false,
            EnableCacheInvalidation = false,
            EnableDistributedIdempotency = false,
            EnableDistributedLocks = false,
            EnablePubSubInvalidation = false,
            ThrowOnCacheErrors = true
        };

        // Assert
        options.EnableQueryCaching.ShouldBeFalse();
        options.EnableCacheInvalidation.ShouldBeFalse();
        options.EnableDistributedIdempotency.ShouldBeFalse();
        options.EnableDistributedLocks.ShouldBeFalse();
        options.EnablePubSubInvalidation.ShouldBeFalse();
        options.ThrowOnCacheErrors.ShouldBeTrue();
    }

    [Fact]
    public void CachingOptions_CanSetAllTimeSpanProperties()
    {
        // Arrange & Act
        var options = new CachingOptions
        {
            DefaultDuration = TimeSpan.FromMinutes(15),
            IdempotencyTtl = TimeSpan.FromHours(48),
            DefaultLockExpiry = TimeSpan.FromMinutes(1),
            DefaultLockWait = TimeSpan.FromSeconds(30),
            DefaultLockRetry = TimeSpan.FromMilliseconds(500)
        };

        // Assert
        options.DefaultDuration.ShouldBe(TimeSpan.FromMinutes(15));
        options.IdempotencyTtl.ShouldBe(TimeSpan.FromHours(48));
        options.DefaultLockExpiry.ShouldBe(TimeSpan.FromMinutes(1));
        options.DefaultLockWait.ShouldBe(TimeSpan.FromSeconds(30));
        options.DefaultLockRetry.ShouldBe(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void CachingOptions_CanSetAllStringProperties()
    {
        // Arrange & Act
        var options = new CachingOptions
        {
            KeyPrefix = "myapp",
            InvalidationChannel = "myapp:invalidate",
            IdempotencyKeyPrefix = "myapp:idem",
            LockKeyPrefix = "myapp:lock"
        };

        // Assert
        options.KeyPrefix.ShouldBe("myapp");
        options.InvalidationChannel.ShouldBe("myapp:invalidate");
        options.IdempotencyKeyPrefix.ShouldBe("myapp:idem");
        options.LockKeyPrefix.ShouldBe("myapp:lock");
    }

    [Fact]
    public void CachingOptions_CanSetDefaultPriority()
    {
        // Arrange & Act
        var options = new CachingOptions
        {
            DefaultPriority = CachePriority.High
        };

        // Assert
        options.DefaultPriority.ShouldBe(CachePriority.High);
    }

    [Fact]
    public void CachingOptions_CanSetSerializerOptions()
    {
        // Arrange
        var serializerOptions = new CacheSerializerOptions
        {
            SerializerType = CacheSerializerType.MessagePack,
            EnableCompression = true,
            CompressionThreshold = 2048
        };

        // Act
        var options = new CachingOptions
        {
            SerializerOptions = serializerOptions
        };

        // Assert
        options.SerializerOptions.SerializerType.ShouldBe(CacheSerializerType.MessagePack);
        options.SerializerOptions.EnableCompression.ShouldBeTrue();
        options.SerializerOptions.CompressionThreshold.ShouldBe(2048);
    }

    #endregion

    #region CacheSerializerOptions Tests

    [Fact]
    public void CacheSerializerOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new CacheSerializerOptions();

        // Assert
        options.SerializerType.ShouldBe(CacheSerializerType.SystemTextJson);
        options.EnableCompression.ShouldBeFalse();
        options.CompressionThreshold.ShouldBe(1024);
    }

    [Fact]
    public void CacheSerializerOptions_CanSetAllProperties()
    {
        // Arrange & Act
        var options = new CacheSerializerOptions
        {
            SerializerType = CacheSerializerType.MessagePack,
            EnableCompression = true,
            CompressionThreshold = 4096
        };

        // Assert
        options.SerializerType.ShouldBe(CacheSerializerType.MessagePack);
        options.EnableCompression.ShouldBeTrue();
        options.CompressionThreshold.ShouldBe(4096);
    }

    [Theory]
    [InlineData(CacheSerializerType.SystemTextJson)]
    [InlineData(CacheSerializerType.MessagePack)]
    public void CacheSerializerOptions_SupportsAllSerializerTypes(CacheSerializerType serializerType)
    {
        // Arrange & Act
        var options = new CacheSerializerOptions
        {
            SerializerType = serializerType
        };

        // Assert
        options.SerializerType.ShouldBe(serializerType);
    }

    #endregion

    #region CacheSerializerType Enum Tests

    [Fact]
    public void CacheSerializerType_HasExpectedValues()
    {
        // Assert
        ((int)CacheSerializerType.SystemTextJson).ShouldBe(0);
        ((int)CacheSerializerType.MessagePack).ShouldBe(1);
    }

    #endregion
}
