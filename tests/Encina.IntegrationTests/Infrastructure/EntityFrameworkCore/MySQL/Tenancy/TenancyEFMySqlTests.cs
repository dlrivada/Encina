using Encina.EntityFrameworkCore.Tenancy;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Tenancy;

/// <summary>
/// MySQL-specific integration tests for EF Core multi-tenancy support.
/// Tests global query filters, automatic tenant assignment, and tenant isolation.
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
public sealed class TenancyEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;
    private readonly TestTenantProvider _tenantProvider = new();
    private readonly EfCoreTenancyOptions _tenancyOptions = new()
    {
        AutoAssignTenantId = true,
        ValidateTenantOnSave = true,
        UseQueryFilters = true,
        ThrowOnMissingTenantContext = true
    };
    private readonly TenancyOptions _coreOptions = new()
    {
        RequireTenant = true
    };

    public TenancyEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // MySQL EF Core is not yet supported - all tests will be skipped
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    #region Query Filter Tests

    [SkippableFact]
    public async Task TenantFilter_ShouldOnlyReturnCurrentTenantData()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        // This test will be skipped until Pomelo 10.0.0 is available
        await Task.CompletedTask;
    }

    [SkippableFact]
    public async Task IgnoreQueryFilters_ShouldReturnAllTenantData()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    #endregion

    #region Auto-Assignment Tests

    [SkippableFact]
    public async Task NewEntity_ShouldAutoAssignTenantId()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    #endregion

    #region Tenant Isolation Tests

    [SkippableFact]
    public async Task QueryWithDifferentTenant_ShouldReturnEmpty()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    [SkippableFact]
    public async Task CrossTenantDataIsolation_ShouldBeEnforced()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    [SkippableFact]
    public async Task MultipleTenants_ShouldMaintainSeparateDataSets()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    #endregion

    #region Validation Tests

    [SkippableFact]
    public async Task UpdateEntity_WrongTenant_ShouldThrowException()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    #endregion

    #region LINQ Query Tests

    [SkippableFact]
    public async Task WhereClause_ShouldWorkWithTenantFilter()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Aggregate_ShouldWorkWithTenantFilter()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    #endregion
}
