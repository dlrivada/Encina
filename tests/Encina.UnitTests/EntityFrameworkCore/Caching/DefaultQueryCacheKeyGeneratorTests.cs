using System.Data.Common;
using Encina.Caching;
using Encina.EntityFrameworkCore.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Encina.UnitTests.EntityFrameworkCore.Caching;

/// <summary>
/// Unit tests for <see cref="DefaultQueryCacheKeyGenerator"/>.
/// </summary>
public class DefaultQueryCacheKeyGeneratorTests
{
    private readonly DefaultQueryCacheKeyGenerator _sut;
    private readonly QueryCacheOptions _options;

    public DefaultQueryCacheKeyGeneratorTests()
    {
        _options = new QueryCacheOptions { KeyPrefix = "test:qc" };
        _sut = new DefaultQueryCacheKeyGenerator(Options.Create(_options));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DefaultQueryCacheKeyGenerator(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region Generate (without tenant) Tests

    [Fact]
    public void Generate_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateMockDbContext();

        // Act & Assert
        var act = () => _sut.Generate(null!, context);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("command");
    }

    [Fact]
    public void Generate_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var command = CreateMockCommand("SELECT * FROM Orders");

        // Act & Assert
        var act = () => _sut.Generate(command, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void Generate_SameCommand_ProducesSameKey()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command1 = CreateMockCommand("SELECT * FROM Orders WHERE Id = @Id", ("@Id", "123"));
        var command2 = CreateMockCommand("SELECT * FROM Orders WHERE Id = @Id", ("@Id", "123"));

        // Act
        var key1 = _sut.Generate(command1, context);
        var key2 = _sut.Generate(command2, context);

        // Assert
        key1.Key.ShouldBe(key2.Key);
    }

    [Fact]
    public void Generate_DifferentParameters_ProducesDifferentKeys()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command1 = CreateMockCommand("SELECT * FROM Orders WHERE Id = @Id", ("@Id", "123"));
        var command2 = CreateMockCommand("SELECT * FROM Orders WHERE Id = @Id", ("@Id", "456"));

        // Act
        var key1 = _sut.Generate(command1, context);
        var key2 = _sut.Generate(command2, context);

        // Assert
        key1.Key.ShouldNotBe(key2.Key);
    }

    [Fact]
    public void Generate_DifferentSql_ProducesDifferentKeys()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command1 = CreateMockCommand("SELECT * FROM Orders");
        var command2 = CreateMockCommand("SELECT * FROM Products");

        // Act
        var key1 = _sut.Generate(command1, context);
        var key2 = _sut.Generate(command2, context);

