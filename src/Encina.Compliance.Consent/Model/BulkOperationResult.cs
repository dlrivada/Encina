namespace Encina.Compliance.Consent;

/// <summary>
/// Result of a bulk consent operation, tracking individual successes and failures.
/// </summary>
/// <remarks>
/// <para>
/// Bulk operations process multiple consent records in a single call. Each individual
/// record may succeed or fail independently. This result captures the overall outcome
/// including counts and detailed error information for failed items.
/// </para>
/// <para>
/// Use <see cref="AllSucceeded"/> to quickly check if every item in the batch succeeded.
/// When <see cref="HasFailures"/> is <c>true</c>, inspect <see cref="Errors"/> for
/// per-item failure details.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await store.BulkRecordConsentAsync(consents, cancellationToken);
/// result.Match(
///     Right: bulk =>
///     {
///         if (bulk.AllSucceeded)
///             logger.LogInformation("All {Count} consents recorded", bulk.SuccessCount);
///         else
///             logger.LogWarning("{Failures} of {Total} consents failed",
///                 bulk.FailureCount, bulk.TotalCount);
///     },
///     Left: error => logger.LogError("Bulk operation failed: {Error}", error.Message));
/// </code>
/// </example>
public sealed record BulkOperationResult
{
    /// <summary>
    /// Gets the number of items that were processed successfully.
    /// </summary>
    public required int SuccessCount { get; init; }

    /// <summary>
    /// Gets the number of items that failed to process.
    /// </summary>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Gets the detailed errors for each failed item.
    /// </summary>
    /// <remarks>
    /// Each entry contains an identifier for the failed item and the <see cref="EncinaError"/>
    /// describing what went wrong. The list count equals <see cref="FailureCount"/>.
    /// </remarks>
    public required IReadOnlyList<BulkOperationError> Errors { get; init; }

    /// <summary>
    /// Gets whether all items in the batch were processed successfully.
    /// </summary>
    public bool AllSucceeded => FailureCount == 0;

    /// <summary>
    /// Gets whether any items in the batch failed to process.
    /// </summary>
    public bool HasFailures => FailureCount > 0;

    /// <summary>
    /// Gets the total number of items in the batch (successes + failures).
    /// </summary>
    public int TotalCount => SuccessCount + FailureCount;

    /// <summary>
    /// Creates a result indicating all items succeeded.
    /// </summary>
    /// <param name="count">The number of items that succeeded.</param>
    /// <returns>A <see cref="BulkOperationResult"/> with zero failures.</returns>
    public static BulkOperationResult Success(int count) => new()
    {
        SuccessCount = count,
        FailureCount = 0,
        Errors = []
    };

    /// <summary>
    /// Creates a result with both successes and failures.
    /// </summary>
    /// <param name="successCount">The number of items that succeeded.</param>
    /// <param name="errors">The detailed errors for each failed item.</param>
    /// <returns>A <see cref="BulkOperationResult"/> with partial success.</returns>
    public static BulkOperationResult Partial(int successCount, IReadOnlyList<BulkOperationError> errors) => new()
    {
        SuccessCount = successCount,
        FailureCount = errors.Count,
        Errors = errors
    };
}

/// <summary>
/// Represents an individual error within a bulk consent operation.
/// </summary>
/// <param name="Identifier">
/// A human-readable identifier for the failed item (e.g., <c>"user-123:marketing"</c>
/// for a subject-purpose pair).
/// </param>
/// <param name="Error">The <see cref="EncinaError"/> describing the failure.</param>
public sealed record BulkOperationError(string Identifier, EncinaError Error);
