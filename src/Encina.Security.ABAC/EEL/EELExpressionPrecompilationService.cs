using System.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.ABAC.EEL;

/// <summary>
/// Hosted service that precompiles all EEL expressions at application startup,
/// providing fail-fast behavior for invalid expressions.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="ABACOptions.ValidateExpressionsAtStartup"/> is <c>true</c> and
/// <see cref="ABACOptions.ExpressionScanAssemblies"/> contains entries, this service:
/// </para>
/// <list type="number">
/// <item><description>Scans the configured assemblies for <see cref="RequireConditionAttribute"/> decorations.</description></item>
/// <item><description>Compiles each discovered expression concurrently via <see cref="EELCompiler"/>.</description></item>
/// <item><description>Throws <see cref="InvalidOperationException"/> if any expression fails to compile,
/// preventing the application from starting with invalid policies.</description></item>
/// </list>
/// <para>
/// This service is registered conditionally by <see cref="ServiceCollectionExtensions.AddEncinaABAC"/>
/// when both <see cref="ABACOptions.ValidateExpressionsAtStartup"/> is <c>true</c> and
/// <see cref="ABACOptions.ExpressionScanAssemblies"/> has entries.
/// </para>
/// </remarks>
internal sealed class EELExpressionPrecompilationService : IHostedService
{
    private readonly EELCompiler _compiler;
    private readonly ABACOptions _options;
    private readonly ILogger<EELExpressionPrecompilationService> _logger;

    public EELExpressionPrecompilationService(
        EELCompiler compiler,
        IOptions<ABACOptions> options,
        ILogger<EELExpressionPrecompilationService> logger)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _compiler = compiler;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var assemblies = _options.ExpressionScanAssemblies;

        if (assemblies.Count == 0)
        {
            _logger.LogDebug(
                "EEL precompilation skipped: no assemblies configured in ExpressionScanAssemblies");
            return;
        }

        var expressions = EELExpressionDiscovery.Discover(assemblies);

        if (expressions.Count == 0)
        {
            _logger.LogDebug(
                "EEL precompilation skipped: no [RequireCondition] expressions found in {AssemblyCount} assembly(ies)",
                assemblies.Count);
            return;
        }

        _logger.LogInformation(
            "Precompiling {Count} EEL expression(s) from {AssemblyCount} assembly(ies)",
            expressions.Count,
            assemblies.Count);

        var stopwatch = Stopwatch.StartNew();
        var failures = new List<(Type RequestType, string Expression, string ErrorMessage)>();

        // Compile expressions concurrently with bounded parallelism.
        var concurrency = Math.Max(1, Environment.ProcessorCount);
        using var semaphore = new SemaphoreSlim(concurrency, concurrency);

        var tasks = expressions.Select(async entry =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await _compiler.CompileAsync(entry.Expression, cancellationToken)
                    .ConfigureAwait(false);

                result.Match(
                    Left: error =>
                    {
                        lock (failures)
                        {
                            failures.Add((entry.RequestType, entry.Expression, error.Message));
                        }
                    },
                    Right: _ => { });
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        stopwatch.Stop();

        if (failures.Count > 0)
        {
            foreach (var (requestType, expression, errorMessage) in failures)
            {
                _logger.LogError(
                    "EEL compilation failed for {RequestType}: expression '{Expression}' — {ErrorMessage}",
                    requestType.FullName,
                    expression,
                    errorMessage);
            }

            throw new InvalidOperationException(
                $"EEL precompilation failed: {failures.Count} expression(s) could not be compiled. " +
                $"See logs for details. Failing expressions: " +
                string.Join("; ", failures.Select(f =>
                    $"[{f.RequestType.Name}] \"{f.Expression}\" → {f.ErrorMessage}")));
        }

        _logger.LogInformation(
            "EEL precompilation completed: {Count} expression(s) compiled in {ElapsedMs}ms",
            expressions.Count,
            stopwatch.ElapsedMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
