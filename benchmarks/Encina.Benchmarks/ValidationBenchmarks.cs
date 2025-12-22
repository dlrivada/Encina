using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Attributes;
using Encina.DataAnnotations;
using Encina.FluentValidation;
using Encina.GuardClauses;
using Encina.MiniValidator;
// FluentValidation extension methods (NotEmpty, EmailAddress, etc.)
using global::FluentValidation;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks;

/// <summary>
/// Benchmarks comparing validation approaches:
/// - FluentValidation (feature-rich, powerful)
/// - DataAnnotations (built-in .NET, zero dependencies)
/// - MiniValidator (lightweight, ~20KB)
/// - GuardClauses (defensive programming)
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ValidationBenchmarks
{
    private IServiceProvider _fluentProvider = default!;
    private IServiceProvider _dataAnnotationsProvider = default!;
    private IServiceProvider _miniValidatorProvider = default!;
    private IServiceProvider _guardsProvider = default!;

    [GlobalSetup]
    public void Setup()
    {
        // FluentValidation setup
        var fluentServices = new ServiceCollection();
        fluentServices.AddEncina();
        fluentServices.AddTransient<global::FluentValidation.IValidator<FluentCommand>, FluentCommandValidator>();
        fluentServices.AddTransient<ValidationPipelineBehavior<FluentCommand, Guid>>();
        fluentServices.AddTransient<IPipelineBehavior<FluentCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<FluentCommand, Guid>>());
        fluentServices.AddTransient<ICommandHandler<FluentCommand, Guid>, FluentCommandHandler>();
        _fluentProvider = fluentServices.BuildServiceProvider();

        // DataAnnotations setup
        var dataAnnotationsServices = new ServiceCollection();
        dataAnnotationsServices.AddEncina();
        dataAnnotationsServices.AddTransient<DataAnnotationsValidationBehavior<DataAnnotationsCommand, Guid>>();
        dataAnnotationsServices.AddTransient<IPipelineBehavior<DataAnnotationsCommand, Guid>>(sp =>
            sp.GetRequiredService<DataAnnotationsValidationBehavior<DataAnnotationsCommand, Guid>>());
        dataAnnotationsServices.AddTransient<ICommandHandler<DataAnnotationsCommand, Guid>, DataAnnotationsCommandHandler>();
        _dataAnnotationsProvider = dataAnnotationsServices.BuildServiceProvider();

        // MiniValidator setup
        var miniValidatorServices = new ServiceCollection();
        miniValidatorServices.AddEncina();
        miniValidatorServices.AddTransient<MiniValidationBehavior<MiniValidatorCommand, Guid>>();
        miniValidatorServices.AddTransient<IPipelineBehavior<MiniValidatorCommand, Guid>>(sp =>
            sp.GetRequiredService<MiniValidationBehavior<MiniValidatorCommand, Guid>>());
        miniValidatorServices.AddTransient<ICommandHandler<MiniValidatorCommand, Guid>, MiniValidatorCommandHandler>();
        _miniValidatorProvider = miniValidatorServices.BuildServiceProvider();

        // Guards setup (no validation behavior, guards used in handler)
        var guardsServices = new ServiceCollection();
        guardsServices.AddEncina();
        guardsServices.AddTransient<ICommandHandler<GuardsCommand, Guid>, GuardsCommandHandler>();
        _guardsProvider = guardsServices.BuildServiceProvider();
    }

    [Benchmark(Baseline = true, Description = "FluentValidation (Valid)")]
    public async Task<Guid> FluentValidation_ValidCommand()
    {
        using var scope = _fluentProvider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new FluentCommand("John Doe", "john@example.com", 25);
        var result = await Encina.Send(command);
        return result.Match(
            Left: error => throw new InvalidOperationException(error.Message),
            Right: id => id);
    }

    [Benchmark(Description = "DataAnnotations (Valid)")]
    public async Task<Guid> DataAnnotations_ValidCommand()
    {
        using var scope = _dataAnnotationsProvider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new DataAnnotationsCommand("John Doe", "john@example.com", 25);
        var result = await Encina.Send(command);
        return result.Match(
            Left: error => throw new InvalidOperationException(error.Message),
            Right: id => id);
    }

    [Benchmark(Description = "MiniValidator (Valid)")]
    public async Task<Guid> MiniValidator_ValidCommand()
    {
        using var scope = _miniValidatorProvider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new MiniValidatorCommand("John Doe", "john@example.com", 25);
        var result = await Encina.Send(command);
        return result.Match(
            Left: error => throw new InvalidOperationException(error.Message),
            Right: id => id);
    }

    [Benchmark(Description = "GuardClauses (Valid)")]
    public async Task<Guid> GuardClauses_ValidCommand()
    {
        using var scope = _guardsProvider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new GuardsCommand("John Doe", "john@example.com", 25);
        var result = await Encina.Send(command);
        return result.Match(
            Left: error => throw new InvalidOperationException(error.Message),
            Right: id => id);
    }

    // FluentValidation types
    private sealed record FluentCommand(string Name, string Email, int Age) : ICommand<Guid>;

    private sealed class FluentCommandValidator : global::FluentValidation.AbstractValidator<FluentCommand>
    {
        public FluentCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Age).InclusiveBetween(18, 120);
        }
    }

    private sealed class FluentCommandHandler : ICommandHandler<FluentCommand, Guid>
    {
        public Task<Either<EncinaError, Guid>> Handle(FluentCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, Guid>(Guid.NewGuid()));
    }

    // DataAnnotations types
    private sealed record DataAnnotationsCommand : ICommand<Guid>
    {
        [Required]
        [MinLength(3)]
        public string Name { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Range(18, 120)]
        public int Age { get; init; }

        public DataAnnotationsCommand(string name, string email, int age)
        {
            Name = name;
            Email = email;
            Age = age;
        }
    }

    private sealed class DataAnnotationsCommandHandler : ICommandHandler<DataAnnotationsCommand, Guid>
    {
        public Task<Either<EncinaError, Guid>> Handle(DataAnnotationsCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, Guid>(Guid.NewGuid()));
    }

    // MiniValidator types
    private sealed record MiniValidatorCommand : ICommand<Guid>
    {
        [Required]
        [MinLength(3)]
        public string Name { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Range(18, 120)]
        public int Age { get; init; }

        public MiniValidatorCommand(string name, string email, int age)
        {
            Name = name;
            Email = email;
            Age = age;
        }
    }

    private sealed class MiniValidatorCommandHandler : ICommandHandler<MiniValidatorCommand, Guid>
    {
        public Task<Either<EncinaError, Guid>> Handle(MiniValidatorCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, Guid>(Guid.NewGuid()));
    }

    // GuardClauses types
    private sealed record GuardsCommand(string? Name, string? Email, int Age) : ICommand<Guid>;

    private sealed class GuardsCommandHandler : ICommandHandler<GuardsCommand, Guid>
    {
        public Task<Either<EncinaError, Guid>> Handle(GuardsCommand request, CancellationToken cancellationToken)
        {
            if (!Guards.TryValidateNotEmpty(request.Name, nameof(request.Name), out var nameError))
                return Task.FromResult(Left<EncinaError, Guid>(nameError));

            if (!Guards.TryValidateEmail(request.Email, nameof(request.Email), out var emailError))
                return Task.FromResult(Left<EncinaError, Guid>(emailError));

            if (!Guards.TryValidateInRange(request.Age, nameof(request.Age), 18, 120, out var ageError))
                return Task.FromResult(Left<EncinaError, Guid>(ageError));

            return Task.FromResult(Right<EncinaError, Guid>(Guid.NewGuid()));
        }
    }
}
