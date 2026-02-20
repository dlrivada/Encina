using Encina.Dapper.SqlServer.Tenancy;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.Tenancy;

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
            DefaultConnectionString = "Server=default;Database=default_db;"
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
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: cs => cs.ShouldBe("Server=default;Database=default_db;"), Left: _ => { });
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
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: cs => cs.ShouldBe("Server=default;Database=default_db;"), Left: _ => { });
    }

    [Fact]
    public async Task GetConnectionStringAsync_DatabasePerTenant_ReturnsTenantConnectionString()
    {
        // Arrange
        var tenant = new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: "Server=tenant1;Database=tenant1_db;");

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenant));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: cs => cs.ShouldBe("Server=tenant1;Database=tenant1_db;"), Left: _ => { });
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
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: cs => cs.ShouldBe("Server=default;Database=default_db;"), Left: _ => { });
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoDefaultConnectionString_ReturnsLeft()
    {
        // Arrange
        var options = Options.Create(new TenancyOptions { DefaultConnectionString = null });

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, options);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => { }, Left: error => error.Message.ShouldContain("No connection string available"));
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
        var result = await factory.CreateConnectionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: connection =>
            {
                connection.ShouldNotBeNull();
                connection.ConnectionString.ShouldBe("Server=default;Database=default_db;");
            },
            Left: _ => { });
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
            ConnectionString: "Server=specific;Database=specific_db;");

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantStore.GetTenantAsync("tenant-specific", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenant));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.CreateConnectionForTenantAsync("tenant-specific");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: connection =>
            {
                connection.ShouldNotBeNull();
                connection.ConnectionString.ShouldBe("Server=specific;Database=specific_db;");
            },
            Left: _ => { });
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_TenantNotFound_ReturnsLeft()
    {
        // Arrange
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantStore.GetTenantAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(result: null));
#pragma warning restore CA2012

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.CreateConnectionForTenantAsync("nonexistent");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => { }, Left: error => error.Message.ShouldContain("not found"));
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
