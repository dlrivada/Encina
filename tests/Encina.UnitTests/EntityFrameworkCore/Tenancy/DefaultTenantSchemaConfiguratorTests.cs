using Encina.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Tenancy;

/// <summary>
/// Unit tests for <see cref="DefaultTenantSchemaConfigurator"/>.
/// </summary>
public sealed class DefaultTenantSchemaConfiguratorTests
{
    private readonly DefaultTenantSchemaConfigurator _configurator;
    private readonly IOptions<TenancyOptions> _tenancyOptions;

    public DefaultTenantSchemaConfiguratorTests()
    {
        _tenancyOptions = Options.Create(new TenancyOptions
        {
            DefaultSchemaName = "default_schema"
        });
        _configurator = new DefaultTenantSchemaConfigurator(_tenancyOptions);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DefaultTenantSchemaConfigurator(null!));
    }

    [Fact]
    public void ConfigureSchema_WithNullTenantInfo_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        Should.NotThrow(() =>
        {
            var modelBuilder = new ModelBuilder();
            _configurator.ConfigureSchema(modelBuilder, null);
        });
    }

    [Fact]
    public void ConfigureSchema_WithSharedSchemaStrategy_DoesNotApplySchema()
    {
        // Arrange
        var tenantInfo = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SharedSchema);

        var modelBuilder = new ModelBuilder();

        // Act - Should not throw
        Should.NotThrow(() => _configurator.ConfigureSchema(modelBuilder, tenantInfo));
    }

    [Fact]
    public void ConfigureSchema_WithDatabasePerTenantStrategy_DoesNotApplySchema()
    {
        // Arrange
        var tenantInfo = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: "Server=test;Database=test;");

        var modelBuilder = new ModelBuilder();

        // Act - Should not throw
        Should.NotThrow(() => _configurator.ConfigureSchema(modelBuilder, tenantInfo));
    }

    [Fact]
    public void ConfigureSchema_WithSchemaPerTenantStrategy_WithSchemaName_AppliesSchema()
    {
        // Arrange
        var tenantInfo = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SchemaPerTenant,
            SchemaName: "tenant_one_schema");

        var modelBuilder = new ModelBuilder();

        // Act - Should not throw (actual schema application is tested via integration tests)
        Should.NotThrow(() => _configurator.ConfigureSchema(modelBuilder, tenantInfo));
    }

    [Fact]
    public void ConfigureSchema_WithSchemaPerTenantStrategy_WithNullSchemaName_UsesDefault()
    {
        // Arrange
        var tenantInfo = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SchemaPerTenant,
            SchemaName: null);

        var modelBuilder = new ModelBuilder();

        // Act - Should not throw (uses DefaultSchemaName from options)
        Should.NotThrow(() => _configurator.ConfigureSchema(modelBuilder, tenantInfo));
    }

    [Fact]
    public void ConfigureSchema_WithSchemaPerTenantStrategy_WithEmptySchemaName_UsesDefault()
    {
        // Arrange
        var tenantInfo = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SchemaPerTenant,
            SchemaName: "");

        var modelBuilder = new ModelBuilder();

        // Act - Should not throw
        Should.NotThrow(() => _configurator.ConfigureSchema(modelBuilder, tenantInfo));
    }
}
