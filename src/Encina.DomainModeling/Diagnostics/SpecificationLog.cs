using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// High-performance logging methods for specification evaluation using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 1400-1499 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class SpecificationLog
{
    /// <summary>Logs when a specification evaluation begins.</summary>
    [LoggerMessage(
        EventId = 1400,
        Level = LogLevel.Debug,
        Message = "Evaluating specification {SpecificationName} with {CriteriaCount} criteria")]
    public static partial void EvaluatingSpecification(
        ILogger logger,
        string specificationName,
        int criteriaCount);

    /// <summary>Logs when a specification evaluation completes.</summary>
    [LoggerMessage(
        EventId = 1401,
        Level = LogLevel.Debug,
        Message = "Specification {SpecificationName} evaluation completed")]
    public static partial void SpecificationEvaluationCompleted(
        ILogger logger,
        string specificationName);
}
