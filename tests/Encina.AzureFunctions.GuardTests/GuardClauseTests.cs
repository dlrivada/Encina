using Encina.AzureFunctions.Health;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.AzureFunctions.GuardTests;

/// <summary>
/// Tests for guard clauses (null checks, argument validation) across public APIs.
/// </summary>
public class GuardClauseTests
{
    [Fact]
    public void ServiceCollectionExtensions_AddEncinaAzureFunctions_ThrowsOnNullServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaAzureFunctions();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void ServiceCollectionExtensions_AddEncinaAzureFunctions_WithOptions_ThrowsOnNullServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaAzureFunctions(_ => { });

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void ServiceCollectionExtensions_AddEncinaAzureFunctions_ThrowsOnNullConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddEncinaAzureFunctions(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }

    [Fact]
    public void AzureFunctionsHealthCheck_Constructor_ThrowsOnNullOptions()
    {
        // Act
        var action = () => new AzureFunctionsHealthCheck(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void EncinaFunctionMiddleware_Constructor_ThrowsOnNullOptions()
    {
        // Act
        var action = () => new EncinaFunctionMiddleware(null!, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void EncinaFunctionMiddleware_Constructor_ThrowsOnNullLogger()
    {
        // Arrange
        var options = Options.Create(new EncinaAzureFunctionsOptions());

        // Act
        var action = () => new EncinaFunctionMiddleware(options, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void FunctionsWorkerApplicationBuilderExtensions_UseEncinaMiddleware_ThrowsOnNullBuilder()
    {
        // Arrange
        IFunctionsWorkerApplicationBuilder builder = null!;

        // Act
        var action = () => builder.UseEncinaMiddleware();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }
}
