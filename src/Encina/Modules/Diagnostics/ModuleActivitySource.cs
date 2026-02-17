using System.Diagnostics;

namespace Encina.Modules.Diagnostics;

/// <summary>
/// Provides the activity source for modular monolith distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.Modules"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// module lifecycle and dispatch traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class ModuleActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Modules", "1.0");

    /// <summary>
    /// The activity source name for external registration (e.g., in OpenTelemetry builder).
    /// </summary>
    internal const string SourceName = "Encina.Modules";

    /// <summary>
    /// Starts a module dispatch activity.
    /// </summary>
    /// <param name="moduleName">The name of the target module.</param>
    /// <param name="requestType">The type of the request being dispatched.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartDispatch(string moduleName, string requestType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.module.dispatch", ActivityKind.Internal);
        activity?.SetTag("module.name", moduleName);
        activity?.SetTag("module.request_type", requestType);
        return activity;
    }

    /// <summary>
    /// Starts a module lifecycle activity (start or stop).
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="operation">The lifecycle operation (start or stop).</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartLifecycle(string moduleName, string operation)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            $"encina.module.{operation}", ActivityKind.Internal);
        activity?.SetTag("module.name", moduleName);
        activity?.SetTag("module.operation", operation);
        return activity;
    }

    /// <summary>
    /// Completes a module activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    internal static void Complete(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks a module activity as failed.
    /// </summary>
    /// <param name="activity">The activity to mark as failed.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    internal static void Failed(Activity? activity, string? errorCode, string? errorMessage)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            activity.SetTag("error.code", errorCode);
        }

        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }
}
