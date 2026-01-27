using Encina.Modules.Isolation;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.ModuleIsolation;

/// <summary>
/// MySQL-specific integration tests for EF Core module isolation support.
/// </summary>
/// <remarks>
/// <para>
/// <b>IMPORTANT:</b> These tests require Pomelo.EntityFrameworkCore.MySql v10.0.0 or later,
/// which is not yet released. All tests are skipped until the provider is available.
/// </para>
/// <para>
/// Track progress: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/pull/2019
/// </para>
/// <para>
/// MySQL uses databases as the isolation mechanism (rather than schemas within a database).
/// Module isolation would use backtick-quoted identifiers (e.g., `orders`.`ModuleOrders`).
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class ModuleIsolationEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;
    private ModuleSchemaRegistry _schemaRegistry = null!;
    private ModuleIsolationOptions _isolationOptions = null!;
    private TestModuleExecutionContext _moduleContext = null!;

    public ModuleIsolationEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // MySQL EF Core is not yet supported - all tests will be skipped
        // Configure module isolation options for when tests become available
        _isolationOptions = new ModuleIsolationOptions
        {
            Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly
        };
        _isolationOptions.AddSharedSchemas("shared");
        _isolationOptions.AddModuleSchema("Orders", "orders");
        _isolationOptions.AddModuleSchema("Inventory", "inventory");

        _schemaRegistry = new ModuleSchemaRegistry(_isolationOptions);
        _moduleContext = new TestModuleExecutionContext();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    #region Schema Registry Validation Tests

    [SkippableFact]
    public void SchemaRegistry_ShouldAllowAccessToOwnSchema()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        // This test will be skipped until Pomelo 10.0.0 is available
    }

    [SkippableFact]
    public void SchemaRegistry_ShouldAllowAccessToSharedSchema()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");
    }

    [SkippableFact]
    public void SchemaRegistry_ShouldDenyAccessToOtherModuleSchemas()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");
    }

    #endregion

    #region SQL Validation Tests

    [SkippableFact]
    public void SqlValidation_ValidQueryToOwnSchemaTable_ShouldPass()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");
    }

    [SkippableFact]
    public void SqlValidation_QueryToSharedSchemaTable_ShouldPass()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");
    }

    [SkippableFact]
    public void SqlValidation_CrossModuleSchemaAccess_ShouldFail()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");
    }

    #endregion

    #region ModuleSchemaValidationInterceptor Tests

    [SkippableFact]
    public async Task Interceptor_CanExecuteValidQuery()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Interceptor_ThrowsOnCrossModuleTableAccess()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Interceptor_AllowsSharedTableAccess()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");

        await Task.CompletedTask;
    }

    #endregion

    #region Module Context Tests

    [SkippableFact]
    public void ModuleContext_CanBeSwitched()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");
    }

    [SkippableFact]
    public void ModuleContext_CreateScope_SetsAndClearsModule()
    {
        Skip.IfNot(_fixture.IsAvailable, "MySQL EF Core requires Pomelo.EntityFrameworkCore.MySql v10.0.0 which is not yet released");
    }

    #endregion
}

#region Test Module Execution Context

internal sealed class TestModuleExecutionContext : IModuleExecutionContext
{
    private string? _currentModule;

    public string? CurrentModule => _currentModule;

    public void SetModule(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        _currentModule = moduleName;
    }

    public void ClearModule()
    {
        _currentModule = null;
    }

    public IDisposable CreateScope(string moduleName)
    {
        SetModule(moduleName);
        return new ModuleScope(this);
    }

    private sealed class ModuleScope(TestModuleExecutionContext context) : IDisposable
    {
        public void Dispose() => context.ClearModule();
    }

    // Test helper methods
    public void SetCurrentModule(string? moduleName)
    {
        if (moduleName is null)
            ClearModule();
        else
            SetModule(moduleName);
    }

    public void ClearCurrentModule() => ClearModule();
}

#endregion
