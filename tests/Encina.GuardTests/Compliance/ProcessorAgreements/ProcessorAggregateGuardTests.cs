using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorAggregate"/> factory method and instance method parameter
/// validation guards (ArgumentException, ArgumentNullException, ArgumentOutOfRangeException).
/// </summary>
public sealed class ProcessorAggregateGuardTests
{
    // ========================================================================
    // Register — factory method guards
    // ========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_NullOrWhiteSpaceName_ThrowsArgumentException(string? name)
    {
        var act = () => ProcessorAggregate.Register(
            Guid.NewGuid(), name!, "DE", null, null, 0,
            SubProcessorAuthorizationType.Specific, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_NullOrWhiteSpaceCountry_ThrowsArgumentException(string? country)
    {
        var act = () => ProcessorAggregate.Register(
            Guid.NewGuid(), "Test Processor", country!, null, null, 0,
            SubProcessorAuthorizationType.Specific, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Register_NegativeDepth_ThrowsArgumentOutOfRangeException()
    {
        var act = () => ProcessorAggregate.Register(
            Guid.NewGuid(), "Test Processor", "DE", null, null, -1,
            SubProcessorAuthorizationType.Specific, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Register_ValidParameters_DoesNotThrow()
    {
        var act = () => ProcessorAggregate.Register(
            Guid.NewGuid(), "Test Processor", "DE", "contact@example.com",
            null, 0, SubProcessorAuthorizationType.Specific, DateTimeOffset.UtcNow);

        Should.NotThrow(act);
    }

    // ========================================================================
    // Update — instance method guards
    // ========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_NullOrWhiteSpaceName_ThrowsArgumentException(string? name)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Update(
            name!, "DE", null, SubProcessorAuthorizationType.Specific, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_NullOrWhiteSpaceCountry_ThrowsArgumentException(string? country)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Update(
            "Updated Name", country!, null, SubProcessorAuthorizationType.Specific, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    // ========================================================================
    // Remove — instance method guards
    // ========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Remove_NullOrWhiteSpaceReason_ThrowsArgumentException(string? reason)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.Remove(reason!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    // ========================================================================
    // AddSubProcessor — instance method guards
    // ========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddSubProcessor_NullOrWhiteSpaceName_ThrowsArgumentException(string? name)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.AddSubProcessor(
            Guid.NewGuid(), name!, 1, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddSubProcessor_ZeroOrNegativeDepth_ThrowsArgumentOutOfRangeException(int depth)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.AddSubProcessor(
            Guid.NewGuid(), "SubProcessor", depth, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    // ========================================================================
    // RemoveSubProcessor — instance method guards
    // ========================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveSubProcessor_NullOrWhiteSpaceReason_ThrowsArgumentException(string? reason)
    {
        var aggregate = CreateActiveAggregate();

        var act = () => aggregate.RemoveSubProcessor(
            Guid.NewGuid(), reason!, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act);
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
}
