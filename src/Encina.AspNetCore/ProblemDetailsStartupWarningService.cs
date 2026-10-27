using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.AspNetCore;

/// <summary>
/// Logs once at startup when <see cref="EncinaAspNetCoreOptions.IncludeErrorMessageInProblemDetails"/>
/// is set: a warning that it is ignored outside Development, or that it is honoured in Development.
/// </summary>
internal sealed class ProblemDetailsStartupWarningService : IHostedService
{
    private readonly IOptions<EncinaAspNetCoreOptions> _options;
    private readonly ILogger<ProblemDetailsStartupWarningService> _logger;
    private readonly IHostEnvironment? _environment;

    public ProblemDetailsStartupWarningService(
        IOptions<EncinaAspNetCoreOptions> options,
        ILogger<ProblemDetailsStartupWarningService> logger,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _options = options;
        _logger = logger;

        // Optional: a plain ServiceCollection has no host environment; that counts as non-Development.
        _environment = serviceProvider.GetService(typeof(IHostEnvironment)) as IHostEnvironment;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.IncludeErrorMessageInProblemDetails)
        {
            return Task.CompletedTask;
        }

        if (ProblemDetailsExtensions.ShouldExposeErrorMessage(_options.Value, _environment))
        {
            ProblemDetailsStartupLog.ErrorMessageOptionHonoured(_logger);
        }
        else
        {
            ProblemDetailsStartupLog.ErrorMessageOptionIgnored(_logger);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
