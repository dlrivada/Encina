using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Compliance.GDPR.Diagnostics;

/// <summary>
/// Provides the dedicated activity source and metrics for lawful basis validation observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.GDPR.LawfulBasis</c>) for
/// fine-grained trace filtering, while reusing the shared <c>Encina.Compliance.GDPR</c> meter
/// from <see cref="GDPRDiagnostics"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>basis</c>, <c>outcome</c>) to enable flexible
/// dashboards without creating separate counters per outcome.
/// </para>
/// </remarks>
internal static class LawfulBasisDiagnostics
{
    internal const string SourceName = "Encina.Compliance.GDPR.LawfulBasis";
    internal const string SourceVersion = "1.0";

    /// <summary>
    /// Dedicated activity source for lawful basis validation traces.
    /// </summary>
    internal static readonly ActivitySource Source = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    internal const string TagBasis = "basis";
    internal const string TagOutcome = "outcome";
    internal const string TagRequestType = "request.type";
    internal const string TagLawfulBasisDeclared = "lawful_basis.declared";
    internal const string TagLawfulBasisValid = "lawful_basis.valid";
    internal const string TagFailureReason = "failure_reason";

    // ---- Counters (reuse existing GDPR meter) ----

    /// <summary>
    /// Total lawful basis validations, tagged with <c>basis</c> and <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> ValidationsTotal =
        GDPRDiagnostics.Meter.CreateCounter<long>(
            "lawful_basis_validations_total",
            description: "Total number of lawful basis validations.");

    /// <summary>
    /// Total consent status checks, tagged with <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> ConsentChecksTotal =
        GDPRDiagnostics.Meter.CreateCounter<long>(
            "lawful_basis_consent_checks_total",
            description: "Total number of consent status checks for consent-based processing.");

    /// <summary>
    /// Total Legitimate Interest Assessment checks, tagged with <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> LiaChecksTotal =
        GDPRDiagnostics.Meter.CreateCounter<long>(
            "lawful_basis_lia_checks_total",
            description: "Total number of Legitimate Interest Assessment checks.");

    // ---- Activity helpers ----

    /// <summary>
    /// Starts a new <c>LawfulBasisValidation</c> activity with initial tags.
    /// </summary>
    /// <param name="requestType">The request type being validated.</param>
    /// <returns>The started activity, or <c>null</c> when no listener is attached.</returns>
    internal static Activity? StartValidation(Type requestType)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("LawfulBasisValidation", ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestType.Name);
        activity?.SetTag(TagLawfulBasisDeclared, false);
        return activity;
    }

    /// <summary>
    /// Completes an existing lawful basis validation activity with final status.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    /// <param name="valid">Whether the validation passed.</param>
    /// <param name="reason">Optional failure reason when <paramref name="valid"/> is <c>false</c>.</param>
    internal static void CompleteValidation(Activity? activity, bool valid, string? reason = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(TagLawfulBasisValid, valid);

        if (valid)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity.SetTag(TagFailureReason, reason ?? "unknown");
            activity.SetStatus(ActivityStatusCode.Error, reason);
        }
    }

    /// <summary>
    /// Sets the <c>lawful_basis.declared</c> and <c>basis</c> tags on the activity.
    /// </summary>
    internal static void SetBasis(Activity? activity, LawfulBasis basis)
    {
        activity?.SetTag(TagLawfulBasisDeclared, true);
        activity?.SetTag(TagBasis, basis.ToString());
    }
}
