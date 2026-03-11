using Encina.Compliance.DPIA.Model;

using LanguageExt;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Provides DPIA templates for structured, repeatable assessments.
/// </summary>
/// <remarks>
/// <para>
/// Templates provide a standardized framework for conducting DPIAs, ensuring that assessments
/// are comprehensive, consistent, and cover all elements required by GDPR Article 35(7).
/// Different processing types may require different templates (e.g., a template for
/// automated decision-making vs. one for large-scale processing of special categories).
/// </para>
/// <para>
/// Per Article 35(7), a DPIA shall contain at least:
/// </para>
/// <list type="bullet">
/// <item><description>(a) A systematic description of the envisaged processing operations and purposes.</description></item>
/// <item><description>(b) An assessment of the necessity and proportionality of the processing.</description></item>
/// <item><description>(c) An assessment of the risks to the rights and freedoms of data subjects.</description></item>
/// <item><description>(d) The measures envisaged to address the risks.</description></item>
/// </list>
/// <para>
/// Templates are matched to processing contexts via <see cref="DPIATemplate.ProcessingType"/>
/// and <see cref="DPIAContext.ProcessingType"/>. When no matching template is found,
/// the assessment engine proceeds without template guidance.
/// </para>
/// <para>
/// Implementations may load templates from configuration files, databases, or code.
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Retrieve a template for automated decision-making assessments
/// var result = await provider.GetTemplateAsync("AutomatedDecisionMaking", ct);
/// result.Match(
///     Right: template => Console.WriteLine($"Template '{template.Name}' has {template.Sections.Count} sections."),
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
///
/// // List all available templates
/// var allTemplates = await provider.GetAllTemplatesAsync(ct);
/// </code>
/// </example>
public interface IDPIATemplateProvider
{
    /// <summary>
    /// Retrieves a DPIA template for the specified processing type.
    /// </summary>
    /// <param name="processingType">
    /// The type of processing to retrieve a template for (e.g., "AutomatedDecisionMaking",
    /// "LargeScaleProcessing"). Matches against <see cref="DPIATemplate.ProcessingType"/>.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="DPIATemplate"/> matching the processing type; or an <see cref="EncinaError"/>
    /// if no template is found or the retrieval failed.
    /// </returns>
    /// <remarks>
    /// Returns a <see cref="DPIAErrors.TemplateNotFoundCode"/> error when no template
    /// matches the specified processing type. This is expected behavior — not all
    /// processing types have pre-defined templates.
    /// </remarks>
    ValueTask<Either<EncinaError, DPIATemplate>> GetTemplateAsync(
        string processingType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all available DPIA templates.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all registered <see cref="DPIATemplate"/> instances;
    /// or an <see cref="EncinaError"/> if the retrieval failed.
    /// Returns an empty list if no templates are configured.
    /// </returns>
    /// <remarks>
    /// Primarily used for administrative interfaces and template management.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DPIATemplate>>> GetAllTemplatesAsync(
        CancellationToken cancellationToken = default);
}
