using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.Retention;

/// <summary>
/// Hosted service that creates fluent-configured retention policies in the store at startup.
/// </summary>
/// <remarks>
/// This service is only registered when fluent policies are configured via
/// <see cref="RetentionOptions.AddPolicy"/> and auto-registration from attributes is disabled.
/// When auto-registration is enabled, the <see cref="RetentionAutoRegistrationHostedService"/>
/// handles both attribute-discovered and fluent-configured policies.
/// </remarks>
internal sealed class RetentionFluentPolicyHostedService : IHostedService
{
    private readonly RetentionFluentPolicyDescriptor _descriptor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionFluentPolicyHostedService> _logger;

    public RetentionFluentPolicyHostedService(
        RetentionFluentPolicyDescriptor descriptor,
        IServiceScopeFactory scopeFactory,
        ILogger<RetentionFluentPolicyHostedService> logger)
    {
        _descriptor = descriptor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_descriptor.Policies.Count == 0)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var policyStore = scope.ServiceProvider.GetRequiredService<IRetentionPolicyStore>();
        var policiesCreated = 0;

        foreach (var descriptor in _descriptor.Policies)
        {
            try
            {
                // Check if a policy already exists for this category
                var existingResult = await policyStore
                    .GetByCategoryAsync(descriptor.DataCategory, cancellationToken)
                    .ConfigureAwait(false);

                var policyExists = existingResult.Match(
                    Right: option => option.IsSome,
                    Left: _ => false);

                if (policyExists)
                {
                    _logger.RetentionPolicyAlreadyExists(descriptor.DataCategory);
                    continue;
                }

                // Create the policy
                var policy = RetentionPolicy.Create(
                    dataCategory: descriptor.DataCategory,
                    retentionPeriod: descriptor.RetentionPeriod,
                    autoDelete: descriptor.AutoDelete,
                    reason: descriptor.Reason ?? "Configured via AddPolicy() fluent API",
                    legalBasis: descriptor.LegalBasis);

                var createResult = await policyStore
                    .CreateAsync(policy, cancellationToken)
                    .ConfigureAwait(false);

                createResult.Match(
                    Right: _ => policiesCreated++,
                    Left: error => _logger.RetentionAutoRegistrationPolicyFailed(
                        descriptor.DataCategory,
                        new InvalidOperationException(error.Message)));
            }
            catch (Exception ex)
            {
                _logger.RetentionAutoRegistrationPolicyFailed(descriptor.DataCategory, ex);
            }
        }

        _logger.RetentionAutoRegistrationCompleted(policiesCreated, 0);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
