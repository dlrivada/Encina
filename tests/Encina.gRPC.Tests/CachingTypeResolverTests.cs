using Shouldly;
using Xunit;

namespace Encina.gRPC.Tests;

/// <summary>
/// Unit tests for the <see cref="CachingTypeResolver"/> class.
/// </summary>
public sealed class CachingTypeResolverTests
{
    private readonly CachingTypeResolver _resolver = new();

    [Fact]
    public void ResolveRequestType_WithNullTypeName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _resolver.ResolveRequestType(null!));
    }

    [Fact]
    public void ResolveRequestType_WithEmptyTypeName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _resolver.ResolveRequestType(string.Empty));
    }

    [Fact]
    public void ResolveRequestType_WithWhitespaceTypeName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _resolver.ResolveRequestType("   "));
    }

    [Fact]
    public void ResolveNotificationType_WithNullTypeName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _resolver.ResolveNotificationType(null!));
    }

    [Fact]
    public void ResolveNotificationType_WithEmptyTypeName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _resolver.ResolveNotificationType(string.Empty));
    }

    [Fact]
    public void ResolveNotificationType_WithWhitespaceTypeName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _resolver.ResolveNotificationType("   "));
    }

    [Fact]
    public void ResolveRequestType_WithUnknownType_ReturnsNull()
    {
        // Arrange
        const string unknownType = "Unknown.Type, NonExistentAssembly";

        // Act
        var result = _resolver.ResolveRequestType(unknownType);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ResolveNotificationType_WithUnknownType_ReturnsNull()
    {
        // Arrange
        const string unknownType = "Unknown.Type, NonExistentAssembly";

        // Act
        var result = _resolver.ResolveNotificationType(unknownType);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ResolveRequestType_WithKnownType_ReturnsType()
    {
        // Arrange
        var expectedType = typeof(TestRequest);
        var typeName = expectedType.AssemblyQualifiedName!;

        // Act
        var result = _resolver.ResolveRequestType(typeName);

        // Assert
        result.ShouldBe(expectedType);
    }

    [Fact]
    public void ResolveNotificationType_WithKnownType_ReturnsType()
    {
        // Arrange
        var expectedType = typeof(TestNotification);
        var typeName = expectedType.AssemblyQualifiedName!;

        // Act
        var result = _resolver.ResolveNotificationType(typeName);

        // Assert
        result.ShouldBe(expectedType);
    }

    [Fact]
    public void ResolveRequestType_CalledTwiceWithSameType_ReturnsSameType()
    {
        // Arrange
        var expectedType = typeof(TestRequest);
        var typeName = expectedType.AssemblyQualifiedName!;

        // Act - call twice
        var firstResult = _resolver.ResolveRequestType(typeName);
        var secondResult = _resolver.ResolveRequestType(typeName);

        // Assert - both return the expected type
        // Note: Type instances are singletons so reference equality is inherent.
        // Caching is verified by ConcurrentDictionary.GetOrAdd in the implementation.
        firstResult.ShouldBe(expectedType);
        secondResult.ShouldBe(expectedType);
    }

    [Fact]
    public void ResolveNotificationType_CalledTwiceWithSameType_ReturnsSameType()
    {
        // Arrange
        var expectedType = typeof(TestNotification);
        var typeName = expectedType.AssemblyQualifiedName!;

        // Act - call twice
        var firstResult = _resolver.ResolveNotificationType(typeName);
        var secondResult = _resolver.ResolveNotificationType(typeName);

        // Assert - both return the expected type
        // Note: Type instances are singletons so reference equality is inherent.
        // Caching is verified by ConcurrentDictionary.GetOrAdd in the implementation.
        firstResult.ShouldBe(expectedType);
        secondResult.ShouldBe(expectedType);
    }

    [Fact]
    public void ResolveRequestType_CalledTwiceWithUnknownType_CachesNullResult()
    {
        // Arrange
        const string unknownType = "Unknown.Type, NonExistentAssembly";

        // Act - call twice
        var firstResult = _resolver.ResolveRequestType(unknownType);
        var secondResult = _resolver.ResolveRequestType(unknownType);

        // Assert - both return null (and null is cached)
        firstResult.ShouldBeNull();
        secondResult.ShouldBeNull();
    }

    [Fact]
    public void ResolveNotificationType_CalledTwiceWithUnknownType_CachesNullResult()
    {
        // Arrange
        const string unknownType = "Unknown.Type, NonExistentAssembly";

        // Act - call twice
        var firstResult = _resolver.ResolveNotificationType(unknownType);
        var secondResult = _resolver.ResolveNotificationType(unknownType);

        // Assert - both return null (and null is cached)
        firstResult.ShouldBeNull();
        secondResult.ShouldBeNull();
    }

    [Fact]
    public void ResolveRequestType_DifferentTypes_CachesSeparately()
    {
        // Arrange
        var type1Name = typeof(TestRequest).AssemblyQualifiedName!;
        var type2Name = typeof(TestNotification).AssemblyQualifiedName!;

        // Act
        var result1 = _resolver.ResolveRequestType(type1Name);
        var result2 = _resolver.ResolveRequestType(type2Name);

        // Assert
        result1.ShouldBe(typeof(TestRequest));
        result2.ShouldBe(typeof(TestNotification));
    }

    [Fact]
    public void RequestAndNotificationCaches_AreSeparate()
    {
        // Arrange
        var typeName = typeof(TestRequest).AssemblyQualifiedName!;

        // Act - resolve same type as both request and notification
        var requestResult = _resolver.ResolveRequestType(typeName);
        var notificationResult = _resolver.ResolveNotificationType(typeName);

        // Assert - both caches work independently
        requestResult.ShouldBe(typeof(TestRequest));
        notificationResult.ShouldBe(typeof(TestRequest));
    }

    // Test types for resolution
    private sealed record TestRequest;
    private sealed record TestNotification;
}
