namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantConnectionFactoryBase{TConnection}"/> and <see cref="TenancyErrors"/>:
/// the fail-closed tenant resolution of #3673.
/// </summary>
public sealed class TenantConnectionFactoryBaseTests
{
    private const string DefaultConnectionString = "Server=default;Database=Shared;";
    private const string TenantConnectionString = "Server=acme;Database=Acme;";
    private const string UnknownTenantId = "unknown-tenant-7f3a";

    private readonly ITenantProvider _tenantProvider = Substitute.For<ITenantProvider>();
    private readonly ITenantStore _tenantStore = Substitute.For<ITenantStore>();

    #region Constructor

    [Fact]
    public void Constructor_NullTenantProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new FakeFactory(
            null!, _tenantStore, Options.Create(new TenantConnectionOptions()), Options.Create(new TenancyOptions())));
    }

    [Fact]
    public void Constructor_NullTenantStore_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new FakeFactory(
            _tenantProvider, null!, Options.Create(new TenantConnectionOptions()), Options.Create(new TenancyOptions())));
    }

    [Fact]
    public void Constructor_NullConnectionOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new FakeFactory(
            _tenantProvider, _tenantStore, null!, Options.Create(new TenancyOptions())));
    }

    [Fact]
    public void Constructor_NullTenancyOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new FakeFactory(
            _tenantProvider, _tenantStore, Options.Create(new TenantConnectionOptions()), null!));
    }

    #endregion

    #region Unknown tenant (fail closed)

    [Fact]
    public async Task CreateConnectionAsync_UnknownCurrentTenant_ReturnsTenantNotFoundWithoutConnection()
    {
        _tenantProvider.GetCurrentTenantId().Returns(UnknownTenantId);
        _tenantStore.GetTenantAsync(UnknownTenantId, Arg.Any<CancellationToken>()).Returns((TenantInfo?)null);
        var factory = CreateFactory(requireTenant: true);

        var result = await factory.CreateConnectionAsync();

        AssertTenantNotFound(result);
        await _tenantStore.Received(1).GetTenantAsync(UnknownTenantId, Arg.Any<CancellationToken>());
        factory.CreatedConnections.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateConnectionAsync_UnknownExplicitTenant_ReturnsTenantNotFoundWithoutConnection()
    {
        _tenantStore.GetTenantAsync(UnknownTenantId, Arg.Any<CancellationToken>()).Returns((TenantInfo?)null);
        var factory = CreateFactory(requireTenant: true);

        var result = await factory.CreateConnectionAsync(UnknownTenantId);

        AssertTenantNotFound(result);
        await _tenantStore.Received(1).GetTenantAsync(UnknownTenantId, Arg.Any<CancellationToken>());
        _tenantProvider.DidNotReceive().GetCurrentTenantId();
        factory.CreatedConnections.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetConnectionStringAsync_UnknownTenantWithRequireTenantFalse_StillReturnsTenantNotFound()
    {
        _tenantStore.GetTenantAsync(UnknownTenantId, Arg.Any<CancellationToken>()).Returns((TenantInfo?)null);
        var factory = CreateFactory(requireTenant: false);

        var result = await factory.GetConnectionStringAsync(UnknownTenantId);

        AssertTenantNotFound(result);
    }

    [Fact]
    public async Task GetConnectionStringAsync_StoreThrows_ExceptionPropagates()
    {
#pragma warning disable CA2012 // Use ValueTasks correctly - required for NSubstitute mocking pattern
        _tenantStore.GetTenantAsync(UnknownTenantId, Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromException<TenantInfo?>(new InvalidOperationException("store down")));
#pragma warning restore CA2012
        var factory = CreateFactory(requireTenant: true);

        await Should.ThrowAsync<InvalidOperationException>(async () => await factory.GetConnectionStringAsync(UnknownTenantId));
    }

    #endregion

    #region No tenant

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateConnectionAsync_NoTenantAndRequireTenant_ReturnsTenantRequiredWithoutConnection(string? currentTenantId)
    {
        _tenantProvider.GetCurrentTenantId().Returns(currentTenantId);
        var factory = CreateFactory(requireTenant: true);

        var result = await factory.CreateConnectionAsync();

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        error.GetCode().ShouldBe(Prelude.Some(TenancyErrors.TenantRequiredCode));
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
        factory.CreatedConnections.ShouldBeEmpty();
        await _tenantStore.DidNotReceiveWithAnyArgs().GetTenantAsync(default!, default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConnectionStringAsync_ExplicitBlankTenantAndRequireTenant_ReturnsTenantRequired(string explicitTenantId)
    {
        _tenantProvider.GetCurrentTenantId().Returns("acme");
        var factory = CreateFactory(requireTenant: true);

        var result = await factory.GetConnectionStringAsync(explicitTenantId);

        result.IsLeft.ShouldBeTrue();
        result.LeftToSeq().Single().GetCode().ShouldBe(Prelude.Some(TenancyErrors.TenantRequiredCode));
        await _tenantStore.DidNotReceiveWithAnyArgs().GetTenantAsync(default!, default);
    }

    [Fact]
    public async Task GetConnectionStringAsync_PassesCancellationTokenToStore()
    {
        using var cts = new CancellationTokenSource();
        _tenantStore.GetTenantAsync("acme", cts.Token)
            .Returns(new TenantInfo("acme", "Acme", TenantIsolationStrategy.DatabasePerTenant, TenantConnectionString));
        var factory = CreateFactory(requireTenant: true);

        var result = await factory.GetConnectionStringAsync("acme", cts.Token);

        result.ShouldBe(Prelude.Right<EncinaError, string>(TenantConnectionString));
        await _tenantStore.Received(1).GetTenantAsync("acme", cts.Token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConnectionStringAsync_NoTenantAndRequireTenantFalse_ReturnsDefaultConnectionString(string? currentTenantId)
    {
        _tenantProvider.GetCurrentTenantId().Returns(currentTenantId);
        var factory = CreateFactory(requireTenant: false);

        var result = await factory.GetConnectionStringAsync();

        result.ShouldBe(Prelude.Right<EncinaError, string>(DefaultConnectionString));
    }

    [Fact]
    public async Task GetConnectionStringAsync_NoTenantRequireTenantFalseAndNoDefault_ReturnsLeft()
    {
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var factory = CreateFactory(requireTenant: false, defaultConnectionString: null);

        var result = await factory.GetConnectionStringAsync();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Known tenant

    [Fact]
    public async Task CreateConnectionAsync_KnownTenantWithConnectionString_CreatesConnectionForTenant()
    {
        _tenantProvider.GetCurrentTenantId().Returns("acme");
        _tenantStore.GetTenantAsync("acme", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo("acme", "Acme", TenantIsolationStrategy.DatabasePerTenant, TenantConnectionString));
        var factory = CreateFactory(requireTenant: true);

        var result = await factory.CreateConnectionAsync();

        result.IsRight.ShouldBeTrue();
        factory.CreatedConnections.ShouldBe([TenantConnectionString]);
    }

    [Fact]
    public async Task GetConnectionStringAsync_KnownTenantWithoutConnectionString_ReturnsDefault()
    {
        _tenantStore.GetTenantAsync("acme", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo("acme", "Acme", TenantIsolationStrategy.SharedSchema));
        var factory = CreateFactory(requireTenant: true);

        var result = await factory.GetConnectionStringAsync("acme");

        result.ShouldBe(Prelude.Right<EncinaError, string>(DefaultConnectionString));
    }

    #endregion

    #region TenancyErrors

    [Fact]
    public void TenantNotFound_HasStableCodeNotFoundCategoryAndNoTenantId()
    {
        var error = TenancyErrors.TenantNotFound();

        TenancyErrors.TenantNotFoundCode.ShouldBe("tenancy.tenant_not_found");
        error.GetCode().ShouldBe(Prelude.Some("tenancy.tenant_not_found"));
        error.GetCategory().ShouldBe(ErrorCategory.NotFound);
        error.Message.ShouldNotContain(UnknownTenantId);
    }

    [Fact]
    public void TenantRequired_HasStableCodeAndInvalidRequestCategory()
    {
        var error = TenancyErrors.TenantRequired();

        TenancyErrors.TenantRequiredCode.ShouldBe("tenancy.tenant_required");
        error.GetCode().ShouldBe(Prelude.Some("tenancy.tenant_required"));
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
    }

    #endregion

    private FakeFactory CreateFactory(bool requireTenant, string? defaultConnectionString = DefaultConnectionString) =>
        new(
            _tenantProvider,
            _tenantStore,
            Options.Create(new TenantConnectionOptions { DefaultConnectionString = defaultConnectionString }),
            Options.Create(new TenancyOptions { RequireTenant = requireTenant }));

    private static void AssertTenantNotFound<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Single();
        error.GetCode().ShouldBe(Prelude.Some(TenancyErrors.TenantNotFoundCode));
        error.GetCategory().ShouldBe(ErrorCategory.NotFound);
        error.Message.ShouldNotContain(UnknownTenantId);
    }

    private sealed class FakeFactory(
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        IOptions<TenantConnectionOptions> options,
        IOptions<TenancyOptions> tenancyOptions)
        : TenantConnectionFactoryBase<FakeConnection>(tenantProvider, tenantStore, options, tenancyOptions)
    {
        public List<string> CreatedConnections { get; } = [];

        protected override ValueTask<FakeConnection> CreateConnectionCoreAsync(
            string connectionString,
            CancellationToken cancellationToken)
        {
            CreatedConnections.Add(connectionString);
            return ValueTask.FromResult(new FakeConnection(connectionString));
        }
    }

    private sealed class FakeConnection(string connectionString) : IDisposable
    {
        public string ConnectionString { get; } = connectionString;

        public void Dispose()
        {
        }
    }
}
