using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Core.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableRegistry"/>.
/// </summary>
public sealed class ReferenceTableRegistryTests
{
    [Fact]
    public void Constructor_WithValidConfigurations_CreatesRegistry()
    {
        // Arrange
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(CountryEntity), new ReferenceTableOptions()),
            new ReferenceTableConfiguration(typeof(CurrencyEntity), new ReferenceTableOptions())
        };

        // Act
        var registry = new ReferenceTableRegistry(configs);

        // Assert
        registry.GetAllConfigurations().Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_WithDuplicateTypes_ThrowsArgumentException()
    {
        // Arrange
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(CountryEntity), new ReferenceTableOptions()),
            new ReferenceTableConfiguration(typeof(CountryEntity), new ReferenceTableOptions())
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new ReferenceTableRegistry(configs));
        ex.Message.ShouldContain("Duplicate");
    }

    [Fact]
    public void Constructor_WithNullConfigurations_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ReferenceTableRegistry(null!));
    }

    [Fact]
    public void IsRegistered_Generic_WhenRegistered_ReturnsTrue()
    {
        // Arrange
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(CountryEntity), new ReferenceTableOptions())
        };
        var registry = new ReferenceTableRegistry(configs);

        // Act & Assert
        registry.IsRegistered<CountryEntity>().ShouldBeTrue();
    }

    [Fact]
    public void IsRegistered_Generic_WhenNotRegistered_ReturnsFalse()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([]);

        // Act & Assert
        registry.IsRegistered<CountryEntity>().ShouldBeFalse();
    }

    [Fact]
    public void IsRegistered_ByType_WhenRegistered_ReturnsTrue()
    {
        // Arrange
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(CountryEntity), new ReferenceTableOptions())
        };
        var registry = new ReferenceTableRegistry(configs);

        // Act & Assert
        registry.IsRegistered(typeof(CountryEntity)).ShouldBeTrue();
    }

    [Fact]
    public void IsRegistered_ByType_WithNull_ThrowsArgumentNullException()
    {
        var registry = new ReferenceTableRegistry([]);
        Assert.Throws<ArgumentNullException>(() => registry.IsRegistered(null!));
    }

    [Fact]
    public void GetConfiguration_Generic_WhenRegistered_ReturnsConfig()
    {
        // Arrange
        var config = new ReferenceTableConfiguration(typeof(CountryEntity), new ReferenceTableOptions());
        var registry = new ReferenceTableRegistry([config]);

        // Act
        var result = registry.GetConfiguration<CountryEntity>();

        // Assert
        result.EntityType.ShouldBe(typeof(CountryEntity));
        result.Options.RefreshStrategy.ShouldBe(RefreshStrategy.Polling);
    }

    [Fact]
    public void GetConfiguration_ByType_WhenNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([]);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.GetConfiguration(typeof(CountryEntity)));
        ex.Message.ShouldContain("not registered as a reference table");
    }

    [Fact]
    public void GetConfiguration_ByType_WithNull_ThrowsArgumentNullException()
    {
        var registry = new ReferenceTableRegistry([]);
        Assert.Throws<ArgumentNullException>(() => registry.GetConfiguration(null!));
    }

    [Fact]
    public void TryGetConfiguration_WhenRegistered_ReturnsTrueAndConfig()
    {
        // Arrange
        var config = new ReferenceTableConfiguration(typeof(CountryEntity), new ReferenceTableOptions());
        var registry = new ReferenceTableRegistry([config]);

        // Act
        var found = registry.TryGetConfiguration(typeof(CountryEntity), out var result);

        // Assert
        found.ShouldBeTrue();
        result.ShouldNotBeNull();
        result!.EntityType.ShouldBe(typeof(CountryEntity));
    }

    [Fact]
    public void TryGetConfiguration_WhenNotRegistered_ReturnsFalse()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([]);

        // Act
        var found = registry.TryGetConfiguration(typeof(CountryEntity), out var result);

        // Assert
        found.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryGetConfiguration_WithNull_ThrowsArgumentNullException()
    {
        var registry = new ReferenceTableRegistry([]);
        Assert.Throws<ArgumentNullException>(() =>
            registry.TryGetConfiguration(null!, out _));
    }

    [Fact]
    public void GetAllConfigurations_ReturnsAllRegistered()
    {
        // Arrange
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(CountryEntity), new ReferenceTableOptions()),
            new ReferenceTableConfiguration(typeof(CurrencyEntity), new ReferenceTableOptions())
        };
        var registry = new ReferenceTableRegistry(configs);

        // Act
        var all = registry.GetAllConfigurations();

        // Assert
        all.Count.ShouldBe(2);
    }

    // Test entity types
    internal sealed class CountryEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    internal sealed class CurrencyEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
