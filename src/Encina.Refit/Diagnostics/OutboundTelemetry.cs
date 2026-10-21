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
        if (errorCode is not null)
        {
            tags.Add("error_code", errorCode);
        }

        if (transient is { } isTransient)
        {
            tags.Add("transient", isTransient);
        }

        if (httpStatus is { } status)
        {
            tags.Add("http.status", status);
        }

        if (tenantId is not null)
        {
            tags.Add("encina.tenant_id", tenantId);
        }

        Requests.Add(1, tags);

        if (activity is null)
        {
            return;
        }

        if (errorCode is not null)
        {
            activity.SetTag("Encina.failure_code", errorCode);
        }

        if (transient is { } t)
        {
            activity.SetTag("transient", t);
        }

        if (httpStatus is { } s)
        {
            activity.SetTag("http.status", s);
        }

        activity.SetStatus(errorCode is null ? ActivityStatusCode.Ok : ActivityStatusCode.Error, errorCode);
        activity.Dispose();
    }
}
