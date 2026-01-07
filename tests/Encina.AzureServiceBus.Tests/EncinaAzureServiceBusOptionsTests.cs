namespace Encina.AzureServiceBus.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaAzureServiceBusOptions"/>.
/// </summary>
public sealed class EncinaAzureServiceBusOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EncinaAzureServiceBusOptions();

        // Assert
        options.ConnectionString.ShouldBe(string.Empty);
        options.DefaultQueueName.ShouldBe("encina-commands");
        options.DefaultTopicName.ShouldBe("encina-events");
        options.SubscriptionName.ShouldBe("default");
        options.UseSessions.ShouldBeFalse();
        options.MaxConcurrentCalls.ShouldBe(10);
        options.PrefetchCount.ShouldBe(10);
        options.MaxAutoLockRenewalDuration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void ConnectionString_CanBeSet()
    {
        // Arrange
        var options = new EncinaAzureServiceBusOptions();

        // Act
        options.ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test";

        // Assert
        options.ConnectionString.ShouldStartWith("Endpoint=sb://");
    }

    [Fact]
    public void DefaultQueueName_CanBeSet()
    {
        // Arrange
        var options = new EncinaAzureServiceBusOptions();

        // Act
        options.DefaultQueueName = "custom-queue";

        // Assert
        options.DefaultQueueName.ShouldBe("custom-queue");
    }

    [Fact]
    public void DefaultTopicName_CanBeSet()
    {
        // Arrange
        var options = new EncinaAzureServiceBusOptions();

        // Act
        options.DefaultTopicName = "custom-topic";

        // Assert
        options.DefaultTopicName.ShouldBe("custom-topic");
    }

    [Fact]
    public void SubscriptionName_CanBeSet()
    {
        // Arrange
        var options = new EncinaAzureServiceBusOptions();

        // Act
        options.SubscriptionName = "my-subscription";

        // Assert
        options.SubscriptionName.ShouldBe("my-subscription");
    }

    [Fact]
    public void UseSessions_CanBeSet()
    {
        // Arrange
        var options = new EncinaAzureServiceBusOptions();

        // Act
        options.UseSessions = true;

        // Assert
        options.UseSessions.ShouldBeTrue();
    }

    [Fact]
    public void MaxConcurrentCalls_CanBeSet()
    {
        // Arrange
        var options = new EncinaAzureServiceBusOptions();

        // Act
        options.MaxConcurrentCalls = 50;

        // Assert
        options.MaxConcurrentCalls.ShouldBe(50);
    }

    [Fact]
    public void PrefetchCount_CanBeSet()
    {
        // Arrange
        var options = new EncinaAzureServiceBusOptions();

        // Act
        options.PrefetchCount = 100;

        // Assert
        options.PrefetchCount.ShouldBe(100);
    }

    [Fact]
    public void MaxAutoLockRenewalDuration_CanBeSet()
    {
        // Arrange
        var options = new EncinaAzureServiceBusOptions();

        // Act
        options.MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10);

        // Assert
        options.MaxAutoLockRenewalDuration.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void ProviderHealthCheck_IsNotNull()
    {
        // Arrange & Act
        var options = new EncinaAzureServiceBusOptions();

        // Assert
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_DefaultEnabled_IsTrue()
    {
        // Arrange & Act
        var options = new EncinaAzureServiceBusOptions();

        // Assert - opt-out design means health checks are enabled by default
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
    }
}
