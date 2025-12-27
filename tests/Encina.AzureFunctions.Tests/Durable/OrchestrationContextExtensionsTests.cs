using Encina.AzureFunctions.Durable;
using FluentAssertions;
using Xunit;

namespace Encina.AzureFunctions.Tests.Durable;

public class OrchestrationContextExtensionsTests
{
    [Fact]
    public void CreateRetryOptions_WithValidParameters_ReturnsTaskOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1));

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithDefaultBackoffCoefficient_UsesDefault()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5));

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithZeroRetries_CreatesValidOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 0,
            firstRetryInterval: TimeSpan.FromSeconds(1));

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithNegativeRetries_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: -1,
            firstRetryInterval: TimeSpan.FromSeconds(5));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateRetryOptions_WithZeroFirstRetryInterval_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.Zero);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateRetryOptions_WithNegativeFirstRetryInterval_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(-5));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateRetryOptions_WithZeroBackoffCoefficient_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: 0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateRetryOptions_WithNegativeBackoffCoefficient_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: -1.0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateRetryOptions_WithHighMaxRetries_CreatesValidOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 100,
            firstRetryInterval: TimeSpan.FromMilliseconds(100));

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithSmallFirstRetryInterval_CreatesValidOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 5,
            firstRetryInterval: TimeSpan.FromMilliseconds(1));

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithLargeMaxRetryInterval_CreatesValidOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 5,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            maxRetryInterval: TimeSpan.FromHours(24));

        // Assert
        options.Should().NotBeNull();
    }
}
