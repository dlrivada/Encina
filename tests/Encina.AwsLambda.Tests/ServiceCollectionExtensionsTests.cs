using Encina.AwsLambda.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Encina.AwsLambda.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAwsLambda_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAwsLambda();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<EncinaAwsLambdaOptions>>();
        options.ShouldNotBeNull();
        options!.Value.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAwsLambda_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAwsLambda(options =>
        {
            options.CorrelationIdHeader = "X-Custom-ID";
            options.EnableSqsBatchItemFailures = false;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<EncinaAwsLambdaOptions>>();
        options.Value.CorrelationIdHeader.ShouldBe("X-Custom-ID");
        options.Value.EnableSqsBatchItemFailures.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaAwsLambda_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAwsLambda();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var healthChecks = serviceProvider.GetServices<IEncinaHealthCheck>();
        var healthCheck = healthChecks.ShouldHaveSingleItem();
        healthCheck.ShouldBeOfType<AwsLambdaHealthCheck>();
    }

    [Fact]
    public void AddEncinaAwsLambda_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaAwsLambda();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAwsLambda_WithNullConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddEncinaAwsLambda(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("configureOptions");
    }

    [Fact]
    public void AddEncinaAwsLambda_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAwsLambda();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAwsLambda_CalledMultipleTimes_DoesNotDuplicateHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAwsLambda();
        services.AddEncinaAwsLambda();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var healthChecks = serviceProvider.GetServices<IEncinaHealthCheck>().ToList();
        healthChecks.ShouldHaveSingleItem();
    }
}
