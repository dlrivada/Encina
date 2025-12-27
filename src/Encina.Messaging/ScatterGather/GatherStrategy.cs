namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Defines the strategy for gathering responses from scatter operations.
/// </summary>
public enum GatherStrategy
{
    /// <summary>
    /// Wait for all scatter handlers to complete before gathering.
    /// All handlers must succeed for the gather to proceed.
    /// </summary>
    WaitForAll = 0,

    /// <summary>
    /// Return as soon as the first scatter handler completes successfully.
    /// Other handlers are cancelled.
    /// </summary>
    WaitForFirst = 1,

    /// <summary>
    /// Wait for a specified number of handlers to complete successfully (quorum).
    /// Remaining handlers are cancelled once quorum is reached.
    /// </summary>
    WaitForQuorum = 2,

    /// <summary>
    /// Wait for all scatter handlers to complete, allowing partial failures.
    /// The gather receives all available results, including failures.
    /// </summary>
    WaitForAllAllowPartial = 3
}
