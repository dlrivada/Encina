using System.Data;
using Encina.Dapper.MySQL.Tenancy;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.MySQL.Tenancy;

/// <summary>
/// Integration tests for the fail-closed tenant resolution of the Dapper MySQL
/// <see cref="TenantConnectionFactory"/> against a real database (#3675), resolved through <c>AddEncinaTenancy</c>.
/// </summary>
[Collection("Dapper-MySQL")]
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
public sealed class TenantConnectionFactoryIntegrationTests
{
    private const string KnownTenantId = "tenant-known";
    private const string UnknownTenantId = "tenant-unknown";

    // A connection string that cannot reach any server: a test that opens the returned connection proves the
    // factory did not hand out this default.
    private const string UnreachableConnectionString =
        "Server=unreachable.invalid;Database=default_db;User=none;Password=none;Connection Timeout=1";

    private readonly MySqlFixture _fixture;

    public TenantConnectionFactoryIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateConnectionAsync_UnknownTenant_ReturnsTenantNotFoundWithoutConnection()
    {
        // Arrange: the default database is reachable, so a fallback would succeed.
        await using var provider = BuildProvider(defaultConnectionString: _fixture.ConnectionString, requireTenant: true);
        await using var scope = provider.CreateAsyncScope();
        SetTenant(scope, UnknownTenantId);
        var factory = scope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

        // Act
        var connection = await factory.CreateConnectionAsync();
        var connectionString = await factory.GetConnectionStringAsync();

        // Assert
        AssertErrorCode(connection, TenancyErrors.TenantNotFoundCode);
        AssertErrorCode(connectionString, TenancyErrors.TenantNotFoundCode);
    }

    [Fact]
    public async Task CreateConnectionAsync_NoTenantAndRequireTenant_ReturnsTenantRequired()
    {
        // Arrange
        await using var provider = BuildProvider(defaultConnectionString: _fixture.ConnectionString, requireTenant: true);
        await using var scope = provider.CreateAsyncScope();
        SetTenant(scope, tenantId: null);
        var factory = scope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        AssertErrorCode(result, TenancyErrors.TenantRequiredCode);
    }

    // The factory treats a whitespace id as absent (string.IsNullOrWhiteSpace), so it also gets the default here.
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateConnectionAsync_NoTenantAndRequireTenantFalse_OpensDefaultDatabase(string? tenantId)
    {
        // Arrange
        await using var provider = BuildProvider(defaultConnectionString: _fixture.ConnectionString, requireTenant: false);
        await using var scope = provider.CreateAsyncScope();
        SetTenant(scope, tenantId);
        var factory = scope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        (await QueryOneAsync(result)).ShouldBe(1);
    }

    [Fact]
    public async Task CreateConnectionAsync_KnownDatabasePerTenantTenant_OpensTenantDatabase()
    {
        // Arrange: only the tenant's own connection string reaches the server.
        await using var provider = BuildProvider(defaultConnectionString: UnreachableConnectionString, requireTenant: true);
        await using var scope = provider.CreateAsyncScope();
        SetTenant(scope, KnownTenantId);
        var factory = scope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        (await QueryOneAsync(result)).ShouldBe(1);
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_UnknownTenant_ReturnsTenantNotFound()
    {
        // Arrange
        await using var provider = BuildProvider(defaultConnectionString: _fixture.ConnectionString, requireTenant: true);
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<ITenantConnectionFactory>();

        // Act
        var result = await factory.CreateConnectionForTenantAsync(UnknownTenantId);

        // Assert
        var error = AssertErrorCode(result, TenancyErrors.TenantNotFoundCode);
        error.Message.ShouldNotContain(UnknownTenantId);
    }

    private ServiceProvider BuildProvider(string defaultConnectionString, bool requireTenant)
    {
        var services = new ServiceCollection();
        services.AddEncinaTenancy(options =>
        {
            options.DefaultConnectionString = defaultConnectionString;
            options.RequireTenant = requireTenant;
            options.Tenants.Add(new TenantInfo(
                KnownTenantId,
                "Known Tenant",
                TenantIsolationStrategy.DatabasePerTenant,
                _fixture.ConnectionString));
        });
        services.AddScoped<ITenantConnectionFactory, TenantConnectionFactory>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static void SetTenant(AsyncServiceScope scope, string? tenantId) =>
        scope.ServiceProvider.GetRequiredService<IRequestContextAccessor>().RequestContext =
            RequestContext.CreateForTest(tenantId: tenantId);

    private static async Task<int> QueryOneAsync(Either<EncinaError, IDbConnection> result)
    {
        result.IsRight.ShouldBeTrue();
        await using var connection = (MySqlConnection)result.RightAsEnumerable().Single();
        await connection.OpenAsync();
        await using var command = new MySqlCommand("SELECT 1", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static EncinaError AssertErrorCode<T>(Either<EncinaError, T> result, string expectedCode)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().Single();
        error.GetCode().IfNone(string.Empty).ShouldBe(expectedCode);
        return error;
    }
}
