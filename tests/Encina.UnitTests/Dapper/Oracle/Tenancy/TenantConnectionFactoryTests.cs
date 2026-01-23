using Encina.Dapper.Oracle.Tenancy;
using Encina.Tenancy;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.Oracle.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantConnectionFactory"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantConnectionFactoryTests
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly IOptions<TenancyOptions> _tenancyOptions;

    public TenantConnectionFactoryTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantStore = Substitute.For<ITenantStore>();
        _tenancyOptions = Options.Create(new TenancyOptions
        {
            DefaultConnectionString = "Data Source=localhost;User Id=test;Password=test"
        });
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantConnectionFactory(null!, _tenantStore, _tenancyOptions));
    }

    [Fact]
    public void Constructor_NullTenantStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantConnectionFactory(_tenantProvider, null!, _tenancyOptions));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantConnectionFactory(_tenantProvider, _tenantStore, null!));
    }

    #endregion

    #region GetConnectionStringAsync Tests

    [Fact]
    public async Task GetConnectionStringAsync_NoTenantContext_ReturnsDefaultConnectionString()
    {
        // Arrange
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var connectionString = await factory.GetConnectionStringAsync();

        // Assert
        connectionString.ShouldBe("Data Source=localhost;User Id=test;Password=test");
    }

    [Fact]
    public async Task GetConnectionStringAsync_SharedSchemaTenant_ReturnsDefaultConnectionString()
    {
        // Arrange
        var tenant = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SharedSchema);

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenant));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var connectionString = await factory.GetConnectionStringAsync();

        // Assert
        connectionString.ShouldBe("Data Source=localhost;User Id=test;Password=test");
    }

    [Fact]
    public async Task GetConnectionStringAsync_DatabasePerTenant_ReturnsTenantConnectionString()
    {
        // Arrange
        var tenant = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: "Data Source=tenant1-host;User Id=tenant1;Password=tenant1pass");

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenant));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var connectionString = await factory.GetConnectionStringAsync();

        // Assert
        connectionString.ShouldBe("Data Source=tenant1-host;User Id=tenant1;Password=tenant1pass");
    }

    [Fact]
    public async Task GetConnectionStringAsync_DatabasePerTenantWithoutConnectionString_ReturnsDefault()
    {
        // Arrange
        var tenant = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: null);

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenant));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var connectionString = await factory.GetConnectionStringAsync();

        // Assert
        connectionString.ShouldBe("Data Source=localhost;User Id=test;Password=test");
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoDefaultConnectionString_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new TenancyOptions { DefaultConnectionString = null });

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, options);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await factory.GetConnectionStringAsync());
        exception.Message.ShouldContain("No connection string available");
    }

    #endregion

    #region CreateConnectionAsync Tests

    [Fact]
    public async Task CreateConnectionAsync_ReturnsConnection()
    {
        // Arrange
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var connection = await factory.CreateConnectionAsync();

        // Assert
        connection.ShouldNotBeNull();
        connection.ConnectionString.ShouldBe("Data Source=localhost;User Id=test;Password=test");
    }

    #endregion

    #region CreateConnectionForTenantAsync Tests

    [Fact]
    public async Task CreateConnectionForTenantAsync_WithValidTenant_ReturnsConnection()
    {
        // Arrange
        var tenant = new TenantInfo(
            TenantId: "tenant-specific",
            Name: "Specific Tenant",
            Strategy: TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: "Data Source=specific-host;User Id=specific;Password=specificpass");

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantStore.GetTenantAsync("tenant-specific", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenant));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var connection = await factory.CreateConnectionForTenantAsync("tenant-specific");

        // Assert
        connection.ShouldNotBeNull();
        connection.ConnectionString.ShouldBe("Data Source=specific-host;User Id=specific;Password=specificpass");
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_TenantNotFound_ThrowsException()
    {
        // Arrange
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantStore.GetTenantAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await factory.CreateConnectionForTenantAsync("nonexistent"));
        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_NullTenantId_ThrowsException()
    {
        // Arrange
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await factory.CreateConnectionForTenantAsync(null!));
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_EmptyTenantId_ThrowsException()
    {
        // Arrange
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await factory.CreateConnectionForTenantAsync(""));
    }

    #endregion
}
