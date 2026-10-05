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
        var activity = StartActivity("Encina.Send", requestType);
        activity?.SetTag(ActivityTagNames.ResponseType, responseType.FullName)
            .SetTag(ActivityTagNames.RequestKind, requestKind);
        return activity;
    }

    /// <summary>
    /// Starts a dispatch activity tagged with the request type and the identity kind of the
    /// dispatch in flight (<c>encina.identity.kind</c>; never the user id), or returns
    /// <see langword="null"/> when nobody listens.
    /// </summary>
    private static Activity? StartActivity(string name, Type requestType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity(name, ActivityKind.Internal)
            ?.SetTag(ActivityTagNames.RequestType, requestType.FullName)
            .SetTag(ActivityTagNames.RequestName, requestType.Name)
            .SetTag(ActivityTagNames.IdentityKind, ToTagValue(AmbientRequestContext.DispatchIdentityKind));
    }

    /// <summary>
    /// The value of the <c>encina.identity.kind</c> tag: the kind only, never the user id.
    /// </summary>
    // crap-exempt: single-question switch — the tag value of each identity kind.
    internal static string ToTagValue(IdentityKind kind) => kind switch
    {
        IdentityKind.User => "user",
        IdentityKind.Service => "service",
        _ => "anonymous"
    };

    internal static void SendCompleted(Activity? activity, bool isSuccess, string? errorCode = null)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            activity.SetTag(ActivityTagNames.FailureReason, errorCode);
        }

        // Only the error code (never EncinaError.Message or an exception message) is exposed as the
        // Activity status description: the message can carry personal data (#1319).
        activity.SetStatus(isSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, errorCode);
        activity.Dispose();
    }

    internal static Activity? StartStreamActivity(Type requestType, Type itemType)
    {
        var activity = StartActivity("Encina.Stream", requestType);
        activity?.SetTag(ActivityTagNames.ItemType, itemType.FullName)
            .SetTag(ActivityTagNames.ItemName, itemType.Name);
        return activity;
    }

    internal static void RecordStreamItemCount(Activity? activity, int itemCount)
    {
        activity?.SetTag(ActivityTagNames.StreamItemCount, itemCount);
    }
}
