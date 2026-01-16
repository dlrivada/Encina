using Encina.AzureFunctions.Durable;

namespace Encina.UnitTests.AzureFunctions.Durable;

public class DurableFunctionsOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new DurableFunctionsOptions();

        // Assert
        options.DefaultMaxRetries.ShouldBe(3);
        options.DefaultFirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(5));
        options.DefaultBackoffCoefficient.ShouldBe(2.0);
        options.DefaultMaxRetryInterval.ShouldBe(TimeSpan.FromMinutes(1));
        options.ContinueCompensationOnError.ShouldBeTrue();
        options.DefaultSagaTimeout.ShouldBeNull();
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_HasCorrectDefaults()
    {
        // Act
        var options = new DurableFunctionsOptions();

        // Assert
        options.ProviderHealthCheck.Name.ShouldBe("encina-durable-functions");
        options.ProviderHealthCheck.Tags.ShouldContain("encina");
        options.ProviderHealthCheck.Tags.ShouldContain("durable-functions");
        options.ProviderHealthCheck.Tags.ShouldContain("ready");
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
        options.DefaultMaxRetries.ShouldBe(5);
        options.DefaultFirstRetryInterval.ShouldBe(TimeSpan.FromSeconds(10));
        options.DefaultBackoffCoefficient.ShouldBe(3.0);
        options.DefaultMaxRetryInterval.ShouldBe(TimeSpan.FromMinutes(5));
        options.ContinueCompensationOnError.ShouldBeFalse();
        options.DefaultSagaTimeout.ShouldBe(TimeSpan.FromHours(1));
    }
}
