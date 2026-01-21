using Encina.AspNetCore;
using Encina.Tenancy;
using NSubstitute;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for DefaultTenantProvider.
/// </summary>
public class DefaultTenantProviderTests
{
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly ITenantStore _tenantStore;
    private readonly DefaultTenantProvider _provider;

    public DefaultTenantProviderTests()
    {
        _requestContextAccessor = Substitute.For<IRequestContextAccessor>();
        _tenantStore = Substitute.For<ITenantStore>();
        _provider = new DefaultTenantProvider(_requestContextAccessor, _tenantStore);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRequestContextAccessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DefaultTenantProvider(null!, _tenantStore));
    }

    [Fact]
    public void Constructor_NullTenantStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DefaultTenantProvider(_requestContextAccessor, null!));
    }

    #endregion

    #region GetCurrentTenantId Tests

    [Fact]
    public void GetCurrentTenantId_WithTenantContext_ReturnsTenantId()
    {
        // Arrange
        var context = RequestContext.Create().WithTenantId("tenant-123");
        _requestContextAccessor.RequestContext.Returns(context);

        // Act
        var result = _provider.GetCurrentTenantId();

        // Assert
        result.ShouldBe("tenant-123");
    }

    [Fact]
    public void GetCurrentTenantId_WithNullContext_ReturnsNull()
    {
        // Arrange
        _requestContextAccessor.RequestContext.Returns((IRequestContext?)null);

        // Act
        var result = _provider.GetCurrentTenantId();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetCurrentTenantId_WithContextButNoTenantId_ReturnsNull()
    {
        // Arrange
        var context = RequestContext.Create();
        _requestContextAccessor.RequestContext.Returns(context);

        // Act
        var result = _provider.GetCurrentTenantId();

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetCurrentTenantAsync Tests

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern")]
    public async Task GetCurrentTenantAsync_WithTenantContext_ReturnsTenantInfo()
    {
        // Arrange
        var tenantId = "tenant-123";
        var tenantInfo = new TenantInfo(tenantId, "Test Tenant", TenantIsolationStrategy.SharedSchema);
        var context = RequestContext.Create().WithTenantId(tenantId);

        _requestContextAccessor.RequestContext.Returns(context);
        _tenantStore.GetTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<TenantInfo?>(tenantInfo));

        // Act
        var result = await _provider.GetCurrentTenantAsync();

        // Assert
        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.Name.ShouldBe("Test Tenant");
    }

    [Fact]
    public async Task GetCurrentTenantAsync_WithNullContext_ReturnsNull()
    {
        // Arrange
        _requestContextAccessor.RequestContext.Returns((IRequestContext?)null);

        // Act
        var result = await _provider.GetCurrentTenantAsync();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentTenantAsync_WithContextButNoTenantId_ReturnsNull()
    {
        // Arrange
        var context = RequestContext.Create();
        _requestContextAccessor.RequestContext.Returns(context);

        // Act
        var result = await _provider.GetCurrentTenantAsync();

        // Assert
        result.ShouldBeNull();
        await _tenantStore.DidNotReceive().GetTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCurrentTenantAsync_WithEmptyTenantId_ReturnsNull()
    {
        // Arrange
        var context = RequestContext.Create().WithTenantId("");
        _requestContextAccessor.RequestContext.Returns(context);

        // Act
        var result = await _provider.GetCurrentTenantAsync();

        // Assert
        result.ShouldBeNull();
        await _tenantStore.DidNotReceive().GetTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCurrentTenantAsync_WithWhitespaceTenantId_ReturnsNull()
    {
        // Arrange
        var context = RequestContext.Create().WithTenantId("   ");
        _requestContextAccessor.RequestContext.Returns(context);

        // Act
        var result = await _provider.GetCurrentTenantAsync();

        // Assert
        result.ShouldBeNull();
        await _tenantStore.DidNotReceive().GetTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern")]
    public async Task GetCurrentTenantAsync_TenantNotInStore_ReturnsNull()
    {
        // Arrange
        var tenantId = "unknown-tenant";
        var context = RequestContext.Create().WithTenantId(tenantId);

        _requestContextAccessor.RequestContext.Returns(context);
        _tenantStore.GetTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<TenantInfo?>(null));

        // Act
        var result = await _provider.GetCurrentTenantAsync();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern")]
    public async Task GetCurrentTenantAsync_PassesCancellationToken()
    {
        // Arrange
        var tenantId = "tenant-123";
        var tenantInfo = new TenantInfo(tenantId, "Test", TenantIsolationStrategy.SharedSchema);
        var context = RequestContext.Create().WithTenantId(tenantId);
        var cts = new CancellationTokenSource();

        _requestContextAccessor.RequestContext.Returns(context);
        _tenantStore.GetTenantAsync(tenantId, cts.Token)
            .Returns(ValueTask.FromResult<TenantInfo?>(tenantInfo));

        // Act
        var result = await _provider.GetCurrentTenantAsync(cts.Token);

        // Assert
        await _tenantStore.Received(1).GetTenantAsync(tenantId, cts.Token);
    }

    #endregion
}
