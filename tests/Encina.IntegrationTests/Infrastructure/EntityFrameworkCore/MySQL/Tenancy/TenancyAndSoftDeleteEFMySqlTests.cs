using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Tenancy;

/// <summary>
/// MySQL-specific integration tests proving that the tenant isolation filter and the soft-delete
/// filter coexist as two independent named EF Core query filters (#1268). Mirrors
/// <see cref="TenancyEFMySqlTests"/> and the SQL Server / PostgreSQL siblings of this test class.
/// </summary>
/// <remarks>
/// <para>
/// <b>IMPORTANT:</b> These tests require Pomelo.EntityFrameworkCore.MySql v10.0.0 or later,
/// which is not yet released. All tests are skipped until the provider is available.
/// </para>
/// <para>
/// Track progress: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class TenancyAndSoftDeleteEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public TenancyAndSoftDeleteEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        // MySQL EF Core is not yet supported - all tests will be skipped
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task TenantFilter_AppliedBeforeSoftDelete_OnlyReturnsCurrentTenantNonDeletedRows()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 (EF Core 10 compatible). See: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019");

        // This code will execute once Pomelo v10 is available
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SoftDeleteFilter_AppliedBeforeTenant_OnlyReturnsCurrentTenantNonDeletedRows()
    {
        Assert.SkipWhen(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 (EF Core 10 compatible). See: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019");

        await Task.CompletedTask;
    }
}
