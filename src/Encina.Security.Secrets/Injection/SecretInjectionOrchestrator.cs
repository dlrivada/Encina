using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Security.Secrets.Injection;

/// <summary>
/// Orchestrates secret injection into request objects by discovering
/// <see cref="InjectSecretAttribute"/>-decorated properties and resolving their values
/// from <see cref="ISecretReader"/>.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator is responsible for:
/// <list type="number">
/// <item>Discovering injectable properties via <see cref="SecretPropertyCache"/></item>
/// <item>Fetching secret values from <see cref="ISecretReader"/></item>
/// <item>Setting property values via compiled setter delegates</item>
/// <item>Handling <see cref="InjectSecretAttribute.FailOnError"/> behavior</item>
/// </list>
/// </para>
/// <para>
/// This class is intended to be used by <see cref="SecretInjectionPipelineBehavior{TRequest, TResponse}"/>
/// and is registered as a scoped service when <see cref="SecretsOptions.EnableSecretInjection"/> is <c>true</c>.
/// </para>
/// </remarks>
internal sealed class SecretInjectionOrchestrator
{
    private readonly ISecretReader _secretReader;
    private readonly ILogger<SecretInjectionOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretInjectionOrchestrator"/> class.
    /// </summary>
    /// <param name="secretReader">The secret reader for fetching secret values.</param>
    /// <param name="logger">The logger instance.</param>
    public SecretInjectionOrchestrator(
        ISecretReader secretReader,
        ILogger<SecretInjectionOrchestrator> logger)
    {
        ArgumentNullException.ThrowIfNull(secretReader);
        ArgumentNullException.ThrowIfNull(logger);

        _secretReader = secretReader;
        _logger = logger;
    }

    /// <summary>
    /// Injects secrets into all properties of <paramref name="request"/> that are
    /// decorated with <see cref="InjectSecretAttribute"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request instance to inject secrets into.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right(injectedCount)</c> on success, where <c>injectedCount</c> is the number
    /// of properties that had secrets injected;
    /// <c>Left(EncinaError)</c> if a required secret fails to load
    /// (when <see cref="InjectSecretAttribute.FailOnError"/> is <c>true</c>).
    /// </returns>
    public async ValueTask<Either<EncinaError, int>> InjectAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken) where TRequest : notnull
    {
        var properties = SecretPropertyCache.GetProperties(typeof(TRequest));

        if (properties.Length == 0)
        {
            return Right<EncinaError, int>(0);
        }

        var injectedCount = 0;

        foreach (var propertyInfo in properties)
        {
            var attribute = propertyInfo.Attribute;

            // Build the secret name, appending version if specified
            var secretName = attribute.Version is not null
                ? $"{attribute.SecretName}/{attribute.Version}"
                : attribute.SecretName;

            var result = await _secretReader.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);

            if (result.IsRight)
            {
                var value = result.Match(Right: v => v, Left: _ => string.Empty);
                propertyInfo.SetValue(request, value);
                injectedCount++;
            }
            else
            {
                if (attribute.FailOnError)
                {
                    Log.SecretInjectionFailed(
                        _logger,
                        typeof(TRequest).Name,
                        attribute.SecretName,
                        propertyInfo.Property.Name);

                    return SecretsErrors.InjectionFailed(attribute.SecretName, propertyInfo.Property.Name);
                }

                Log.SecretInjectionSkippedOnError(_logger, attribute.SecretName, propertyInfo.Property.Name);
            }
        }

        return Right<EncinaError, int>(injectedCount);
    }
}
