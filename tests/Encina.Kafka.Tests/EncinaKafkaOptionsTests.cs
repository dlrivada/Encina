namespace Encina.Kafka.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaKafkaOptions"/>.
/// </summary>
public sealed class EncinaKafkaOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EncinaKafkaOptions();

        // Assert
        options.BootstrapServers.ShouldBe("localhost:9092");
        options.GroupId.ShouldBe("encina-consumer");
        options.DefaultCommandTopic.ShouldBe("encina-commands");
        options.DefaultEventTopic.ShouldBe("encina-events");
        options.AutoOffsetReset.ShouldBe("earliest");
        options.EnableAutoCommit.ShouldBeFalse();
        options.Acks.ShouldBe("all");
        options.EnableIdempotence.ShouldBeTrue();
        options.MessageTimeoutMs.ShouldBe(30000);
    }

    [Fact]
    public void BootstrapServers_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.BootstrapServers = "kafka1:9092,kafka2:9092";

        // Assert
        options.BootstrapServers.ShouldBe("kafka1:9092,kafka2:9092");
    }

    [Fact]
    public void GroupId_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.GroupId = "my-consumer-group";

        // Assert
        options.GroupId.ShouldBe("my-consumer-group");
    }

    [Fact]
    public void DefaultCommandTopic_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.DefaultCommandTopic = "custom-commands";

        // Assert
        options.DefaultCommandTopic.ShouldBe("custom-commands");
    }

    [Fact]
    public void DefaultEventTopic_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.DefaultEventTopic = "custom-events";

        // Assert
        options.DefaultEventTopic.ShouldBe("custom-events");
    }

    [Fact]
    public void AutoOffsetReset_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.AutoOffsetReset = "latest";

        // Assert
        options.AutoOffsetReset.ShouldBe("latest");
    }

    [Fact]
    public void EnableAutoCommit_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.EnableAutoCommit = true;

        // Assert
        options.EnableAutoCommit.ShouldBeTrue();
    }

    [Fact]
    public void Acks_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.Acks = "leader";

        // Assert
        options.Acks.ShouldBe("leader");
    }

    [Fact]
    public void EnableIdempotence_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.EnableIdempotence = false;

        // Assert
        options.EnableIdempotence.ShouldBeFalse();
    }

    [Fact]
    public void MessageTimeoutMs_CanBeSet()
    {
        // Arrange
        var options = new EncinaKafkaOptions();

        // Act
        options.MessageTimeoutMs = 60000;

        // Assert
        options.MessageTimeoutMs.ShouldBe(60000);
    }

    [Fact]
    public void ProviderHealthCheck_IsNotNull()
    {
        // Arrange & Act
        var options = new EncinaKafkaOptions();

        // Assert
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_DefaultEnabled_IsTrue()
    {
        // Arrange & Act
        var options = new EncinaKafkaOptions();

        // Assert - opt-out design means health checks are enabled by default
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
    }
}
