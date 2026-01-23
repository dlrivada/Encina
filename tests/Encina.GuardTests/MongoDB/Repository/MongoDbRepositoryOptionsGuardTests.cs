using System.Reflection;
using Encina.MongoDB.Repository;
using Shouldly;

namespace Encina.GuardTests.MongoDB.Repository;

/// <summary>
/// Guard clause tests for <see cref="MongoDbRepositoryOptions{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "MongoDB")]
public sealed class MongoDbRepositoryOptionsGuardTests
{
    [Fact]
    public void Validate_NullIdProperty_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<MongoOptionsTestEntity, Guid>
        {
            CollectionName = "test_entities",
            IdProperty = null
        };

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            InvokeValidate(options));
        ex.Message.ShouldContain("IdProperty");
        ex.Message.ShouldContain("MongoOptionsTestEntity");
    }

    [Fact]
    public void GetEffectiveCollectionName_WithCollectionName_ReturnsConfiguredName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<MongoOptionsTestEntity, Guid>
        {
            CollectionName = "custom_entities"
        };

        // Act
        var result = InvokeGetEffectiveCollectionName(options);

        // Assert
        result.ShouldBe("custom_entities");
    }

    [Fact]
    public void GetEffectiveCollectionName_WithNullCollectionName_ReturnsDefaultName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<MongoOptionsTestEntity, Guid>
        {
            CollectionName = null
        };

        // Act
        var result = InvokeGetEffectiveCollectionName(options);

        // Assert
        result.ShouldBe("mongooptionstestentitys");
    }

    [Fact]
    public void GetEffectiveCollectionName_WithEmptyCollectionName_ReturnsDefaultName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<MongoOptionsTestEntity, Guid>
        {
            CollectionName = string.Empty
        };

        // Act
        var result = InvokeGetEffectiveCollectionName(options);

        // Assert
        result.ShouldBe("mongooptionstestentitys");
    }

    [Fact]
    public void GetEffectiveCollectionName_WithWhitespaceCollectionName_ReturnsDefaultName()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<MongoOptionsTestEntity, Guid>
        {
            CollectionName = "   "
        };

        // Act
        var result = InvokeGetEffectiveCollectionName(options);

        // Assert
        result.ShouldBe("mongooptionstestentitys");
    }

    [Fact]
    public void Validate_WithIdProperty_DoesNotThrow()
    {
        // Arrange
        var options = new MongoDbRepositoryOptions<MongoOptionsTestEntity, Guid>
        {
            CollectionName = "test_entities",
            IdProperty = e => e.Id
        };

        // Act & Assert - Should not throw
        Should.NotThrow(() => InvokeValidate(options));
    }

    /// <summary>
    /// Helper method to invoke internal Validate method via reflection.
    /// Handles TargetInvocationException to unwrap the actual exception.
    /// </summary>
    private static void InvokeValidate<TEntity, TId>(MongoDbRepositoryOptions<TEntity, TId> options)
        where TEntity : class
        where TId : notnull
    {
        var method = typeof(MongoDbRepositoryOptions<TEntity, TId>)
            .GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Instance);

        if (method is null)
        {
            throw new InvalidOperationException("Validate method not found");
        }

        try
        {
            method.Invoke(options, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Helper method to invoke internal GetEffectiveCollectionName method via reflection.
    /// </summary>
    private static string InvokeGetEffectiveCollectionName<TEntity, TId>(MongoDbRepositoryOptions<TEntity, TId> options)
        where TEntity : class
        where TId : notnull
    {
        var method = typeof(MongoDbRepositoryOptions<TEntity, TId>)
            .GetMethod("GetEffectiveCollectionName", BindingFlags.NonPublic | BindingFlags.Instance);

        if (method is null)
        {
            throw new InvalidOperationException("GetEffectiveCollectionName method not found");
        }

        return (string)method.Invoke(options, null)!;
    }
}

/// <summary>
/// Test entity for MongoDB options guard tests.
/// </summary>
public sealed class MongoOptionsTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
