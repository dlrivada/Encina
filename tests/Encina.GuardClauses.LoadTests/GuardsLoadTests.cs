using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NBomber.CSharp;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.GuardClauses.LoadTests;

/// <summary>
/// Load tests for Guards with Encina.
/// Verifies performance, concurrency, and throughput under stress conditions.
/// </summary>
[Trait("Category", "Load")]
public sealed class GuardsLoadTests
{
    private readonly ITestOutputHelper _output;

    public GuardsLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private sealed record ValidateCommand(string? Email, int Age) : ICommand<Guid>;

    private sealed class ValidateHandler : ICommandHandler<ValidateCommand, Guid>
    {
        public Task<Either<MediatorError, Guid>> Handle(ValidateCommand request, CancellationToken cancellationToken)
        {
            if (!Guards.TryValidateNotEmpty(request.Email, nameof(request.Email), out var emailError))
                return Task.FromResult(Left<MediatorError, Guid>(emailError));

            if (!Guards.TryValidateEmail(request.Email, nameof(request.Email), out var formatError))
                return Task.FromResult(Left<MediatorError, Guid>(formatError));

            if (!Guards.TryValidateInRange(request.Age, nameof(request.Age), 18, 120, out var ageError))
                return Task.FromResult(Left<MediatorError, Guid>(ageError));

            return Task.FromResult(Right<MediatorError, Guid>(Guid.NewGuid()));
        }
    }

    [Fact]
    public void HighConcurrency_ValidCommands_ShouldHandleLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<ICommandHandler<ValidateCommand, Guid>, ValidateHandler>();

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("valid_commands_guards", async context =>
        {
            using var scope = provider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var command = new ValidateCommand("user@example.com", 25);
            var result = await mediator.Send(command);
            return result.IsRight ? Response.Ok() : Response.Fail<Guid>(statusCode: "validation_failed");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();

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
        services.AddTransient<ICommandHandler<ValidateCommand, Guid>, ValidateHandler>();

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("invalid_commands_guards", async context =>
        {
            using var scope = provider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var command = new ValidateCommand("", 15); // Invalid
            var result = await mediator.Send(command);
            return result.IsLeft ? Response.Ok() : Response.Fail<Guid>(statusCode: "expected_validation_failure");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();

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
        services.AddTransient<ICommandHandler<ValidateCommand, Guid>, ValidateHandler>();

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("mixed_commands_guards", async context =>
        {
            using var scope = provider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var command = context.InvocationNumber % 2 == 0
                ? new ValidateCommand("user@example.com", 25)
                : new ValidateCommand("", 15);
            var result = await mediator.Send(command);
            var expectedOutcome = context.InvocationNumber % 2 == 0 ? result.IsRight : result.IsLeft;
            return expectedOutcome ? Response.Ok() : Response.Fail<Guid>(statusCode: "unexpected");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner.RegisterScenarios(scenario).WithoutReports().Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }
}
