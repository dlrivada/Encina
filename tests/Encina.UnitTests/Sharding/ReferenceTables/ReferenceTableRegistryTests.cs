using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableRegistry"/>.
/// </summary>
public sealed class ReferenceTableRegistryTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Country
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class Currency
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
    }

    private sealed class Region
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static ReferenceTableConfiguration CreateConfig<T>() where T : class
        => new(typeof(T), new ReferenceTableOptions());

    private static ReferenceTableConfiguration CreateConfig<T>(Action<ReferenceTableOptions> configure) where T : class
    {
        var options = new ReferenceTableOptions();
        configure(options);
        return new(typeof(T), options);
    }

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConfigurations_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReferenceTableRegistry(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configurations");
    }

    [Fact]
    public void Constructor_DuplicateEntityTypes_ThrowsArgumentException()
    {
        // Arrange
        var configs = new[]
        {
            CreateConfig<Country>(),
            CreateConfig<Country>()
        };

        // Act & Assert
        var act = () => new ReferenceTableRegistry(configs);
        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("configurations");
    }

    [Fact]
    public void Constructor_EmptyConfigurations_CreatesEmptyRegistry()
    {
        // Act
        var registry = new ReferenceTableRegistry([]);

        // Assert
        registry.GetAllConfigurations().Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_ValidConfigurations_CreatesRegistry()
    {
        // Arrange
        var configs = new[] { CreateConfig<Country>(), CreateConfig<Currency>() };

        // Act
        var registry = new ReferenceTableRegistry(configs);

        // Assert
        registry.GetAllConfigurations().Count.ShouldBe(2);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  IsRegistered (generic)
    // ────────────────────────────────────────────────────────────

    #region IsRegistered Generic Tests

    [Fact]
    public void IsRegistered_Generic_RegisteredType_ReturnsTrue()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        registry.IsRegistered<Country>().ShouldBeTrue();
    }

    [Fact]
    public void IsRegistered_Generic_UnregisteredType_ReturnsFalse()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        registry.IsRegistered<Currency>().ShouldBeFalse();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  IsRegistered (Type)
    // ────────────────────────────────────────────────────────────

    #region IsRegistered Type Tests

    [Fact]
    public void IsRegistered_Type_RegisteredType_ReturnsTrue()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        registry.IsRegistered(typeof(Country)).ShouldBeTrue();
    }

    [Fact]
    public void IsRegistered_Type_UnregisteredType_ReturnsFalse()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        registry.IsRegistered(typeof(Currency)).ShouldBeFalse();
    }

    [Fact]
    public void IsRegistered_Type_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        Action act = () => registry.IsRegistered((Type)null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  GetConfiguration (generic)
    // ────────────────────────────────────────────────────────────

    #region GetConfiguration Generic Tests

    [Fact]
    public void GetConfiguration_Generic_RegisteredType_ReturnsConfiguration()
    {
        // Arrange
        var expected = CreateConfig<Country>(o => o.PollingInterval = TimeSpan.FromMinutes(10));
        var registry = new ReferenceTableRegistry([expected]);

        // Act
        var config = registry.GetConfiguration<Country>();

        // Assert
        config.EntityType.ShouldBe(typeof(Country));
        config.Options.PollingInterval.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void GetConfiguration_Generic_UnregisteredType_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        var act = () => registry.GetConfiguration<Currency>();
        act.ShouldThrow<InvalidOperationException>();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  GetConfiguration (Type)
    // ────────────────────────────────────────────────────────────

    #region GetConfiguration Type Tests

    [Fact]
    public void GetConfiguration_Type_RegisteredType_ReturnsConfiguration()
    {
        // Arrange
        var expected = CreateConfig<Country>(o => o.BatchSize = 500);
        var registry = new ReferenceTableRegistry([expected]);

        // Act
        var config = registry.GetConfiguration(typeof(Country));

        // Assert
        config.EntityType.ShouldBe(typeof(Country));
        config.Options.BatchSize.ShouldBe(500);
    }

    [Fact]
    public void GetConfiguration_Type_UnregisteredType_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        var act = () => registry.GetConfiguration(typeof(Currency));
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void GetConfiguration_Type_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        var act = () => registry.GetConfiguration((Type)null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  TryGetConfiguration
    // ────────────────────────────────────────────────────────────

    #region TryGetConfiguration Tests

    [Fact]
    public void TryGetConfiguration_RegisteredType_ReturnsTrueWithConfiguration()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act
        var result = registry.TryGetConfiguration(typeof(Country), out var config);

        // Assert
        result.ShouldBeTrue();
        config.ShouldNotBeNull();
        config!.EntityType.ShouldBe(typeof(Country));
    }

    [Fact]
    public void TryGetConfiguration_UnregisteredType_ReturnsFalseWithNull()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act
        var result = registry.TryGetConfiguration(typeof(Currency), out var config);

        // Assert
        result.ShouldBeFalse();
        config.ShouldBeNull();
    }

    [Fact]
    public void TryGetConfiguration_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act & Assert
        Action act = () => registry.TryGetConfiguration(null!, out _);
        act.ShouldThrow<ArgumentNullException>();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  GetAllConfigurations
    // ────────────────────────────────────────────────────────────

    #region GetAllConfigurations Tests

    [Fact]
    public void GetAllConfigurations_MultipleRegistrations_ReturnsAll()
    {
        // Arrange
        var configs = new[]
        {
            CreateConfig<Country>(),
            CreateConfig<Currency>(),
            CreateConfig<Region>()
        };
        var registry = new ReferenceTableRegistry(configs);

        // Act
        var all = registry.GetAllConfigurations();

        // Assert
        all.Count.ShouldBe(3);
    }

    [Fact]
    public void GetAllConfigurations_EmptyRegistry_ReturnsEmpty()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([]);

        // Act & Assert
        registry.GetAllConfigurations().Count.ShouldBe(0);
    }

    [Fact]
    public void GetAllConfigurations_ReturnsReadOnlyCollection()
    {
        // Arrange
        var registry = new ReferenceTableRegistry([CreateConfig<Country>()]);

        // Act
        var all = registry.GetAllConfigurations();

        // Assert
        all.ShouldBeAssignableTo<IReadOnlyCollection<ReferenceTableConfiguration>>();
    }

    #endregion
}
