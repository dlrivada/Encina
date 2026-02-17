using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Security.Diagnostics;

/// <summary>
/// Provides the activity source and meter for Encina security observability.
/// </summary>
internal static class SecurityDiagnostics
{
    internal const string SourceName = "Encina.Security";
    internal const string SourceVersion = "1.0";

    internal static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Counters
    internal static readonly Counter<long> AuthorizationTotal =
        Meter.CreateCounter<long>("security.authorization.total",
            description: "Total number of security authorization evaluations.");

    internal static readonly Counter<long> AuthorizationAllowed =
        Meter.CreateCounter<long>("security.authorization.allowed",
            description: "Number of authorization evaluations that were allowed.");

    internal static readonly Counter<long> AuthorizationDenied =
        Meter.CreateCounter<long>("security.authorization.denied",
            description: "Number of authorization evaluations that were denied.");

    // Histogram
    internal static readonly Histogram<double> AuthorizationDuration =
        Meter.CreateHistogram<double>("security.authorization.duration",
            unit: "ms",
            description: "Duration of security authorization evaluations in milliseconds.");

    // Tag names
    internal const string TagRequestType = "security.request_type";
    internal const string TagUserId = "security.user_id";
    internal const string TagOutcome = "security.outcome";
    internal const string TagDenialReason = "security.denial_reason";
    internal const string TagAttributeType = "security.attribute_type";

    internal static Activity? StartAuthorize(string requestTypeName)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Security.Authorize", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        return activity;
    }

    internal static void SetUserId(Activity? activity, string? userId)
    {
        if (activity is not null && userId is not null)
        {
            activity.SetTag(TagUserId, userId);
        }
    }

    internal static void RecordAttributeEvaluated(Activity? activity, string attributeTypeName)
    {
        activity?.AddEvent(new ActivityEvent($"{attributeTypeName}.evaluated"));
    }

    internal static void RecordAllowed(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "allowed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    internal static void RecordDenied(Activity? activity, string denialReason)
    {
        activity?.SetTag(TagOutcome, "denied");
        activity?.SetTag(TagDenialReason, denialReason);
        activity?.SetStatus(ActivityStatusCode.Error, denialReason);
    }
}
