using System.Data;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using MySqlFactory = Encina.Dapper.MySQL.Tenancy.TenantConnectionFactory;
using PostgreSqlFactory = Encina.Dapper.PostgreSQL.Tenancy.TenantConnectionFactory;
using SqlServerFactory = Encina.Dapper.SqlServer.Tenancy.TenantConnectionFactory;

namespace Encina.GuardTests.Dapper.Tenancy;

/// <summary>
/// Guard clause tests for the Dapper <c>TenantConnectionFactory</c> of SQL Server, PostgreSQL and MySQL (#3675).
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Dapper")]
public sealed class DapperTenantConnectionFactoryGuardTests
{
    public static TheoryData<string> Providers => new() { "SqlServer", "PostgreSQL", "MySQL" };

    [Theory]
    [MemberData(nameof(Providers))]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException(string provider)
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            Create(provider, null!, Substitute.For<ITenantStore>(), CreateOptions()));
        ex.ParamName.ShouldBe("tenantProvider");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void Constructor_NullTenantStore_ThrowsArgumentNullException(string provider)
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            Create(provider, Substitute.For<ITenantProvider>(), null!, CreateOptions()));
        ex.ParamName.ShouldBe("tenantStore");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void Constructor_NullOptions_ThrowsArgumentNullException(string provider)
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            Create(provider, Substitute.For<ITenantProvider>(), Substitute.For<ITenantStore>(), null!));
        ex.ParamName.ShouldBe("options");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task CreateConnectionForTenantAsync_NullTenantId_ThrowsArgumentNullException(string provider)
    {
        var createForTenant = Create(provider, Substitute.For<ITenantProvider>(), Substitute.For<ITenantStore>(), CreateOptions());

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await createForTenant(null!));
        ex.ParamName.ShouldBe("tenantId");
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task CreateConnectionForTenantAsync_EmptyTenantId_ThrowsArgumentException(string provider)
    {
        var createForTenant = Create(provider, Substitute.For<ITenantProvider>(), Substitute.For<ITenantStore>(), CreateOptions());

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await createForTenant(string.Empty));
        ex.ParamName.ShouldBe("tenantId");
    }

    // A whitespace id passes the argument guard (ThrowIfNullOrEmpty); the store does not know it, so the valid result
    // is the coded tenant-not-found error, never a connection to the default database.
    [Theory]
    [MemberData(nameof(Providers))]
    public async Task CreateConnectionForTenantAsync_WhitespaceTenantId_ReturnsTenantNotFound(string provider)
    {
        var store = Substitute.For<ITenantStore>();
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = store.GetTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012
        var createForTenant = Create(provider, Substitute.For<ITenantProvider>(), store, CreateOptions());

        var result = await createForTenant("   ");

        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(TenancyErrors.TenantNotFoundCode);
    }

    private static IOptions<TenancyOptions> CreateOptions() =>
        Options.Create(new TenancyOptions { DefaultConnectionString = "Server=default;Database=default_db;" });

    // Each Dapper package declares its own ITenantConnectionFactory, so the tests drive the factories through the
    // explicit-id method they all share.
    private static Func<string, ValueTask<Either<EncinaError, IDbConnection>>> Create(
        string provider,
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        IOptions<TenancyOptions> options) => provider switch
        {
            "SqlServer" => Bind(new SqlServerFactory(tenantProvider, tenantStore, options)),
            "PostgreSQL" => Bind(new PostgreSqlFactory(tenantProvider, tenantStore, options)),
            "MySQL" => Bind(new MySqlFactory(tenantProvider, tenantStore, options)),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider."),
        };

    private static Func<string, ValueTask<Either<EncinaError, IDbConnection>>> Bind(SqlServerFactory factory) =>
        tenantId => factory.CreateConnectionForTenantAsync(tenantId);

    private static Func<string, ValueTask<Either<EncinaError, IDbConnection>>> Bind(PostgreSqlFactory factory) =>
        tenantId => factory.CreateConnectionForTenantAsync(tenantId);

    private static Func<string, ValueTask<Either<EncinaError, IDbConnection>>> Bind(MySqlFactory factory) =>
        tenantId => factory.CreateConnectionForTenantAsync(tenantId);
}
