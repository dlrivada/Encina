using Encina.Dapper.MySQL.Tenancy;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.MySQL.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantConnectionFactory"/>.
/// </summary>
/// <remarks>
/// The factory fails closed (#3675): a present tenant id the store does not know returns
/// <see cref="TenancyErrors.TenantNotFoundCode"/>, a missing tenant id returns <see cref="TenancyErrors.TenantRequiredCode"/>
/// while <see cref="TenancyOptions.RequireTenant"/> is true, and the default connection string serves a missing tenant only
/// when <see cref="TenancyOptions.RequireTenant"/> is false.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class TenantConnectionFactoryTests
{
    private const string DefaultConnectionString = "Server=localhost;Database=default_db;User=test;Password=test";
    private const string TenantConnectionString = "Server=tenant1;Database=tenant1_db;User=tenant1;Password=secret";
    private const string SpecificConnectionString = "Server=specific;Database=specific_db;User=specific;Password=pass";
    private const string UnknownTenantId = "unknown-tenant";

    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly IOptions<TenancyOptions> _tenancyOptions;

    public TenantConnectionFactoryTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantStore = Substitute.For<ITenantStore>();
        _tenancyOptions = Options.Create(new TenancyOptions
        {
            DefaultConnectionString = DefaultConnectionString
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

    // Replaces GetConnectionStringAsync_NoTenantContext_ReturnsDefaultConnectionString deliberately (#3675, decision D5):
    // with RequireTenant true (the default) a missing tenant is refused instead of reaching the default database.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConnectionStringAsync_NoTenantIdAndRequireTenant_ReturnsTenantRequired(string? tenantId)
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        AssertErrorCode(result, TenancyErrors.TenantRequiredCode);
        await _tenantProvider.DidNotReceive().GetCurrentTenantAsync(Arg.Any<CancellationToken>());
    }

    // The factory treats a blank id as absent with string.IsNullOrWhiteSpace (decision D6), so whitespace also
    // gets the default connection string when RequireTenant is false.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConnectionStringAsync_NoTenantIdAndRequireTenantFalse_ReturnsDefaultConnectionString(string? tenantId)
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);
        var factory = CreateFactory(requireTenant: false);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.ShouldBe(Either<EncinaError, string>.Right(DefaultConnectionString));
        await _tenantProvider.DidNotReceive().GetCurrentTenantAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetConnectionStringAsync_UnknownTenantId_ReturnsTenantNotFound(bool requireTenant)
    {
        // Arrange: no opt-out exists for an unknown tenant (decision D9), whatever RequireTenant says.
        SetCurrentTenant(UnknownTenantId, tenant: null);
        var factory = CreateFactory(requireTenant);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        var error = AssertErrorCode(result, TenancyErrors.TenantNotFoundCode);
        error.GetCategory().ShouldBe(ErrorCategory.NotFound);
        error.Message.ShouldNotContain(UnknownTenantId);
    }

    [Fact]
    public async Task GetConnectionStringAsync_TenantProviderThrows_PropagatesException()
    {
        // Arrange: a store failure propagates and never falls back to the default (decision D10).
        _tenantProvider.GetCurrentTenantId().Returns("tenant-1");
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromException<TenantInfo?>(new InvalidOperationException("store unavailable")));
#pragma warning restore CA2012
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await factory.GetConnectionStringAsync());
    }

    [Fact]
    public async Task GetConnectionStringAsync_SharedSchemaTenant_ReturnsDefaultConnectionString()
    {
        // Arrange
        SetCurrentTenant("tenant-1", new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SharedSchema));
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.ShouldBe(Either<EncinaError, string>.Right(DefaultConnectionString));
    }

    [Fact]
    public async Task GetConnectionStringAsync_DatabasePerTenant_ReturnsTenantConnectionString()
    {
        // Arrange
        SetCurrentTenant("tenant-1", new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: TenantConnectionString));
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.ShouldBe(Either<EncinaError, string>.Right(TenantConnectionString));
    }

    // A known DatabasePerTenant tenant without a connection string keeps the default here; #3686 tracks it.
    [Fact]
    public async Task GetConnectionStringAsync_DatabasePerTenantWithoutConnectionString_ReturnsDefault()
    {
        // Arrange
        SetCurrentTenant("tenant-1", new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.DatabasePerTenant,
            ConnectionString: null));
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.ShouldBe(Either<EncinaError, string>.Right(DefaultConnectionString));
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoDefaultConnectionString_ReturnsLeft()
    {
        // Arrange
        var options = Options.Create(new TenancyOptions { DefaultConnectionString = null, RequireTenant = false });
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
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
    public async Task CreateConnectionAsync_KnownTenant_ReturnsConnection()
    {
        // Arrange
        SetCurrentTenant("tenant-1", new TenantInfo(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Strategy: TenantIsolationStrategy.SharedSchema));
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: connection =>
            {
                connection.ShouldNotBeNull();
                connection.ConnectionString.ShouldBe(DefaultConnectionString);
                connection.Dispose();
            },
            Left: _ => { });
    }

    [Fact]
    public async Task CreateConnectionAsync_UnknownTenantId_ReturnsTenantNotFound()
    {
        // Arrange
        SetCurrentTenant(UnknownTenantId, tenant: null);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        AssertErrorCode(result, TenancyErrors.TenantNotFoundCode);
    }

    [Fact]
    public async Task CreateConnectionAsync_NoTenantIdAndRequireTenant_ReturnsTenantRequired()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _tenancyOptions);

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        AssertErrorCode(result, TenancyErrors.TenantRequiredCode);
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
            ConnectionString: SpecificConnectionString);

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
                connection.ConnectionString.ShouldBe(SpecificConnectionString);
                connection.Dispose();
            },
            Left: _ => { });
    }

    // Updated deliberately (#3675, decision D8): the explicit-id path returns the coded TenancyErrors.TenantNotFound,
    // whose message no longer carries the tenant id.
    [Fact]
    public async Task CreateConnectionForTenantAsync_TenantNotFound_ReturnsTenantNotFound()
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
        var error = AssertErrorCode(result, TenancyErrors.TenantNotFoundCode);
        error.Message.ShouldNotContain("nonexistent");
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

    private TenantConnectionFactory CreateFactory(bool requireTenant) =>
        new(_tenantProvider, _tenantStore, Options.Create(new TenancyOptions
        {
            DefaultConnectionString = DefaultConnectionString,
            RequireTenant = requireTenant
        }));

    private void SetCurrentTenant(string tenantId, TenantInfo? tenant)
    {
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _tenantProvider.GetCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantInfo?>(tenant));
#pragma warning restore CA2012
    }

    private static EncinaError AssertErrorCode<T>(Either<EncinaError, T> result, string expectedCode)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().Single();
        error.GetCode().IfNone(string.Empty).ShouldBe(expectedCode);
        return error;
    }
}
