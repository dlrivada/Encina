using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Audit;

/// <summary>
/// Stops the host at startup when the audit module would run without a PII masker, unless the application
/// explicitly opted out with <see cref="OperationAuditOptions.AllowUnmaskedPayloads"/>.
/// </summary>
/// <remarks>
/// A missing masker, or the pass-through <see cref="NullPiiMasker"/>, would let raw personal data reach the audit
/// store. The only opt-out is explicit and logged at Warning on every start (SPEC-002 DEC-006).
/// </remarks>
internal sealed class OperationAuditMaskingStartupValidator : IHostedLifecycleService
{
    internal const string MissingMaskerMessage =
        "The audit module requires an IPiiMasker. Register one (for example DefaultSensitiveDataRedactor, "
        + "or AddEncinaPII() from Encina.Security.PII), or set OperationAuditOptions.AllowUnmaskedPayloads = true "
        + "to run explicitly without masking (set it in the configure action of AddEncinaAudit, not only through "
        + "configuration binding).";

    private readonly IServiceProvider _services;
    private readonly OperationAuditOptions _options;
    private readonly ILogger<OperationAuditMaskingStartupValidator> _logger;

    public OperationAuditMaskingStartupValidator(
        IServiceProvider services,
        IOptions<OperationAuditOptions> options,
        ILogger<OperationAuditMaskingStartupValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Validates before any hosted service starts, so a missing masker stops the host before a request is audited.
    /// </summary>
    /// <exception cref="InvalidOperationException">No masker is registered and the opt-out is not set.</exception>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        var masker = _services.GetService<IPiiMasker>();
        if (masker is not null and not NullPiiMasker)
        {
            return Task.CompletedTask;
        }

        // The opt-out registers NullPiiMasker only when the configure action of AddEncinaAudit sets the flag. A flag
        // bound from IConfiguration alone leaves no masker, and the audit pipeline could not be built at all.
        if (masker is null || !_options.AllowUnmaskedPayloads)
        {
            throw new InvalidOperationException(MissingMaskerMessage);
        }

        Log.AuditRunsWithoutPiiMasker(_logger);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
