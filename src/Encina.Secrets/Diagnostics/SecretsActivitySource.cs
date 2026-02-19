using System.Diagnostics;

namespace Encina.Secrets.Diagnostics;

/// <summary>
/// Provides the <see cref="ActivitySource"/> for Encina.Secrets distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Activities are created for each secret management operation (get, set, delete, list, exists).
/// Each activity includes standard tags for the secret provider, operation name, and outcome.
/// </para>
/// <para>
/// When no listeners are registered (i.e., OpenTelemetry is not configured), activities are
/// not created, resulting in zero overhead.
/// </para>
/// </remarks>
internal static class SecretsActivitySource
{
    /// <summary>
    /// The activity source name used for OpenTelemetry registration.
    /// </summary>
    public const string Name = "Encina.Secrets";

    /// <summary>
    /// The activity source version.
    /// </summary>
    public const string Version = "1.0";

    private static readonly ActivitySource Source = new(Name, Version);

    /// <summary>
    /// Starts a new activity for a secret operation.
    /// </summary>
    /// <param name="operationName">The operation name (e.g., "get", "set", "delete").</param>
    /// <param name="secretName">The secret name, or <c>null</c> if secret name recording is disabled.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    public static Activity? StartOperation(string operationName, string? secretName = null)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity($"encina.secrets.{operationName}", ActivityKind.Client);
        activity?.SetTag("secrets.operation", operationName);

        if (secretName is not null)
        {
            activity?.SetTag("secrets.name", secretName);
        }

        return activity;
    }

    /// <summary>
    /// Marks an activity as successfully completed.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    public static void SetSuccess(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("secrets.success", true);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Marks an activity as failed with an error code and message.
    /// </summary>
    /// <param name="activity">The activity to mark as failed.</param>
    /// <param name="errorCode">The error code from <see cref="SecretsErrorCodes"/>.</param>
    /// <param name="errorMessage">The error message.</param>
    public static void SetError(Activity? activity, string errorCode, string errorMessage)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("secrets.success", false);
        activity.SetTag("secrets.error_code", errorCode);
        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
    }
}
