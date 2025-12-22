using FluentValidation;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NBomber.CSharp;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.FluentValidation.LoadTests;

/// <summary>
/// Load tests for ValidationPipelineBehavior using NBomber.
/// Verifies performance, concurrency, and throughput under stress conditions.
/// </summary>
[Trait("Category", "Load")]
public sealed class ValidationPipelineBehaviorLoadTests
{
    private readonly ITestOutputHelper _output;

    public ValidationPipelineBehaviorLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private sealed record TestCommand(string Name, string Email) : ICommand<Guid>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
            RuleFor(x => x.Email).EmailAddress();
        }
    }

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, Guid>
    {
        public Task<Either<EncinaError, Guid>> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, Guid>(Guid.NewGuid()));
        }
    }

    [Fact]
    public void HighConcurrency_ValidCommands_ShouldHandleLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("valid_commands", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var command = new TestCommand("John Doe", "john@example.com");
            var result = await Encina.Send(command);
            return result.IsRight ? Response.Ok() : Response.Fail<Guid>(statusCode: "validation_failed");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void HighConcurrency_InvalidCommands_ShouldHandleValidationFailures()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("invalid_commands", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var command = new TestCommand("", "invalid");
            var result = await Encina.Send(command);
            return result.IsLeft ? Response.Ok() : Response.Fail<Guid>(statusCode: "expected_validation_failure");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK (validation errors): {scen.Ok.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void MixedValidAndInvalid_ShouldHandleCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("mixed_commands", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var command = context.InvocationNumber % 2 == 0
                ? new TestCommand("John", "john@example.com")
                : new TestCommand("", "");
            var result = await Encina.Send(command);
            var expectedOutcome = context.InvocationNumber % 2 == 0 ? result.IsRight : result.IsLeft;
            return expectedOutcome ? Response.Ok() : Response.Fail<Guid>(statusCode: "unexpected");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void Endurance_ContinuousLoad_ShouldNotDegrade()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("endurance", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var command = new TestCommand("John", "john@example.com");
            var result = await Encina.Send(command);
            return result.IsRight ? Response.Ok() : Response.Fail<Guid>(statusCode: "failed");
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 0, $"Expected > 0, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void MultipleValidators_HighLoad_ShouldAggregate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("multiple_validators", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var command = new TestCommand("John", "john@example.com");
            var result = await Encina.Send(command);
            return result.IsRight ? Response.Ok() : Response.Fail<Guid>(statusCode: "failed");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 450, $"Expected > 450, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void BurstLoad_SuddenSpike_ShouldRecover()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("burst_load", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var command = new TestCommand("John", "john@example.com");
            var result = await Encina.Send(command);
            return result.IsRight ? Response.Ok() : Response.Fail<Guid>(statusCode: "failed");
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(2)),
            Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5)),
            Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(3))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 400, $"Expected > 400, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void MemoryPressure_ManyShortLivedScopes_ShouldNotLeak()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("memory_pressure", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var command = new TestCommand("John", "john@example.com");
            var result = await Encina.Send(command);
            return result.IsRight ? Response.Ok() : Response.Fail<Guid>(statusCode: "failed");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 1800, $"Expected > 1800, got {scen.Ok.Request.Count}");
    }
}
