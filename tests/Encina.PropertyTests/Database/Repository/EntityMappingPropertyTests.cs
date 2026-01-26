using FsCheck;
using FsCheck.Xunit;
using Shouldly;
using ADOMySQLRepository = Encina.ADO.MySQL.Repository;
using ADOPostgreSQLRepository = Encina.ADO.PostgreSQL.Repository;
using ADOSqliteRepository = Encina.ADO.Sqlite.Repository;
using ADOSqlServerRepository = Encina.ADO.SqlServer.Repository;
using DapperMySQLRepository = Encina.Dapper.MySQL.Repository;
using DapperPostgreSQLRepository = Encina.Dapper.PostgreSQL.Repository;
using DapperSqliteRepository = Encina.Dapper.Sqlite.Repository;
using DapperSqlServerRepository = Encina.Dapper.SqlServer.Repository;

namespace Encina.PropertyTests.Database.Repository;

/// <summary>
/// Property-based tests for EntityMappingBuilder across all providers.
/// Verifies invariants that MUST hold for ALL entity configurations.
/// </summary>
[Trait("Category", "Property")]
public sealed class EntityMappingPropertyTests
{
    #region ADO Provider Entity Mapping Tests

    [Fact]
    public void Property_ADOSqliteMapping_TableNameIsPreservedAfterBuild()
    {
        // Property: Table name specified in ToTable() MUST be preserved in the built mapping
        const string tableName = "TestOrders";
        var mapping = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName)
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.TableName.ShouldBe(tableName, "Table name must be preserved after Build()");
    }

    [Fact]
    public void Property_ADOSqlServerMapping_TableNameIsPreservedAfterBuild()
    {
        const string tableName = "TestOrders";
        var mapping = new ADOSqlServerRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName)
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.TableName.ShouldBe(tableName);
    }

    [Fact]
    public void Property_ADOPostgreSQLMapping_TableNameIsPreservedAfterBuild()
    {
        const string tableName = "test_orders";
        var mapping = new ADOPostgreSQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName)
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.TableName.ShouldBe(tableName);
    }

    [Fact]
    public void Property_ADOMySQLMapping_TableNameIsPreservedAfterBuild()
    {
        const string tableName = "test_orders";
        var mapping = new ADOMySQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName)
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.TableName.ShouldBe(tableName);
    }

    #endregion

    #region Dapper Provider Entity Mapping Tests

    [Fact]
    public void Property_DapperSqliteMapping_TableNameIsPreservedAfterBuild()
    {
        const string tableName = "TestOrders";
        var mapping = new DapperSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName)
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.TableName.ShouldBe(tableName);
    }

    [Fact]
    public void Property_DapperSqlServerMapping_TableNameIsPreservedAfterBuild()
    {
        const string tableName = "TestOrders";
        var mapping = new DapperSqlServerRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName)
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.TableName.ShouldBe(tableName);
    }

    [Fact]
    public void Property_DapperPostgreSQLMapping_TableNameIsPreservedAfterBuild()
    {
        const string tableName = "test_orders";
        var mapping = new DapperPostgreSQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName)
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.TableName.ShouldBe(tableName);
    }

    [Fact]
    public void Property_DapperMySQLMapping_TableNameIsPreservedAfterBuild()
    {
        const string tableName = "test_orders";
        var mapping = new DapperMySQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName)
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.TableName.ShouldBe(tableName);
    }

    #endregion

    #region Column Mappings Preservation Tests

    [Fact]
    public void Property_ADOSqliteMapping_ColumnMappingsArePreservedAfterBuild()
    {
        // Property: All column mappings MUST be preserved after Build()
        var mapping = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id, "entity_id")
            .MapProperty(e => e.Name, "entity_name")
            .MapProperty(e => e.Value, "entity_value")
            .MapProperty(e => e.CreatedAtUtc, "created_at")
            .Build();

        mapping.ColumnMappings.ShouldContainKey("Id");
        mapping.ColumnMappings["Id"].ShouldBe("entity_id");
        mapping.ColumnMappings.ShouldContainKey("Name");
        mapping.ColumnMappings["Name"].ShouldBe("entity_name");
        mapping.ColumnMappings.ShouldContainKey("Value");
        mapping.ColumnMappings["Value"].ShouldBe("entity_value");
        mapping.ColumnMappings.ShouldContainKey("CreatedAtUtc");
        mapping.ColumnMappings["CreatedAtUtc"].ShouldBe("created_at");
    }

    [Fact]
    public void Property_DapperSqlServerMapping_ColumnMappingsArePreservedAfterBuild()
    {
        var mapping = new DapperSqlServerRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id, "EntityId")
            .MapProperty(e => e.Name, "EntityName")
            .MapProperty(e => e.Value)
            .Build();

        mapping.ColumnMappings.ShouldContainKey("Id");
        mapping.ColumnMappings["Id"].ShouldBe("EntityId");
        mapping.ColumnMappings.ShouldContainKey("Name");
        mapping.ColumnMappings["Name"].ShouldBe("EntityName");
        mapping.ColumnMappings.ShouldContainKey("Value");
        mapping.ColumnMappings["Value"].ShouldBe("Value"); // Default to property name
    }

    [Property(MaxTest = 100)]
    public bool Property_AllProviders_IdColumnIsAutomaticallyExcludedFromUpdates(Guid id)
    {
        // Property: HasId() MUST automatically add the ID property to UpdateExcludedProperties
        var adoSqlite = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id).MapProperty(e => e.Name).Build();
        var adoSqlServer = new ADOSqlServerRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id).MapProperty(e => e.Name).Build();
        var dapperSqlite = new DapperSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id).MapProperty(e => e.Name).Build();
        var dapperPostgres = new DapperPostgreSQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        return adoSqlite.UpdateExcludedProperties.Contains("Id")
            && adoSqlServer.UpdateExcludedProperties.Contains("Id")
            && dapperSqlite.UpdateExcludedProperties.Contains("Id")
            && dapperPostgres.UpdateExcludedProperties.Contains("Id");
    }

    #endregion

    #region InsertExcluded and UpdateExcluded Tests

    [Fact]
    public void Property_ADOSqliteMapping_ExcludeFromInsertWorksCorrectly()
    {
        // Property: ExcludeFromInsert() MUST add property to InsertExcludedProperties
        var mapping = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .MapProperty(e => e.Value)
            .ExcludeFromInsert(e => e.Id)
            .ExcludeFromInsert(e => e.CreatedAtUtc)
            .Build();

        mapping.InsertExcludedProperties.ShouldContain("Id", "Id should be excluded from inserts");
        mapping.InsertExcludedProperties.ShouldContain("CreatedAtUtc", "CreatedAtUtc should be excluded from inserts");
        mapping.InsertExcludedProperties.ShouldNotContain("Name", "Name should NOT be excluded from inserts");
    }

    [Fact]
    public void Property_DapperMySQLMapping_ExcludeFromUpdateWorksCorrectly()
    {
        // Property: ExcludeFromUpdate() MUST add property to UpdateExcludedProperties
        var mapping = new DapperMySQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("test_entities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .MapProperty(e => e.CreatedAtUtc)
            .ExcludeFromUpdate(e => e.CreatedAtUtc)
            .Build();

        mapping.UpdateExcludedProperties.ShouldContain("Id", "Id is automatically excluded from updates");
        mapping.UpdateExcludedProperties.ShouldContain("CreatedAtUtc", "CreatedAtUtc should be excluded from updates");
        mapping.UpdateExcludedProperties.ShouldNotContain("Name", "Name should NOT be excluded from updates");
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Name")]
    [InlineData("Value")]
    [InlineData("CreatedAtUtc")]
    public void Property_AllADOProviders_ExcludeFromUpdatePreservesPropertyName(string propertyName)
    {
        // Property: ExcludeFromUpdate preserves the exact property name
        var mapping = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .MapProperty(e => e.Value)
            .MapProperty(e => e.CreatedAtUtc);

        // Apply exclusion based on property name
        if (propertyName == "Id") mapping.ExcludeFromUpdate(e => e.Id);
        if (propertyName == "Name") mapping.ExcludeFromUpdate(e => e.Name);
        if (propertyName == "Value") mapping.ExcludeFromUpdate(e => e.Value);
        if (propertyName == "CreatedAtUtc") mapping.ExcludeFromUpdate(e => e.CreatedAtUtc);

        var result = mapping.Build();
        result.UpdateExcludedProperties.ShouldContain(propertyName);
    }

    #endregion

    #region GetId Functionality Tests

    [Fact]
    public void Property_ADOSqliteMapping_GetIdReturnsCorrectValue()
    {
        // Property: GetId MUST return the actual ID value from the entity
        var expectedId = Guid.NewGuid();
        var entity = new RepositoryTestEntity { Id = expectedId, Name = "Test" };

        var mapping = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name)
            .Build();

        mapping.GetId(entity).ShouldBe(expectedId);
    }

    [Property(MaxTest = 100)]
    public bool Property_AllProviders_GetIdReturnsConsistentValues(Guid id, NonEmptyString name)
    {
        // Property: GetId MUST always return the same value for the same entity
        var entity = new RepositoryTestEntity { Id = id, Name = name.Get };

        var adoSqlite = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id).MapProperty(e => e.Name).Build();
        var dapperSqlite = new DapperSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        return adoSqlite.GetId(entity) == id && dapperSqlite.GetId(entity) == id;
    }

    #endregion

    #region IdColumnName Tests

    [Theory]
    [InlineData("Id")]
    [InlineData("EntityId")]
    [InlineData("entity_id")]
    [InlineData("ID")]
    public void Property_AllADOProviders_IdColumnNameIsPreserved(string idColumnName)
    {
        // Property: Custom ID column name MUST be preserved
        var adoSqlite = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id, idColumnName).MapProperty(e => e.Name).Build();
        var adoSqlServer = new ADOSqlServerRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id, idColumnName).MapProperty(e => e.Name).Build();
        var adoPostgres = new ADOPostgreSQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id, idColumnName).MapProperty(e => e.Name).Build();
        var adoMySQL = new ADOMySQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id, idColumnName).MapProperty(e => e.Name).Build();

        adoSqlite.IdColumnName.ShouldBe(idColumnName);
        adoSqlServer.IdColumnName.ShouldBe(idColumnName);
        adoPostgres.IdColumnName.ShouldBe(idColumnName);
        adoMySQL.IdColumnName.ShouldBe(idColumnName);
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("EntityId")]
    [InlineData("entity_id")]
    public void Property_AllDapperProviders_IdColumnNameIsPreserved(string idColumnName)
    {
        var dapperSqlite = new DapperSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id, idColumnName).MapProperty(e => e.Name).Build();
        var dapperSqlServer = new DapperSqlServerRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id, idColumnName).MapProperty(e => e.Name).Build();
        var dapperPostgres = new DapperPostgreSQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id, idColumnName).MapProperty(e => e.Name).Build();
        var dapperMySQL = new DapperMySQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id, idColumnName).MapProperty(e => e.Name).Build();

        dapperSqlite.IdColumnName.ShouldBe(idColumnName);
        dapperSqlServer.IdColumnName.ShouldBe(idColumnName);
        dapperPostgres.IdColumnName.ShouldBe(idColumnName);
        dapperMySQL.IdColumnName.ShouldBe(idColumnName);
    }

    #endregion

    #region Build Validation Tests

    [Fact]
    public void Property_ADOSqliteMapping_BuildWithoutTableNameThrowsInvalidOperationException()
    {
        // Property: Build without ToTable MUST throw InvalidOperationException
        var builder = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .HasId(e => e.Id)
            .MapProperty(e => e.Name);

        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Table name must be specified");
    }

    [Fact]
    public void Property_DapperPostgreSQLMapping_BuildWithoutHasIdThrowsInvalidOperationException()
    {
        // Property: Build without HasId MUST throw InvalidOperationException
        var builder = new DapperPostgreSQLRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("test_entities")
            .MapProperty(e => e.Name);

        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Primary key must be specified");
    }

    [Fact]
    public void Property_AllProviders_BuildWithOnlyHasIdSucceeds()
    {
        // Property: Build with only table and id should succeed
        // HasId automatically adds the ID column to mappings
        var adoSqlite = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test").HasId(e => e.Id);

        // This should succeed because HasId adds a column mapping
        var mapping = adoSqlite.Build();
        mapping.ColumnMappings.Count.ShouldBe(1);
        mapping.ColumnMappings.ShouldContainKey("Id");
    }

    #endregion

    #region Cross-Provider Consistency Tests

    [Fact]
    public void Property_AllADODapperProviders_ProduceSameMappingStructure()
    {
        // Property: All ADO/Dapper providers MUST produce structurally identical mappings
        const string tableName = "TestEntities";
        const string idColumn = "entity_id";
        const string nameColumn = "entity_name";

        var adoSqlite = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName).HasId(e => e.Id, idColumn).MapProperty(e => e.Name, nameColumn).Build();
        var adoSqlServer = new ADOSqlServerRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName).HasId(e => e.Id, idColumn).MapProperty(e => e.Name, nameColumn).Build();
        var dapperSqlite = new DapperSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName).HasId(e => e.Id, idColumn).MapProperty(e => e.Name, nameColumn).Build();
        var dapperSqlServer = new DapperSqlServerRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable(tableName).HasId(e => e.Id, idColumn).MapProperty(e => e.Name, nameColumn).Build();

        // All should have same table name
        adoSqlite.TableName.ShouldBe(adoSqlServer.TableName);
        adoSqlServer.TableName.ShouldBe(dapperSqlite.TableName);
        dapperSqlite.TableName.ShouldBe(dapperSqlServer.TableName);

        // All should have same ID column name
        adoSqlite.IdColumnName.ShouldBe(adoSqlServer.IdColumnName);
        adoSqlServer.IdColumnName.ShouldBe(dapperSqlite.IdColumnName);
        dapperSqlite.IdColumnName.ShouldBe(dapperSqlServer.IdColumnName);

        // All should have same column mappings count
        adoSqlite.ColumnMappings.Count.ShouldBe(adoSqlServer.ColumnMappings.Count);
        adoSqlServer.ColumnMappings.Count.ShouldBe(dapperSqlite.ColumnMappings.Count);
        dapperSqlite.ColumnMappings.Count.ShouldBe(dapperSqlServer.ColumnMappings.Count);
    }

    [Property(MaxTest = 100)]
    public bool Property_EntityMappingBuilder_ColumnMappingCountMatchesMapPropertyCalls(bool mapName, bool mapValue, bool mapCreated)
    {
        // Property: Number of column mappings equals HasId (1) + MapProperty calls
        var builder = new ADOSqliteRepository.EntityMappingBuilder<RepositoryTestEntity, Guid>()
            .ToTable("Test")
            .HasId(e => e.Id); // 1 mapping

        var expectedCount = 1; // Start with Id mapping from HasId
        if (mapName)
        {
            builder.MapProperty(e => e.Name);
            expectedCount++;
        }
        if (mapValue)
        {
            builder.MapProperty(e => e.Value);
            expectedCount++;
        }
        if (mapCreated)
        {
            builder.MapProperty(e => e.CreatedAtUtc);
            expectedCount++;
        }

        var mapping = builder.Build();
        return mapping.ColumnMappings.Count == expectedCount;
    }

    #endregion
}

/// <summary>
/// Shared test entity for repository property tests.
/// </summary>
public sealed class RepositoryTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsActive { get; set; }
}
