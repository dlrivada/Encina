namespace Encina.Audit.Marten.Projections;

/// <summary>
/// Converts the <see cref="DateTime"/> instants of the operation audit contract to the
/// <see cref="DateTimeOffset"/> the read model stores, so Marten maps the column to
/// <c>timestamptz</c> and Npgsql accepts UTC values for it.
/// </summary>
internal static class AuditTimestamps
{
    /// <summary>
    /// Returns the same instant as a UTC <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">
    /// A UTC or local instant. A value of unspecified kind is read as UTC, because every audit
    /// timestamp is documented as UTC.
    /// </param>
    internal static DateTimeOffset ToUtcOffset(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => new DateTimeOffset(value.ToUniversalTime(), TimeSpan.Zero),
        _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero),
    };
}