        // Assert
        key1.Key.ShouldNotBe(key2.Key);
    }

    [Fact]
    public void Generate_KeyStartsWithPrefix()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command = CreateMockCommand("SELECT * FROM Orders");

        // Act
        var result = _sut.Generate(command, context);

        // Assert
        result.Key.ShouldStartWith("test:qc:");
    }

    [Fact]
    public void Generate_NoTablesFound_UsesUnknownPrefix()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command = CreateMockCommand("SELECT 1");

        // Act
        var result = _sut.Generate(command, context);

        // Assert
        result.Key.ShouldContain("unknown");
        result.EntityTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Generate_KeyContainsHash()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command = CreateMockCommand("SELECT * FROM Orders");

        // Act
        var result = _sut.Generate(command, context);

        // Assert — key format: prefix:entityType:hash
        var parts = result.Key.Split(':');
        parts.Length.ShouldBeGreaterThanOrEqualTo(3);

        // The hash should be 16 hex characters
        var hash = parts[^1];
        hash.Length.ShouldBe(16);
        hash.ShouldMatch("^[0-9a-f]{16}$");
    }

    [Fact]
    public void Generate_ReturnsEntityTypes()
    {
        // Arrange
        var context = CreateMockDbContext("Orders");
        var command = CreateMockCommand("SELECT * FROM Orders");

        // Act
        var result = _sut.Generate(command, context);

        // Assert
        result.EntityTypes.ShouldNotBeEmpty();
    }

    #endregion

    #region Generate (with tenant) Tests

    [Fact]
    public void GenerateWithTenant_WithNullRequestContext_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command = CreateMockCommand("SELECT * FROM Orders");

        // Act & Assert
        var act = () => _sut.Generate(command, context, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void GenerateWithTenant_IncludesTenantInKey()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command = CreateMockCommand("SELECT * FROM Orders");
        var requestContext = CreateRequestContext(tenantId: "tenant-abc");

        // Act
        var result = _sut.Generate(command, context, requestContext);

        // Assert
        result.Key.ShouldContain("tenant-abc");
    }

    [Fact]
    public void GenerateWithTenant_EmptyTenantId_ExcludesTenantFromKey()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command = CreateMockCommand("SELECT * FROM Orders");
        var requestContext = CreateRequestContext(tenantId: "");

        // Act
        var result = _sut.Generate(command, context, requestContext);

        // Assert — no double-colon from empty tenant
        result.Key.ShouldNotContain("::");
    }

    [Fact]
    public void GenerateWithTenant_DifferentTenants_ProduceDifferentKeys()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command1 = CreateMockCommand("SELECT * FROM Orders WHERE Id = @Id", ("@Id", "1"));
        var command2 = CreateMockCommand("SELECT * FROM Orders WHERE Id = @Id", ("@Id", "1"));
        var tenant1 = CreateRequestContext(tenantId: "tenant-1");
        var tenant2 = CreateRequestContext(tenantId: "tenant-2");

        // Act
        var key1 = _sut.Generate(command1, context, tenant1);
        var key2 = _sut.Generate(command2, context, tenant2);

        // Assert
        key1.Key.ShouldNotBe(key2.Key);
    }

    [Fact]
    public void GenerateWithTenant_SameTenant_ProducesSameKey()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command1 = CreateMockCommand("SELECT * FROM Orders WHERE Id = @Id", ("@Id", "1"));
        var command2 = CreateMockCommand("SELECT * FROM Orders WHERE Id = @Id", ("@Id", "1"));
        var tenant = CreateRequestContext(tenantId: "tenant-1");

        // Act
        var key1 = _sut.Generate(command1, context, tenant);
        var key2 = _sut.Generate(command2, context, tenant);

        // Assert
        key1.Key.ShouldBe(key2.Key);
    }

    #endregion

    #region Entity Type Resolution Tests

    [Fact]
    public void Generate_WithMappedEntityType_UsesClrTypeName()
    {
        // Arrange
        var context = CreateMockDbContext("Orders", clrTypeName: "Order");
        var command = CreateMockCommand("SELECT * FROM Orders");

        // Act
        var result = _sut.Generate(command, context);

        // Assert
        result.EntityTypes.ShouldContain("OrderEntity");
        result.Key.ShouldContain("OrderEntity");
    }

    [Fact]
    public void Generate_WithUnmappedTable_UsesRawTableName()
    {
        // Arrange — no entity types mapped in the context
        var context = CreateMockDbContext();
        var command = CreateMockCommand("SELECT * FROM UnknownTable");

        // Act
        var result = _sut.Generate(command, context);

        // Assert
        result.EntityTypes.ShouldContain("UnknownTable");
    }

    [Fact]
    public void Generate_WithMultipleJoinedTables_ReturnsAllEntityTypes()
    {
        // Arrange
        var context = CreateMockDbContext("Orders", "Customers");
        var command = CreateMockCommand(
            "SELECT * FROM Orders INNER JOIN Customers ON Orders.CustomerId = Customers.Id");

        // Act
        var result = _sut.Generate(command, context);

        // Assert
        result.EntityTypes.Count.ShouldBe(2);
    }

    #endregion

    #region Hash Determinism Tests

    [Fact]
    public void Generate_ParameterOrder_AffectsHash()
    {
        // Arrange — parameters in different order produce same hash
        // because DbCommand.Parameters collection is iterated in ordinal order.
        var context = CreateMockDbContext();
        var command = CreateMockCommand(
            "SELECT * FROM Orders WHERE Id = @Id AND Name = @Name",
            ("@Id", "1"), ("@Name", "test"));

        // Act — generate twice with same command
        var key1 = _sut.Generate(command, context);
        var key2 = _sut.Generate(command, context);

        // Assert
        key1.Key.ShouldBe(key2.Key);
    }

    [Fact]
    public void Generate_NullParameterValue_HandledGracefully()
    {
        // Arrange
        var context = CreateMockDbContext();
        var command = CreateMockCommand("SELECT * FROM Orders WHERE Name = @Name", ("@Name", null));

        // Act
        var result = _sut.Generate(command, context);

        // Assert
        result.ShouldNotBeNull();
        result.Key.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region Test Helpers

    private static DbCommand CreateMockCommand(
        string commandText,
        params (string Name, object? Value)[] parameters)
    {
        var command = Substitute.For<DbCommand>();
        command.CommandText.Returns(commandText);

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

    private static DbContext CreateMockDbContext(params string[] tableNames)
    {
        var entityTypes = new List<IEntityType>();

        // Map table names to real CLR types for entity type simulation.
        // The key generator uses entityType.ClrType.Name to get the entity name.
        var typeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["Orders"] = typeof(OrderEntity),
            ["Customers"] = typeof(CustomerEntity),
            ["Products"] = typeof(ProductEntity),
            ["UnknownTable"] = typeof(UnknownTableEntity)
        };

        foreach (var tableName in tableNames)
        {
            if (typeMap.TryGetValue(tableName, out var clrType))
            {
                var entityType = Substitute.For<IEntityType>();
                entityType.GetTableName().Returns(tableName);
                entityType.ClrType.Returns(clrType);
                entityTypes.Add(entityType);
            }
        }

        var model = Substitute.For<IModel>();
        model.GetEntityTypes().Returns(entityTypes);

        var context = Substitute.For<DbContext>();
        context.Model.Returns(model);

        return context;
    }

    private static DbContext CreateMockDbContext(
        string tableName,
        string? clrTypeName)
    {
        var entityTypes = new List<IEntityType>();

        var entityType = Substitute.For<IEntityType>();
        entityType.GetTableName().Returns(tableName);

        // Use a real type whose Name matches clrTypeName
        var clrType = clrTypeName switch
        {
            "Order" => typeof(OrderEntity),
            "Customer" => typeof(CustomerEntity),
            _ => typeof(OrderEntity) // default
        };
        entityType.ClrType.Returns(clrType);
        entityTypes.Add(entityType);

        var model = Substitute.For<IModel>();
        model.GetEntityTypes().Returns(entityTypes);

        var context = Substitute.For<DbContext>();
        context.Model.Returns(model);

        return context;
    }

    private static IRequestContext CreateRequestContext(
        string? tenantId = null,
        string? userId = null)
    {
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.TenantId.Returns(tenantId ?? string.Empty);
        requestContext.UserId.Returns(userId ?? string.Empty);
        requestContext.CorrelationId.Returns("test-correlation");
        return requestContext;
    }

    /// <summary>
    /// Minimal fake DbParameterCollection for testing.
    /// </summary>
    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => ((System.Collections.ICollection)_parameters).SyncRoot;

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_parameters).CopyTo(array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _parameters.RemoveAll(p => p.ParameterName == parameterName);
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters.First(p => p.ParameterName == parameterName);
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var idx = IndexOf(parameterName);
            if (idx >= 0) _parameters[idx] = value;
        }

        public override void AddRange(Array values)
        {
            foreach (DbParameter value in values)
            {
                _parameters.Add(value);
            }
        }
    }

    // Simple entity classes used for CLR type name resolution in mock entity types.
    // The DefaultQueryCacheKeyGenerator uses entityType.ClrType.Name to resolve entity names.
    private sealed class OrderEntity;
    private sealed class CustomerEntity;
    private sealed class ProductEntity;
    private sealed class UnknownTableEntity;

    #endregion
}
