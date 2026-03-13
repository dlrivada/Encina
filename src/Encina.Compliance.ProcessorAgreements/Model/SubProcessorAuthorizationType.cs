namespace Encina.Compliance.ProcessorAgreements.Model;

/// <summary>
/// The type of written authorization granted by the controller for sub-processor engagement.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 28(2) states: "The processor shall not engage another processor without prior
/// specific or general written authorisation of the controller." This enum distinguishes between
/// the two authorization models.
/// </para>
/// <para>
/// Under <see cref="General"/> authorization, the processor must inform the controller of any
/// intended changes concerning the addition or replacement of sub-processors, thereby giving the
/// controller the opportunity to object (Article 28(2), second subparagraph).
/// </para>
/// </remarks>
public enum SubProcessorAuthorizationType
{
    /// <summary>
    /// The controller has granted specific authorization for each individual sub-processor.
    /// </summary>
    /// <remarks>
    /// Each sub-processor must be explicitly approved before engagement. This provides
    /// the controller with maximum control over the processing chain.
    /// </remarks>
    Specific = 0,

    /// <summary>
    /// The controller has granted general authorization for sub-processor engagement.
    /// </summary>
    /// <remarks>
    /// The processor must inform the controller of any intended changes concerning
    /// the addition or replacement of sub-processors, giving the controller the
    /// opportunity to object to such changes per Article 28(2).
    /// </remarks>
    General = 1
}
