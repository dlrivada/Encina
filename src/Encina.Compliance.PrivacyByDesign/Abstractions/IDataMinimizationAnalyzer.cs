using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Analyzes request types for data minimization compliance using reflection-based
/// attribute inspection.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides the low-level analysis engine that inspects request type properties
/// for <see cref="NotStrictlyNecessaryAttribute"/>, <see cref="PurposeLimitationAttribute"/>,
/// and <see cref="PrivacyDefaultAttribute"/> decorations, then evaluates the current request
/// instance against those declarations.
/// </para>
/// <para>
/// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
/// organisational measures for ensuring that, by default, only personal data which are
/// necessary for each specific purpose of the processing are processed."
/// </para>
/// <para>
/// Implementations should cache reflection results per request type for performance.
/// The <c>DataMinimizationPipelineBehavior</c> caches attribute presence at the generic
/// type level, and this analyzer should similarly avoid repeated reflection.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Analyze a request instance
/// var report = await analyzer.AnalyzeAsync(createOrderCommand, cancellationToken);
/// report.Match(
///     Right: r => Console.WriteLine($"Score: {r.MinimizationScore:P0}"),
///     Left: error => Console.WriteLine($"Analysis failed: {error.Message}"));
///
/// // Inspect defaults
/// var defaults = await analyzer.InspectDefaultsAsync(updatePreferencesCommand, cancellationToken);
/// </code>
/// </example>
public interface IDataMinimizationAnalyzer
{
    /// <summary>
    /// Analyzes a request instance for data minimization compliance, producing a detailed
    /// <see cref="MinimizationReport"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request to analyze.</typeparam>
    /// <param name="request">The request instance to analyze.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="MinimizationReport"/> containing necessary/unnecessary field breakdowns,
    /// a minimization score (0.0–1.0), and actionable recommendations;
    /// or an <see cref="EncinaError"/> if the analysis infrastructure fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The analysis inspects each property of <typeparamref name="TRequest"/> for
    /// <see cref="NotStrictlyNecessaryAttribute"/>. Properties without the attribute are
    /// classified as necessary; properties with it are classified as unnecessary.
    /// The minimization score is calculated as
    /// <c>necessary / (necessary + unnecessary)</c>.
    /// </para>
    /// <para>
    /// When an unnecessary field has a non-default value in the request instance, a
    /// recommendation is generated suggesting its removal or conversion to optional.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, MinimizationReport>> AnalyzeAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;

    /// <summary>
    /// Inspects the privacy default declarations on a request instance and checks whether
    /// each field's actual value matches its declared default.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request to inspect.</typeparam>
    /// <param name="request">The request instance to inspect.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="DefaultPrivacyFieldInfo"/> for every field decorated
    /// with <see cref="PrivacyDefaultAttribute"/>, indicating whether the actual value
    /// matches the declared privacy-respecting default;
    /// or an <see cref="EncinaError"/> if the inspection infrastructure fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 25(2), personal data should not be made accessible by default.
    /// Fields whose actual values deviate from their declared defaults represent explicit
    /// opt-ins to more permissive data processing.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>> InspectDefaultsAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;
}
