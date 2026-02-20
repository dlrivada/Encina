using Encina;
using Encina.ADO.Sqlite.Tenancy;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.Sqlite.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantConnectionFactory"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantConnectionFactoryTests
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly IOptions<TenancyOptions> _options;

    public TenantConnectionFactoryTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantStore = Substitute.For<ITenantStore>();
        _options = Options.Create(new TenancyOptions
        {
            DefaultConnectionString = "Data Source=:memory:"
        });
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantConnectionFactory(null!, _tenantStore, _options));
    }

    [Fact]
    public void Constructor_NullTenantStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantConnectionFactory(_tenantProvider, null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantConnectionFactory(_tenantProvider, _tenantStore, null!));
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoTenantContext_ReturnsDefaultConnectionString()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: cs => cs.ShouldBe("Data Source=:memory:"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetConnectionStringAsync_SharedSchemaTenant_ReturnsDefaultConnectionString()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-1");
        _tenantStore.GetTenantAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(
                TenantId: "tenant-1",
                Name: "Tenant One",
                Strategy: TenantIsolationStrategy.SharedSchema));

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: cs => cs.ShouldBe("Data Source=:memory:"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetConnectionStringAsync_DatabasePerTenant_ReturnsTenantConnectionString()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-2");
        _tenantStore.GetTenantAsync("tenant-2", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(
                TenantId: "tenant-2",
                Name: "Tenant Two",
                Strategy: TenantIsolationStrategy.DatabasePerTenant,
                ConnectionString: "Data Source=tenant2.db"));

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: cs => cs.ShouldBe("Data Source=tenant2.db"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetConnectionStringAsync_DatabasePerTenantWithoutConnectionString_ReturnsDefault()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-3");
        _tenantStore.GetTenantAsync("tenant-3", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(
                TenantId: "tenant-3",
                Name: "Tenant Three",
                Strategy: TenantIsolationStrategy.DatabasePerTenant,
                ConnectionString: null));

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: cs => cs.ShouldBe("Data Source=:memory:"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoDefaultConnectionString_ReturnsLeft()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var emptyOptions = Options.Create(new TenancyOptions { DefaultConnectionString = null });
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, emptyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("No default connection string configured"));
    }

    [Fact]
    public async Task CreateConnectionAsync_ReturnsConnection()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: connection =>
            {
                connection.State.ShouldBe(System.Data.ConnectionState.Open);
                connection.Dispose();
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_NullTenantId_ThrowsException()
    {
        // Arrange
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => factory.CreateConnectionForTenantAsync(null!).AsTask());
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_EmptyTenantId_ThrowsException()
    {
        // Arrange
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => factory.CreateConnectionForTenantAsync(string.Empty).AsTask());
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_TenantNotFound_ReturnsLeft()
    {
        // Arrange
        _tenantStore.GetTenantAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((TenantInfo?)null);

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.CreateConnectionForTenantAsync("nonexistent");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task CreateConnectionForTenantAsync_WithValidTenant_ReturnsConnection()
    {
        // Arrange
        _tenantStore.GetTenantAsync("tenant-1", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(
                TenantId: "tenant-1",
                Name: "Tenant One",
                Strategy: TenantIsolationStrategy.SharedSchema));

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.CreateConnectionForTenantAsync("tenant-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: connection =>
            {
                connection.State.ShouldBe(System.Data.ConnectionState.Open);
                connection.Dispose();
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
        await _tenantStore.Received(1).GetTenantAsync("tenant-1", Arg.Any<CancellationToken>());
    }
}
