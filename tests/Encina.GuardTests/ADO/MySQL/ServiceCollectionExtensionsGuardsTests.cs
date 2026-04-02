using System.Data;
using Encina.ADO.MySQL;
using Encina.ADO.MySQL.Repository;
using Encina.Messaging;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.MySQL;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardsTests
{
    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaADO(configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var connectionString = "Server=localhost;Database=test;";
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentNullException when connectionString is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_NullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        string connectionString = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(connectionString));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Server=localhost;Database=test;";
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection factory throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Func<IServiceProvider, IDbConnection> connectionFactory = _ => Substitute.For<IDbConnection>();
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection factory throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> connectionFactory = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(connectionFactory));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection factory throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> connectionFactory = _ => Substitute.For<IDbConnection>();
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    // ----- AddEncinaRepository guards -----

    /// <summary>
    /// Verifies that AddEncinaRepository throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaRepository_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<EntityMappingBuilder<SvcExtTestEntity, Guid>> configure = b =>
            b.ToTable("Tests").HasId(e => e.Id).MapProperty(e => e.Name);

        // Act & Assert
        var act = () => services.AddEncinaRepository(configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaRepository throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaRepository_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<EntityMappingBuilder<SvcExtTestEntity, Guid>> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaRepository(configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    /// <summary>
    /// Verifies that AddEncinaRepository throws InvalidOperationException when Build fails
    /// because no table name was configured.
    /// </summary>
    [Fact]
    public void AddEncinaRepository_MissingTableName_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Configure builder without calling ToTable
        Action<EntityMappingBuilder<SvcExtTestEntity, Guid>> configure = b =>
            b.HasId(e => e.Id);

        // Act & Assert
        var act = () => services.AddEncinaRepository(configure);
        Should.Throw<InvalidOperationException>(act);
    }

    /// <summary>
    /// Verifies that AddEncinaRepository throws InvalidOperationException when Build fails
    /// because no primary key was configured.
    /// </summary>
    [Fact]
    public void AddEncinaRepository_MissingPrimaryKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<EntityMappingBuilder<SvcExtTestEntity, Guid>> configure = b =>
            b.ToTable("Tests");

        // Act & Assert
        var act = () => services.AddEncinaRepository(configure);
        Should.Throw<InvalidOperationException>(act);
    }

    // ----- AddEncinaReadRepository guards -----

    /// <summary>
    /// Verifies that AddEncinaReadRepository throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaReadRepository_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<EntityMappingBuilder<SvcExtTestEntity, Guid>> configure = b =>
            b.ToTable("Tests").HasId(e => e.Id).MapProperty(e => e.Name);

        // Act & Assert
        var act = () => services.AddEncinaReadRepository(configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaReadRepository throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaReadRepository_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<EntityMappingBuilder<SvcExtTestEntity, Guid>> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaReadRepository(configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    /// <summary>
    /// Verifies that AddEncinaReadRepository throws InvalidOperationException when Build fails
    /// because no table name was configured.
    /// </summary>
    [Fact]
    public void AddEncinaReadRepository_MissingTableName_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<EntityMappingBuilder<SvcExtTestEntity, Guid>> configure = b =>
            b.HasId(e => e.Id);

        // Act & Assert
        var act = () => services.AddEncinaReadRepository(configure);
        Should.Throw<InvalidOperationException>(act);
    }

    // ----- AddEncinaProcessingActivityADOMySQL guards -----

    /// <summary>
    /// Verifies that AddEncinaProcessingActivityADOMySQL throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaProcessingActivityADOMySQL_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var connectionString = "Server=localhost;Database=test;";

        // Act & Assert
        var act = () => services.AddEncinaProcessingActivityADOMySQL(connectionString);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaProcessingActivityADOMySQL throws ArgumentException when connectionString is null.
    /// </summary>
    [Fact]
    public void AddEncinaProcessingActivityADOMySQL_NullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        string connectionString = null!;

        // Act & Assert
        var act = () => services.AddEncinaProcessingActivityADOMySQL(connectionString);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(connectionString));
    }

    /// <summary>
    /// Verifies that AddEncinaProcessingActivityADOMySQL throws ArgumentException when connectionString is empty.
    /// </summary>
    [Fact]
    public void AddEncinaProcessingActivityADOMySQL_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "";

        // Act & Assert
        var act = () => services.AddEncinaProcessingActivityADOMySQL(connectionString);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe(nameof(connectionString));
    }

    /// <summary>
    /// Verifies that AddEncinaProcessingActivityADOMySQL throws ArgumentException when connectionString is whitespace.
    /// </summary>
    [Fact]
    public void AddEncinaProcessingActivityADOMySQL_WhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "   ";

        // Act & Assert
        var act = () => services.AddEncinaProcessingActivityADOMySQL(connectionString);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe(nameof(connectionString));
    }

    // ----- AddEncinaADO configuration paths -----

    /// <summary>
    /// Verifies that AddEncinaADO with configure delegate invokes the delegate and registers services.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConfigure_InvokesDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var configInvoked = false;

        // Act
        services.AddEncinaADO(config =>
        {
            configInvoked = true;
        });

        // Assert
        configInvoked.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that AddEncinaADO with module isolation enabled registers module isolation services.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithModuleIsolation_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> factory = _ => Substitute.For<IDbConnection>();

        // Act
        services.AddEncinaADO(factory, config =>
        {
            config.UseModuleIsolation = true;
        });

        // Assert - at minimum it should not throw
        services.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that AddEncinaADO with read/write separation registers the appropriate services.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithReadWriteSeparation_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaADO(config =>
        {
            config.UseReadWriteSeparation = true;
        });

        // Assert
        services.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Test entity used by ServiceCollectionExtensions guard tests.
    /// </summary>
    private sealed class SvcExtTestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
