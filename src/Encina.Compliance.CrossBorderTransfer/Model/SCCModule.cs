namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Identifies the Standard Contractual Clauses (SCC) module applicable to
/// an international data transfer under GDPR Article 46(2)(c).
/// </summary>
/// <remarks>
/// <para>
/// The European Commission's Implementing Decision (EU) 2021/914 defines four SCC modules
/// based on the roles of the data exporter and importer. Each module contains specific
/// clauses tailored to the relationship between the parties.
/// </para>
/// <para>
/// Post-Schrems II (CJEU C-311/18), supplementary measures may be required regardless
/// of which module is used, particularly when transferring data to countries without
/// an adequacy decision.
/// </para>
/// </remarks>
public enum SCCModule
{
    /// <summary>
    /// Module 1: Transfer from an EU/EEA controller to a third-country controller.
    /// </summary>
    /// <remarks>
    /// Both parties act as independent controllers. The data importer determines the
    /// purposes and means of processing independently. Requires the most comprehensive
    /// data subject rights provisions.
    /// </remarks>
    ControllerToController = 0,

    /// <summary>
    /// Module 2: Transfer from an EU/EEA controller to a third-country processor.
    /// </summary>
    /// <remarks>
    /// The most commonly used module. The data importer processes personal data on behalf
    /// of the exporter. Includes obligations equivalent to Article 28 (processor requirements)
    /// and requires the processor to assist with data subject rights requests.
    /// </remarks>
    ControllerToProcessor = 1,

    /// <summary>
    /// Module 3: Transfer from an EU/EEA processor to a third-country sub-processor.
    /// </summary>
    /// <remarks>
    /// Applies when a processor engaged by an EU/EEA controller transfers data to a
    /// sub-processor in a third country. Requires prior authorization from the controller
    /// and flow-down of data protection obligations.
    /// </remarks>
    ProcessorToProcessor = 2,

    /// <summary>
    /// Module 4: Transfer from a third-country processor to an EU/EEA controller.
    /// </summary>
    /// <remarks>
    /// Covers the reverse scenario where a processor outside the EU/EEA transfers data
    /// back to a controller in the EU/EEA. Less commonly used but relevant for
    /// return transfers and data repatriation scenarios.
    /// </remarks>
    ProcessorToController = 3
}
