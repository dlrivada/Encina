using Encina.AmazonSQS;

namespace Encina.UnitTests.AmazonSQS;

/// <summary>
/// Unit tests for <see cref="EncinaAmazonSQSOptions"/>.
/// </summary>
public sealed class EncinaAmazonSQSOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EncinaAmazonSQSOptions();

        // Assert
        options.Region.ShouldBe("us-east-1");
        options.DefaultQueueUrl.ShouldBeNull();
        options.DefaultTopicArn.ShouldBeNull();
        options.UseFifoQueues.ShouldBeFalse();
        options.MaxNumberOfMessages.ShouldBe(10);
        options.VisibilityTimeoutSeconds.ShouldBe(30);
        options.WaitTimeSeconds.ShouldBe(20);
        options.UseContentBasedDeduplication.ShouldBeFalse();
    }

    [Fact]
    public void Region_CanBeSet()
    {
        // Arrange
        var options = new EncinaAmazonSQSOptions();

        // Act
        options.Region = "eu-west-1";

        // Assert
        options.Region.ShouldBe("eu-west-1");
    }

    [Fact]
    public void DefaultQueueUrl_CanBeSet()
    {
        // Arrange
        var options = new EncinaAmazonSQSOptions();

        // Act
        options.DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue";

        // Assert
        options.DefaultQueueUrl.ShouldStartWith("https://sqs");
    }

    [Fact]
    public void DefaultTopicArn_CanBeSet()
    {
        // Arrange
        var options = new EncinaAmazonSQSOptions();

        // Act
        options.DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:my-topic";

        // Assert
        options.DefaultTopicArn.ShouldStartWith("arn:aws:sns");
    }

    [Fact]
    public void UseFifoQueues_CanBeSet()
    {
        // Arrange
        var options = new EncinaAmazonSQSOptions();

        // Act
        options.UseFifoQueues = true;

        // Assert
        options.UseFifoQueues.ShouldBeTrue();
    }

    [Fact]
    public void MaxNumberOfMessages_CanBeSet()
    {
        // Arrange
        var options = new EncinaAmazonSQSOptions();

        // Act
        options.MaxNumberOfMessages = 5;

        // Assert
        options.MaxNumberOfMessages.ShouldBe(5);
    }

    [Fact]
    public void VisibilityTimeoutSeconds_CanBeSet()
    {
        // Arrange
        var options = new EncinaAmazonSQSOptions();

        // Act
        options.VisibilityTimeoutSeconds = 60;

        // Assert
        options.VisibilityTimeoutSeconds.ShouldBe(60);
    }

    [Fact]
    public void WaitTimeSeconds_CanBeSet()
    {
        // Arrange
        var options = new EncinaAmazonSQSOptions();

        // Act
        options.WaitTimeSeconds = 10;

        // Assert
        options.WaitTimeSeconds.ShouldBe(10);
    }

    [Fact]
    public void UseContentBasedDeduplication_CanBeSet()
    {
        // Arrange
        var options = new EncinaAmazonSQSOptions();

        // Act
        options.UseContentBasedDeduplication = true;

        // Assert
        options.UseContentBasedDeduplication.ShouldBeTrue();
    }

    [Fact]
    public void ProviderHealthCheck_IsNotNull()
    {
        // Arrange & Act
        var options = new EncinaAmazonSQSOptions();

        // Assert
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_DefaultEnabled_IsTrue()
    {
        // Arrange & Act
        var options = new EncinaAmazonSQSOptions();

        // Assert - opt-out design means health checks are enabled by default
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
    }
}
