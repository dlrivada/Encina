namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Input rules every store must see applied identically, enforced once before any I/O so that the
/// ten providers cannot diverge on them.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>
/// An instant must have <see cref="DateTimeKind.Utc"/>. It is rejected, not converted: a
/// <see cref="DateTimeKind.Unspecified"/> value has no defined instant, so <c>ToUniversalTime()</c> would guess
/// the server's local zone and the providers would disagree on what they stored.
/// </description></item>
/// <item><description>
/// An identity value (source message id, source pattern, request type, tenant id) must not start or end
/// with white space: SQL Server and MySQL ignore trailing spaces when they compare strings (PAD SPACE),
/// PostgreSQL and MongoDB do not, so the same two values would be one capture on some providers and two on others.
/// </description></item>
/// </list>
/// </remarks>
internal static class DeadLetterInputs
{
    /// <summary>Throws when <paramref name="value"/> is not a UTC instant.</summary>
    public static void RequireUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"The value must have DateTimeKind.Utc (was {value.Kind}); the dead letter queue stores UTC instants and does not guess a zone.",
                name);
        }
    }

    /// <summary>Throws when <paramref name="value"/> is set and not a UTC instant.</summary>
    public static void RequireUtc(DateTime? value, string name)
    {
        if (value.HasValue)
        {
            RequireUtc(value.Value, name);
        }
    }

    /// <summary>
    /// Returns a timestamp read from an Encina source row (an <c>*AtUtc</c> column that Encina wrote from
    /// <see cref="TimeProvider"/> as UTC) as a UTC instant. Unlike caller input, its zone is known: a provider
    /// that drops <see cref="DateTime.Kind"/> on read returns <see cref="DateTimeKind.Unspecified"/> for a UTC
    /// value, which is relabelled; a <see cref="DateTimeKind.Local"/> value is converted.
    /// </summary>
    // crap-exempt: single-question switch — maps the DateTimeKind of a stored UTC column to a UTC instant.
    public static DateTime AsUtc(DateTime storedUtc) => storedUtc.Kind switch
    {
        DateTimeKind.Utc => storedUtc,
        DateTimeKind.Local => storedUtc.ToUniversalTime(),
        _ => DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc)
    };

    /// <summary>Applies the instant and identity rules to every field of <paramref name="filter"/> that has one.</summary>
    public static void ValidateFilter(DeadLetterFilter filter)
    {
        RequireTrimmed(filter.SourcePattern, nameof(filter.SourcePattern));
        RequireTrimmed(filter.RequestType, nameof(filter.RequestType));
        RequireTrimmed(filter.SourceMessageId, nameof(filter.SourceMessageId));
        RequireTrimmed(filter.TenantId, nameof(filter.TenantId));
        RequireUtc(filter.DeadLetteredAfterUtc, nameof(filter.DeadLetteredAfterUtc));
        RequireUtc(filter.DeadLetteredBeforeUtc, nameof(filter.DeadLetteredBeforeUtc));
        RequireUtc(filter.ExpiresAtOrBeforeUtc, nameof(filter.ExpiresAtOrBeforeUtc));
    }

    /// <summary>Throws when <paramref name="value"/> starts or ends with white space.</summary>
    public static void RequireTrimmed(string? value, string name)
    {
        if (!string.IsNullOrEmpty(value) && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1])))
        {
            throw new ArgumentException(
                "The value must not start or end with white space; providers compare trailing spaces differently.",
                name);
        }
    }
}
