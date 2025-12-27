using Encina.AwsLambda.Health;
using Encina.Messaging.Health;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
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
        options.Value.CorrelationIdHeader.Should().Be("X-Custom-ID");
        options.Value.EnableSqsBatchItemFailures.Should().BeFalse();
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
        healthChecks.Should().ContainSingle()
            .Which.Should().BeOfType<AwsLambdaHealthCheck>();
    }

    [Fact]
    public void AddEncinaAwsLambda_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaAwsLambda();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddEncinaAwsLambda_WithNullConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddEncinaAwsLambda(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }

    [Fact]
    public void AddEncinaAwsLambda_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAwsLambda();

        // Assert
        result.Should().BeSameAs(services);
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
        healthChecks.Should().ContainSingle();
    }
}
