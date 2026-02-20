using Encina.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantDbContextFactory{TContext}"/>.
/// </summary>
public sealed class TenantDbContextFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly IOptions<TenancyOptions> _tenancyOptions;

    public TenantDbContextFactoryTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantStore = Substitute.For<ITenantStore>();
        _tenancyOptions = Options.Create(new TenancyOptions
        {
            DefaultConnectionString = "Server=test;Database=default;"
        });
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantDbContextFactory<TestDbContext>(
                null!,
                _tenantProvider,
                _tenantStore,
                _tenancyOptions));
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantDbContextFactory<TestDbContext>(
                _serviceProvider,
                null!,
                _tenantStore,
                _tenancyOptions));
    }

    [Fact]
    public void Constructor_NullTenantStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantDbContextFactory<TestDbContext>(
                _serviceProvider,
                _tenantProvider,
                null!,
                _tenancyOptions));
    }

    [Fact]
    public void Constructor_NullTenancyOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantDbContextFactory<TestDbContext>(
                _serviceProvider,
                _tenantProvider,
                _tenantStore,
                null!));
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() =>
            new TenantDbContextFactory<TestDbContext>(
                _serviceProvider,
                _tenantProvider,
                _tenantStore,
                _tenancyOptions));
    }

    #endregion

    #region GetConnectionStringAsync Tests

    [Fact]
    public async Task GetConnectionStringAsync_WithDedicatedDatabase_ReturnsTenantConnectionString()
    {
        // Arrange
        var tenantInfo = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: "Server=tenant1;Database=tenant1_db;");

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenantInfo));
#pragma warning restore CA2012

        var factory = new TenantDbContextFactory<TestDbContext>(
            _serviceProvider,
            _tenantProvider,
            _tenantStore,
            _tenancyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.ShouldBeRight().ShouldBe("Server=tenant1;Database=tenant1_db;");
    }

    [Fact]
    public async Task GetConnectionStringAsync_WithSharedDatabase_ReturnsDefaultConnectionString()
    {
        // Arrange
        var tenantInfo = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SharedSchema);

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenantInfo));
#pragma warning restore CA2012

        var factory = new TenantDbContextFactory<TestDbContext>(
            _serviceProvider,
            _tenantProvider,
            _tenantStore,
            _tenancyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.ShouldBeRight().ShouldBe("Server=test;Database=default;");
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoTenantContext_ReturnsDefaultConnectionString()
    {
        // Arrange
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012

        var factory = new TenantDbContextFactory<TestDbContext>(
            _serviceProvider,
            _tenantProvider,
            _tenantStore,
            _tenancyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.ShouldBeRight().ShouldBe("Server=test;Database=default;");
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoConnectionStringAvailable_ReturnsError()
    {
        // Arrange
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012

        var options = Options.Create(new TenancyOptions { DefaultConnectionString = null });
        var factory = new TenantDbContextFactory<TestDbContext>(
            _serviceProvider,
            _tenantProvider,
            _tenantStore,
            options);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        var error = result.ShouldBeLeft();
        error.Message.ShouldContain("No connection string available");
    }

    #endregion

    #region ConfigureOptions Tests

    [Fact]
    public void ConfigureOptions_WithCustomConfigurator_UsesCustomConfigurator()
    {
        // Arrange
        var customConfiguratorCalled = false;
        Func<DbContextOptionsBuilder<TestDbContext>, IServiceProvider, TenantInfo?, DbContextOptionsBuilder<TestDbContext>> customConfigurator =
            (builder, sp, tenantInfo) =>
            {
                customConfiguratorCalled = true;
                return builder;
            };

        var tenantInfo = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SharedSchema);

        _tenantProvider.GetCurrentTenantId().Returns("tenant-1");
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantStore.GetTenantAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenantInfo));
#pragma warning restore CA2012

        var factory = new TenantDbContextFactory<TestDbContext>(
            _serviceProvider,
            _tenantProvider,
            _tenantStore,
            _tenancyOptions,
            customConfigurator);

        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        factory.ConfigureOptions(optionsBuilder);

        // Assert
        customConfiguratorCalled.ShouldBeTrue();
    }

    [Fact]
    public void ConfigureOptions_WithNoTenantContext_DoesNotQueryTenantStore()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);

        var factory = new TenantDbContextFactory<TestDbContext>(
            _serviceProvider,
            _tenantProvider,
            _tenantStore,
            _tenancyOptions);

        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        factory.ConfigureOptions(optionsBuilder);

        // Assert
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute verification
        _ = _tenantStore.DidNotReceive().GetTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
#pragma warning restore CA2012
    }

    #endregion

    /// <summary>
    /// Test DbContext for factory tests.
    /// </summary>
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }
    }
}
