#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorAggregate"/>.
/// </summary>
public class ProcessorAggregateTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    #region Register Tests

    [Fact]
    public void Register_ValidParameters_SetsProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Stripe";
        var country = "US";
        var contactEmail = "dpo@stripe.com";
        var authorizationType = SubProcessorAuthorizationType.General;

        // Act
        var aggregate = ProcessorAggregate.Register(
            id, name, country, contactEmail,
            parentProcessorId: null, depth: 0, authorizationType, FixedNow);

        // Assert
        aggregate.Id.Should().Be(id);
        aggregate.Name.Should().Be(name);
        aggregate.Country.Should().Be(country);
        aggregate.ContactEmail.Should().Be(contactEmail);
        aggregate.ParentProcessorId.Should().BeNull();
        aggregate.Depth.Should().Be(0);
        aggregate.AuthorizationType.Should().Be(authorizationType);
        aggregate.IsRemoved.Should().BeFalse();
        aggregate.CreatedAtUtc.Should().Be(FixedNow);
        aggregate.LastUpdatedAtUtc.Should().Be(FixedNow);
    }

    [Fact]
    public void Register_WithParentProcessor_SetsHierarchy()
    {
        // Arrange
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        // Act
        var aggregate = ProcessorAggregate.Register(
            id, "Sub-Processor", "DE", null,
            parentProcessorId: parentId, depth: 1,
            SubProcessorAuthorizationType.Specific, FixedNow);

        // Assert
        aggregate.ParentProcessorId.Should().Be(parentId);
        aggregate.Depth.Should().Be(1);
    }

    [Fact]
    public void Register_WithTenantAndModule_SetsScoping()
    {
        // Arrange
        var tenantId = "tenant-42";
        var moduleId = "billing-module";

        // Act
        var aggregate = ProcessorAggregate.Register(
            Guid.NewGuid(), "AWS", "US", null,
            parentProcessorId: null, depth: 0,
            SubProcessorAuthorizationType.General, FixedNow,
            tenantId: tenantId, moduleId: moduleId);

        // Assert
        aggregate.TenantId.Should().Be(tenantId);
        aggregate.ModuleId.Should().Be(moduleId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => ProcessorAggregate.Register(
            Guid.NewGuid(), name!, "US", null,
            parentProcessorId: null, depth: 0,
            SubProcessorAuthorizationType.General, FixedNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_NullOrWhitespaceCountry_ThrowsArgumentException(string? country)
    {
        // Act
        var act = () => ProcessorAggregate.Register(
            Guid.NewGuid(), "Stripe", country!, null,
            parentProcessorId: null, depth: 0,
            SubProcessorAuthorizationType.General, FixedNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("country");
    }

    [Fact]
    public void Register_NegativeDepth_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => ProcessorAggregate.Register(
            Guid.NewGuid(), "Stripe", "US", null,
            parentProcessorId: null, depth: -1,
            SubProcessorAuthorizationType.General, FixedNow);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("depth");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ValidParameters_UpdatesProperties()
    {
        // Arrange
        var aggregate = CreateDefaultProcessor();
        var updatedAt = FixedNow.AddDays(10);

        // Act
        aggregate.Update("Updated Name", "DE", "new@email.com",
            SubProcessorAuthorizationType.Specific, updatedAt);

        // Assert
        aggregate.Name.Should().Be("Updated Name");
        aggregate.Country.Should().Be("DE");
        aggregate.ContactEmail.Should().Be("new@email.com");
        aggregate.AuthorizationType.Should().Be(SubProcessorAuthorizationType.Specific);
        aggregate.LastUpdatedAtUtc.Should().Be(updatedAt);
    }

    [Fact]
    public void Update_WhenRemoved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultProcessor();
        aggregate.Remove("No longer needed", FixedNow.AddDays(1));

        // Act
        var act = () => aggregate.Update("New Name", "DE", null,
            SubProcessorAuthorizationType.General, FixedNow.AddDays(2));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_ValidReason_SetsIsRemovedTrue()
    {
        // Arrange
        var aggregate = CreateDefaultProcessor();
        var removedAt = FixedNow.AddDays(5);

        // Act
        aggregate.Remove("Contract terminated", removedAt);

        // Assert
        aggregate.IsRemoved.Should().BeTrue();
        aggregate.LastUpdatedAtUtc.Should().Be(removedAt);
    }

    [Fact]
    public void Remove_WhenAlreadyRemoved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultProcessor();
        aggregate.Remove("First removal", FixedNow.AddDays(1));

        // Act
        var act = () => aggregate.Remove("Second removal", FixedNow.AddDays(2));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region AddSubProcessor Tests

    [Fact]
    public void AddSubProcessor_ValidParameters_UpdatesLastUpdated()
    {
        // Arrange
        var aggregate = CreateDefaultProcessor();
        var addedAt = FixedNow.AddDays(3);

        // Act
        aggregate.AddSubProcessor(Guid.NewGuid(), "Sub-Processor A", depth: 1, addedAt);

        // Assert
        aggregate.LastUpdatedAtUtc.Should().Be(addedAt);
    }

    [Fact]
    public void AddSubProcessor_WhenRemoved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultProcessor();
        aggregate.Remove("Removed", FixedNow.AddDays(1));

        // Act
        var act = () => aggregate.AddSubProcessor(
            Guid.NewGuid(), "Sub-Processor A", depth: 1, FixedNow.AddDays(2));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region RemoveSubProcessor Tests

    [Fact]
    public void RemoveSubProcessor_WhenRemoved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDefaultProcessor();
        aggregate.Remove("Removed", FixedNow.AddDays(1));

        // Act
        var act = () => aggregate.RemoveSubProcessor(
            Guid.NewGuid(), "No longer needed", FixedNow.AddDays(2));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Helpers

    private static ProcessorAggregate CreateDefaultProcessor()
    {
        return ProcessorAggregate.Register(
            Guid.NewGuid(), "Stripe", "US", "dpo@stripe.com",
            parentProcessorId: null, depth: 0,
            SubProcessorAuthorizationType.General, FixedNow);
    }

    #endregion
}
