namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Aggregated result of evaluating all 10 mandatory NIS2 cybersecurity measures (Art. 21(2)).
/// </summary>
/// <remarks>
/// <para>
/// Produced by the <c>INIS2ComplianceValidator</c>, this result provides a comprehensive
/// view of the entity's compliance posture against the NIS2 Directive requirements.
/// </para>
/// <para>
/// Per Art. 21(1), essential and important entities must take "appropriate and proportionate"
/// measures. Full compliance requires all 10 measures in Art. 21(2) to be satisfied, though
/// the depth and scope of each measure should be proportionate to the entity's size, risk
/// exposure, and the criticality of its services.
/// </para>
/// </remarks>
public sealed record NIS2ComplianceResult
{
    /// <summary>
    /// Whether all 10 mandatory measures are satisfied.
    /// </summary>
    /// <remarks>
    /// <c>true</c> only when every measure in <see cref="MeasureResults"/> has
    /// <see cref="NIS2MeasureResult.IsSatisfied"/> set to <c>true</c>.
    /// </remarks>
    public required bool IsCompliant { get; init; }

    /// <summary>
    /// The entity type classification under which the compliance was evaluated.
    /// </summary>
    public required NIS2EntityType EntityType { get; init; }

    /// <summary>
    /// The sector under which the compliance was evaluated.
    /// </summary>
    public required NIS2Sector Sector { get; init; }

    /// <summary>
    /// Individual results for each of the 10 mandatory measures.
    /// </summary>
    public required IReadOnlyList<NIS2MeasureResult> MeasureResults { get; init; }

    /// <summary>
    /// Measures that are not currently satisfied.
    /// </summary>
    public required IReadOnlyList<NIS2Measure> MissingMeasures { get; init; }

    /// <summary>
    /// Timestamp when the compliance evaluation was performed (UTC).
    /// </summary>
    public required DateTimeOffset EvaluatedAtUtc { get; init; }

    /// <summary>
    /// Percentage of measures satisfied (0–100).
    /// </summary>
    /// <remarks>
    /// Computed as <c>SatisfiedCount / TotalCount * 100</c>.
    /// A value of 100 indicates full compliance (<see cref="IsCompliant"/> is <c>true</c>).
    /// </remarks>
    public int CompliancePercentage =>
        MeasureResults.Count == 0
            ? 0
            : (int)(MeasureResults.Count(r => r.IsSatisfied) * 100L / MeasureResults.Count);

    /// <summary>
    /// Number of measures that are not satisfied.
    /// </summary>
    public int MissingCount => MissingMeasures.Count;

    /// <summary>
    /// Creates a compliance result from individual measure evaluation results.
    /// </summary>
    /// <param name="entityType">The entity type classification.</param>
    /// <param name="sector">The sector classification.</param>
    /// <param name="measureResults">Results from evaluating each measure.</param>
    /// <param name="evaluatedAtUtc">Timestamp of the evaluation.</param>
    /// <returns>A new <see cref="NIS2ComplianceResult"/> with computed compliance status.</returns>
    public static NIS2ComplianceResult Create(
        NIS2EntityType entityType,
        NIS2Sector sector,
        IReadOnlyList<NIS2MeasureResult> measureResults,
        DateTimeOffset evaluatedAtUtc)
    {
        var missing = measureResults
            .Where(r => !r.IsSatisfied)
            .Select(r => r.Measure)
            .ToList();

        return new NIS2ComplianceResult
        {
            IsCompliant = missing.Count == 0,
            EntityType = entityType,
            Sector = sector,
            MeasureResults = measureResults,
            MissingMeasures = missing,
            EvaluatedAtUtc = evaluatedAtUtc
        };
    }
}
