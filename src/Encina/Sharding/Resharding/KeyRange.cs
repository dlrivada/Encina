namespace Encina.Sharding.Resharding;

/// <summary>
/// Describes a contiguous range on the consistent hash ring.
/// </summary>
/// <param name="RingStart">The inclusive start position on the hash ring.</param>
/// <param name="RingEnd">The exclusive end position on the hash ring.</param>
public sealed record KeyRange(ulong RingStart, ulong RingEnd);
