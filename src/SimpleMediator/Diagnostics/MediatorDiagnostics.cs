using System.Diagnostics;

namespace SimpleMediator;

/// <summary>
/// Provides the activity source consumed by telemetry-oriented behaviors.
/// </summary>
internal static class MediatorDiagnostics
{
    internal static readonly ActivitySource ActivitySource = new("SimpleMediator", "1.0");

    internal static Activity? SendStarted(Type requestType, Type responseType, string requestKind)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("SimpleMediator.Send", ActivityKind.Internal);
        activity?.SetTag("mediator.request_type", requestType.FullName);
        activity?.SetTag("mediator.request_name", requestType.Name);
        activity?.SetTag("mediator.response_type", responseType.FullName);
        activity?.SetTag("mediator.request_kind", requestKind);
        return activity;
    }

    internal static void SendCompleted(Activity? activity, bool isSuccess, string? errorCode = null, string? errorMessage = null)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            activity.SetTag("mediator.failure_reason", errorCode);
        }

        activity.SetStatus(isSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }
}
