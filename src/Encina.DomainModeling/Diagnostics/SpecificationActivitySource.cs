using System.Diagnostics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Provides helper methods to enrich existing activities with specification pattern context.
/// </summary>
/// <remarks>
/// <para>
/// Specification evaluation occurs within repository operations that already have their own
/// activity spans. Rather than creating separate spans, this class enriches existing activities
/// with specification-related tags.
/// </para>
/// </remarks>
internal static class SpecificationActivitySource
{
    /// <summary>
    /// Enriches an existing activity with specification context.
    /// </summary>
    /// <param name="activity">The activity to enrich (typically from a repository operation).</param>
    /// <param name="specificationName">The specification type name.</param>
    /// <param name="criteriaCount">The optional number of criteria in the specification.</param>
    internal static void EnrichWithSpecification(Activity? activity, string specificationName, int? criteriaCount = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("specification.name", specificationName);

        if (criteriaCount.HasValue)
        {
            activity.SetTag("specification.criteria_count", criteriaCount.Value);
        }
    }
}
