using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Security.Secrets.Rotation;

/// <summary>
/// Orchestrates the secret rotation workflow: generate → rotate → notify.
/// </summary>
/// <remarks>
/// <para>
/// The coordinator manages the three-step rotation process:
/// <list type="number">
/// <item>Calls <see cref="ISecretRotationHandler.GenerateNewSecretAsync"/> to create a new secret value</item>
/// <item>Calls <see cref="ISecretRotator.RotateSecretAsync"/> to persist the rotation in the vault</item>
/// <item>Calls <see cref="ISecretRotationHandler.OnRotationAsync"/> to notify the handler of the change</item>
/// </list>
/// </para>
/// <para>
/// If any step fails, the process stops and returns the error. Partial rotations
/// (e.g., vault rotated but notification failed) are logged for manual remediation.
/// </para>
/// </remarks>
public sealed class SecretRotationCoordinator
{
    private readonly ISecretRotator? _rotator;
    private readonly IEnumerable<ISecretRotationHandler> _handlers;
    private readonly ISecretReader? _reader;
    private readonly ILogger<SecretRotationCoordinator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SecretRotationCoordinator"/>.
    /// </summary>
    /// <param name="handlers">The registered rotation handlers.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="rotator">The optional secret rotator for vault-side rotation.</param>
    /// <param name="reader">The optional secret reader for retrieving current values.</param>
    public SecretRotationCoordinator(
        IEnumerable<ISecretRotationHandler> handlers,
        ILogger<SecretRotationCoordinator> logger,
        ISecretRotator? rotator = null,
        ISecretReader? reader = null)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(logger);

        _handlers = handlers;
        _logger = logger;
        _rotator = rotator;
        _reader = reader;
    }

    /// <summary>
    /// Executes the full rotation workflow for a secret.
    /// </summary>
    /// <param name="secretName">The name of the secret to rotate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on successful rotation;
    /// <c>Left(EncinaError)</c> with <see cref="SecretsErrors.RotationFailedCode"/> on failure.
    /// </returns>
    /// <remarks>
    /// Uses the first registered <see cref="ISecretRotationHandler"/> to generate and handle
    /// the new secret value. If no handler is registered, returns a rotation failed error.
    /// </remarks>
    public async ValueTask<Either<EncinaError, Unit>> RotateWithCallbacksAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        Log.RotationStarted(_logger, secretName);

        // Find a handler for this rotation
        var handler = _handlers.FirstOrDefault();
        if (handler is null)
        {
            var reason = $"No rotation handler registered for secret '{secretName}'.";
            Log.RotationFailed(_logger, secretName, reason);
            return SecretsErrors.RotationFailed(secretName, reason);
        }

        // Step 1: Get the current value (if reader is available)
        string? oldValue = null;
        if (_reader is not null)
        {
            var currentResult = await _reader.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
            oldValue = currentResult.MatchUnsafe(Right: v => v, Left: _ => (string?)null);
        }

        // Step 2: Generate a new secret value
        var generateResult = await handler.GenerateNewSecretAsync(secretName, cancellationToken).ConfigureAwait(false);

        if (generateResult.IsLeft)
        {
            var reason = "Failed to generate new secret value.";
            Log.RotationFailed(_logger, secretName, reason);
            return generateResult.Map<Unit>(_ => Unit.Default);
        }

        var newValue = generateResult.Match(Right: v => v, Left: _ => string.Empty);

        // Step 3: Rotate in the vault (if rotator is available)
        if (_rotator is not null)
        {
            var rotateResult = await _rotator.RotateSecretAsync(secretName, cancellationToken).ConfigureAwait(false);

            if (rotateResult.IsLeft)
            {
                var reason = "Vault rotation failed.";
                Log.RotationFailed(_logger, secretName, reason);
                return rotateResult;
            }
        }

        // Step 4: Notify the handler of the rotation
        var notifyResult = await handler.OnRotationAsync(
            secretName,
            oldValue ?? string.Empty,
            newValue,
            cancellationToken).ConfigureAwait(false);

        if (notifyResult.IsLeft)
        {
            var reason = "Rotation notification callback failed.";
            Log.RotationFailed(_logger, secretName, reason);
            return notifyResult;
        }

        Log.RotationCompleted(_logger, secretName);
        return Right<EncinaError, Unit>(Unit.Default);
    }
}
