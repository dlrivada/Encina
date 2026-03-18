#pragma warning disable CA2012 // Use ValueTasks correctly

using BenchmarkDotNet.Attributes;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.ProcessorAgreements;

/// <summary>
/// Benchmarks for the ProcessorValidationPipelineBehavior end-to-end overhead.
/// Measures the cost of DPA validation in the Encina pipeline across enforcement modes:
/// - Block mode with valid DPA (allowed fast path)
/// - Block mode with invalid DPA (blocked)
/// - Warn mode (logs but proceeds)
/// - Disabled mode (skip validation entirely)
/// - No attribute (skip branch — attribute cache lookup only)
/// </summary>
/// <remarks>
/// <para>
/// The pipeline behavior checks every decorated request against the processor agreement system.
/// This benchmark measures the overhead per enforcement mode to guide configuration decisions.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*ProcessorAgreementPipelineBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*ProcessorAgreementPipeline*" --job short
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class ProcessorAgreementPipelineBenchmarks
{
    private ServiceProvider _blockValidProvider = null!;
    private ServiceProvider _blockInvalidProvider = null!;
    private ServiceProvider _warnProvider = null!;
    private ServiceProvider _disabledProvider = null!;
    private ServiceProvider _noAttributeProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _blockValidProvider = BuildProvider(ProcessorAgreementEnforcementMode.Block, hasValidDPA: true);
        _blockInvalidProvider = BuildProvider(ProcessorAgreementEnforcementMode.Block, hasValidDPA: false);
        _warnProvider = BuildProvider(ProcessorAgreementEnforcementMode.Warn, hasValidDPA: false);
        _disabledProvider = BuildProvider(ProcessorAgreementEnforcementMode.Disabled, hasValidDPA: false);
        _noAttributeProvider = BuildProvider(ProcessorAgreementEnforcementMode.Block, hasValidDPA: true, useNoAttributeCommand: true);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _blockValidProvider.Dispose();
        _blockInvalidProvider.Dispose();
        _warnProvider.Dispose();
        _disabledProvider.Dispose();
        _noAttributeProvider.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Pipeline — Block mode (valid DPA, allowed)")]
    public async Task<int> Pipeline_BlockMode_ValidDPA()
    {
        using var scope = _blockValidProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineBenchCommand(Guid.NewGuid().ToString()));
        return result.Match(Right: v => v, Left: _ => -1);
    }

    [Benchmark(Description = "Pipeline — Block mode (no DPA, blocked)")]
    public async Task<int> Pipeline_BlockMode_Blocked()
    {
        using var scope = _blockInvalidProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineBenchCommand(Guid.NewGuid().ToString()));
        return result.Match(Right: v => v, Left: _ => -1);
    }

    [Benchmark(Description = "Pipeline — Warn mode (logs, proceeds)")]
    public async Task<int> Pipeline_WarnMode()
    {
        using var scope = _warnProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineBenchCommand(Guid.NewGuid().ToString()));
        return result.Match(Right: v => v, Left: _ => -1);
    }

    [Benchmark(Description = "Pipeline — Disabled mode (skip all)")]
    public async Task<int> Pipeline_DisabledMode()
    {
        using var scope = _disabledProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineBenchCommand(Guid.NewGuid().ToString()));
        return result.Match(Right: v => v, Left: _ => -1);
    }

    [Benchmark(Description = "Pipeline — No attribute (cache lookup only)")]
    public async Task<int> Pipeline_NoAttribute()
    {
        using var scope = _noAttributeProvider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new NoPipelineBenchCommand("test"));
        return result.Match(Right: v => v, Left: _ => -1);
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Commands & Handlers
    // ────────────────────────────────────────────────────────────

    [RequiresProcessor(ProcessorId = "00000000-0000-0000-0000-000000000001")]
    private sealed record PipelineBenchCommand(string ProcessorIdValue) : IRequest<int>;

    private sealed class PipelineBenchHandler : IRequestHandler<PipelineBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(PipelineBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    private sealed record NoPipelineBenchCommand(string Data) : IRequest<int>;

    private sealed class NoPipelineBenchHandler : IRequestHandler<NoPipelineBenchCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoPipelineBenchCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Service Provider Builder
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(
        ProcessorAgreementEnforcementMode mode,
        bool hasValidDPA,
        bool useNoAttributeCommand = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        if (useNoAttributeCommand)
        {
            services.AddEncina(config =>
                config.RegisterServicesFromAssemblyContaining<NoPipelineBenchCommand>());
            services.AddScoped<IRequestHandler<NoPipelineBenchCommand, int>, NoPipelineBenchHandler>();
        }
        else
        {
            services.AddEncina(config =>
                config.RegisterServicesFromAssemblyContaining<PipelineBenchCommand>());
            services.AddScoped<IRequestHandler<PipelineBenchCommand, int>, PipelineBenchHandler>();
        }

        // Mock DPA service
        var dpaService = Substitute.For<IDPAService>();
        dpaService.HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(hasValidDPA)));

        if (!hasValidDPA)
        {
            dpaService.ValidateDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(Right<EncinaError, DPAValidationResult>(new DPAValidationResult
                {
                    ProcessorId = Guid.Empty.ToString(),
                    IsValid = false,
                    MissingTerms = [],
                    Warnings = [],
                    ValidatedAtUtc = DateTimeOffset.UtcNow
                })));
        }

        // Register mocked service BEFORE AddEncinaProcessorAgreements (TryAdd semantics)
        services.AddScoped<IDPAService>(_ => dpaService);
        services.AddScoped<IProcessorService>(_ => Substitute.For<IProcessorService>());

        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = mode;
        });

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }
}
