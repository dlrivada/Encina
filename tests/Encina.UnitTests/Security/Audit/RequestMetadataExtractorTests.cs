using System.Reflection;
using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for RequestMetadataExtractor (internal class).
/// Uses reflection to access the internal type.
/// </summary>
public class RequestMetadataExtractorTests
{
    private static readonly Type ExtractorType;
    private static readonly MethodInfo ExtractFromTypeNameMethod;
    private static readonly MethodInfo TryExtractEntityIdMethod;
    private static readonly MethodInfo ClearCacheMethod;

    static RequestMetadataExtractorTests()
    {
        var assembly = typeof(AuditEntry).Assembly;
        ExtractorType = assembly.GetType("Encina.Security.Audit.RequestMetadataExtractor")!;

        ExtractFromTypeNameMethod = ExtractorType.GetMethod(
            "ExtractFromTypeName",
            BindingFlags.Public | BindingFlags.Static)!;

        TryExtractEntityIdMethod = ExtractorType.GetMethod(
            "TryExtractEntityId",
            BindingFlags.Public | BindingFlags.Static)!;

        ClearCacheMethod = ExtractorType.GetMethod(
            "ClearCache",
            BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    public RequestMetadataExtractorTests()
    {
        // Clear cache before each test for isolation
        ClearCacheMethod?.Invoke(null, null);
    }

    private static (string EntityType, string Action) ExtractFromTypeName(Type type)
    {
        var result = ExtractFromTypeNameMethod.Invoke(null, [type]);
        var tuple = ((string, string))result!;
        return tuple;
    }

    private static string? TryExtractEntityId(object request)
    {
        return (string?)TryExtractEntityIdMethod.Invoke(null, [request]);
    }

    #region ExtractFromTypeName Tests

    [Theory]
    [InlineData(typeof(CreateOrderCommand), "Order", "Create")]
    [InlineData(typeof(UpdateCustomerCommand), "Customer", "Update")]
    [InlineData(typeof(DeleteProductCommand), "Product", "Delete")]
    [InlineData(typeof(GetUserQuery), "User", "Get")]
    [InlineData(typeof(ListOrdersQuery), "Orders", "List")]
    [InlineData(typeof(FindCustomerQuery), "Customer", "Find")]
    [InlineData(typeof(SearchProductsQuery), "Products", "Search")]
    [InlineData(typeof(AddItemCommand), "Item", "Add")]
    [InlineData(typeof(RemoveItemCommand), "Item", "Remove")]
    [InlineData(typeof(SetStatusCommand), "Status", "Set")]
    [InlineData(typeof(ClearCacheCommand), "Cache", "Clear")]
    [InlineData(typeof(ExecutePaymentCommand), "Payment", "Execute")]
    [InlineData(typeof(ProcessOrderCommand), "Order", "Process")]
    [InlineData(typeof(SendEmailCommand), "Email", "Send")]
    [InlineData(typeof(ValidateAddressCommand), "Address", "Validate")]
    [InlineData(typeof(CheckInventoryCommand), "Inventory", "Check")]
    [InlineData(typeof(VerifyIdentityCommand), "Identity", "Verify")]
    public void ExtractFromTypeName_WithStandardPattern_ShouldExtractCorrectly(
        Type requestType, string expectedEntity, string expectedAction)
    {
        // Act
        var (entityType, action) = ExtractFromTypeName(requestType);

        // Assert
        entityType.Should().Be(expectedEntity);
        action.Should().Be(expectedAction);
    }

    [Theory]
    [InlineData(typeof(SomeCommand), "Some", "Unknown")]
    [InlineData(typeof(SomeQuery), "Some", "Unknown")]
    public void ExtractFromTypeName_WithoutActionPrefix_ShouldFallbackToUnknown(
        Type requestType, string expectedEntity, string expectedAction)
    {
        // Act
        var (entityType, action) = ExtractFromTypeName(requestType);

        // Assert
        entityType.Should().Be(expectedEntity);
        action.Should().Be(expectedAction);
    }

    [Fact]
    public void ExtractFromTypeName_WithNoSuffix_ShouldReturnFullName()
    {
        // Act
        var (entityType, action) = ExtractFromTypeName(typeof(CustomRequest));

        // Assert
        entityType.Should().Be("CustomRequest");
        action.Should().Be("Unknown");
    }

    [Fact]
    public void ExtractFromTypeName_WithNullType_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => ExtractFromTypeNameMethod.Invoke(null, [null]);

        // Assert
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentNullException>();
    }

    #endregion

    #region TryExtractEntityId Tests

    [Fact]
    public void TryExtractEntityId_WithIdProperty_ShouldExtractId()
    {
        // Arrange
        var request = new RequestWithId { Id = Guid.NewGuid() };

        // Act
        var result = TryExtractEntityId(request);

        // Assert
        result.Should().Be(request.Id.ToString());
    }

    [Fact]
    public void TryExtractEntityId_WithEntityIdProperty_ShouldExtractEntityId()
    {
        // Arrange
        var request = new RequestWithEntityId { EntityId = "entity-123" };

        // Act
        var result = TryExtractEntityId(request);

        // Assert
        result.Should().Be("entity-123");
    }

    [Fact]
    public void TryExtractEntityId_WithCustomIdProperty_ShouldExtractIt()
    {
        // Arrange
        var request = new RequestWithOrderId { OrderId = 12345 };

        // Act
        var result = TryExtractEntityId(request);

        // Assert
        result.Should().Be("12345");
    }

    [Fact]
    public void TryExtractEntityId_IdHasPriorityOverEntityId()
    {
        // Arrange
        var request = new RequestWithBothIds { Id = "primary-id", EntityId = "secondary-id" };

        // Act
        var result = TryExtractEntityId(request);

        // Assert
        result.Should().Be("primary-id");
    }

    [Fact]
    public void TryExtractEntityId_EntityIdHasPriorityOverSuffixId()
    {
        // Arrange
        var request = new RequestWithEntityIdAndSuffixId { EntityId = "entity-id", CustomerId = "customer-id" };

        // Act
        var result = TryExtractEntityId(request);

        // Assert
        result.Should().Be("entity-id");
    }

    [Fact]
    public void TryExtractEntityId_WithNullRequest_ShouldReturnNull()
    {
        // Act
        var result = TryExtractEntityId(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryExtractEntityId_WithNoIdProperty_ShouldReturnNull()
    {
        // Arrange
        var request = new RequestWithoutId { Name = "Test" };

        // Act
        var result = TryExtractEntityId(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryExtractEntityId_WithNullIdValue_ShouldReturnNull()
    {
        // Arrange
        var request = new RequestWithNullableId { Id = null };

        // Act
        var result = TryExtractEntityId(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryExtractEntityId_ShouldCachePropertyLookup()
    {
        // Arrange
        var request1 = new RequestWithId { Id = Guid.NewGuid() };
        var request2 = new RequestWithId { Id = Guid.NewGuid() };

        // Act - Call twice with same type
        var result1 = TryExtractEntityId(request1);
        var result2 = TryExtractEntityId(request2);

        // Assert - Both should work (cache should not cause issues)
        result1.Should().Be(request1.Id.ToString());
        result2.Should().Be(request2.Id.ToString());
    }

    #endregion

    #region Test Request Types

    // Command/Query types for ExtractFromTypeName tests
    private sealed class CreateOrderCommand { }
    private sealed class UpdateCustomerCommand { }
    private sealed class DeleteProductCommand { }
    private sealed class GetUserQuery { }
    private sealed class ListOrdersQuery { }
    private sealed class FindCustomerQuery { }
    private sealed class SearchProductsQuery { }
    private sealed class AddItemCommand { }
    private sealed class RemoveItemCommand { }
    private sealed class SetStatusCommand { }
    private sealed class ClearCacheCommand { }
    private sealed class ExecutePaymentCommand { }
    private sealed class ProcessOrderCommand { }
    private sealed class SendEmailCommand { }
    private sealed class ValidateAddressCommand { }
    private sealed class CheckInventoryCommand { }
    private sealed class VerifyIdentityCommand { }
    private sealed class SomeCommand { }
    private sealed class SomeQuery { }
    private sealed class CustomRequest { }

    // Request types for TryExtractEntityId tests
    private sealed class RequestWithId { public Guid Id { get; init; } }
    private sealed class RequestWithEntityId { public string? EntityId { get; init; } }
    private sealed class RequestWithOrderId { public int OrderId { get; init; } }
    private sealed class RequestWithBothIds { public string? Id { get; init; } public string? EntityId { get; init; } }
    private sealed class RequestWithEntityIdAndSuffixId { public string? EntityId { get; init; } public string? CustomerId { get; init; } }
    private sealed class RequestWithoutId { public string? Name { get; init; } }
    private sealed class RequestWithNullableId { public string? Id { get; init; } }

    #endregion
}
