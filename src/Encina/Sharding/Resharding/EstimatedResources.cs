namespace Encina.Sharding.Resharding;

/// <summary>
/// Estimated resource requirements for a resharding operation.
/// </summary>
/// <param name="TotalRows">Total estimated rows to migrate across all steps.</param>
/// <param name="TotalBytes">Total estimated bytes to migrate across all steps.</param>
/// <param name="EstimatedDuration">Estimated wall-clock duration for the full resharding workflow.</param>
public sealed record EstimatedResources(
    long TotalRows,
    long TotalBytes,
    TimeSpan EstimatedDuration);
