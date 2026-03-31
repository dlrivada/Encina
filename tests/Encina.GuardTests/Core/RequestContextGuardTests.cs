namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="RequestContext"/> to verify parameter validation.
/// </summary>
public class RequestContextGuardTests
{
    #region Create(string correlationId)

    /// <summary>
    /// Verifies that Create throws ArgumentException when correlationId is null.
    /// </summary>
    [Fact]
    public void Create_NullCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        string correlationId = null!;

        // Act & Assert
        var act = () => RequestContext.Create(correlationId);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("correlationId");
    }

    /// <summary>
    /// Verifies that Create throws ArgumentException when correlationId is empty.
    /// </summary>
    [Fact]
    public void Create_EmptyCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var correlationId = string.Empty;

        // Act & Assert
        var act = () => RequestContext.Create(correlationId);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("correlationId");
    }

    /// <summary>
    /// Verifies that Create throws ArgumentException when correlationId is whitespace.
    /// </summary>
    [Fact]
    public void Create_WhitespaceCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var correlationId = "   ";

        // Act & Assert
        var act = () => RequestContext.Create(correlationId);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("correlationId");
    }

    /// <summary>
    /// Verifies that Create succeeds with a valid correlationId.
    /// </summary>
    [Fact]
    public void Create_ValidCorrelationId_ReturnsContextWithId()
    {
        // Arrange
        var correlationId = "test-correlation-123";

        // Act
        var result = RequestContext.Create(correlationId);

        // Assert
        result.ShouldNotBeNull();
        result.CorrelationId.ShouldBe(correlationId);
    }

    /// <summary>
    /// Verifies that Create with correlationId sets Timestamp to a recent UTC value.
    /// </summary>
    [Fact]
    public void Create_ValidCorrelationId_SetsTimestampToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = RequestContext.Create("test-123");

        // Assert
        var after = DateTimeOffset.UtcNow;
        result.Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        result.Timestamp.ShouldBeLessThanOrEqualTo(after);
    }

    #endregion

    #region Create() (parameterless)

    /// <summary>
    /// Verifies that parameterless Create generates a non-empty correlationId.
    /// </summary>
    [Fact]
    public void Create_Parameterless_GeneratesNonEmptyCorrelationId()
    {
        // Act
        var result = RequestContext.Create();

        // Assert
        result.CorrelationId.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifies that parameterless Create sets empty metadata.
    /// </summary>
    [Fact]
    public void Create_Parameterless_HasEmptyMetadata()
    {
        // Act
        var result = RequestContext.Create();

        // Assert
        result.Metadata.ShouldNotBeNull();
        result.Metadata.Count.ShouldBe(0);
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
        var context = RequestContext.Create();
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
        var context = RequestContext.Create();

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
        var context = RequestContext.Create();

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
        var original = RequestContext.Create();

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
        var context = RequestContext.Create();

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
        var context = RequestContext.Create();

        // Act
        var result = context
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", "value2")
            .WithMetadata("key3", 42);

        // Assert
        result.Metadata.Count.ShouldBe(3);
    }

    #endregion

    #region WithUserId / WithTenantId / WithIdempotencyKey

    /// <summary>
    /// Verifies that WithUserId returns a new instance with the userId set.
    /// </summary>
    [Fact]
    public void WithUserId_SetsUserId_ReturnsNewInstance()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var result = context.WithUserId("user-123");

        // Assert
        result.ShouldNotBeSameAs(context);
        result.UserId.ShouldBe("user-123");
    }

    /// <summary>
    /// Verifies that WithUserId allows null to clear the userId.
    /// </summary>
    [Fact]
    public void WithUserId_Null_ClearsUserId()
    {
        // Arrange
        var context = RequestContext.Create().WithUserId("user-123");

        // Act
        var result = context.WithUserId(null);

        // Assert
        result.UserId.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that WithTenantId returns a new instance with the tenantId set.
    /// </summary>
    [Fact]
    public void WithTenantId_SetsTenantId_ReturnsNewInstance()
    {
        // Arrange
        var context = RequestContext.Create();

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
        var context = RequestContext.Create();

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
        var result = RequestContext.CreateForTest(
            userId: "user-1",
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
        var context = RequestContext.Create("my-correlation-id");

        // Act
        var result = context.ToString();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("my-correlation-id");
    }

    #endregion
}
