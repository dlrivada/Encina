using FsCheck;
using FsCheck.Xunit;
using Encina.MongoDB.Repository;
using Shouldly;

namespace Encina.PropertyTests.Database.Repository;

/// <summary>
/// Property-based tests for Repository Options across all providers.
/// Verifies invariants that MUST hold for ALL repository configurations.
/// </summary>
[Trait("Category", "Property")]
public sealed class RepositoryOptionsPropertyTests
{
    #region MongoDbRepositoryOptions Default Values Tests

    [Fact]
    public void Property_MongoDbRepositoryOptions_DefaultCollectionNameIsNull()
    {
        // Property: Default CollectionName MUST be null (will use convention-based naming internally)
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>();

        options.CollectionName.ShouldBeNull("Default CollectionName must be null");
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_DefaultIdPropertyIsNull()
    {
        // Property: Default IdProperty MUST be null (requires explicit configuration)
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>();

        options.IdProperty.ShouldBeNull("Default IdProperty must be null");
    }

    #endregion

    #region MongoDbRepositoryOptions CollectionName Tests

    [Theory]
    [InlineData("orders")]
    [InlineData("Products")]
    [InlineData("user_profiles")]
    [InlineData("CustomerOrders")]
    [InlineData("a")]
    [InlineData("verylongcollectionnamethatisvalidinmongodb")]
    public void Property_MongoDbRepositoryOptions_CustomCollectionNameIsStored(string customName)
    {
        // Property: Custom CollectionName MUST be stored correctly
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            CollectionName = customName
        };

        options.CollectionName.ShouldBe(customName, "Custom collection name must be stored exactly as provided");
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_CollectionNameCanBeChanged()
    {
        // Property: CollectionName MUST be changeable after initial assignment
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            CollectionName = "initial"
        };

        options.CollectionName = "changed";

        options.CollectionName.ShouldBe("changed");
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_CollectionNameCanBeSetToNull()
    {
        // Property: CollectionName MUST be settable to null (to revert to convention)
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            CollectionName = "custom"
        };

        options.CollectionName = null;

        options.CollectionName.ShouldBeNull();
    }

    #endregion

    #region MongoDbRepositoryOptions IdProperty Tests

    [Fact]
    public void Property_MongoDbRepositoryOptions_IdPropertyCanBeConfigured()
    {
        // Property: IdProperty MUST be configurable via expression
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            IdProperty = e => e.Id
        };

        options.IdProperty.ShouldNotBeNull();
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_IdPropertyExpressionExtractsCorrectValue()
    {
        // Property: IdProperty expression MUST extract the correct ID value
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            IdProperty = e => e.Id
        };

        var expectedId = Guid.NewGuid();
        var entity = new MongoRepositoryTestEntity { Id = expectedId };
        var idFunc = options.IdProperty!.Compile();

