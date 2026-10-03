using Encina.Quartz;
using Encina.UnitTests.Quartz.Fakers;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Quartz;

public class QuartzRegistrationCompletenessTests
{
    [Fact]
    public void AddEncinaQuartz_BuildsProviderWithValidateOnBuildAndValidateScopes()
    {
        // Arrange
        var services = CreateServices(options => options.ExposeResponseInJobContext = true);

        // Act
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Assert
        provider.GetRequiredService<EncinaQuartzOptions>().ExposeResponseInJobContext.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaQuartz_OptionIsOffByDefault()
    {
        // Arrange
        var services = CreateServices(configureOptions: null);

        // Act
        using var provider = services.BuildServiceProvider(validateScopes: true);

        // Assert
        provider.GetRequiredService<EncinaQuartzOptions>().ExposeResponseInJobContext.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestJobCreatedFromContainer_HonorsRegisteredExposeResponseOption(bool expose)
    {
        // Arrange
        var services = CreateServices(options => options.ExposeResponseInJobContext = expose);
        var encina = Substitute.For<IEncina>();
        var response = new TestResponse("response");
        encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(response));
        services.AddSingleton(encina);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var job = ActivatorUtilities.CreateInstance<QuartzRequestJob<TestRequest, TestResponse>>(provider);
        var context = Substitute.For<IJobExecutionContext>();
        var jobDetail = Substitute.For<IJobDetail>();
        jobDetail.Key.Returns(new JobKey("test-job"));
        jobDetail.JobDataMap.Returns(new JobDataMap { { QuartzConstants.RequestKey, new TestRequest("data") } });
        context.JobDetail.Returns(jobDetail);

        // Act
        await job.Execute(context);

        // Assert
        if (expose)
        {
            context.Received(1).Result = response;
        }
        else
        {
            context.DidNotReceive().Result = Arg.Any<object?>();
        }
    }

    private static ServiceCollection CreateServices(Action<EncinaQuartzOptions>? configureOptions)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Services the generic host provides to Quartz's hosted services.
        services.AddSingleton(Substitute.For<IHostApplicationLifetime>());
        services.AddEncinaQuartz(configureOptions: configureOptions);
        return services;
    }
}
