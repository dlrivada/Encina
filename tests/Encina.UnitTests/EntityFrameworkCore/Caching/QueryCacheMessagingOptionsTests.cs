using Encina.EntityFrameworkCore.Caching;
using Encina.Messaging.Caching;

namespace Encina.UnitTests.EntityFrameworkCore.Caching;

/// <summary>
/// Unit tests for <see cref="QueryCacheMessagingOptions"/>.
/// </summary>
public class QueryCacheMessagingOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Enabled_DefaultIsTrue()
    {
        // Arrange
        var options = new QueryCacheMessagingOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void DefaultExpiration_DefaultIsFiveMinutes()
    {
        // Arrange
        var options = new QueryCacheMessagingOptions();

        // Assert
        options.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void KeyPrefix_DefaultIsSmQc()
    {
        // Arrange
        var options = new QueryCacheMessagingOptions();

        // Assert
        options.KeyPrefix.ShouldBe("sm:qc");
    }

    [Fact]
    public void ThrowOnCacheErrors_DefaultIsFalse()
    {
        // Arrange
        var options = new QueryCacheMessagingOptions();

        // Assert
        options.ThrowOnCacheErrors.ShouldBeFalse();
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void Enabled_CanBeSet()
    {
        // Arrange
        var options = new QueryCacheMessagingOptions { Enabled = false };

        // Assert
        options.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void DefaultExpiration_CanBeSet()
    {
        // Arrange
        var expected = TimeSpan.FromMinutes(15);
        var options = new QueryCacheMessagingOptions { DefaultExpiration = expected };

        // Assert
        options.DefaultExpiration.ShouldBe(expected);
    }

    [Fact]
    public void KeyPrefix_CanBeSet()
    {
        // Arrange
        var options = new QueryCacheMessagingOptions { KeyPrefix = "myapp:qc" };

        // Assert
        options.KeyPrefix.ShouldBe("myapp:qc");
    }

    [Fact]
    public void ThrowOnCacheErrors_CanBeSet()
    {
        // Arrange
        var options = new QueryCacheMessagingOptions { ThrowOnCacheErrors = true };

        // Assert
        options.ThrowOnCacheErrors.ShouldBeTrue();
    }

    #endregion

    #region Default Consistency Tests

    [Fact]
    public void DefaultValues_MatchQueryCacheOptionsDefaults()
    {
        // Arrange — verify messaging options defaults match EF Core options defaults
        var messagingOptions = new QueryCacheMessagingOptions();
        var efCoreOptions = new QueryCacheOptions();

        // Assert — key properties should have consistent defaults
        messagingOptions.DefaultExpiration.ShouldBe(efCoreOptions.DefaultExpiration);
        messagingOptions.KeyPrefix.ShouldBe(efCoreOptions.KeyPrefix);
        messagingOptions.ThrowOnCacheErrors.ShouldBe(efCoreOptions.ThrowOnCacheErrors);
    }

    #endregion
}
