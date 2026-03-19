using System.Net.Sockets;
using LanguageExt;

namespace Encina.Security.Secrets.Resilience;

/// <summary>
/// Classifies secret errors and exceptions as transient (retriable) or permanent.
/// </summary>
internal static class SecretsTransientErrorDetector
{
    /// <summary>
    /// Determines whether the specified <see cref="EncinaError"/> represents a transient failure
    /// that should be retried.
    /// </summary>
    /// <param name="error">The error to classify.</param>
    /// <returns><c>true</c> if the error is transient and the operation should be retried.</returns>
    public static bool IsTransient(EncinaError error)
    {
        return error.GetCode().Match(
            Some: code => code == SecretsErrors.ProviderUnavailableCode,
            None: () => false);
    }

    /// <summary>
    /// Determines whether the specified exception represents a transient failure
    /// that should be retried.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns><c>true</c> if the exception is transient and the operation should be retried.</returns>
    public static bool IsTransientException(Exception exception) =>
        exception is HttpRequestException
            or TimeoutException
            or IOException
            or SocketException
            or TransientSecretException
            || (exception is TaskCanceledException tce && tce.InnerException is TimeoutException);
}
