namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Filters and paging for the review queue (elevations not yet reviewed, oldest first).
/// </summary>
public sealed record BreakTheGlassReviewQuery
{
    /// <summary>The default page size.</summary>
    public const int DefaultPageSize = 50;

    /// <summary>The largest page size a store honours.</summary>
    public const int MaxPageSize = 500;

    /// <summary>Gets the scope to keep, or <see langword="null"/> for every scope.</summary>
    public string? Scope { get; init; }

    /// <summary>Gets the status to keep, or <see langword="null"/> for every status.</summary>
    public BreakTheGlassStatus? Status { get; init; }

    /// <summary>Gets the inclusive lower bound of the opening time, or <see langword="null"/>.</summary>
    public DateTimeOffset? OpenedFromUtc { get; init; }

    /// <summary>Gets the exclusive upper bound of the opening time, or <see langword="null"/>.</summary>
    public DateTimeOffset? OpenedToUtc { get; init; }

    /// <summary>Gets the 1-based page number.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Gets the page size (1 to <see cref="MaxPageSize"/>).</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
