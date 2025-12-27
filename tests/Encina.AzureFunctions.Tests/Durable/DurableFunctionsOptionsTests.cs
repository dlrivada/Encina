using Encina.AzureFunctions.Durable;
using FluentAssertions;
using Xunit;

namespace Encina.AzureFunctions.Tests.Durable;

public class DurableFunctionsOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new DurableFunctionsOptions();

        // Assert
        options.DefaultMaxRetries.Should().Be(3);
        options.DefaultFirstRetryInterval.Should().Be(TimeSpan.FromSeconds(5));
        options.DefaultBackoffCoefficient.Should().Be(2.0);
        options.DefaultMaxRetryInterval.Should().Be(TimeSpan.FromMinutes(1));
        options.ContinueCompensationOnError.Should().BeTrue();
        options.DefaultSagaTimeout.Should().BeNull();
        options.ProviderHealthCheck.Should().NotBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_HasCorrectDefaults()
    {
        // Act
        var options = new DurableFunctionsOptions();

        // Assert
        options.ProviderHealthCheck.Name.Should().Be("encina-durable-functions");
        options.ProviderHealthCheck.Tags.Should().Contain("encina");
        options.ProviderHealthCheck.Tags.Should().Contain("durable-functions");
        options.ProviderHealthCheck.Tags.Should().Contain("ready");
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        // Arrange
        var options = new DurableFunctionsOptions();

        // Act
        options.DefaultMaxRetries = 5;
        options.DefaultFirstRetryInterval = TimeSpan.FromSeconds(10);
        options.DefaultBackoffCoefficient = 3.0;
        options.DefaultMaxRetryInterval = TimeSpan.FromMinutes(5);
        options.ContinueCompensationOnError = false;
        options.DefaultSagaTimeout = TimeSpan.FromHours(1);

        // Assert
        options.DefaultMaxRetries.Should().Be(5);
        options.DefaultFirstRetryInterval.Should().Be(TimeSpan.FromSeconds(10));
        options.DefaultBackoffCoefficient.Should().Be(3.0);
        options.DefaultMaxRetryInterval.Should().Be(TimeSpan.FromMinutes(5));
        options.ContinueCompensationOnError.Should().BeFalse();
        options.DefaultSagaTimeout.Should().Be(TimeSpan.FromHours(1));
    }
}
