using System.Diagnostics;

namespace Encina.Polly.Diagnostics;

/// <summary>
/// The <c>Encina.Retry</c> activity of the retry behavior, on the <c>Encina</c> activity source.
/// </summary>
/// <remarks>
/// Tags carry the request name, the tenant id (<c>encina.tenant_id</c>, omitted when there is none) and an error
/// code or exception type. They never carry a response body, an error message, a URL or an identifier of a
/// data subject (SPEC-002 REQ-062).
/// </remarks>
internal static class RetryTelemetry
{
    private static readonly ActivitySource ActivitySource = new("Encina", "1.0");

    internal static Activity? StartRetry(string requestName, string? tenantId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("Encina.Retry", ActivityKind.Internal);
        activity?.SetTag("Encina.request_name", requestName);
        if (tenantId is not null)
        {
            activity?.SetTag("encina.tenant_id", tenantId);
        }

        return activity;
    }

    internal static void Complete(Activity? activity, bool isSuccess, string? errorCode)
    {
        if (activity is null)
        {
            return;
        }

        if (errorCode is not null)
        {
            activity.SetTag("Encina.failure_code", errorCode);
        }

        activity.SetStatus(isSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error, errorCode);
    }
}
