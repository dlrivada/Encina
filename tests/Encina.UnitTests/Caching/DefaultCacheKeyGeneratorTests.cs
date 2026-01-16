using Encina.Caching;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching;

/// <summary>
/// Unit tests for <see cref="DefaultCacheKeyGenerator"/>.
/// </summary>
public class DefaultCacheKeyGeneratorTests
{
    private readonly DefaultCacheKeyGenerator _sut;
    private readonly CachingOptions _options;

    public DefaultCacheKeyGeneratorTests()
    {
        _options = new CachingOptions { KeyPrefix = "cache" };
        _sut = new DefaultCacheKeyGenerator(Options.Create(_options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DefaultCacheKeyGenerator(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void GenerateKey_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        var act = () => _sut.GenerateKey<TestQuery, string>(null!, context);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public void GenerateKey_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new TestQuery("test");

        // Act & Assert
        var act = () => _sut.GenerateKey<TestQuery, string>(request, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void GenerateKey_WithTenant_IncludesTenantInKey()
    {
        // Arrange
        var request = new TestQuery("test");
        var context = CreateContext(tenantId: "tenant123");

        // Act
        var key = _sut.GenerateKey<TestQuery, string>(request, context);

        // Assert
        key.ShouldStartWith("cache:");
        key.ShouldContain("t:tenant123");
    }

    [Fact]
    public void GenerateKey_WithoutTenant_DoesNotIncludeTenantInKey()
    {
        // Arrange
        var request = new TestQuery("test");
        var context = CreateContext(tenantId: null);

        // Act
        var key = _sut.GenerateKey<TestQuery, string>(request, context);

        // Assert
        key.ShouldStartWith("cache:");
        key.ShouldNotContain("t:");
    }

    [Fact]
    public void GenerateKey_SameRequest_GeneratesSameKey()
    {
        // Arrange
        var request1 = new TestQuery("test");
        var request2 = new TestQuery("test");
        var context = CreateContext();

        // Act
        var key1 = _sut.GenerateKey<TestQuery, string>(request1, context);
        var key2 = _sut.GenerateKey<TestQuery, string>(request2, context);

        // Assert
        key1.ShouldBe(key2);
    }

    [Fact]
    public void GenerateKey_DifferentRequest_GeneratesDifferentKey()
    {
        // Arrange
        var request1 = new TestQuery("test1");
        var request2 = new TestQuery("test2");
        var context = CreateContext();

        // Act
        var key1 = _sut.GenerateKey<TestQuery, string>(request1, context);
        var key2 = _sut.GenerateKey<TestQuery, string>(request2, context);

        // Assert
        key1.ShouldNotBe(key2);
    }

    [Fact]
    public void GenerateKey_WithKeyTemplate_UsesTemplate()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CachedQueryWithTemplate(productId);
        var context = CreateContext(tenantId: "tenant1");

        // Act
        var key = _sut.GenerateKey<CachedQueryWithTemplate, string>(request, context);

        // Assert
        key.ShouldContain("product:");
        key.ShouldContain(productId.ToString("N"));
    }

    [Fact]
    public void GenerateKey_WithVaryByUser_IncludesUserInKey()
    {
        // Arrange
        var request = new CachedQueryVaryByUser("test");
        var context = CreateContext(userId: "user123");

        // Act
        var key = _sut.GenerateKey<CachedQueryVaryByUser, string>(request, context);

        // Assert
        key.ShouldContain("u:user123");
    }

    [Fact]
    public void GeneratePattern_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.GeneratePattern<TestQuery>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void GeneratePattern_WithTenant_IncludesTenantInPattern()
    {
        // Arrange
        var context = CreateContext(tenantId: "tenant1");

        // Act
        var pattern = _sut.GeneratePattern<TestQuery>(context);

        // Assert
        pattern.ShouldContain("t:tenant1");
        pattern.ShouldEndWith("*");
    }

    [Fact]
    public void GeneratePattern_WithoutTenant_IncludesWildcardForTenant()
    {
        // Arrange
        var context = CreateContext(tenantId: null);

        // Act
        var pattern = _sut.GeneratePattern<TestQuery>(context);

        // Assert
        pattern.ShouldContain("t:*");
    }

    [Fact]
    public void GeneratePatternFromTemplate_WithNullKeyTemplate_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        var act = () => _sut.GeneratePatternFromTemplate<TestQuery>(null!, null!, context);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyTemplate");
    }

    [Fact]
    public void GeneratePatternFromTemplate_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.GeneratePatternFromTemplate<TestQuery>("template", null!, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void GeneratePatternFromTemplate_WithTenant_IncludesPrefix()
    {
        // Arrange
        var request = new TestQuery("test");
        var context = CreateContext(tenantId: "tenant1");

        // Act
        var pattern = _sut.GeneratePatternFromTemplate("products:*", request, context);

        // Assert
        pattern.ShouldStartWith("cache:");
        pattern.ShouldContain("t:tenant1");
        pattern.ShouldContain("products:*");
    }

    [Fact]
    public void GenerateKey_DifferentTenants_GeneratesDifferentKeys()
    {
        // Arrange
        var request = new TestQuery("test");
        var context1 = CreateContext(tenantId: "tenant1");
        var context2 = CreateContext(tenantId: "tenant2");

        // Act
        var key1 = _sut.GenerateKey<TestQuery, string>(request, context1);
        var key2 = _sut.GenerateKey<TestQuery, string>(request, context2);

        // Assert
        key1.ShouldNotBe(key2);
    }

    [Fact]
    public void GenerateKey_ComplexRequest_GeneratesConsistentKey()
    {
        // Arrange
        var request = new ComplexQuery(
            Id: Guid.Parse("12345678-1234-1234-1234-123456789abc"),
            Name: "Test",
            Count: 42);
        var context = CreateContext();

        // Act
        var key1 = _sut.GenerateKey<ComplexQuery, string>(request, context);
        var key2 = _sut.GenerateKey<ComplexQuery, string>(request, context);

        // Assert
        key1.ShouldBe(key2);
    }

    private static IRequestContext CreateContext(
        string? tenantId = "default-tenant",
        string? userId = null,
        string correlationId = "corr-123")
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(tenantId ?? string.Empty);
        context.UserId.Returns(userId ?? string.Empty);
        context.CorrelationId.Returns(correlationId);
        return context;
    }

    // Test request without Cache attribute
    private sealed record TestQuery(string Value) : IRequest<string>;

    // Test request with key template
    [Cache(DurationSeconds = 300, KeyTemplate = "product:{ProductId}")]
    private sealed record CachedQueryWithTemplate(Guid ProductId) : IRequest<string>;

    // Test request with VaryByUser
    [Cache(DurationSeconds = 300, VaryByUser = true)]
    private sealed record CachedQueryVaryByUser(string Value) : IRequest<string>;

    // Complex request for hash testing
    private sealed record ComplexQuery(Guid Id, string Name, int Count) : IRequest<string>;
}
