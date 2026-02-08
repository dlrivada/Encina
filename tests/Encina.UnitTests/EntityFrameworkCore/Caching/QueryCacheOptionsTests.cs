using Encina.EntityFrameworkCore.Caching;

namespace Encina.UnitTests.EntityFrameworkCore.Caching;

/// <summary>
/// Unit tests for <see cref="QueryCacheOptions"/>.
/// </summary>
public class QueryCacheOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Enabled_DefaultIsFalse()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Assert
        options.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void DefaultExpiration_DefaultIsFiveMinutes()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Assert
        options.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void KeyPrefix_DefaultIsSmQc()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Assert
        options.KeyPrefix.ShouldBe("sm:qc");
    }

    [Fact]
    public void ExcludedEntityTypes_DefaultIsEmpty()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Assert
        options.ExcludedEntityTypes.ShouldBeEmpty();
    }

    [Fact]
    public void ThrowOnCacheErrors_DefaultIsFalse()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Assert
        options.ThrowOnCacheErrors.ShouldBeFalse();
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void Enabled_CanBeSet()
    {
        // Arrange
        var options = new QueryCacheOptions { Enabled = true };

        // Assert
        options.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void DefaultExpiration_CanBeSet()
    {
        // Arrange
        var expected = TimeSpan.FromMinutes(30);
        var options = new QueryCacheOptions { DefaultExpiration = expected };

        // Assert
        options.DefaultExpiration.ShouldBe(expected);
    }

    [Fact]
    public void KeyPrefix_CanBeSet()
    {
        // Arrange
        var options = new QueryCacheOptions { KeyPrefix = "custom:prefix" };

        // Assert
        options.KeyPrefix.ShouldBe("custom:prefix");
    }

    [Fact]
    public void ThrowOnCacheErrors_CanBeSet()
    {
        // Arrange
        var options = new QueryCacheOptions { ThrowOnCacheErrors = true };

        // Assert
        options.ThrowOnCacheErrors.ShouldBeTrue();
    }

    #endregion

    #region ExcludeType Tests

    [Fact]
    public void ExcludeType_AddsTypeNameToExcludedSet()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Act
        options.ExcludeType<TestEntity>();

        // Assert
        options.ExcludedEntityTypes.ShouldContain(nameof(TestEntity));
    }

    [Fact]
    public void ExcludeType_MultipleTypes_AddsAll()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Act
        options.ExcludeType<TestEntity>()
               .ExcludeType<AnotherEntity>();

        // Assert
        options.ExcludedEntityTypes.Count.ShouldBe(2);
        options.ExcludedEntityTypes.ShouldContain(nameof(TestEntity));
        options.ExcludedEntityTypes.ShouldContain(nameof(AnotherEntity));
    }

    [Fact]
    public void ExcludeType_ReturnsSameInstance_ForChaining()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Act
        var result = options.ExcludeType<TestEntity>();

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ExcludeType_DuplicateType_DoesNotAddTwice()
    {
        // Arrange
        var options = new QueryCacheOptions();

        // Act
        options.ExcludeType<TestEntity>()
               .ExcludeType<TestEntity>();

        // Assert
        options.ExcludedEntityTypes.Count.ShouldBe(1);
    }

    #endregion

    // Test entity types for ExcludeType tests
    private sealed class TestEntity;
    private sealed class AnotherEntity;
}
