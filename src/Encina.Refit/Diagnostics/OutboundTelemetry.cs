using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Refit.Diagnostics;

/// <summary>
/// The <c>Encina.Outbound.Send</c> activity and the <c>encina.outbound.requests_total</c> counter of the Refit handler,
/// on the <c>Encina</c> activity source and meter.
/// </summary>
/// <remarks>
/// Tags carry the request name, the error code, <c>transient</c>, the HTTP status and the tenant id
/// (<c>encina.tenant_id</c>, omitted when there is none). They never carry a response body, an error message,
/// a URL or an identifier of a data subject (SPEC-002 REQ-062).
/// </remarks>
internal static class OutboundTelemetry
{
    private static readonly ActivitySource ActivitySource = new("Encina", "1.0");
    private static readonly Meter Meter = new("Encina", "1.0");

    private static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "encina.outbound.requests_total",
        unit: "{requests}",
        description: "Total number of outbound API calls, by outcome.");

    internal static Activity? Start(string requestName, string? tenantId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Outbound.Send", ActivityKind.Client);
        activity?.SetTag("Encina.request_name", requestName);
        if (tenantId is not null)
        {
            activity?.SetTag("encina.tenant_id", tenantId);
        }

        return activity;
    }

    internal static void Complete(
        Activity? activity,
        string requestName,
        string? errorCode,
        bool? transient,
        int? httpStatus,
        string? tenantId)
    {
        var tags = new TagList { { "request_name", requestName }, { "outcome", errorCode is null ? "success" : "failure" } };
        AddIfPresent(ref tags, "error_code", errorCode);
        AddIfPresent(ref tags, "transient", transient);
        AddIfPresent(ref tags, "http.status", httpStatus);
        AddIfPresent(ref tags, "encina.tenant_id", tenantId);

        Requests.Add(1, tags);

        if (activity is null)
        {
            return;
        }

        // SetTag with a null value adds nothing
        activity.SetTag("Encina.failure_code", errorCode);
        activity.SetTag("transient", transient);
        activity.SetTag("http.status", httpStatus);
        activity.SetStatus(errorCode is null ? ActivityStatusCode.Ok : ActivityStatusCode.Error, errorCode);
        activity.Dispose();
    }

    private static void AddIfPresent(ref TagList tags, string key, object? value)
    {
        if (value is not null)
        {
            tags.Add(key, value);
        }
    }
}
