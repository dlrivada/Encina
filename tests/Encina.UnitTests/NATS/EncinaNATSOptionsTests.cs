using Encina.NATS;

namespace Encina.UnitTests.NATS;

/// <summary>
/// Unit tests for <see cref="EncinaNATSOptions"/>.
/// </summary>
public sealed class EncinaNATSOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EncinaNATSOptions();

        // Assert
        options.Url.ShouldBe("nats://localhost:4222");
        options.SubjectPrefix.ShouldBe("encina");
        options.UseJetStream.ShouldBeFalse();
        options.StreamName.ShouldBe("ENCINA");
        options.ConsumerName.ShouldBe("encina-consumer");
        options.UseDurableConsumer.ShouldBeTrue();
        options.AckWait.ShouldBe(TimeSpan.FromSeconds(30));
        options.MaxDeliver.ShouldBe(5);
    }

    [Fact]
    public void Url_CanBeSet()
    {
        // Arrange
        var options = new EncinaNATSOptions();

        // Act
        options.Url = "nats://server1:4222,nats://server2:4222";

        // Assert
        options.Url.ShouldBe("nats://server1:4222,nats://server2:4222");
    }

    [Fact]
    public void SubjectPrefix_CanBeSet()
    {
        // Arrange
        var options = new EncinaNATSOptions();

        // Act
        options.SubjectPrefix = "myapp";

        // Assert
        options.SubjectPrefix.ShouldBe("myapp");
    }

    [Fact]
    public void UseJetStream_CanBeSet()
    {
        // Arrange
        var options = new EncinaNATSOptions();

        // Act
        options.UseJetStream = true;

        // Assert
        options.UseJetStream.ShouldBeTrue();
    }

    [Fact]
    public void StreamName_CanBeSet()
    {
        // Arrange
        var options = new EncinaNATSOptions();

        // Act
        options.StreamName = "MYSTREAM";

        // Assert
        options.StreamName.ShouldBe("MYSTREAM");
    }

    [Fact]
    public void ConsumerName_CanBeSet()
    {
        // Arrange
        var options = new EncinaNATSOptions();

        // Act
        options.ConsumerName = "my-consumer";

        // Assert
        options.ConsumerName.ShouldBe("my-consumer");
    }

    [Fact]
    public void UseDurableConsumer_CanBeSet()
    {
        // Arrange
        var options = new EncinaNATSOptions();

        // Act
        options.UseDurableConsumer = false;

        // Assert
        options.UseDurableConsumer.ShouldBeFalse();
    }

    [Fact]
    public void AckWait_CanBeSet()
    {
        // Arrange
        var options = new EncinaNATSOptions();

        // Act
        options.AckWait = TimeSpan.FromMinutes(1);

        // Assert
        options.AckWait.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void MaxDeliver_CanBeSet()
    {
        // Arrange
        var options = new EncinaNATSOptions();

        // Act
        options.MaxDeliver = 10;

        // Assert
        options.MaxDeliver.ShouldBe(10);
    }

    [Fact]
    public void ProviderHealthCheck_IsNotNull()
    {
        // Arrange & Act
        var options = new EncinaNATSOptions();

        // Assert
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_DefaultEnabled_IsTrue()
    {
        // Arrange & Act
        var options = new EncinaNATSOptions();

        // Assert - opt-out design means health checks are enabled by default
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
    }
}
