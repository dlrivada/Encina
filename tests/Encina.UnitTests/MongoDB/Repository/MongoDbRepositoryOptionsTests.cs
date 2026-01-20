using Encina.MongoDB.Repository;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Repository;

/// <summary>
/// Unit tests for <see cref="MongoDbRepositoryOptions{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Unit")]
public class MongoDbRepositoryOptionsTests
{
    #region Property Tests

    [Fact]
    public void IdProperty_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>();

        // Act
        options.IdProperty = x => x.Id;

        // Assert
        options.IdProperty.ShouldNotBeNull();
    }

    [Fact]
    public void IdProperty_DefaultsToNull()
    {
        // Arrange & Act
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>();

        // Assert
        options.IdProperty.ShouldBeNull();
    }

    [Fact]
    public void CollectionName_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>();

        // Act
        options.CollectionName = "custom_collection";

        // Assert
        options.CollectionName.ShouldBe("custom_collection");
    }

    [Fact]
    public void CollectionName_DefaultsToNull()
    {
        // Arrange & Act
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>();

        // Assert
        options.CollectionName.ShouldBeNull();
    }

    [Fact]
    public void IdProperty_CanBeSetToExpression()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>();

        // Act
        options.IdProperty = doc => doc.Id;

        // Assert
        options.IdProperty.ShouldNotBeNull();
        // Verify the expression compiles and works
        var compiled = options.IdProperty.Compile();
        var testDoc = new OptionsTestDocument { Id = Guid.NewGuid() };
        compiled(testDoc).ShouldBe(testDoc.Id);
    }

    #endregion

    #region GetEffectiveCollectionName Tests

    [Fact]
    public void GetEffectiveCollectionName_WithCustomCollectionName_ReturnsCustomName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>
        {
            CollectionName = "my_custom_collection"
        };

        // Act
        var result = options.GetEffectiveCollectionName();

        // Assert
        result.ShouldBe("my_custom_collection");
    }

    [Fact]
    public void GetEffectiveCollectionName_WithNullCollectionName_ReturnsDefaultName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>
        {
            CollectionName = null
        };

        // Act
        var result = options.GetEffectiveCollectionName();

        // Assert
        result.ShouldBe("optionstestdocuments");
    }

    [Fact]
    public void GetEffectiveCollectionName_WithEmptyCollectionName_ReturnsDefaultName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>
        {
            CollectionName = string.Empty
        };

        // Act
        var result = options.GetEffectiveCollectionName();

        // Assert
        result.ShouldBe("optionstestdocuments");
    }

    [Fact]
    public void GetEffectiveCollectionName_WithWhitespaceCollectionName_ReturnsDefaultName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>
        {
            CollectionName = "   "
        };

        // Act
        var result = options.GetEffectiveCollectionName();

        // Assert
        result.ShouldBe("optionstestdocuments");
    }

    [Fact]
    public void GetEffectiveCollectionName_DefaultNaming_ConvertsToLowercase()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<UpperCaseDocument, Guid>();

        // Act
        var result = options.GetEffectiveCollectionName();

        // Assert
        result.ShouldBe("uppercasedocuments");
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithIdPropertyConfigured_DoesNotThrow()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>
        {
            IdProperty = x => x.Id
        };

        // Act & Assert
        Should.NotThrow(() => options.Validate());
    }

    [Fact]
    public void Validate_WithoutIdProperty_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<OptionsTestDocument, Guid>();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => options.Validate());
        exception.Message.ShouldContain("IdProperty must be configured");
        exception.Message.ShouldContain("OptionsTestDocument");
    }

    [Fact]
    public void Validate_WithNullIdProperty_ThrowsWithEntityTypeName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<UpperCaseDocument, Guid>
        {
            IdProperty = null
        };

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => options.Validate());
        exception.Message.ShouldContain("UpperCaseDocument");
    }

    #endregion
}

/// <summary>
/// Test document for MongoDbRepositoryOptions tests.
/// </summary>
public class OptionsTestDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Test document with uppercase name for testing naming conventions.
/// </summary>
public class UpperCaseDocument
{
    public Guid Id { get; set; }
}
