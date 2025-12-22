namespace Encina.Caching.Tests;

/// <summary>
/// Unit tests for <see cref="CacheAttribute"/>.
/// </summary>
public class CacheAttributeTests
{
    [Fact]
    public void CacheAttribute_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var attribute = new CacheAttribute();

        // Assert
        attribute.DurationSeconds.Should().Be(300);
        attribute.KeyTemplate.Should().BeNull();
        attribute.VaryByUser.Should().BeFalse();
        attribute.VaryByTenant.Should().BeTrue();
        attribute.Priority.Should().Be(CachePriority.Normal);
        attribute.SlidingExpiration.Should().BeFalse();
        attribute.MaxAbsoluteExpirationSeconds.Should().BeNull();
    }

    [Fact]
    public void Duration_ReturnsTimeSpanFromDurationSeconds()
    {
        // Arrange
        var attribute = new CacheAttribute { DurationSeconds = 600 };

        // Act & Assert
        attribute.Duration.Should().Be(TimeSpan.FromSeconds(600));
    }

    [Fact]
    public void MaxAbsoluteExpiration_WhenSet_ReturnsTimeSpan()
    {
        // Arrange
        var attribute = new CacheAttribute { MaxAbsoluteExpirationSeconds = 3600 };

        // Act & Assert
        attribute.MaxAbsoluteExpiration.Should().Be(TimeSpan.FromSeconds(3600));
    }

    [Fact]
    public void MaxAbsoluteExpiration_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var attribute = new CacheAttribute();

        // Act & Assert
        attribute.MaxAbsoluteExpiration.Should().BeNull();
    }

    [Fact]
    public void CacheAttribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var type = typeof(TestCachedQuery);
        var attribute = type.GetCustomAttributes(typeof(CacheAttribute), false).FirstOrDefault() as CacheAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.DurationSeconds.Should().Be(120);
        attribute.KeyTemplate.Should().Be("test:{Id}");
    }

    [Fact]
    public void CacheAttribute_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var attribute = new CacheAttribute
        {
            DurationSeconds = 900,
            KeyTemplate = "custom:{key}",
            VaryByUser = true,
            VaryByTenant = false,
            Priority = CachePriority.High,
            SlidingExpiration = true,
            MaxAbsoluteExpirationSeconds = 7200
        };

        // Assert
        attribute.DurationSeconds.Should().Be(900);
        attribute.KeyTemplate.Should().Be("custom:{key}");
        attribute.VaryByUser.Should().BeTrue();
        attribute.VaryByTenant.Should().BeFalse();
        attribute.Priority.Should().Be(CachePriority.High);
        attribute.SlidingExpiration.Should().BeTrue();
        attribute.MaxAbsoluteExpirationSeconds.Should().Be(7200);
        attribute.Duration.Should().Be(TimeSpan.FromSeconds(900));
        attribute.MaxAbsoluteExpiration.Should().Be(TimeSpan.FromSeconds(7200));
    }

    [Cache(DurationSeconds = 120, KeyTemplate = "test:{Id}")]
    private sealed record TestCachedQuery(Guid Id) : IRequest<string>;
}
