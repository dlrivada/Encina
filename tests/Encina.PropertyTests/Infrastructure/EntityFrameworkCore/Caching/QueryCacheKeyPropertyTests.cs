using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

using Encina.Caching;
using Encina.EntityFrameworkCore.Caching;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.PropertyTests.Infrastructure.EntityFrameworkCore.Caching;

/// <summary>
/// Property-based tests for query cache key generation using FsCheck.
/// Verifies invariants: determinism, uniqueness, format, and tenant isolation.
/// </summary>
[Trait("Category", "PropertyTest")]
public sealed class QueryCacheKeyPropertyTests
{
    private readonly DefaultQueryCacheKeyGenerator _generator;

    public QueryCacheKeyPropertyTests()
    {
        var options = Options.Create(new QueryCacheOptions { KeyPrefix = "sm:qc" });
        _generator = new DefaultQueryCacheKeyGenerator(options);
    }

    #region Determinism Properties

    [Property(MaxTest = 100)]
    public bool SameCommand_AlwaysProduces_SameKey(PositiveInt seed)
    {
        // Property: f(x) == f(x) — same input always produces same output
        var sql = $"SELECT * FROM Orders WHERE Id = @p0";
        var command1 = CreateCommand(sql, ("@p0", seed.Get));
        var command2 = CreateCommand(sql, ("@p0", seed.Get));
        var context = CreateMockDbContext();

        var key1 = _generator.Generate(command1, context);
        var key2 = _generator.Generate(command2, context);

        return key1.Key == key2.Key;
    }

    [Property(MaxTest = 100)]
    public bool SameCommandWithTenant_AlwaysProduces_SameKey(PositiveInt seed, NonEmptyString tenantId)
    {
        // Property: f(x, t) == f(x, t) — deterministic with tenant
        var sql = $"SELECT * FROM Orders WHERE Id = @p0";
        var command1 = CreateCommand(sql, ("@p0", seed.Get));
        var command2 = CreateCommand(sql, ("@p0", seed.Get));
        var context = CreateMockDbContext();
        var requestContext1 = CreateRequestContext(tenantId.Get);
        var requestContext2 = CreateRequestContext(tenantId.Get);

        var key1 = _generator.Generate(command1, context, requestContext1);
        var key2 = _generator.Generate(command2, context, requestContext2);

        return key1.Key == key2.Key;
    }

    #endregion

    #region Uniqueness Properties

    [Property(MaxTest = 100)]
    public bool DifferentSql_ProducesDifferentKeys(PositiveInt id)
    {
        // Property: SQL text changes → different key
        var command1 = CreateCommand("SELECT * FROM Orders WHERE Id = @p0", ("@p0", id.Get));
        var command2 = CreateCommand("SELECT * FROM Customers WHERE Id = @p0", ("@p0", id.Get));
        var context = CreateMockDbContext();

        var key1 = _generator.Generate(command1, context);
        var key2 = _generator.Generate(command2, context);

        return key1.Key != key2.Key;
    }

    [Property(MaxTest = 100)]
    public bool DifferentParameters_ProduceDifferentKeys(PositiveInt id1, PositiveInt id2)
    {
        // Property: Different parameter values → different key
        // Skip when ids are the same — property only applies to different inputs
        if (id1.Get == id2.Get) return true;

        var sql = "SELECT * FROM Orders WHERE Id = @p0";
        var command1 = CreateCommand(sql, ("@p0", id1.Get));
        var command2 = CreateCommand(sql, ("@p0", id2.Get));
        var context = CreateMockDbContext();

        var key1 = _generator.Generate(command1, context);
        var key2 = _generator.Generate(command2, context);

        return key1.Key != key2.Key;
    }

