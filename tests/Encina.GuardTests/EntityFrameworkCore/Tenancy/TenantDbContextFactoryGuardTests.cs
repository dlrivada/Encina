using Encina.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.EntityFrameworkCore.Tenancy;

/// <summary>
/// Guard clause tests for <see cref="TenantDbContextFactory{TContext}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class TenantDbContextFactoryGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;
        var tenantProvider = Substitute.For<ITenantProvider>();
        var tenantStore = Substitute.For<ITenantStore>();
        var tenancyOptions = Options.Create(new TenancyOptions());

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantDbContextFactory<TestTenantDbContext>(
                serviceProvider, tenantProvider, tenantStore, tenancyOptions));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        ITenantProvider tenantProvider = null!;
        var tenantStore = Substitute.For<ITenantStore>();
        var tenancyOptions = Options.Create(new TenancyOptions());

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantDbContextFactory<TestTenantDbContext>(
                serviceProvider, tenantProvider, tenantStore, tenancyOptions));
        ex.ParamName.ShouldBe("tenantProvider");
    }

    [Fact]
    public void Constructor_NullTenantStore_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var tenantProvider = Substitute.For<ITenantProvider>();
        ITenantStore tenantStore = null!;
        var tenancyOptions = Options.Create(new TenancyOptions());

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantDbContextFactory<TestTenantDbContext>(
                serviceProvider, tenantProvider, tenantStore, tenancyOptions));
        ex.ParamName.ShouldBe("tenantStore");
    }

    [Fact]
    public void Constructor_NullTenancyOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var tenantProvider = Substitute.For<ITenantProvider>();
        var tenantStore = Substitute.For<ITenantStore>();
        IOptions<TenancyOptions> tenancyOptions = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TenantDbContextFactory<TestTenantDbContext>(
                serviceProvider, tenantProvider, tenantStore, tenancyOptions));
        ex.ParamName.ShouldBe("tenancyOptions");
    }

    #endregion

    #region Test Infrastructure

    private sealed class TestTenantDbContext : DbContext
    {
        public TestTenantDbContext(DbContextOptions<TestTenantDbContext> options) : base(options)
        {
        }
    }

    #endregion
}
