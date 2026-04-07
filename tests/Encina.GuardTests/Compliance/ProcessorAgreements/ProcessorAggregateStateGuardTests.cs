using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorAggregate"/> state transition validation guards
/// that throw <see cref="InvalidOperationException"/> when called on a removed processor.
/// </summary>
public sealed class ProcessorAggregateStateGuardTests
{
    // ========================================================================
    // Update — not allowed when removed
    // ========================================================================

    [Fact]
    public void Update_WhenRemoved_ThrowsInvalidOperationException()
    {
        var aggregate = CreateRemovedAggregate();

        var act = () => aggregate.Update(
            "Updated Name", "US", "new@example.com",
            SubProcessorAuthorizationType.General, DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // Remove — not allowed when already removed
    // ========================================================================

    [Fact]
    public void Remove_WhenAlreadyRemoved_ThrowsInvalidOperationException()
    {
        var aggregate = CreateRemovedAggregate();

        var act = () => aggregate.Remove("another reason", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // AddSubProcessor — not allowed when removed
    // ========================================================================

    [Fact]
    public void AddSubProcessor_WhenRemoved_ThrowsInvalidOperationException()
    {
        var aggregate = CreateRemovedAggregate();

        var act = () => aggregate.AddSubProcessor(
            Guid.NewGuid(), "SubProcessor", 1, DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // RemoveSubProcessor — not allowed when removed
    // ========================================================================

    [Fact]
    public void RemoveSubProcessor_WhenRemoved_ThrowsInvalidOperationException()
    {
        var aggregate = CreateRemovedAggregate();

        var act = () => aggregate.RemoveSubProcessor(
            Guid.NewGuid(), "No longer needed", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(act);
    }

    // ========================================================================
    // Verify active state allows operations (positive controls)
    // ========================================================================

    [Fact]
    public void Update_WhenActive_DoesNotThrow()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Update(
            "Updated Name", "DE", "contact@example.com",
            SubProcessorAuthorizationType.Specific, DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    [Fact]
    public void Remove_WhenActive_DoesNotThrow()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Remove("No longer needed", DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    [Fact]
    public void AddSubProcessor_WhenActive_DoesNotThrow()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.AddSubProcessor(
            Guid.NewGuid(), "SubProcessor", 1, DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    [Fact]
    public void RemoveSubProcessor_WhenActive_DoesNotThrow()
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.RemoveSubProcessor(
            Guid.NewGuid(), "Reason", DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static ProcessorAggregate CreateActiveAggregate()
    {
        return ProcessorAggregate.Register(
            Guid.NewGuid(), "Test Processor", "DE", "contact@example.com",
            null, 0, SubProcessorAuthorizationType.Specific, DateTimeOffset.UtcNow);
    }

    private static ProcessorAggregate CreateRemovedAggregate()
    {
        var aggregate = CreateActiveAggregate();
        aggregate.Remove("Contract ended", DateTimeOffset.UtcNow);
        return aggregate;
    }
}
