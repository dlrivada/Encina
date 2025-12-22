using System.Diagnostics;

namespace Encina;

/// <summary>
/// Provides the activity source consumed by telemetry-oriented behaviors.
/// </summary>
internal static class EncinaDiagnostics
{
    internal static readonly ActivitySource ActivitySource = new("Encina", "1.0");

    internal static Activity? SendStarted(Type requestType, Type responseType, string requestKind)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        Activity? activity = ActivitySource.StartActivity("Encina.Send", ActivityKind.Internal);
        activity?.SetTag("Encina.request_type", requestType.FullName);
        activity?.SetTag("Encina.request_name", requestType.Name);
        activity?.SetTag("Encina.response_type", responseType.FullName);
        activity?.SetTag("Encina.request_kind", requestKind);
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
            activity.SetTag("Encina.failure_reason", errorCode);
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

        Activity? activity = ActivitySource.StartActivity("Encina.Stream", ActivityKind.Internal);
        activity?.SetTag("Encina.request_type", requestType.FullName);
        activity?.SetTag("Encina.request_name", requestType.Name);
        activity?.SetTag("Encina.item_type", itemType.FullName);
        activity?.SetTag("Encina.item_name", itemType.Name);
        return activity;
    }

    internal static void RecordStreamItemCount(Activity? activity, int itemCount)
    {
        activity?.SetTag("Encina.stream_item_count", itemCount);
    }
}
