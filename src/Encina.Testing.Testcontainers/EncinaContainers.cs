using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Factory class for creating pre-configured container fixtures.
/// </summary>
/// <remarks>
/// <para>
/// This factory provides convenient methods for creating container fixtures
/// with either default configurations or custom settings.
/// </para>
/// <para>
/// For most xUnit test scenarios, use the fixtures directly with
/// <c>IClassFixture&lt;T&gt;</c> or <c>ICollectionFixture&lt;T&gt;</c>.
/// Use this factory when you need programmatic control over fixture creation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create with defaults
/// var sqlServer = EncinaContainers.SqlServer();
/// await sqlServer.InitializeAsync();
///
/// // Create with custom configuration
/// var postgres = EncinaContainers.PostgreSql(builder => builder
///     .WithImage("postgres:16-alpine")
///     .WithDatabase("custom_db"));
/// await postgres.InitializeAsync();
/// </code>
/// </example>
public static class EncinaContainers
{
    /// <summary>
    /// Creates a new SQL Server container fixture with default configuration.
    /// </summary>
    /// <returns>A new <see cref="SqlServerContainerFixture"/> instance.</returns>
    public static SqlServerContainerFixture SqlServer() => new();

    /// <summary>
    /// Creates a new SQL Server container fixture with custom configuration.
    /// </summary>
    /// <param name="configure">Action to configure the container builder.</param>
    /// <returns>A new <see cref="ConfiguredContainerFixture{TContainer}"/> instance.</returns>
    /// <example>
    /// <code>
    /// var fixture = EncinaContainers.SqlServer(builder => builder
    ///     .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
    ///     .WithPassword("CustomP@ssw0rd!"));
    /// await fixture.InitializeAsync();
    /// </code>
    /// </example>
    public static ConfiguredContainerFixture<MsSqlContainer> SqlServer(Action<MsSqlBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new MsSqlBuilder()
            .WithCleanUp(true);
        configure(builder);
        return new ConfiguredContainerFixture<MsSqlContainer>(builder.Build());
    }

    /// <summary>
    /// Creates a new PostgreSQL container fixture with default configuration.
    /// </summary>
    /// <returns>A new <see cref="PostgreSqlContainerFixture"/> instance.</returns>
    public static PostgreSqlContainerFixture PostgreSql() => new();

    /// <summary>
    /// Creates a new PostgreSQL container fixture with custom configuration.
    /// </summary>
    /// <param name="configure">Action to configure the container builder.</param>
    /// <returns>A new <see cref="ConfiguredContainerFixture{TContainer}"/> instance.</returns>
    /// <example>
    /// <code>
    /// var fixture = EncinaContainers.PostgreSql(builder => builder
    ///     .WithImage("postgres:16-alpine")
    ///     .WithDatabase("custom_db"));
    /// await fixture.InitializeAsync();
    /// </code>
    /// </example>
    public static ConfiguredContainerFixture<PostgreSqlContainer> PostgreSql(Action<PostgreSqlBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new PostgreSqlBuilder()
            .WithCleanUp(true);
        configure(builder);
        return new ConfiguredContainerFixture<PostgreSqlContainer>(builder.Build());
    }

    /// <summary>
    /// Creates a new MySQL container fixture with default configuration.
    /// </summary>
    /// <returns>A new <see cref="MySqlContainerFixture"/> instance.</returns>
    public static MySqlContainerFixture MySql() => new();

    /// <summary>
    /// Creates a new MySQL container fixture with custom configuration.
    /// </summary>
    /// <param name="configure">Action to configure the container builder.</param>
    /// <returns>A new <see cref="ConfiguredContainerFixture{TContainer}"/> instance.</returns>
    /// <example>
    /// <code>
    /// var fixture = EncinaContainers.MySql(builder => builder
    ///     .WithImage("mysql:8.0")
    ///     .WithDatabase("custom_db"));
    /// await fixture.InitializeAsync();
    /// </code>
    /// </example>
    public static ConfiguredContainerFixture<MySqlContainer> MySql(Action<MySqlBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new MySqlBuilder()
            .WithCleanUp(true);
        configure(builder);
        return new ConfiguredContainerFixture<MySqlContainer>(builder.Build());
    }

    /// <summary>
    /// Creates a new MongoDB container fixture with default configuration.
    /// </summary>
    /// <returns>A new <see cref="MongoDbContainerFixture"/> instance.</returns>
    public static MongoDbContainerFixture MongoDb() => new();

    /// <summary>
    /// Creates a new MongoDB container fixture with custom configuration.
    /// </summary>
    /// <param name="configure">Action to configure the container builder.</param>
    /// <returns>A new <see cref="ConfiguredContainerFixture{TContainer}"/> instance.</returns>
    /// <example>
    /// <code>
    /// var fixture = EncinaContainers.MongoDb(builder => builder
    ///     .WithImage("mongo:6"));
    /// await fixture.InitializeAsync();
    /// </code>
    /// </example>
    public static ConfiguredContainerFixture<MongoDbContainer> MongoDb(Action<MongoDbBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new MongoDbBuilder()
            .WithCleanUp(true);
        configure(builder);
        return new ConfiguredContainerFixture<MongoDbContainer>(builder.Build());
    }

    /// <summary>
    /// Creates a new Redis container fixture with default configuration.
    /// </summary>
    /// <returns>A new <see cref="RedisContainerFixture"/> instance.</returns>
    public static RedisContainerFixture Redis() => new();

    /// <summary>
    /// Creates a new Redis container fixture with custom configuration.
    /// </summary>
    /// <param name="configure">Action to configure the container builder.</param>
    /// <returns>A new <see cref="ConfiguredContainerFixture{TContainer}"/> instance.</returns>
    /// <example>
    /// <code>
    /// var fixture = EncinaContainers.Redis(builder => builder
    ///     .WithImage("redis:6-alpine"));
    /// await fixture.InitializeAsync();
    /// </code>
    /// </example>
    public static ConfiguredContainerFixture<RedisContainer> Redis(Action<RedisBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new RedisBuilder()
            .WithCleanUp(true);
        configure(builder);
        return new ConfiguredContainerFixture<RedisContainer>(builder.Build());
    }
}
