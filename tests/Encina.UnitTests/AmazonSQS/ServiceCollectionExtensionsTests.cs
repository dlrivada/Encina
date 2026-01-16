using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Encina.AmazonSQS;
using Encina.Messaging.Health;

namespace Encina.UnitTests.AmazonSQS;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAmazonSQS_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => services!.AddEncinaAmazonSQS());
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAmazonSQS_WithoutConfiguration_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaAmazonSQSOptions>>();

        options.Value.Region.ShouldBe("us-east-1");
        options.Value.MaxNumberOfMessages.ShouldBe(10);
        options.Value.VisibilityTimeoutSeconds.ShouldBe(30);
        options.Value.WaitTimeSeconds.ShouldBe(20);
        options.Value.UseFifoQueues.ShouldBeFalse();
        options.Value.UseContentBasedDeduplication.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaAmazonSQS_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS(opt =>
        {
            opt.Region = "eu-west-1";
            opt.DefaultQueueUrl = "https://sqs.eu-west-1.amazonaws.com/123456789012/custom-queue";
            opt.DefaultTopicArn = "arn:aws:sns:eu-west-1:123456789012:custom-topic";
            opt.UseFifoQueues = true;
            opt.MaxNumberOfMessages = 5;
            opt.VisibilityTimeoutSeconds = 60;
            opt.WaitTimeSeconds = 10;
            opt.UseContentBasedDeduplication = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaAmazonSQSOptions>>();

        options.Value.Region.ShouldBe("eu-west-1");
        options.Value.DefaultQueueUrl.ShouldBe("https://sqs.eu-west-1.amazonaws.com/123456789012/custom-queue");
        options.Value.DefaultTopicArn.ShouldBe("arn:aws:sns:eu-west-1:123456789012:custom-topic");
        options.Value.UseFifoQueues.ShouldBeTrue();
        options.Value.MaxNumberOfMessages.ShouldBe(5);
        options.Value.VisibilityTimeoutSeconds.ShouldBe(60);
        options.Value.WaitTimeSeconds.ShouldBe(10);
        options.Value.UseContentBasedDeduplication.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaAmazonSQS_RegistersPublisherAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAmazonSQSMessagePublisher));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaAmazonSQS_RegistersSQSClientAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAmazonSQS));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaAmazonSQS_RegistersSNSClientAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAmazonSimpleNotificationService));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaAmazonSQS_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAmazonSQS();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAmazonSQS_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS(opt =>
        {
            opt.ProviderHealthCheck.Enabled = true;
            opt.ProviderHealthCheck.Name = "custom-sqs";
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaAmazonSQS_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS(opt =>
        {
            opt.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaAmazonSQS_MultipleInvocations_DoesNotDuplicatePublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS();
        services.AddEncinaAmazonSQS();

        // Assert - Should only have one publisher registration due to TryAddScoped
        var publisherDescriptors = services.Where(d =>
            d.ServiceType == typeof(IAmazonSQSMessagePublisher)).ToList();
        publisherDescriptors.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("us-east-1")]
    [InlineData("eu-west-1")]
    [InlineData("ap-southeast-1")]
    public void AddEncinaAmazonSQS_WithDifferentRegions_RegistersCorrectly(string region)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAmazonSQS(opt => opt.Region = region);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAmazonSQS));
        descriptor.ShouldNotBeNull();
    }
}