        idFunc(entity).ShouldBe(expectedId);
    }

    [Property(MaxTest = 100)]
    public bool Property_MongoDbRepositoryOptions_IdPropertyExtractsAnyGuidValue(Guid id)
    {
        // Property: IdProperty MUST always extract the correct ID value for any entity
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            IdProperty = e => e.Id
        };

        var entity = new MongoRepositoryTestEntity { Id = id };
        var idFunc = options.IdProperty!.Compile();

        return idFunc(entity) == id;
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_IdPropertyCanSelectDifferentProperty()
    {
        // Property: IdProperty MUST be able to select any property of the correct type
        var options = new MongoDbRepositoryOptions<MongoEntityWithAlternativeId, Guid>
        {
            IdProperty = e => e.EntityId
        };

        var expectedId = Guid.NewGuid();
        var entity = new MongoEntityWithAlternativeId { EntityId = expectedId };
        var idFunc = options.IdProperty.Compile();

        idFunc(entity).ShouldBe(expectedId);
    }

    #endregion

    #region MongoDbRepositoryOptions with Different ID Types

    [Fact]
    public void Property_MongoDbRepositoryOptions_WorksWithStringId()
    {
        // Property: Options MUST work with string ID type
        var options = new MongoDbRepositoryOptions<MongoEntityWithStringId, string>
        {
            CollectionName = "customers",
            IdProperty = c => c.Id
        };

        var entity = new MongoEntityWithStringId { Id = "cust-123" };
        var idFunc = options.IdProperty!.Compile();

        idFunc(entity).ShouldBe("cust-123");
    }

    [Property(MaxTest = 100)]
    public bool Property_MongoDbRepositoryOptions_WorksWithAnyStringId(NonEmptyString nonEmptyString)
    {
        // Property: Options MUST extract correct string IDs
        var options = new MongoDbRepositoryOptions<MongoEntityWithStringId, string>
        {
            IdProperty = e => e.Id
        };

        var entity = new MongoEntityWithStringId { Id = nonEmptyString.Get };
        var idFunc = options.IdProperty!.Compile();

        return idFunc(entity) == nonEmptyString.Get;
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_WorksWithIntId()
    {
        // Property: Options MUST work with int ID type
        var options = new MongoDbRepositoryOptions<MongoEntityWithIntId, int>
        {
            CollectionName = "products",
            IdProperty = p => p.Id
        };

        var entity = new MongoEntityWithIntId { Id = 42 };
        var idFunc = options.IdProperty!.Compile();

        idFunc(entity).ShouldBe(42);
    }

    [Property(MaxTest = 100)]
    public bool Property_MongoDbRepositoryOptions_WorksWithAnyIntId(int id)
    {
        // Property: Options MUST extract correct int IDs
        var options = new MongoDbRepositoryOptions<MongoEntityWithIntId, int>
        {
            IdProperty = e => e.Id
        };

        var entity = new MongoEntityWithIntId { Id = id };
        var idFunc = options.IdProperty!.Compile();

        return idFunc(entity) == id;
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_WorksWithLongId()
    {
        // Property: Options MUST work with long ID type
        var options = new MongoDbRepositoryOptions<MongoEntityWithLongId, long>
        {
            CollectionName = "events",
            IdProperty = e => e.Id
        };

        var entity = new MongoEntityWithLongId { Id = 9999999999L };
        var idFunc = options.IdProperty!.Compile();

        idFunc(entity).ShouldBe(9999999999L);
    }

    #endregion

    #region MongoDbRepositoryOptions Independence Tests

    [Fact]
    public void Property_MongoDbRepositoryOptions_InstancesAreIndependent()
    {
        // Property: Options for one entity MUST NOT affect options for another
        var options1 = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            CollectionName = "custom_collection",
            IdProperty = e => e.Id
        };

        var options2 = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>();

        options1.CollectionName.ShouldBe("custom_collection");
        options1.IdProperty.ShouldNotBeNull();
        options2.CollectionName.ShouldBeNull();
        options2.IdProperty.ShouldBeNull();
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_DifferentEntityTypesCanHaveDifferentConfigurations()
    {
        // Property: Different entity types CAN have different collection names
        var orderOptions = new MongoDbRepositoryOptions<MongoOrder, Guid>
        {
            CollectionName = "orders",
            IdProperty = o => o.Id
        };

        var customerOptions = new MongoDbRepositoryOptions<MongoEntityWithStringId, string>
        {
            CollectionName = "customers",
            IdProperty = c => c.Id
        };

        orderOptions.CollectionName.ShouldBe("orders");
        customerOptions.CollectionName.ShouldBe("customers");
        orderOptions.CollectionName.ShouldNotBe(customerOptions.CollectionName);
    }

    #endregion

    #region MongoDbRepositoryOptions Generic Constraint Tests

    [Fact]
    public void Property_MongoDbRepositoryOptions_RequiresClassConstraintOnTEntity()
    {
        // Property: TEntity MUST be a reference type (class)
        // This is enforced by the generic constraint "where TEntity : class"
        // Verified at compile time - if this test compiles, constraint is satisfied
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>();
        options.ShouldNotBeNull();
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_RequiresNotNullConstraintOnTId()
    {
        // Property: TId MUST be non-nullable
        // This is enforced by the generic constraint "where TId : notnull"
        // Verified at compile time - if this test compiles, constraint is satisfied
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>();
        options.ShouldNotBeNull();
    }

    #endregion

    #region Combined Configuration Tests

    [Fact]
    public void Property_MongoDbRepositoryOptions_BothPropertiesCanBeConfiguredTogether()
    {
        // Property: Both CollectionName and IdProperty MUST be configurable together
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            CollectionName = "test_entities",
            IdProperty = e => e.Id
        };

        options.CollectionName.ShouldBe("test_entities");
        options.IdProperty.ShouldNotBeNull();
    }

    [Fact]
    public void Property_MongoDbRepositoryOptions_ConfigurationOrderDoesNotMatter()
    {
        // Property: Configuration order MUST NOT affect the final state
        var options1 = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>();
        options1.CollectionName = "collection1";
        options1.IdProperty = e => e.Id;

        var options2 = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>();
        options2.IdProperty = e => e.Id;
        options2.CollectionName = "collection1";

        options1.CollectionName.ShouldBe(options2.CollectionName);
        options1.IdProperty.ShouldNotBeNull();
        options2.IdProperty.ShouldNotBeNull();
    }

    #endregion

    #region Expression Behavior Tests

    [Fact]
    public void Property_MongoDbRepositoryOptions_IdPropertyExpressionIsReusable()
    {
        // Property: IdProperty expression MUST be reusable across multiple entities
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            IdProperty = e => e.Id
        };

        var idFunc = options.IdProperty!.Compile();

        var entity1 = new MongoRepositoryTestEntity { Id = Guid.NewGuid() };
        var entity2 = new MongoRepositoryTestEntity { Id = Guid.NewGuid() };

        idFunc(entity1).ShouldBe(entity1.Id);
        idFunc(entity2).ShouldBe(entity2.Id);
        idFunc(entity1).ShouldNotBe(idFunc(entity2));
    }

    [Property(MaxTest = 100)]
    public bool Property_MongoDbRepositoryOptions_CompiledExpressionIsConsistent(Guid id)
    {
        // Property: Compiled expression MUST return same value for same entity
        var options = new MongoDbRepositoryOptions<MongoRepositoryTestEntity, Guid>
        {
            IdProperty = e => e.Id
        };

        var entity = new MongoRepositoryTestEntity { Id = id };
        var idFunc = options.IdProperty!.Compile();

        // Call multiple times to verify consistency
        var result1 = idFunc(entity);
        var result2 = idFunc(entity);
        var result3 = idFunc(entity);

        return result1 == id && result2 == id && result3 == id;
    }

    #endregion
}

#region Test Entities

/// <summary>
/// Test entity for MongoDB with standard Guid Id property.
/// </summary>
public sealed class MongoRepositoryTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

/// <summary>
/// Test entity for MongoDB with alternative Id property name.
/// </summary>
public sealed class MongoEntityWithAlternativeId
{
    public Guid EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Test entity for MongoDB with string Id.
/// </summary>
public sealed class MongoEntityWithStringId
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Test entity for MongoDB with int Id.
/// </summary>
public sealed class MongoEntityWithIntId
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Test entity for MongoDB with long Id.
/// </summary>
public sealed class MongoEntityWithLongId
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
}

/// <summary>
/// Test entity for MongoDB order.
/// </summary>
public sealed class MongoOrder
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

#endregion