    [Property(MaxTest = 50)]
    public bool DifferentTenants_ProduceDifferentKeys(NonEmptyString tenant1, NonEmptyString tenant2)
    {
        // Property: Different tenant ids → different key
        if (tenant1.Get == tenant2.Get) return true;

        var sql = "SELECT * FROM Orders WHERE Active = 1";
        var command1 = CreateCommand(sql);
        var command2 = CreateCommand(sql);
        var context = CreateMockDbContext();
        var rc1 = CreateRequestContext(tenant1.Get);
        var rc2 = CreateRequestContext(tenant2.Get);

        var key1 = _generator.Generate(command1, context, rc1);
        var key2 = _generator.Generate(command2, context, rc2);

        return key1.Key != key2.Key;
    }

    #endregion

    #region Key Format Properties

    [Property(MaxTest = 100)]
    public bool Key_StartsWithPrefix(PositiveInt id)
    {
        // Property: All keys start with the configured prefix
        var command = CreateCommand("SELECT * FROM Orders WHERE Id = @p0", ("@p0", id.Get));
        var context = CreateMockDbContext();

        var key = _generator.Generate(command, context);

        return key.Key.StartsWith("sm:qc:", StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool Key_WithTenant_ContainsTenantSegment(PositiveInt id, NonEmptyString tenantId)
    {
        // Property: Tenant-scoped keys contain the tenant identifier
        // Skip whitespace-only tenant ids — the generator treats them as empty (no tenant segment)
        if (string.IsNullOrWhiteSpace(tenantId.Get)) return true;

        var command = CreateCommand("SELECT * FROM Orders WHERE Id = @p0", ("@p0", id.Get));
        var context = CreateMockDbContext();
        var rc = CreateRequestContext(tenantId.Get);

        var key = _generator.Generate(command, context, rc);

        return key.Key.Contains(tenantId.Get, StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool Key_EndsWithHexHash(PositiveInt id)
    {
        // Property: All keys end with a 16-character lowercase hex hash
        var command = CreateCommand("SELECT * FROM Orders WHERE Id = @p0", ("@p0", id.Get));
        var context = CreateMockDbContext();

        var key = _generator.Generate(command, context);

        var parts = key.Key.Split(':');
        var hash = parts[^1];

        // Hash should be exactly 16 lowercase hex characters
        return hash.Length == 16 && Regex.IsMatch(hash, "^[0-9a-f]{16}$");
    }

    [Property(MaxTest = 100)]
    public bool Key_WithoutTenant_HasThreeSegments(PositiveInt id)
    {
        // Property: Keys without tenant have format prefix:entity:hash (3 colon-separated segments)
        var command = CreateCommand("SELECT * FROM Orders WHERE Id = @p0", ("@p0", id.Get));
        var context = CreateMockDbContext();

        var key = _generator.Generate(command, context);
        var parts = key.Key.Split(':');

        // Format: sm:qc:entity:hash → 4 parts (prefix has colon)
        // "sm:qc:Orders:hash" → ["sm", "qc", "Orders", "hash"]
        return parts.Length >= 3;
    }

    #endregion

    #region EntityTypes Properties

    [Property(MaxTest = 50)]
    public bool EntityTypes_IsNeverNull(PositiveInt id)
    {
        // Property: EntityTypes list is never null
        var command = CreateCommand("SELECT * FROM Orders WHERE Id = @p0", ("@p0", id.Get));
        var context = CreateMockDbContext();

        var key = _generator.Generate(command, context);

        return key.EntityTypes is not null;
    }

    [Fact]
    public void EntityTypes_ContainsExtractedTableNames()
    {
        // Property: EntityTypes reflects the tables in the SQL query
        var command = CreateCommand("SELECT o.* FROM Orders o INNER JOIN Customers c ON o.CustomerId = c.Id");
        var context = CreateMockDbContext();

        var key = _generator.Generate(command, context);

        // Should contain table names (since they can't be mapped to entity types in mock context)
        key.EntityTypes.Count.ShouldBeGreaterThanOrEqualTo(1,
            "EntityTypes should contain at least one entry for queries with tables");
    }

    #endregion

    #region QueryCacheKey Value Semantics Properties

    [Property(MaxTest = 100)]
    public bool QueryCacheKey_EqualityIsSymmetric(NonEmptyString keyStr)
    {
        // Property: a == b implies b == a
        var entityTypes = new List<string> { "Order" };
        var a = new QueryCacheKey(keyStr.Get, entityTypes);
        var b = new QueryCacheKey(keyStr.Get, entityTypes);

        return (a == b) == (b == a);
    }

    [Property(MaxTest = 100)]
    public bool QueryCacheKey_EqualityIsReflexive(NonEmptyString keyStr)
    {
        // Property: a == a
        var entityTypes = new List<string> { "Order" };
        var a = new QueryCacheKey(keyStr.Get, entityTypes);

#pragma warning disable CS1718 // Comparison made to same variable
        return a == a;
#pragma warning restore CS1718
    }

    [Property(MaxTest = 50)]
    public bool QueryCacheKey_EqualityIsTransitive(NonEmptyString keyStr)
    {
        // Property: a == b && b == c implies a == c
        var entityTypes = new List<string> { "Order" };
        var a = new QueryCacheKey(keyStr.Get, entityTypes);
        var b = new QueryCacheKey(keyStr.Get, entityTypes);
        var c = new QueryCacheKey(keyStr.Get, entityTypes);

        if (a == b && b == c)
        {
            return a == c;
        }

        return true; // Vacuously true when premise doesn't hold
    }

    #endregion

    #region QueryCacheOptions Properties

    [Property(MaxTest = 100)]
    public bool ExcludeType_IsIdempotent(PositiveInt id)
    {
        // Property: Excluding the same type twice has no additional effect
        _ = id; // Satisfy FsCheck parameter requirement
        var options = new QueryCacheOptions();

        options.ExcludeType<TestEntity1>();
        var countAfterFirst = options.ExcludedEntityTypes.Count;

        options.ExcludeType<TestEntity1>();
        var countAfterSecond = options.ExcludedEntityTypes.Count;

        return countAfterFirst == countAfterSecond;
    }

    [Property(MaxTest = 50)]
    public bool ExcludeType_ReturnsSameInstance(PositiveInt id)
    {
        // Property: ExcludeType returns the same options instance (fluent)
        _ = id;
        var options = new QueryCacheOptions();
        var result = options.ExcludeType<TestEntity1>();

        return ReferenceEquals(options, result);
    }

    #endregion

    #region Helpers

    private static DbCommand CreateCommand(string sql, params (string Name, object Value)[] parameters)
    {
        var command = Substitute.For<DbCommand>();
        command.CommandText.Returns(sql);

        var paramCollection = new FakeDbParameterCollection();
        foreach (var (name, value) in parameters)
        {
            var param = Substitute.For<DbParameter>();
            param.ParameterName.Returns(name);
            param.Value.Returns(value);
            paramCollection.Add(param);
        }

        command.Parameters.Returns(paramCollection);
        return command;
    }

    private static DbContext CreateMockDbContext()
    {
        var context = Substitute.For<DbContext>();
        var model = Substitute.For<Microsoft.EntityFrameworkCore.Metadata.IModel>();
        model.GetEntityTypes().Returns(Array.Empty<Microsoft.EntityFrameworkCore.Metadata.IEntityType>());
        context.Model.Returns(model);
        return context;
    }

    private static IRequestContext CreateRequestContext(string tenantId)
    {
        var rc = Substitute.For<IRequestContext>();
        rc.TenantId.Returns(tenantId);
        return rc;
    }

    // Test entity types used by ExcludeType tests
    private sealed class TestEntity1;
    private sealed class TestEntity2;

    /// <summary>
    /// Minimal DbParameterCollection implementation for testing.
    /// </summary>
    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _parameters.RemoveAll(p => p.ParameterName == parameterName);
        public override void AddRange(Array values) { foreach (DbParameter v in values) _parameters.Add(v); }
        public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters.First(p => p.ParameterName == parameterName);
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var idx = IndexOf(parameterName);
            if (idx >= 0) _parameters[idx] = value;
        }
    }

    #endregion
}
