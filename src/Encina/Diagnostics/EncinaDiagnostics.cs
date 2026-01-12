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

        var activity = ActivitySource.StartActivity("Encina.Send", ActivityKind.Internal);
        activity?.SetTag(ActivityTagNames.RequestType, requestType.FullName);
        activity?.SetTag(ActivityTagNames.RequestName, requestType.Name);
        activity?.SetTag(ActivityTagNames.ResponseType, responseType.FullName);
        activity?.SetTag(ActivityTagNames.RequestKind, requestKind);
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
            activity.SetTag(ActivityTagNames.FailureReason, errorCode);
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
        activity?.SetTag(ActivityTagNames.RequestType, requestType.FullName);
        activity?.SetTag(ActivityTagNames.RequestName, requestType.Name);
        activity?.SetTag(ActivityTagNames.ItemType, itemType.FullName);
        activity?.SetTag(ActivityTagNames.ItemName, itemType.Name);
        return activity;
    }

    internal static void RecordStreamItemCount(Activity? activity, int itemCount)
    {
        activity?.SetTag(ActivityTagNames.StreamItemCount, itemCount);
    }
}
