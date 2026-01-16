using Encina.Testing.Testcontainers;
namespace Encina.UnitTests.Testing.Testcontainers;

/// <summary>
/// Unit tests for <see cref="EncinaContainers"/> factory class.
/// </summary>
public class EncinaContainersTests
{
    [Fact]
    public void SqlServer_ShouldReturnSqlServerContainerFixture()
    {
        // Act
        var fixture = EncinaContainers.SqlServer();

        // Assert
        fixture.ShouldNotBeNull();
        fixture.ShouldBeOfType<SqlServerContainerFixture>();
    }

    [Fact]
    public async Task SqlServer_WithConfiguration_ShouldReturnConfiguredFixture()
    {
        // Act
        var fixture = EncinaContainers.SqlServer(builder =>
            builder.WithPassword("CustomP@ss!"));

        try
        {
            // Assert
            fixture.ShouldNotBeNull();
            await fixture.InitializeAsync();
            fixture.Container.ShouldNotBeNull();
        }
        finally
        {
            if (fixture is not null)
            {
                await fixture.DisposeAsync();
            }
        }
    }

    [Fact]
    public void SqlServer_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.SqlServer(null!));
    }

    [Fact]
    public void PostgreSql_ShouldReturnPostgreSqlContainerFixture()
    {
        // Act
        var fixture = EncinaContainers.PostgreSql();

        // Assert
        fixture.ShouldNotBeNull();
        fixture.ShouldBeOfType<PostgreSqlContainerFixture>();
    }

    [Fact]
    public async Task PostgreSql_WithConfiguration_ShouldReturnConfiguredFixture()
    {
        // Act
        var fixture = EncinaContainers.PostgreSql(builder =>
            builder.WithDatabase("custom_db"));

        try
        {
            // Assert
            fixture.ShouldNotBeNull();
            await fixture.InitializeAsync();
            fixture.Container.ShouldNotBeNull();
        }
        finally
        {
            if (fixture is not null)
            {
                await fixture.DisposeAsync();
            }
        }
    }

    [Fact]
    public void PostgreSql_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.PostgreSql(null!));
    }

    [Fact]
    public void MySql_ShouldReturnMySqlContainerFixture()
    {
        // Act
        var fixture = EncinaContainers.MySql();

        // Assert
        fixture.ShouldNotBeNull();
        fixture.ShouldBeOfType<MySqlContainerFixture>();
    }

    [Fact]
    public async Task MySql_WithConfiguration_ShouldReturnConfiguredFixture()
    {
        // Act
        var fixture = EncinaContainers.MySql(builder =>
            builder.WithDatabase("custom_db"));

        try
        {
            // Assert
            fixture.ShouldNotBeNull();
            await fixture.InitializeAsync();
            fixture.Container.ShouldNotBeNull();
        }
        finally
        {
            if (fixture is not null)
            {
                await fixture.DisposeAsync();
            }
        }
    }

    [Fact]
    public void MySql_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.MySql(null!));
    }

    [Fact]
    public void MongoDb_ShouldReturnMongoDbContainerFixture()
    {
        // Act
        var fixture = EncinaContainers.MongoDb();

        // Assert
        fixture.ShouldNotBeNull();
        fixture.ShouldBeOfType<MongoDbContainerFixture>();
    }

    [Fact]
    public async Task MongoDb_WithConfiguration_ShouldReturnConfiguredFixture()
    {
        // Act
        var fixture = EncinaContainers.MongoDb(builder =>
            builder.WithImage("mongo:6"));

        try
        {
            // Assert
            fixture.ShouldNotBeNull();
            await fixture.InitializeAsync();
            fixture.Container.ShouldNotBeNull();
        }
        finally
        {
            if (fixture is not null)
            {
                await fixture.DisposeAsync();
            }
        }
    }

    [Fact]
    public void MongoDb_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.MongoDb(null!));
    }

    [Fact]
    public void Redis_ShouldReturnRedisContainerFixture()
    {
        // Act
        var fixture = EncinaContainers.Redis();

        // Assert
        fixture.ShouldNotBeNull();
        fixture.ShouldBeOfType<RedisContainerFixture>();
    }

    [Fact]
    public async Task Redis_WithConfiguration_ShouldReturnConfiguredFixture()
    {
        // Act
        var fixture = EncinaContainers.Redis(builder =>
            builder.WithImage("redis:6-alpine"));

        try
        {
            // Assert
            fixture.ShouldNotBeNull();
            await fixture.InitializeAsync();
            fixture.Container.ShouldNotBeNull();
        }
        finally
        {
            if (fixture is not null)
            {
                await fixture.DisposeAsync();
            }
        }
    }

    [Fact]
    public void Redis_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.Redis(null!));
    }
}
