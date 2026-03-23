using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Behaviors;
using Encina.Compliance.Attestation.Health;
using Encina.Compliance.Attestation.Providers;
using Encina.Compliance.Attestation.Validation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Attestation;

/// <summary>
/// Extension methods for configuring Encina Attestation compliance services.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const long MaxHttpResponseBytes = 1_048_576; // 1 MB — SEC-7

    /// <summary>
    /// Adds Encina audit attestation services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Action to configure <see cref="AttestationOptions"/> and select a provider.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Exactly one provider must be configured via the options:
    /// <list type="bullet">
    /// <item><see cref="AttestationOptionsExtensions.UseInMemory"/> — for testing and development</item>
    /// <item><see cref="AttestationOptionsExtensions.UseHashChain"/> — for self-hosted production (zero cost)</item>
    /// <item><see cref="AttestationOptionsExtensions.UseHttp"/> — for external HTTP attestation endpoints</item>
    /// </list>
    /// </para>
    /// <para>
    /// When using <see cref="AttestationOptionsExtensions.UseHttp"/>, SSRF protection is enforced
    /// at startup via <see cref="IValidateOptions{TOptions}"/>: only HTTPS endpoints are accepted
    /// and loopback/link-local addresses are rejected unless
    /// <see cref="HttpAttestationOptions.AllowInsecureHttp"/> is set to <c>true</c>.
    /// </para>
    /// <para>
    /// The <see cref="Behaviors.AttestationPipelineBehavior{TRequest,TResponse}"/> is registered
    /// automatically and activates on requests decorated with <see cref="Attributes.AttestDecisionAttribute"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaAttestation(options =>
    /// {
    ///     options.UseInMemory();
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no attestation provider is configured.</exception>
    public static IServiceCollection AddEncinaAttestation(
        this IServiceCollection services,
        Action<AttestationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AttestationOptions();
        configure(options);

        services.Configure(configure);
        services.TryAddSingleton(TimeProvider.System);

        // Register the selected provider
        if (options.UseInMemoryProvider)
        {
            services.TryAddSingleton<IAuditAttestationProvider, InMemoryAttestationProvider>();
        }
        else if (options.HashChainOptions is not null)
        {
            services.Configure<HashChainOptions>(hc =>
            {
                hc.StoragePath = options.HashChainOptions.StoragePath;
                hc.HashAlgorithm = options.HashChainOptions.HashAlgorithm;
                hc.HmacKey = options.HashChainOptions.HmacKey;
            });
            services.TryAddSingleton<IAuditAttestationProvider, HashChainAttestationProvider>();
        }
        else if (options.HttpOptions is not null)
        {
            if (options.HttpOptions.AttestEndpointUrl is null)
            {
                throw new InvalidOperationException(
                    "HttpAttestationOptions.AttestEndpointUrl must be configured when using the HTTP attestation provider.");
            }

            services.Configure<HttpAttestationOptions>(http =>
            {
                http.AttestEndpointUrl = options.HttpOptions.AttestEndpointUrl;
                http.VerifyEndpointUrl = options.HttpOptions.VerifyEndpointUrl;
                http.AuthHeader = options.HttpOptions.AuthHeader;
                http.AllowInsecureHttp = options.HttpOptions.AllowInsecureHttp;
            });

            // SEC-1: validate SSRF-sensitive options at startup
            services.TryAddSingleton<IValidateOptions<HttpAttestationOptions>, HttpAttestationOptionsValidator>();
            services.AddOptions<HttpAttestationOptions>().ValidateOnStart();

            // SEC-7: cap response body size at 1 MB to prevent memory exhaustion
            services.AddHttpClient<HttpAttestationProvider>(client =>
            {
                client.MaxResponseContentBufferSize = MaxHttpResponseBytes;
            });

            services.TryAddSingleton<IAuditAttestationProvider, HttpAttestationProvider>();
        }
        else
        {
            throw new InvalidOperationException(
                "No attestation provider configured. Call UseInMemory(), UseHashChain(), or UseHttp() on the options.");
        }

        // ARCH-1: register the attestation pipeline behavior (activates on [AttestDecision] attributes)
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(AttestationPipelineBehavior<,>));

        if (options.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<AttestationHealthCheck>(
                    AttestationHealthCheck.DefaultName,
                    tags: AttestationHealthCheck.Tags);
        }

        return services;
    }
}
