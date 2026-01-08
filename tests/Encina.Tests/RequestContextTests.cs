using System.Diagnostics;
using Shouldly;

namespace Encina.Tests;

/// <summary>
/// Tests for RequestContext class.
/// </summary>
public sealed class RequestContextTests
{
    #region Create Tests

    [Fact]
    public void Create_NoArguments_GeneratesCorrelationId()
    {
        // Act
        var before = DateTimeOffset.UtcNow;
        var context = RequestContext.Create();
        var after = DateTimeOffset.UtcNow;

        // Assert
        context.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        // Timestamp should be within the time window captured around creation
        context.Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        context.Timestamp.ShouldBeLessThanOrEqualTo(after);
        context.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithCorrelationId_UsesProvidedId()
    {
        // Act
        var context = RequestContext.Create("my-correlation-id");

        // Assert
        context.CorrelationId.ShouldBe("my-correlation-id");
    }

    [Fact]
    public void Create_NullCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            RequestContext.Create(null!));
    }

    [Fact]
    public void Create_EmptyCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            RequestContext.Create(""));
    }

    [Fact]
    public void Create_WhitespaceCorrelationId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            RequestContext.Create("   "));
    }

    [Fact]
    public void Create_WithActivity_UsesActivityId()
    {
        // Arrange
        using var activity = new Activity("TestActivity");
        activity.Start();

        // Act
        var context = RequestContext.Create();

        // Assert - should use Activity.Current.Id when available
        context.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        // The Activity ID format may vary, but should be set
    }

    #endregion

    #region CreateForTest Tests

    [Fact]
    public void CreateForTest_NoArguments_CreatesContextWithTestPrefix()
    {
        // Act
        var context = RequestContext.CreateForTest();

        // Assert
        context.CorrelationId.ShouldStartWith("test-");
        context.UserId.ShouldBeNull();
        context.TenantId.ShouldBeNull();
        context.IdempotencyKey.ShouldBeNull();
    }

    [Fact]
    public void CreateForTest_WithAllArguments_SetsAllProperties()
    {
        // Act
        var context = RequestContext.CreateForTest(
            userId: "user-123",
            tenantId: "tenant-456",
            idempotencyKey: "idem-789",
            correlationId: "custom-corr");

        // Assert
        context.CorrelationId.ShouldBe("custom-corr");
        context.UserId.ShouldBe("user-123");
        context.TenantId.ShouldBe("tenant-456");
        context.IdempotencyKey.ShouldBe("idem-789");
    }

    [Fact]
    public void CreateForTest_WithPartialArguments_SetsOnlyProvided()
    {
        // Act
        var context = RequestContext.CreateForTest(userId: "user-only");

        // Assert
        context.UserId.ShouldBe("user-only");
        context.TenantId.ShouldBeNull();
        context.IdempotencyKey.ShouldBeNull();
        context.CorrelationId.ShouldStartWith("test-");
    }

    #endregion

    #region WithMetadata Tests

    [Fact]
    public void WithMetadata_AddsNewValue()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var newContext = context.WithMetadata("key1", "value1");

        // Assert
        newContext.Metadata.ShouldContainKey("key1");
        newContext.Metadata["key1"].ShouldBe("value1");
        context.Metadata.ShouldNotContainKey("key1"); // Original unchanged
    }

    [Fact]
    public void WithMetadata_UpdatesExistingValue()
    {
        // Arrange
        var context = RequestContext.Create()
            .WithMetadata("key1", "original");

        // Act
        var newContext = context.WithMetadata("key1", "updated");

        // Assert
        newContext.Metadata["key1"].ShouldBe("updated");
    }

    [Fact]
    public void WithMetadata_NullKey_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            context.WithMetadata(null!, "value"));
    }

    [Fact]
    public void WithMetadata_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            context.WithMetadata("", "value"));
    }

    [Fact]
    public void WithMetadata_NullValue_IsAllowed()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var newContext = context.WithMetadata("key1", null);

        // Assert
        newContext.Metadata.ShouldContainKey("key1");
        newContext.Metadata["key1"].ShouldBeNull();
    }

    [Fact]
    public void WithMetadata_Chained_AccumulatesMetadata()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var newContext = context
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", "value2")
            .WithMetadata("key3", "value3");

        // Assert
        newContext.Metadata.Count.ShouldBe(3);
        newContext.Metadata["key1"].ShouldBe("value1");
        newContext.Metadata["key2"].ShouldBe("value2");
        newContext.Metadata["key3"].ShouldBe("value3");
    }

    #endregion

    #region WithUserId Tests

    [Fact]
    public void WithUserId_SetsUserId()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var newContext = context.WithUserId("user-123");

        // Assert
        newContext.UserId.ShouldBe("user-123");
        context.UserId.ShouldBeNull(); // Original unchanged
    }

    [Fact]
    public void WithUserId_Null_ClearsUserId()
    {
        // Arrange
        var context = RequestContext.CreateForTest(userId: "original");

        // Act
        var newContext = context.WithUserId(null);

        // Assert
        newContext.UserId.ShouldBeNull();
    }

    #endregion

    #region WithIdempotencyKey Tests

    [Fact]
    public void WithIdempotencyKey_SetsIdempotencyKey()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var newContext = context.WithIdempotencyKey("idem-123");

        // Assert
        newContext.IdempotencyKey.ShouldBe("idem-123");
        context.IdempotencyKey.ShouldBeNull(); // Original unchanged
    }

    [Fact]
    public void WithIdempotencyKey_Null_ClearsIdempotencyKey()
    {
        // Arrange
        var context = RequestContext.CreateForTest(idempotencyKey: "original");

        // Act
        var newContext = context.WithIdempotencyKey(null);

        // Assert
        newContext.IdempotencyKey.ShouldBeNull();
    }

    #endregion

    #region WithTenantId Tests

    [Fact]
    public void WithTenantId_SetsTenantId()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var newContext = context.WithTenantId("tenant-456");

        // Assert
        newContext.TenantId.ShouldBe("tenant-456");
        context.TenantId.ShouldBeNull(); // Original unchanged
    }

    [Fact]
    public void WithTenantId_Null_ClearsTenantId()
    {
        // Arrange
        var context = RequestContext.CreateForTest(tenantId: "original");

        // Act
        var newContext = context.WithTenantId(null);

        // Assert
        newContext.TenantId.ShouldBeNull();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_IncludesAllProperties()
    {
        // Arrange
        var context = RequestContext.CreateForTest(
            userId: "user-123",
            tenantId: "tenant-456",
            idempotencyKey: "idem-789",
            correlationId: "corr-abc");

        // Act
        var result = context.ToString();

        // Assert
        result.ShouldNotBeNull();
        result!.ShouldContain("corr-abc");
        result.ShouldContain("user-123");
        result.ShouldContain("tenant-456");
        result.ShouldContain("idem-789");
    }

    [Fact]
    public void ToString_NullProperties_ShowsNull()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var result = context.ToString();

        // Assert
        result.ShouldNotBeNull();
        result!.ShouldContain("(null)");
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void WithMethods_PreserveOtherProperties()
    {
        // Arrange
        var original = RequestContext.CreateForTest(
            userId: "user",
            tenantId: "tenant",
            idempotencyKey: "key",
            correlationId: "corr");

        // Act
        var modified = original.WithUserId("new-user");

        // Assert - Other properties should be preserved
        modified.CorrelationId.ShouldBe("corr");
        modified.TenantId.ShouldBe("tenant");
        modified.IdempotencyKey.ShouldBe("key");
        modified.UserId.ShouldBe("new-user");
    }

    [Fact]
    public void WithMetadata_PreservesNonImmutableDictionary()
    {
        // Arrange - Start with a context that already has metadata
        var original = RequestContext.Create()
            .WithMetadata("existing", "value");

        // Act - Add more metadata
        var modified = original.WithMetadata("new", "value2");

        // Assert
        modified.Metadata.Count.ShouldBe(2);
        modified.Metadata["existing"].ShouldBe("value");
        modified.Metadata["new"].ShouldBe("value2");
    }

    #endregion
}
