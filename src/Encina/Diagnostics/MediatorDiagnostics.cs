using System.Diagnostics;

namespace Encina;

/// <summary>
/// Provides the activity source consumed by telemetry-oriented behaviors.
/// </summary>
internal static class MediatorDiagnostics
{
    internal static readonly ActivitySource ActivitySource = new("Encina", "1.0");

    internal static Activity? SendStarted(Type requestType, Type responseType, string requestKind)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Send", ActivityKind.Internal);
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

    internal static Activity? StartStreamActivity(Type requestType, Type itemType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Stream", ActivityKind.Internal);
        activity?.SetTag("mediator.request_type", requestType.FullName);
        activity?.SetTag("mediator.request_name", requestType.Name);
        activity?.SetTag("mediator.item_type", itemType.FullName);
        activity?.SetTag("mediator.item_name", itemType.Name);
        return activity;
    }

    internal static void RecordStreamItemCount(Activity? activity, int itemCount)
    {
        activity?.SetTag("mediator.stream_item_count", itemCount);
    }
}
