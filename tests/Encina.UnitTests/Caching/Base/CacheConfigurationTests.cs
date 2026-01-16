using Encina.Caching;
namespace Encina.UnitTests.Caching.Base;

/// <summary>
/// Unit tests for <see cref="CacheConfiguration{TRequest}"/>.
/// </summary>
public class CacheConfigurationTests
{
    #region Default Values Tests

    [Fact]
    public void CacheConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new CacheConfiguration<TestQuery>();

        // Assert
        config.Duration.ShouldBe(TimeSpan.FromMinutes(5));
        config.SlidingExpiration.ShouldBeFalse();
        config.MaxAbsoluteExpiration.ShouldBeNull();
        config.Priority.ShouldBe(CachePriority.Normal);
        config.VaryByUser.ShouldBeFalse();
        config.VaryByTenant.ShouldBeTrue();
        config.KeyGenerator.ShouldBeNull();
    }

    [Fact]
    public void CacheConfiguration_CanSetAllProperties()
    {
        // Arrange & Act
        var config = new CacheConfiguration<TestQuery>
        {
            Duration = TimeSpan.FromMinutes(10),
            SlidingExpiration = true,
            MaxAbsoluteExpiration = TimeSpan.FromHours(1),
            Priority = CachePriority.High,
            VaryByUser = true,
            VaryByTenant = false,
            KeyGenerator = (q, ctx) => $"custom:{q.Id}"
        };

        // Assert
        config.Duration.ShouldBe(TimeSpan.FromMinutes(10));
        config.SlidingExpiration.ShouldBeTrue();
        config.MaxAbsoluteExpiration.ShouldBe(TimeSpan.FromHours(1));
        config.Priority.ShouldBe(CachePriority.High);
        config.VaryByUser.ShouldBeTrue();
        config.VaryByTenant.ShouldBeFalse();
        config.KeyGenerator.ShouldNotBeNull();
    }

    #endregion

    #region GenerateKey Tests

    [Fact]
    public void GenerateKey_WithCustomKeyGenerator_UsesCustomGenerator()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            KeyGenerator = (query, ctx) => $"product:{query.Id}"
        };
        var query = new TestQuery(Guid.NewGuid());
        var context = CreateRequestContext();

        // Act
        var key = config.GenerateKey(query, context);

        // Assert
        key.ShouldBe($"product:{query.Id}");
    }

    [Fact]
    public void GenerateKey_WithoutCustomGenerator_UsesDefaultGeneration()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = false,
            VaryByUser = false
        };
        var query = new TestQuery(Guid.NewGuid());
        var context = CreateRequestContext();

        // Act
        var key = config.GenerateKey(query, context);

        // Assert
        key.ShouldContain("TestQuery");
        key.ShouldNotBeEmpty();
    }

    [Fact]
    public void GenerateKey_WithVaryByTenant_IncludesTenantInKey()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = true,
            VaryByUser = false
        };
        var query = new TestQuery(Guid.NewGuid());
        var context = CreateRequestContext(tenantId: "tenant-123");

        // Act
        var key = config.GenerateKey(query, context);

        // Assert
        key.ShouldContain("t:tenant-123");
    }

    [Fact]
    public void GenerateKey_WithVaryByUser_IncludesUserInKey()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = false,
            VaryByUser = true
        };
        var query = new TestQuery(Guid.NewGuid());
        var context = CreateRequestContext(userId: "user-456");

        // Act
        var key = config.GenerateKey(query, context);

        // Assert
        key.ShouldContain("u:user-456");
    }

    [Fact]
    public void GenerateKey_WithBothVaryByOptions_IncludesBothInKey()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = true,
            VaryByUser = true
        };
        var query = new TestQuery(Guid.NewGuid());
        var context = CreateRequestContext(tenantId: "tenant-abc", userId: "user-xyz");

        // Act
        var key = config.GenerateKey(query, context);

        // Assert
        key.ShouldContain("t:tenant-abc");
        key.ShouldContain("u:user-xyz");
    }

    [Fact]
    public void GenerateKey_WithEmptyTenantId_DoesNotIncludeTenantPart()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = true,
            VaryByUser = false
        };
        var query = new TestQuery(Guid.NewGuid());
        var context = CreateRequestContext(tenantId: string.Empty);

        // Act
        var key = config.GenerateKey(query, context);

        // Assert
        key.ShouldNotContain("t:");
    }

    [Fact]
    public void GenerateKey_WithEmptyUserId_DoesNotIncludeUserPart()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = false,
            VaryByUser = true
        };
        var query = new TestQuery(Guid.NewGuid());
        var context = CreateRequestContext(userId: string.Empty);

        // Act
        var key = config.GenerateKey(query, context);

        // Assert
        key.ShouldNotContain("u:");
    }

    [Fact]
    public void GenerateKey_WithNullRequest_HandlesGracefully()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = false,
            VaryByUser = false
        };
        var context = CreateRequestContext();

        // Act
        var key = config.GenerateKey(null!, context);

        // Assert
        key.ShouldContain("TestQuery");
        key.ShouldContain("00000000"); // Hash of 0
    }

    [Fact]
    public void GenerateKey_SameRequest_ProducesSameKey()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = true,
            VaryByUser = false
        };
        var queryId = Guid.NewGuid();
        var query1 = new TestQuery(queryId);
        var query2 = new TestQuery(queryId);
        var context = CreateRequestContext(tenantId: "tenant-1");

        // Act
        var key1 = config.GenerateKey(query1, context);
        var key2 = config.GenerateKey(query2, context);

        // Assert
        key1.ShouldBe(key2);
    }

    [Fact]
    public void GenerateKey_DifferentRequests_ProduceDifferentKeys()
    {
        // Arrange
        var config = new CacheConfiguration<TestQuery>
        {
            VaryByTenant = false,
            VaryByUser = false
        };
        var query1 = new TestQuery(Guid.NewGuid());
        var query2 = new TestQuery(Guid.NewGuid());
        var context = CreateRequestContext();

        // Act
        var key1 = config.GenerateKey(query1, context);
        var key2 = config.GenerateKey(query2, context);

        // Assert
        key1.ShouldNotBe(key2);
    }

    #endregion

    #region ICacheConfiguration Interface Tests

    [Fact]
    public void CacheConfiguration_ImplementsICacheConfiguration()
    {
        // Arrange & Act
        ICacheConfiguration<TestQuery> config = new CacheConfiguration<TestQuery>();

        // Assert
        config.ShouldNotBeNull();
        config.Duration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    #endregion

    #region Helper Methods

    private static IRequestContext CreateRequestContext(
        string tenantId = "default-tenant",
        string userId = "default-user")
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(tenantId);
        context.UserId.Returns(userId);
        context.CorrelationId.Returns(Guid.NewGuid().ToString());
        return context;
    }

    #endregion

    #region Test Types

    private sealed record TestQuery(Guid Id) : IRequest<string>;

    #endregion
}
