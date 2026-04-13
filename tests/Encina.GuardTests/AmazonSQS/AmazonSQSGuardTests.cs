using Amazon.SimpleNotificationService;
using Amazon.SQS;

using Encina.AmazonSQS;
using Encina.AmazonSQS.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.AmazonSQS;

/// <summary>
/// Guard tests for Encina.AmazonSQS covering constructor and method null guards
/// for <see cref="AmazonSQSMessagePublisher"/>, <see cref="AmazonSQSHealthCheck"/>,
/// <see cref="ServiceCollectionExtensions"/>, and <see cref="EncinaAmazonSQSOptions"/>.
/// </summary>
[Trait("Category", "Guard")]
public sealed class AmazonSQSGuardTests
{
    private static readonly IAmazonSQS SqsClient = Substitute.For<IAmazonSQS>();
    private static readonly IAmazonSimpleNotificationService SnsClient = Substitute.For<IAmazonSimpleNotificationService>();
    private static readonly IOptions<EncinaAmazonSQSOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new EncinaAmazonSQSOptions());

    // ─── AmazonSQSMessagePublisher constructor guards ───

    [Fact]
    public void Constructor_NullSqsClient_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AmazonSQSMessagePublisher(null!, SnsClient,
                NullLogger<AmazonSQSMessagePublisher>.Instance, Options));
    }

    [Fact]
    public void Constructor_NullSnsClient_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AmazonSQSMessagePublisher(SqsClient, null!,
                NullLogger<AmazonSQSMessagePublisher>.Instance, Options));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AmazonSQSMessagePublisher(SqsClient, SnsClient, null!, Options));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AmazonSQSMessagePublisher(SqsClient, SnsClient,
                NullLogger<AmazonSQSMessagePublisher>.Instance, null!));
    }

    [Fact]
    public void Constructor_ValidArgs_Constructs()
    {
        var sut = new AmazonSQSMessagePublisher(SqsClient, SnsClient,
            NullLogger<AmazonSQSMessagePublisher>.Instance, Options);
        sut.ShouldNotBeNull();
    }

    // ─── AmazonSQSMessagePublisher method guards ───

    [Fact]
    public async Task SendToQueueAsync_NullMessage_Throws()
    {
        var sut = new AmazonSQSMessagePublisher(SqsClient, SnsClient,
            NullLogger<AmazonSQSMessagePublisher>.Instance, Options);

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SendToQueueAsync<object>(null!));
    }

    [Fact]
    public async Task PublishToTopicAsync_NullMessage_Throws()
    {
        var sut = new AmazonSQSMessagePublisher(SqsClient, SnsClient,
            NullLogger<AmazonSQSMessagePublisher>.Instance, Options);

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.PublishToTopicAsync<object>(null!));
    }

    [Fact]
    public async Task SendBatchAsync_NullMessages_Throws()
    {
        var sut = new AmazonSQSMessagePublisher(SqsClient, SnsClient,
            NullLogger<AmazonSQSMessagePublisher>.Instance, Options);

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SendBatchAsync<object>(null!));
    }

    [Fact]
    public async Task SendToFifoQueueAsync_NullMessage_Throws()
    {
        var sut = new AmazonSQSMessagePublisher(SqsClient, SnsClient,
            NullLogger<AmazonSQSMessagePublisher>.Instance, Options);

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SendToFifoQueueAsync<object>(null!, "group-1"));
    }

    [Fact]
    public async Task SendToFifoQueueAsync_NullMessageGroupId_Throws()
    {
        var sut = new AmazonSQSMessagePublisher(SqsClient, SnsClient,
            NullLogger<AmazonSQSMessagePublisher>.Instance, Options);

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SendToFifoQueueAsync(new { Id = 1 }, null!));
    }

    // ─── AmazonSQSHealthCheck ───

    [Fact]
    public void AmazonSQSHealthCheck_Constructs()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new AmazonSQSHealthCheck(sp, null);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaAmazonSQS_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaAmazonSQS(_ => { }));
    }

    [Fact]
    public void AddEncinaAmazonSQS_ValidServices_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(SqsClient);
        services.AddSingleton(SnsClient);

        var result = services.AddEncinaAmazonSQS(_ => { });

        result.ShouldNotBeNull();
        services.ShouldContain(sd => sd.ServiceType == typeof(IAmazonSQSMessagePublisher));
    }

    // ─── EncinaAmazonSQSOptions defaults ───

    [Fact]
    public void EncinaAmazonSQSOptions_Defaults()
    {
        var options = new EncinaAmazonSQSOptions();

        options.ShouldNotBeNull();
        options.Region.ShouldBe("us-east-1");
        options.DefaultQueueUrl.ShouldBeNull();
        options.DefaultTopicArn.ShouldBeNull();
        options.UseFifoQueues.ShouldBeFalse();
        options.MaxNumberOfMessages.ShouldBe(10);
        options.VisibilityTimeoutSeconds.ShouldBe(30);
        options.WaitTimeSeconds.ShouldBe(20);
        options.UseContentBasedDeduplication.ShouldBeFalse();
    }
}
