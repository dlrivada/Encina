using Hangfire;
using Hangfire.Common;

namespace Encina.Hangfire;

/// <summary>
/// Builds a Hangfire retry policy that retries transient Encina job failures and does not retry
/// permanent ones.
/// </summary>
/// <remarks>
/// <para>
/// Hangfire's <see cref="AutomaticRetryAttribute"/> is sealed, so Encina does not subclass it. Instead,
/// <see cref="Create(int)"/> returns an <see cref="AutomaticRetryAttribute"/> whose
/// <see cref="AutomaticRetryAttribute.ExceptOn"/> contains <see cref="EncinaJobPermanentFailureException"/>:
/// a permanent failure moves the job straight to the Failed state, while
/// <see cref="EncinaJobFailedException"/> and any other exception are retried up to
/// <see cref="AutomaticRetryAttribute.Attempts"/> times.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Replace Hangfire's global retry filter (10 attempts, retries everything)
/// GlobalJobFilters.Filters.UseEncinaAutomaticRetry(attempts: 5);
///
/// // Or add the filter yourself, e.g. to a specific job type
/// var retry = EncinaAutomaticRetry.Create(attempts: 3);
/// </code>
/// </example>
public static class EncinaAutomaticRetry
{
    /// <summary>
    /// The default number of retry attempts (10), the same as Hangfire's
    /// <see cref="AutomaticRetryAttribute.DefaultRetryAttempts"/>.
    /// </summary>
    public const int DefaultAttempts = 10;

    /// <summary>
    /// Creates an <see cref="AutomaticRetryAttribute"/> that retries up to <paramref name="attempts"/>
    /// times, except for <see cref="EncinaJobPermanentFailureException"/>, and leaves the job in the
    /// Failed state when the attempts are exhausted.
    /// </summary>
    /// <param name="attempts">The maximum number of automatic retry attempts.</param>
    /// <returns>The configured retry filter.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attempts"/> is negative.</exception>
    public static AutomaticRetryAttribute Create(int attempts = DefaultAttempts)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attempts);

        return new AutomaticRetryAttribute
        {
            Attempts = attempts,
            ExceptOn = [typeof(EncinaJobPermanentFailureException)],
            OnAttemptsExceeded = AttemptsExceededAction.Fail
        };
    }

    /// <summary>
    /// Replaces every <see cref="AutomaticRetryAttribute"/> in <paramref name="filters"/> with the
    /// filter returned by <see cref="Create(int)"/>.
    /// </summary>
    /// <param name="filters">The filter collection, typically <see cref="GlobalJobFilters.Filters"/>.</param>
    /// <param name="attempts">The maximum number of automatic retry attempts.</param>
    /// <returns>The same filter collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filters"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attempts"/> is negative.</exception>
    public static JobFilterCollection UseEncinaAutomaticRetry(
        this JobFilterCollection filters,
        int attempts = DefaultAttempts)
    {
        ArgumentNullException.ThrowIfNull(filters);

        var retryFilter = Create(attempts);

        var existing = filters
            .Select(filter => filter.Instance)
            .OfType<AutomaticRetryAttribute>()
            .ToList();

        foreach (var instance in existing)
        {
            filters.Remove(instance);
        }

        filters.Add(retryFilter);
        return filters;
    }
}
