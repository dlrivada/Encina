using Testcontainers.MsSql;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Pre-configured xUnit fixture for SQL Server using Testcontainers.
/// Provides a throwaway SQL Server instance for integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This fixture manages the lifecycle of a SQL Server container by inheriting
/// common functionality from <see cref="ContainerFixtureBase{TContainer}"/>.
/// </para>
/// <para>
/// The Docker image and SA password are configurable via constructor parameters,
/// environment variables, or defaults. Configuration precedence (highest to lowest):
/// <list type="number">
///   <item><description>Constructor parameters</description></item>
///   <item><description>Environment variables (ENCINA_SQLSERVER_IMAGE, ENCINA_SQLSERVER_PASSWORD)</description></item>
///   <item><description>Hardcoded defaults</description></item>
/// </list>
/// </para>
/// <para>
/// Use this fixture with xUnit's <c>IClassFixture&lt;T&gt;</c> pattern for shared
/// container across tests in a class, or <c>ICollectionFixture&lt;T&gt;</c> for
/// shared container across multiple test classes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using defaults
/// public class OrderRepositoryTests : IClassFixture&lt;SqlServerContainerFixture&gt;
/// {
///     private readonly SqlServerContainerFixture _db;
///
///     public OrderRepositoryTests(SqlServerContainerFixture db) => _db = db;
///
///     [Fact]
///     public async Task CreateOrder_ShouldPersist()
///     {
///         await using var connection = new SqlConnection(_db.ConnectionString);
///         await connection.OpenAsync();
///         // Test with real SQL Server...
///     }
/// }
///
/// // Using custom image
/// var fixture = new SqlServerContainerFixture(image: "mcr.microsoft.com/mssql/server:2019-latest");
///
/// // Using custom password
/// var fixture = new SqlServerContainerFixture(password: "MySecurePassword123");
/// </code>
/// </example>
public class SqlServerContainerFixture : ContainerFixtureBase<MsSqlContainer>
{
    /// <summary>
    /// Default SQL Server Docker image tag with latest cumulative updates.
    /// </summary>
    private const string DefaultSqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";

    /// <summary>
    /// Default SQL Server SA password.
    /// </summary>
    private const string DefaultPassword = "StrongP@ssw0rd!";

    /// <summary>
    /// Environment variable name for SQL Server Docker image override.
    /// </summary>
    private const string ImageEnvironmentVariable = "ENCINA_SQLSERVER_IMAGE";

    /// <summary>
    /// Environment variable name for SQL Server SA password override.
    /// </summary>
    private const string PasswordEnvironmentVariable = "ENCINA_SQLSERVER_PASSWORD";

    private readonly string _image;
    private readonly string _password;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerContainerFixture"/> class
    /// with optional custom image and password.
    /// </summary>
    /// <param name="image">
    /// Docker image tag for SQL Server (e.g., "mcr.microsoft.com/mssql/server:2022-latest").
    /// If null or empty, uses environment variable ENCINA_SQLSERVER_IMAGE, or defaults to <see cref="DefaultSqlServerImage"/>.
    /// </param>
    /// <param name="password">
    /// SQL Server SA password. If null or empty, uses environment variable ENCINA_SQLSERVER_PASSWORD,
    /// or defaults to <see cref="DefaultPassword"/>.
    /// </param>
    public SqlServerContainerFixture(string? image = null, string? password = null)
    {
        _image = ResolveConfigValue(image, ImageEnvironmentVariable, DefaultSqlServerImage);
        _password = ResolveConfigValue(password, PasswordEnvironmentVariable, DefaultPassword);
    }

    /// <summary>
    /// Resolves a configuration value by checking parameter, environment variable, and default in order.
    /// </summary>
    /// <param name="parameterValue">The explicit parameter value from constructor.</param>
    /// <param name="environmentVariableName">The environment variable name to check if parameter is null or empty.</param>
    /// <param name="defaultValue">The default value to use if both parameter and environment variable are null or empty.</param>
    /// <returns>
    /// The resolved value in this order of precedence:
    /// 1. Parameter value (if not null or whitespace)
    /// 2. Environment variable value (if not null or whitespace)
    /// 3. Default value
    /// </returns>
    private static string ResolveConfigValue(string? parameterValue, string environmentVariableName, string defaultValue)
    {
        // Prefer explicit parameter if not null or empty
        if (!string.IsNullOrWhiteSpace(parameterValue))
        {
            return parameterValue;
        }

        // Try environment variable if parameter was null/empty
        var envValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        // Fall back to default if both parameter and env var are null/empty
        return defaultValue;
    }

    /// <summary>
    /// Builds and configures the SQL Server container using the configured image and password.
    /// </summary>
    /// <remarks>
    /// Uses the image and password values from constructor parameters, environment variables,
    /// or defaults. The container is configured with automatic cleanup enabled.
    /// </remarks>
    /// <returns>A pre-configured SQL Server container.</returns>
    protected override MsSqlContainer BuildContainer()
    {
        return new MsSqlBuilder()
            .WithImage(_image)
            .WithPassword(_password)
            .WithCleanUp(true)
            .Build();
    }
}
