using Encina.Testing.Identity;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="RequestContext"/> to verify parameter validation.
/// </summary>
public class RequestContextGuardTests
{
    #region CreateAnonymousAt(DateTimeOffset timestamp, string correlationId)

    /// <summary>
    /// Verifies that CreateAnonymousAt throws ArgumentException when correlationId is null.
    /// </summary>
    [Fact]
    public void CreateAnonymousAt_NullCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        string correlationId = null!;
        var timestamp = TimeProvider.System.GetUtcNow();

        // Act & Assert
        var act = () => RequestContext.CreateAnonymousAt(timestamp, correlationId);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("correlationId");
    }

    /// <summary>
    /// Verifies that CreateAnonymousAt throws ArgumentException when correlationId is empty.
    /// </summary>
    [Fact]
    public void CreateAnonymousAt_EmptyCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var correlationId = string.Empty;
        var timestamp = TimeProvider.System.GetUtcNow();

        // Act & Assert
        var act = () => RequestContext.CreateAnonymousAt(timestamp, correlationId);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("correlationId");
    }

    /// <summary>
    /// Verifies that CreateAnonymousAt throws ArgumentException when correlationId is whitespace.
    /// </summary>
    [Fact]
    public void CreateAnonymousAt_WhitespaceCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var correlationId = "   ";
        var timestamp = TimeProvider.System.GetUtcNow();

        // Act & Assert
        var act = () => RequestContext.CreateAnonymousAt(timestamp, correlationId);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("correlationId");
    }

    /// <summary>
    /// Verifies that CreateAnonymousAt succeeds with a valid correlationId.
    /// </summary>
    [Fact]
    public void CreateAnonymousAt_ValidCorrelationId_ReturnsContextWithId()
    {
        // Arrange
        var correlationId = "test-correlation-123";
        var timestamp = TimeProvider.System.GetUtcNow();

        // Act
        var result = RequestContext.CreateAnonymousAt(timestamp, correlationId);

        // Assert
        result.ShouldNotBeNull();
        result.CorrelationId.ShouldBe(correlationId);
    }

    /// <summary>
    /// Verifies that CreateAnonymousAt uses the provided timestamp.
    /// </summary>
    [Fact]
    public void CreateAnonymousAt_ValidCorrelationId_UsesProvidedTimestamp()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var result = RequestContext.CreateAnonymousAt(timestamp, "test-123");

        // Assert
        result.Timestamp.ShouldBe(timestamp);
    }

    #endregion

    #region CreateAnonymousAt() (with auto-generated correlationId)

    /// <summary>
    /// Verifies that CreateAnonymousAt generates a non-empty correlationId when not provided.
    /// </summary>
    [Fact]
    public void CreateAnonymousAt_DefaultCorrelationId_GeneratesNonEmptyCorrelationId()
    {
        // Act
        var result = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");

        // Assert
        result.CorrelationId.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region WithMetadata

    /// <summary>
    /// Verifies that WithMetadata throws ArgumentException when key is null.
    /// </summary>
    [Fact]
    public void WithMetadata_NullKey_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");
        string key = null!;

        // Act & Assert
        var act = () => context.WithMetadata(key, "value");
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that WithMetadata throws ArgumentException when key is empty.
    /// </summary>
    [Fact]
    public void WithMetadata_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");

        // Act & Assert
        var act = () => context.WithMetadata(string.Empty, "value");
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that WithMetadata throws ArgumentException when key is whitespace.
    /// </summary>
    [Fact]
    public void WithMetadata_WhitespaceKey_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");

        // Act & Assert
        var act = () => context.WithMetadata("   ", "value");
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that WithMetadata returns a new instance (immutable).
    /// </summary>
    [Fact]
    public void WithMetadata_ValidKey_ReturnsNewInstance()
    {
        // Arrange
        var original = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");

        // Act
        var result = original.WithMetadata("key1", "value1");

        // Assert
        result.ShouldNotBeSameAs(original);
        result.Metadata.ShouldContainKey("key1");
        original.Metadata.ShouldNotContainKey("key1");
    }

    /// <summary>
    /// Verifies that WithMetadata allows null values.
    /// </summary>
    [Fact]
    public void WithMetadata_NullValue_DoesNotThrow()
    {
        // Arrange
        var context = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");

        // Act
        var result = context.WithMetadata("key1", null);

        // Assert
        result.Metadata.ShouldContainKey("key1");
        result.Metadata["key1"].ShouldBeNull();
    }

    /// <summary>
    /// Verifies that multiple WithMetadata calls accumulate metadata.
    /// </summary>
    [Fact]
    public void WithMetadata_MultipleCalls_AccumulatesMetadata()
    {
        // Arrange
        var context = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");

        // Act
        var result = context
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", "value2")
            .WithMetadata("key3", 42);

        // Assert
        result.Metadata.Count.ShouldBe(3);
    }

    #endregion

    #region WithTenantId / WithIdempotencyKey

    /// <summary>
    /// Verifies that WithTenantId returns a new instance with the tenantId set.
    /// </summary>
    [Fact]
    public void WithTenantId_SetsTenantId_ReturnsNewInstance()
    {
        // Arrange
        var context = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");

        // Act
        var result = context.WithTenantId("tenant-abc");

        // Assert
        result.ShouldNotBeSameAs(context);
        result.TenantId.ShouldBe("tenant-abc");
    }

    /// <summary>
    /// Verifies that WithIdempotencyKey returns a new instance with the key set.
    /// </summary>
    [Fact]
    public void WithIdempotencyKey_SetsKey_ReturnsNewInstance()
    {
        // Arrange
        var context = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "test-corr");

        // Act
        var result = context.WithIdempotencyKey("idem-key-001");

        // Assert
        result.ShouldNotBeSameAs(context);
        result.IdempotencyKey.ShouldBe("idem-key-001");
    }

    #endregion

    #region CreateForTest

    /// <summary>
    /// Verifies that CreateForTest creates a context with the specified values.
    /// </summary>
    [Fact]
    public void CreateForTest_WithAllParameters_SetsAllValues()
    {
        // Act
        var result = TestRequestContext.For(
            TestIdentity.User("user-1"),
            tenantId: "tenant-1",
            idempotencyKey: "key-1",
            correlationId: "corr-1");

        // Assert
        result.UserId.ShouldBe("user-1");
        result.TenantId.ShouldBe("tenant-1");
        result.IdempotencyKey.ShouldBe("key-1");
        result.CorrelationId.ShouldBe("corr-1");
    }

    /// <summary>
    /// Verifies that CreateForTest with no parameters generates default values.
    /// </summary>
    [Fact]
    public void CreateForTest_NoParameters_GeneratesDefaults()
    {
        // Act
        var result = RequestContext.CreateForTest();

        // Assert
        result.CorrelationId.ShouldStartWith("test-");
        result.UserId.ShouldBeNull();
        result.TenantId.ShouldBeNull();
        result.IdempotencyKey.ShouldBeNull();
    }

    #endregion

    #region ToString

    /// <summary>
    /// Verifies that ToString includes the correlationId.
    /// </summary>
    [Fact]
    public void ToString_IncludesCorrelationId()
    {
        // Arrange
        var context = RequestContext.CreateAnonymousAt(TimeProvider.System.GetUtcNow(), "my-correlation-id");

        // Act
        var result = context.ToString();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("my-correlation-id");
    }

    #endregion
}
