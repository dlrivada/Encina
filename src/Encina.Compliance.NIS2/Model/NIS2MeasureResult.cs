namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Result of evaluating a single NIS2 cybersecurity measure (Art. 21(2)).
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="NIS2Measure"/> is evaluated by an <c>INIS2MeasureEvaluator</c>
/// implementation, which returns a <see cref="NIS2MeasureResult"/> indicating whether
/// the measure is satisfied and providing details and actionable recommendations.
/// </para>
/// <para>
/// These results are aggregated by the <c>INIS2ComplianceValidator</c> into a
/// comprehensive <see cref="NIS2ComplianceResult"/>.
/// </para>
/// </remarks>
public sealed record NIS2MeasureResult
{
    /// <summary>
    /// The measure that was evaluated.
    /// </summary>
    public required NIS2Measure Measure { get; init; }

    /// <summary>
    /// Whether the measure is currently satisfied based on the evaluation criteria.
    /// </summary>
    public required bool IsSatisfied { get; init; }

    /// <summary>
    /// Human-readable details explaining the evaluation outcome.
    /// </summary>
    /// <remarks>
    /// When <see cref="IsSatisfied"/> is <c>true</c>, this describes how the measure
    /// is being met. When <c>false</c>, this explains what is missing or non-compliant.
    /// </remarks>
    public required string Details { get; init; }

    /// <summary>
    /// Actionable recommendations for achieving or improving compliance with this measure.
    /// </summary>
    /// <remarks>
    /// Empty when the measure is fully satisfied with no further improvements needed.
    /// When the measure is not satisfied, contains specific steps the entity should take.
    /// </remarks>
    public IReadOnlyList<string> Recommendations { get; init; } = [];

    /// <summary>
    /// Creates a result indicating the measure is satisfied.
    /// </summary>
    /// <param name="measure">The evaluated measure.</param>
    /// <param name="details">Details explaining how the measure is met.</param>
    /// <returns>A <see cref="NIS2MeasureResult"/> with <see cref="IsSatisfied"/> set to <c>true</c>.</returns>
    public static NIS2MeasureResult Satisfied(NIS2Measure measure, string details) =>
        new()
        {
            Measure = measure,
            IsSatisfied = true,
            Details = details
        };

    /// <summary>
    /// Creates a result indicating the measure is not satisfied.
    /// </summary>
    /// <param name="measure">The evaluated measure.</param>
    /// <param name="details">Details explaining what is missing or non-compliant.</param>
    /// <param name="recommendations">Actionable recommendations for achieving compliance.</param>
    /// <returns>A <see cref="NIS2MeasureResult"/> with <see cref="IsSatisfied"/> set to <c>false</c>.</returns>
    public static NIS2MeasureResult NotSatisfied(
        NIS2Measure measure,
        string details,
        IReadOnlyList<string> recommendations) =>
        new()
        {
            Measure = measure,
            IsSatisfied = false,
            Details = details,
            Recommendations = recommendations
        };
}
