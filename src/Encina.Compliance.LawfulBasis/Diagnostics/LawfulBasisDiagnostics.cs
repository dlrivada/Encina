using System.Diagnostics;
using System.Diagnostics.Metrics;

using GDPR = Encina.Compliance.GDPR;

namespace Encina.Compliance.LawfulBasis.Diagnostics;

/// <summary>
/// Provides the activity source and meter for lawful basis compliance observability.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> (<c>Encina.Compliance.LawfulBasis</c>) for
/// fine-grained trace filtering and a dedicated <see cref="Meter"/> for metric aggregation.
/// </para>
/// <para>
/// All counters use tag-based dimensions (<c>basis</c>, <c>outcome</c>) to enable flexible
/// dashboards without creating separate counters per outcome.
/// </para>
/// </remarks>
internal static class LawfulBasisDiagnostics
{
    internal const string SourceName = "Encina.Compliance.LawfulBasis";
    internal const string SourceVersion = "1.0";

    /// <summary>
    /// Activity source for lawful basis validation and service operation traces.
    /// </summary>
    internal static readonly ActivitySource Source = new(SourceName, SourceVersion);

    /// <summary>
    /// Meter for lawful basis metrics.
    /// </summary>
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // ---- Tag constants ----

    internal const string TagBasis = "basis";
    internal const string TagOutcome = "outcome";
    internal const string TagRequestType = "request.type";
    internal const string TagLawfulBasisDeclared = "lawful_basis.declared";
    internal const string TagLawfulBasisValid = "lawful_basis.valid";
    internal const string TagFailureReason = "failure_reason";
    internal const string TagOperation = "operation";

    // ---- Pipeline counters ----

    /// <summary>
    /// Total lawful basis validations, tagged with <c>basis</c> and <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> ValidationsTotal =
        Meter.CreateCounter<long>(
            "lawful_basis.validations.total",
            description: "Total number of lawful basis validations.");

    /// <summary>
    /// Total consent status checks, tagged with <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> ConsentChecksTotal =
        Meter.CreateCounter<long>(
            "lawful_basis.consent_checks.total",
            description: "Total number of consent status checks for consent-based processing.");

    /// <summary>
    /// Total Legitimate Interest Assessment checks, tagged with <c>outcome</c>.
    /// </summary>
    internal static readonly Counter<long> LiaChecksTotal =
        Meter.CreateCounter<long>(
            "lawful_basis.lia_checks.total",
            description: "Total number of Legitimate Interest Assessment checks.");

    // ---- Aggregate operation counters ----

    /// <summary>
    /// Total lawful basis registrations created.
    /// </summary>
    internal static readonly Counter<long> RegistrationsCreated =
        Meter.CreateCounter<long>(
            "lawful_basis.registrations.created",
            description: "Number of lawful basis registrations created.");

    /// <summary>
    /// Total lawful basis registrations revoked.
    /// </summary>
    internal static readonly Counter<long> RegistrationsRevoked =
        Meter.CreateCounter<long>(
            "lawful_basis.registrations.revoked",
            description: "Number of lawful basis registrations revoked.");

    /// <summary>
    /// Total lawful basis changes (basis updated on existing registration).
    /// </summary>
    internal static readonly Counter<long> BasisChanged =
        Meter.CreateCounter<long>(
            "lawful_basis.registrations.basis_changed",
            description: "Number of lawful basis changes on existing registrations.");

    /// <summary>
    /// Total LIAs created.
    /// </summary>
    internal static readonly Counter<long> LIACreated =
        Meter.CreateCounter<long>(
            "lawful_basis.lia.created",
            description: "Number of Legitimate Interest Assessments created.");

    /// <summary>
    /// Total LIAs approved.
    /// </summary>
    internal static readonly Counter<long> LIAApproved =
        Meter.CreateCounter<long>(
            "lawful_basis.lia.approved",
            description: "Number of Legitimate Interest Assessments approved.");

    /// <summary>
    /// Total LIAs rejected.
    /// </summary>
    internal static readonly Counter<long> LIARejected =
        Meter.CreateCounter<long>(
            "lawful_basis.lia.rejected",
            description: "Number of Legitimate Interest Assessments rejected.");

    // ---- Histogram ----

    /// <summary>
    /// Duration of lawful basis validation pipeline checks in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> ValidationDuration =
        Meter.CreateHistogram<double>(
            "lawful_basis.validation.duration",
            unit: "ms",
            description: "Duration of lawful basis pipeline validations in milliseconds.");

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
    internal static void SetBasis(Activity? activity, GDPR.LawfulBasis basis)
    {
        activity?.SetTag(TagLawfulBasisDeclared, true);
        activity?.SetTag(TagBasis, basis.ToString());
    }
}
