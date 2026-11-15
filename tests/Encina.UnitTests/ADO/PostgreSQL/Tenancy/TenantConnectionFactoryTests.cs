using Encina;
using Encina.ADO.PostgreSQL.Tenancy;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.PostgreSQL.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantConnectionFactory"/>.
/// </summary>
/// <remarks>
/// The fail-closed tests (#3674) use a default connection string that cannot be opened: a <c>Left</c>
/// returned without an exception proves that no connection was created for an unknown or missing tenant.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class TenantConnectionFactoryTests
{
    private const string DefaultConnectionString = "Host=localhost;Database=DefaultDb;Username=test;Password=test";

    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly IOptions<TenancyOptions> _options;

    public TenantConnectionFactoryTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantStore = Substitute.For<ITenantStore>();
        _options = CreateOptions(requireTenant: true);
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

    // #3674: a missing tenant id used to fall back to the default database; under RequireTenant
    // (the default) it is now refused. Null, empty and whitespace ids are all "absent" (IsNullOrWhiteSpace).
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConnectionStringAsync_NoTenantIdAndRequireTenant_ReturnsTenantRequired(string? tenantId)
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        AssertLeftWithCode(result, TenancyErrors.TenantRequiredCode);
        await _tenantStore.DidNotReceiveWithAnyArgs().GetTenantAsync(default!, default);
    }

    // With RequireTenant = false the class treats null, empty and whitespace ids alike
    // (string.IsNullOrWhiteSpace) and uses the default connection string without asking the store.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConnectionStringAsync_NoTenantIdAndTenantNotRequired_ReturnsDefaultConnectionString(string? tenantId)
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns(tenantId);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, CreateOptions(requireTenant: false));

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.Match(
            Right: cs => cs.ShouldBe(DefaultConnectionString),
            Left: _ => throw new InvalidOperationException("Expected Right"));
        await _tenantStore.DidNotReceiveWithAnyArgs().GetTenantAsync(default!, default);
    }

    // #3674: an unknown tenant id used to fall back to the default database; it is now refused
    // whatever RequireTenant says, with a coded error that carries no tenant id.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetConnectionStringAsync_UnknownTenant_ReturnsTenantNotFound(bool requireTenant)
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("unknown-tenant");
        _tenantStore.GetTenantAsync("unknown-tenant", Arg.Any<CancellationToken>())
            .Returns((TenantInfo?)null);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, CreateOptions(requireTenant));

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        var error = AssertLeftWithCode(result, TenancyErrors.TenantNotFoundCode);
        error.Message.ShouldNotContain("unknown-tenant");
    }

    [Fact]
    public async Task GetConnectionStringAsync_TenantStoreThrows_PropagatesException()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("tenant-1");
        _tenantStore.GetTenantAsync("tenant-1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("store unavailable"));
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => factory.GetConnectionStringAsync().AsTask());
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
            Right: cs => cs.ShouldBe(DefaultConnectionString),
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
                ConnectionString: "Host=localhost;Database=Tenant2Db;Username=test;Password=test"));

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.GetConnectionStringAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: cs => cs.ShouldBe("Host=localhost;Database=Tenant2Db;Username=test;Password=test"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    // A known DatabasePerTenant tenant without a connection string keeps the default database;
    // whether it should fail closed is tracked separately (#3686), not by #3674.
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
            Right: cs => cs.ShouldBe(DefaultConnectionString),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoDefaultConnectionString_ReturnsLeft()
    {
        // Arrange: RequireTenant = false so a missing tenant reaches the default connection string lookup
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var emptyOptions = Options.Create(new TenancyOptions { DefaultConnectionString = null, RequireTenant = false });
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
    public async Task CreateConnectionAsync_NoTenantAndTenantNotRequired_AttemptsToOpenDefaultConnection()
    {
        // Arrange: RequireTenant = false resolves the default connection string, which cannot be opened here
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, CreateOptions(requireTenant: false));

        // Act & Assert: the open attempt throws because no server accepts these credentials, proving the default was used
        await Should.ThrowAsync<Exception>(() => factory.CreateConnectionAsync().AsTask());
    }

    [Fact]
    public async Task CreateConnectionAsync_NoTenantAndRequireTenant_ReturnsTenantRequiredWithoutOpening()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        AssertLeftWithCode(result, TenancyErrors.TenantRequiredCode);
    }

    [Fact]
    public async Task CreateConnectionAsync_UnknownTenant_ReturnsTenantNotFoundWithoutOpening()
    {
        // Arrange
        _tenantProvider.GetCurrentTenantId().Returns("unknown-tenant");
        _tenantStore.GetTenantAsync("unknown-tenant", Arg.Any<CancellationToken>())
            .Returns((TenantInfo?)null);
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.CreateConnectionAsync();

        // Assert
        AssertLeftWithCode(result, TenancyErrors.TenantNotFoundCode);
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateConnectionForTenantAsync_BlankTenantId_ThrowsArgumentException(string tenantId)
    {
        // Arrange
        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => factory.CreateConnectionForTenantAsync(tenantId).AsTask());
        await _tenantStore.DidNotReceiveWithAnyArgs().GetTenantAsync(default!, default);
    }

    // #3674: the explicit-id path used to answer "Tenant '<id>' not found."; it now returns the same
    // coded error as the current-tenant path, whose message carries no tenant id.
    [Fact]
    public async Task CreateConnectionForTenantAsync_TenantNotFound_ReturnsTenantNotFound()
    {
        // Arrange
        _tenantStore.GetTenantAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((TenantInfo?)null);

        var factory = new TenantConnectionFactory(_tenantProvider, _tenantStore, _options);

        // Act
        var result = await factory.CreateConnectionForTenantAsync("nonexistent");

        // Assert
        var error = AssertLeftWithCode(result, TenancyErrors.TenantNotFoundCode);
        error.Message.ShouldNotContain("nonexistent");
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

        // Act - This will throw because the connection string is not valid for actual connection
        await Should.ThrowAsync<Exception>(
            () => factory.CreateConnectionForTenantAsync("tenant-1").AsTask());

        // The test verifies that the tenant is looked up and connection string is resolved
        await _tenantStore.Received(1).GetTenantAsync("tenant-1", Arg.Any<CancellationToken>());
    }

    private static IOptions<TenancyOptions> CreateOptions(bool requireTenant) =>
        Options.Create(new TenancyOptions
        {
            DefaultConnectionString = DefaultConnectionString,
            RequireTenant = requireTenant
        });

    private static EncinaError AssertLeftWithCode<T>(Either<EncinaError, T> result, string expectedCode)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        error.GetCode().ShouldBe(Prelude.Some(expectedCode));
        return error;
    }
}
