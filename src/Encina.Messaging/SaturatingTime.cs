namespace Encina.Messaging;

/// <summary>
/// Time arithmetic that saturates instead of throwing.
/// </summary>
internal static class SaturatingTime
{
    /// <summary>
    /// Adds <paramref name="delay"/> to <paramref name="nowUtc"/>, saturating at <see cref="DateTime.MaxValue"/>
    /// (UTC) instead of throwing when a very large delay (for example a far-future Retry-After) would overflow.
    /// </summary>
    internal static DateTime AddSaturating(DateTime nowUtc, TimeSpan delay)
    {
        var maxUtc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        return delay >= maxUtc - nowUtc ? maxUtc : nowUtc.Add(delay);
    }
}
