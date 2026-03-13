namespace Encina.Compliance.ProcessorAgreements.Model;

/// <summary>
/// Lifecycle status of a Data Processing Agreement between a controller and a processor.
/// </summary>
/// <remarks>
/// <para>
/// Tracks the agreement through its contractual lifecycle as required by GDPR Article 28(3),
/// which mandates that "processing by a processor shall be governed by a contract or other
/// legal act [...] that is binding on the processor."
/// </para>
/// <para>
/// The typical lifecycle flow is:
/// <c>Active → PendingRenewal → Active</c> (renewal), or
/// <c>Active → Expired</c> (lapsed), or
/// <c>Active → Terminated</c> (explicit termination).
/// </para>
/// </remarks>
public enum DPAStatus
{
    /// <summary>
    /// The agreement is currently active and valid for processing operations.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The agreement has passed its expiration date without renewal.
    /// </summary>
    /// <remarks>
    /// Processing operations relying on this agreement should be blocked or warned
    /// by the <c>ProcessorValidationPipelineBehavior</c> until a new agreement is signed.
    /// </remarks>
    Expired = 1,

    /// <summary>
    /// The agreement is approaching expiration and requires renewal.
    /// </summary>
    /// <remarks>
    /// This status triggers <c>DPAExpiringNotification</c>
    /// to alert compliance teams about upcoming renewal deadlines.
    /// </remarks>
    PendingRenewal = 2,

    /// <summary>
    /// The agreement has been explicitly terminated by one of the parties.
    /// </summary>
    /// <remarks>
    /// Per Article 28(3)(g), upon termination the processor must delete or return all personal data
    /// and certify that it has done so, unless Union or Member State law requires storage.
    /// </remarks>
    Terminated = 3
}
