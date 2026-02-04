using System.Text.Json;
using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;

namespace Encina.UnitTests.DomainModeling.Concurrency;

/// <summary>
/// Tests for <see cref="ConcurrencyConflictInfo{TEntity}"/>.
/// </summary>
public sealed class ConcurrencyConflictInfoTests
{
    #region Constructor and Properties Tests

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var database = new TestEntity { Id = current.Id, Name = "Database" };

        // Act
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);

        // Assert
        conflictInfo.CurrentEntity.ShouldBe(current);
        conflictInfo.ProposedEntity.ShouldBe(proposed);
        conflictInfo.DatabaseEntity.ShouldBe(database);
    }

    [Fact]
    public void Constructor_AllowsNullDatabaseEntity()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };

        // Act
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, null);

        // Assert
        conflictInfo.CurrentEntity.ShouldBe(current);
        conflictInfo.ProposedEntity.ShouldBe(proposed);
        conflictInfo.DatabaseEntity.ShouldBeNull();
    }

    #endregion

    #region WasDeleted Property Tests

    [Fact]
    public void WasDeleted_WhenDatabaseEntityIsNull_ReturnsTrue()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };

        // Act
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, null);

        // Assert
        conflictInfo.WasDeleted.ShouldBeTrue();
    }

    [Fact]
    public void WasDeleted_WhenDatabaseEntityIsNotNull_ReturnsFalse()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var database = new TestEntity { Id = current.Id, Name = "Database" };

        // Act
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);

        // Assert
        conflictInfo.WasDeleted.ShouldBeFalse();
    }

    #endregion

    #region ToDictionary Tests (Without Serializer)

    [Fact]
    public void ToDictionary_WithoutSerializer_ContainsEntityType()
    {
        // Arrange
        var conflictInfo = CreateConflictInfo();

        // Act
        var dictionary = conflictInfo.ToDictionary();

        // Assert
        dictionary.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.EntityTypeKey);
        dictionary[ConcurrencyConflictInfo<TestEntity>.EntityTypeKey].ShouldBe("TestEntity");
    }

    [Fact]
    public void ToDictionary_WithoutSerializer_ContainsEntitiesAsObjects()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var database = new TestEntity { Id = current.Id, Name = "Database" };
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);

        // Act
        var dictionary = conflictInfo.ToDictionary();

        // Assert
        dictionary.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.CurrentEntityKey);
        dictionary.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.ProposedEntityKey);
        dictionary.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.DatabaseEntityKey);

        dictionary[ConcurrencyConflictInfo<TestEntity>.CurrentEntityKey].ShouldBe(current);
        dictionary[ConcurrencyConflictInfo<TestEntity>.ProposedEntityKey].ShouldBe(proposed);
        dictionary[ConcurrencyConflictInfo<TestEntity>.DatabaseEntityKey].ShouldBe(database);
    }

    [Fact]
    public void ToDictionary_WithoutSerializer_WhenDatabaseNull_ContainsNull()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, null);

        // Act
        var dictionary = conflictInfo.ToDictionary();

        // Assert
        dictionary.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.DatabaseEntityKey);
        dictionary[ConcurrencyConflictInfo<TestEntity>.DatabaseEntityKey].ShouldBeNull();
    }

    [Fact]
    public void ToDictionary_WithoutSerializer_ReturnsImmutableDictionary()
    {
        // Arrange
        var conflictInfo = CreateConflictInfo();

        // Act
        var dictionary = conflictInfo.ToDictionary();

        // Assert
        dictionary.ShouldBeOfType<System.Collections.Immutable.ImmutableDictionary<string, object?>>();
    }

    #endregion

    #region ToDictionary Tests (With Serializer)

    [Fact]
    public void ToDictionary_WithSerializer_SerializesEntitiesToJson()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var database = new TestEntity { Id = current.Id, Name = "Database" };
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var dictionary = conflictInfo.ToDictionary(options);

        // Assert
        dictionary[ConcurrencyConflictInfo<TestEntity>.CurrentEntityKey].ShouldBeOfType<string>();
        dictionary[ConcurrencyConflictInfo<TestEntity>.ProposedEntityKey].ShouldBeOfType<string>();
        dictionary[ConcurrencyConflictInfo<TestEntity>.DatabaseEntityKey].ShouldBeOfType<string>();

        var currentJson = dictionary[ConcurrencyConflictInfo<TestEntity>.CurrentEntityKey] as string;
        currentJson.ShouldNotBeNull();
        currentJson!.ShouldContain("\"name\":\"Current\"");
    }

    [Fact]
    public void ToDictionary_WithSerializer_WhenDatabaseNull_SerializesAsNull()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, null);
        var options = new JsonSerializerOptions();

        // Act
        var dictionary = conflictInfo.ToDictionary(options);

        // Assert
        dictionary[ConcurrencyConflictInfo<TestEntity>.DatabaseEntityKey].ShouldBeNull();
    }

    [Fact]
    public void ToDictionary_WithSerializer_StillContainsEntityType()
    {
        // Arrange
        var conflictInfo = CreateConflictInfo();
        var options = new JsonSerializerOptions();

        // Act
        var dictionary = conflictInfo.ToDictionary(options);

        // Assert
        dictionary[ConcurrencyConflictInfo<TestEntity>.EntityTypeKey].ShouldBe("TestEntity");
    }

    #endregion

    #region Dictionary Key Constants Tests

    [Fact]
    public void DictionaryKeyConstants_HaveExpectedValues()
    {
        // Assert
        ConcurrencyConflictInfo<TestEntity>.CurrentEntityKey.ShouldBe("CurrentEntity");
        ConcurrencyConflictInfo<TestEntity>.ProposedEntityKey.ShouldBe("ProposedEntity");
        ConcurrencyConflictInfo<TestEntity>.DatabaseEntityKey.ShouldBe("DatabaseEntity");
        ConcurrencyConflictInfo<TestEntity>.EntityTypeKey.ShouldBe("EntityType");
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var current = new TestEntity { Id = id, Name = "Current" };
        var proposed = new TestEntity { Id = id, Name = "Proposed" };
        var database = new TestEntity { Id = id, Name = "Database" };

        var info1 = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);
        var info2 = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);

        // Act & Assert
        info1.ShouldBe(info2);
        (info1 == info2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var current1 = new TestEntity { Id = id, Name = "Current1" };
        var current2 = new TestEntity { Id = id, Name = "Current2" };
        var proposed = new TestEntity { Id = id, Name = "Proposed" };
        var database = new TestEntity { Id = id, Name = "Database" };

        var info1 = new ConcurrencyConflictInfo<TestEntity>(current1, proposed, database);
        var info2 = new ConcurrencyConflictInfo<TestEntity>(current2, proposed, database);

        // Act & Assert
        info1.ShouldNotBe(info2);
        (info1 != info2).ShouldBeTrue();
    }

    [Fact]
    public void With_CreatesNewInstanceWithModifiedProperty()
    {
        // Arrange
        var id = Guid.NewGuid();
        var current = new TestEntity { Id = id, Name = "Current" };
        var proposed = new TestEntity { Id = id, Name = "Proposed" };
        var database = new TestEntity { Id = id, Name = "Database" };
        var newDatabase = new TestEntity { Id = id, Name = "New Database" };

        var original = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);

        // Act
        var modified = original with { DatabaseEntity = newDatabase };

        // Assert
        modified.CurrentEntity.ShouldBe(current);
        modified.ProposedEntity.ShouldBe(proposed);
        modified.DatabaseEntity.ShouldBe(newDatabase);
        original.DatabaseEntity.ShouldBe(database); // Original unchanged
    }

    #endregion

    #region Integration with RepositoryErrors Tests

    [Fact]
    public void ConflictInfo_CanBeUsedWithRepositoryErrors()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var database = new TestEntity { Id = current.Id, Name = "Database" };
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, database);

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

        // Assert
        var code = error.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode));

        var details = error.GetDetails();
        details.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.EntityTypeKey);
        details.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.CurrentEntityKey);
        details.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.ProposedEntityKey);
        details.ShouldContainKey(ConcurrencyConflictInfo<TestEntity>.DatabaseEntityKey);
    }

    [Fact]
    public void ConflictInfo_WhenDeleted_MessageIndicatesDeletion()
    {
        // Arrange
        var current = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var proposed = new TestEntity { Id = current.Id, Name = "Proposed" };
        var conflictInfo = new ConcurrencyConflictInfo<TestEntity>(current, proposed, null);

        // Act
        var error = RepositoryErrors.ConcurrencyConflict(conflictInfo);

        // Assert
        error.Message.ShouldContain("deleted");
    }

    #endregion

    #region Test Infrastructure

    private static ConcurrencyConflictInfo<TestEntity> CreateConflictInfo()
    {
        var id = Guid.NewGuid();
        return new ConcurrencyConflictInfo<TestEntity>(
            new TestEntity { Id = id, Name = "Current" },
            new TestEntity { Id = id, Name = "Proposed" },
            new TestEntity { Id = id, Name = "Database" });
    }

    private sealed class TestEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    #endregion
}
