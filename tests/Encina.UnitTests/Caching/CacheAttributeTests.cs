using Encina.Caching;
namespace Encina.UnitTests.Caching;

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
        attribute.DurationSeconds.ShouldBe(300);
        attribute.KeyTemplate.ShouldBeNull();
        attribute.VaryByUser.ShouldBeFalse();
        attribute.VaryByTenant.ShouldBeTrue();
        attribute.Priority.ShouldBe(CachePriority.Normal);
        attribute.SlidingExpiration.ShouldBeFalse();
        attribute.MaxAbsoluteExpirationSeconds.ShouldBeNull();
    }

    [Fact]
    public void Duration_ReturnsTimeSpanFromDurationSeconds()
    {
        // Arrange
        var attribute = new CacheAttribute { DurationSeconds = 600 };

        // Act & Assert
        attribute.Duration.ShouldBe(TimeSpan.FromSeconds(600));
    }

    [Fact]
    public void MaxAbsoluteExpiration_WhenSet_ReturnsTimeSpan()
    {
        // Arrange
        var attribute = new CacheAttribute { MaxAbsoluteExpirationSeconds = 3600 };

        // Act & Assert
        attribute.MaxAbsoluteExpiration.ShouldBe(TimeSpan.FromSeconds(3600));
    }

    [Fact]
    public void MaxAbsoluteExpiration_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var attribute = new CacheAttribute();

        // Act & Assert
        attribute.MaxAbsoluteExpiration.ShouldBeNull();
    }

    [Fact]
    public void CacheAttribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var type = typeof(TestCachedQuery);
        var attribute = type.GetCustomAttributes(typeof(CacheAttribute), false).FirstOrDefault() as CacheAttribute;

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.DurationSeconds.ShouldBe(120);
        attribute.KeyTemplate.ShouldBe("test:{Id}");
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
        attribute.DurationSeconds.ShouldBe(900);
        attribute.KeyTemplate.ShouldBe("custom:{key}");
        attribute.VaryByUser.ShouldBeTrue();
        attribute.VaryByTenant.ShouldBeFalse();
        attribute.Priority.ShouldBe(CachePriority.High);
        attribute.SlidingExpiration.ShouldBeTrue();
        attribute.MaxAbsoluteExpirationSeconds.ShouldBe(7200);
        attribute.Duration.ShouldBe(TimeSpan.FromSeconds(900));
        attribute.MaxAbsoluteExpiration.ShouldBe(TimeSpan.FromSeconds(7200));
    }

    [Cache(DurationSeconds = 120, KeyTemplate = "test:{Id}")]
    private sealed record TestCachedQuery(Guid Id) : IRequest<string>;
}
