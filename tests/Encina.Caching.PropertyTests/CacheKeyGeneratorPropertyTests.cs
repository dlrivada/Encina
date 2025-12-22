using FsCheck;
using FsCheck.Xunit;

namespace Encina.Caching.PropertyTests;

/// <summary>
/// Property-based tests for cache key generation that verify invariants hold for all inputs.
/// </summary>
public sealed class CacheKeyGeneratorPropertyTests
{
    private readonly DefaultCacheKeyGenerator _generator;

    public CacheKeyGeneratorPropertyTests()
    {
        var options = Options.Create(new CachingOptions { KeyPrefix = "test" });
        _generator = new DefaultCacheKeyGenerator(options);
    }

    #region Key Generation Invariants

    [Property(MaxTest = 100)]
    public bool SameRequestProducesSameKey(int id)
    {
        var request1 = new TestRequest(id);
        var request2 = new TestRequest(id);
        var context = CreateContext();

        var key1 = _generator.GenerateKey<TestRequest, string>(request1, context);
        var key2 = _generator.GenerateKey<TestRequest, string>(request2, context);

        return key1 == key2;
    }

    [Property(MaxTest = 100)]
    public bool DifferentRequestsProduceDifferentKeys(int id)
    {
        if (id >= int.MaxValue) return true;

        var request1 = new TestRequest(id);
        var request2 = new TestRequest(id + 1);
        var context = CreateContext();

        var key1 = _generator.GenerateKey<TestRequest, string>(request1, context);
        var key2 = _generator.GenerateKey<TestRequest, string>(request2, context);

        return key1 != key2;
    }

    [Property(MaxTest = 100)]
    public bool KeysContainPrefix(int id)
    {
        var request = new TestRequest(id);
        var context = CreateContext();

        var key = _generator.GenerateKey<TestRequest, string>(request, context);

        return key.StartsWith("test:", StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool KeysContainTypeName(int id)
    {
        var request = new TestRequest(id);
        var context = CreateContext();

        var key = _generator.GenerateKey<TestRequest, string>(request, context);

        return key.Contains("TestRequest", StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool KeysAreNonEmpty(int id)
    {
        var request = new TestRequest(id);
        var context = CreateContext();

        var key = _generator.GenerateKey<TestRequest, string>(request, context);

        return !string.IsNullOrEmpty(key);
    }

    #endregion

    #region Tenant Isolation Invariants

    [Property(MaxTest = 100)]
    public bool DifferentTenantsProduceDifferentKeys(int id, NonEmptyString tenant1, NonEmptyString tenant2)
    {
        if (tenant1.Get == tenant2.Get) return true;

        var request = new TestRequest(id);
        var context1 = CreateContext(tenantId: tenant1.Get);
        var context2 = CreateContext(tenantId: tenant2.Get);

        var key1 = _generator.GenerateKey<TestRequest, string>(request, context1);
        var key2 = _generator.GenerateKey<TestRequest, string>(request, context2);

        return key1 != key2;
    }

    [Property(MaxTest = 100)]
    public bool SameTenantProducesSameKey(int id, NonEmptyString tenant)
    {
        var request = new TestRequest(id);
        var context1 = CreateContext(tenantId: tenant.Get);
        var context2 = CreateContext(tenantId: tenant.Get);

        var key1 = _generator.GenerateKey<TestRequest, string>(request, context1);
        var key2 = _generator.GenerateKey<TestRequest, string>(request, context2);

        return key1 == key2;
    }

    #endregion

    #region Pattern Generation Invariants

    [Property(MaxTest = 100)]
    public bool PatternContainsWildcard(NonEmptyString tenant)
    {
        var context = CreateContext(tenantId: tenant.Get);

        var pattern = _generator.GeneratePattern<TestRequest>(context);

        return pattern.Contains('*');
    }

    [Property(MaxTest = 100)]
    public bool PatternContainsPrefix(NonEmptyString tenant)
    {
        var context = CreateContext(tenantId: tenant.Get);

        var pattern = _generator.GeneratePattern<TestRequest>(context);

        return pattern.StartsWith("test:", StringComparison.Ordinal);
    }

    #endregion

    #region Determinism Invariants

    [Property(MaxTest = 100)]
    public bool KeyGenerationIsDeterministic(int id, PositiveInt iterations)
    {
        var actualIterations = Math.Min(iterations.Get % 10 + 1, 10);
        var request = new TestRequest(id);
        var context = CreateContext();

        var firstKey = _generator.GenerateKey<TestRequest, string>(request, context);

        for (var i = 1; i < actualIterations; i++)
        {
            var nextKey = _generator.GenerateKey<TestRequest, string>(request, context);
            if (firstKey != nextKey) return false;
        }

        return true;
    }

    #endregion

    private static TestRequestContext CreateContext(string? tenantId = null, string? userId = null)
    {
        return new TestRequestContext(tenantId ?? "default-tenant", userId ?? "default-user");
    }

    [Cache(DurationSeconds = 300)]
    private sealed record TestRequest(int Id) : IRequest<string>;

    private sealed class TestRequestContext : IRequestContext
    {
        public TestRequestContext(string tenantId, string userId)
        {
            TenantId = tenantId;
            UserId = userId;
        }

        public string? TenantId { get; }
        public string? UserId { get; }
        public string CorrelationId => "test-correlation";
        public string? IdempotencyKey => null;
        public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
        public IReadOnlyDictionary<string, object?> Metadata => new Dictionary<string, object?>();

        public IRequestContext WithMetadata(string key, object? value) => this;
        public IRequestContext WithUserId(string? userId) => new TestRequestContext(TenantId!, userId!);
        public IRequestContext WithIdempotencyKey(string? idempotencyKey) => this;
        public IRequestContext WithTenantId(string? tenantId) => new TestRequestContext(tenantId!, UserId!);
    }
}
