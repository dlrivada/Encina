using Encina.AmazonSQS;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.AmazonSQS;

/// <summary>
/// Registration-level endpoint validation for <see cref="ServiceCollectionExtensions.AddEncinaAmazonSQS"/> (#852).
/// </summary>
public sealed class ServiceCollectionExtensionsEndpointValidationTests
{
    private const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/orders";

    [Fact]
    public void AddEncinaAmazonSQS_UnsafeDefaultQueueUrl_ThrowsAtRegistration()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<OptionsValidationException>(() =>
            services.AddEncinaAmazonSQS(o => o.DefaultQueueUrl = "http://169.254.169.254/latest"));

        ex.Message.ShouldContain("DefaultQueueUrl");
    }

    [Fact]
    public void AddEncinaAmazonSQS_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();

        services.AddEncinaAmazonSQS(o => o.DefaultQueueUrl = QueueUrl);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<EncinaAmazonSQSOptions>) &&
            d.ImplementationType == typeof(EncinaAmazonSQSOptionsValidator));
    }

    [Fact]
    public void AddEncinaAmazonSQS_ValidOptions_ProviderBuildsWithValidateOnBuildAndScopes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAmazonSQS(o => o.DefaultQueueUrl = QueueUrl);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IOptions<EncinaAmazonSQSOptions>>().Value.DefaultQueueUrl.ShouldBe(QueueUrl);
    }

    [Fact]
    public void AddEncinaAmazonSQS_WithOptOuts_LogsWarningOnce()
    {
        var logs = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(logs));
        services.AddEncinaAmazonSQS(o =>
        {
            o.DefaultQueueUrl = "http://localhost:4566/000000000000/orders";
            o.AllowInsecureHttp = true;
            o.AllowLocalEndpoints = true;
        });

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<EncinaAmazonSQSOptions>>().Value;
        _ = provider.GetRequiredService<IOptionsMonitor<EncinaAmazonSQSOptions>>().CurrentValue;

        logs.Count(4363, LogLevel.Warning).ShouldBe(1);
    }
}
