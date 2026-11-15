using Encina.Tenancy;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using MySqlTenantConnectionFactory = Encina.ADO.MySQL.Tenancy.TenantConnectionFactory;
using PostgreSqlTenantConnectionFactory = Encina.ADO.PostgreSQL.Tenancy.TenantConnectionFactory;
using SqlServerTenantConnectionFactory = Encina.ADO.SqlServer.Tenancy.TenantConnectionFactory;

namespace Encina.GuardTests.ADO;

/// <summary>
/// Guard clause tests for the ADO.NET <c>TenantConnectionFactory</c> of SQL Server, PostgreSQL and MySQL.
/// </summary>
/// <remarks>
/// The three classes share one implementation shape (#3674), so one file drives the same guards for each
/// provider. A blank tenant id (null, empty or whitespace) on the explicit-id path is an invalid argument
/// and never reaches the tenant store.
/// </remarks>
[Trait("Category", "Guard")]
public sealed class TenantConnectionFactoryGuardTests
{
    public static TheoryData<string> Providers => new() { "SqlServer", "PostgreSQL", "MySQL" };

    [Theory]
    [MemberData(nameof(Providers))]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException(string provider)
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            Construct(provider, null, Substitute.For<ITenantStore>(), CreateOptions()));
        ex.ParamName.ShouldBe("tenantProvider");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void Constructor_NullTenantStore_ThrowsArgumentNullException(string provider)
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            Construct(provider, Substitute.For<ITenantProvider>(), null, CreateOptions()));
        ex.ParamName.ShouldBe("tenantStore");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void Constructor_NullOptions_ThrowsArgumentNullException(string provider)
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            Construct(provider, Substitute.For<ITenantProvider>(), Substitute.For<ITenantStore>(), null));
        ex.ParamName.ShouldBe("options");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task CreateConnectionForTenantAsync_NullTenantId_ThrowsArgumentNullException(string provider)
    {
        // Arrange
        var tenantStore = Substitute.For<ITenantStore>();
        var createForTenant = CreateForTenant(provider, tenantStore);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => createForTenant(null!));
        ex.ParamName.ShouldBe("tenantId");
        await tenantStore.DidNotReceiveWithAnyArgs().GetTenantAsync(default!, default);
    }

    [Theory]
    [InlineData("SqlServer", "")]
    [InlineData("SqlServer", "   ")]
    [InlineData("PostgreSQL", "")]
    [InlineData("PostgreSQL", "   ")]
    [InlineData("MySQL", "")]
    [InlineData("MySQL", "   ")]
    public async Task CreateConnectionForTenantAsync_BlankTenantId_ThrowsArgumentException(string provider, string tenantId)
    {
        // Arrange
        var tenantStore = Substitute.For<ITenantStore>();
        var createForTenant = CreateForTenant(provider, tenantStore);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() => createForTenant(tenantId));
        ex.ParamName.ShouldBe("tenantId");
        await tenantStore.DidNotReceiveWithAnyArgs().GetTenantAsync(default!, default);
    }

    private static IOptions<TenancyOptions> CreateOptions() =>
        Options.Create(new TenancyOptions { DefaultConnectionString = "Server=localhost;Database=Guard;" });

    private static Action Construct(
        string provider,
        ITenantProvider? tenantProvider,
        ITenantStore? tenantStore,
        IOptions<TenancyOptions>? options) => provider switch
        {
            "SqlServer" => () => _ = new SqlServerTenantConnectionFactory(tenantProvider!, tenantStore!, options!),
            "PostgreSQL" => () => _ = new PostgreSqlTenantConnectionFactory(tenantProvider!, tenantStore!, options!),
            "MySQL" => () => _ = new MySqlTenantConnectionFactory(tenantProvider!, tenantStore!, options!),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider."),
        };

    private static Func<string, Task> CreateForTenant(string provider, ITenantStore tenantStore)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        var options = CreateOptions();

        return provider switch
        {
            "SqlServer" => id => new SqlServerTenantConnectionFactory(tenantProvider, tenantStore, options)
                .CreateConnectionForTenantAsync(id).AsTask(),
            "PostgreSQL" => id => new PostgreSqlTenantConnectionFactory(tenantProvider, tenantStore, options)
                .CreateConnectionForTenantAsync(id).AsTask(),
            "MySQL" => id => new MySqlTenantConnectionFactory(tenantProvider, tenantStore, options)
                .CreateConnectionForTenantAsync(id).AsTask(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider."),
        };
    }
}
