using System.Net.Http.Headers;

namespace Encina.Recoverability;

/// <summary>
/// Turns a <c>Retry-After</c> header (RFC 9110 section 10.2.3) into the delay the caller has to wait.
/// </summary>
/// <remarks>
/// The header is either a number of seconds (<c>delay-seconds</c>) or an HTTP date. A date is turned into a
/// delay with <see cref="TimeProvider.GetUtcNow"/>; a date in the past gives <see cref="TimeSpan.Zero"/>.
/// A missing, unparseable or negative value gives <see langword="null"/>: absent is not zero.
/// </remarks>
public static class RetryAfterParser
{
    /// <summary>
    /// Reads the delay of a parsed <c>Retry-After</c> header.
    /// </summary>
    /// <param name="header">The parsed header, or <see langword="null"/> when the response has none.</param>
    /// <param name="timeProvider">The clock that turns an HTTP date into a delay.</param>
    /// <returns>The delay, or <see langword="null"/> when there is no valid header.</returns>
    public static TimeSpan? Parse(RetryConditionHeaderValue? header, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (header is null)
        {
            return null;
        }

        if (header.Delta is { } delta)
        {
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        if (header.Date is { } date)
        {
            var remaining = date - timeProvider.GetUtcNow();
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        return null;
    }

    /// <summary>
    /// Reads the delay of a raw <c>Retry-After</c> header value.
    /// </summary>
    /// <param name="value">The raw header value, or <see langword="null"/> when the response has none.</param>
    /// <param name="timeProvider">The clock that turns an HTTP date into a delay.</param>
    /// <returns>The delay, or <see langword="null"/> when the value is missing or not a valid header.</returns>
    public static TimeSpan? Parse(string? value, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return RetryConditionHeaderValue.TryParse(value, out var header)
            ? Parse(header, timeProvider)
            : null;
    }
}
