namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Derives the <see cref="ErrorCategory"/> of an aggregate scatter-gather failure (ADR-062, #3723).
/// </summary>
/// <remarks>
/// <c>scattergather.quorum_not_reached</c> and <c>scattergather.all_scatters_failed</c> hide the errors of the
/// scatter handlers that failed. The aggregate takes the category those errors share, so a fan-out whose handlers
/// all answered <see cref="ErrorCategory.NotFound"/> is not reported as a server fault. Mixed categories, or no
/// failed scatter to read, answer <see cref="ErrorCategory.Internal"/>; an operation cut by its own time budget
/// answers <see cref="ErrorCategory.Timeout"/>.
/// </remarks>
internal static class ScatterGatherFailureCategory
{
    /// <summary>
    /// Gets the category of the aggregate failure of a fan-out.
    /// </summary>
    /// <typeparam name="TResponse">The type of response of the scatter handlers.</typeparam>
    /// <param name="results">The results of the scatter handlers.</param>
    /// <param name="timedOut">Whether the operation's time budget expired.</param>
    /// <returns>The category shared by the failed scatters, or <see cref="ErrorCategory.Internal"/>.</returns>
    internal static ErrorCategory Derive<TResponse>(
        IEnumerable<ScatterExecutionResult<TResponse>> results,
        bool timedOut)
    {
        if (timedOut)
        {
            return ErrorCategory.Timeout;
        }

        return SharedCategory(results) ?? ErrorCategory.Internal;
    }

    // The one category all failed scatters share; null when none failed, Internal when they disagree.
    private static ErrorCategory? SharedCategory<TResponse>(IEnumerable<ScatterExecutionResult<TResponse>> results)
    {
        ErrorCategory? shared = null;
        foreach (var result in results)
        {
            var category = FailureCategory(result);
            if (category is null)
            {
                continue;
            }

            if (shared is not null && shared != category)
            {
                return ErrorCategory.Internal;
            }

            shared = category;
        }

        return shared;
    }

    private static ErrorCategory? FailureCategory<TResponse>(ScatterExecutionResult<TResponse> result)
        => result.Result.Case is EncinaError error ? error.GetCategory() : null;
}
