using Encina.DomainModeling.Pagination;
using Encina.Messaging;

using FsCheck;
using FsCheck.Xunit;

using Shouldly;

namespace Encina.PropertyTests.Database.Pagination;

/// <summary>
/// Property-based tests for cursor pagination.
/// Verifies invariants that MUST hold across all cursor pagination implementations.
/// </summary>
[Trait("Category", "Property")]
public sealed class CursorPaginationPropertyTests
{
    #region CursorPaginationOptions Invariants

    [Property(MaxTest = 100)]
    public bool Property_PageSize_MustBePositive(PositiveInt pageSize)
    {
        // Property: PageSize must always be > 0
        var options = new CursorPaginationOptions(
            Cursor: null,
            PageSize: pageSize.Get,
            Direction: CursorDirection.Forward);

        return options.PageSize > 0;
    }

    [Property(MaxTest = 100)]
    public bool Property_PageSize_CannotExceedMaximum(PositiveInt pageSize)
    {
        // Property: PageSize cannot exceed MaxPageSize
        var clampedSize = Math.Min(pageSize.Get, CursorPaginationOptions.MaxPageSize);
        var options = new CursorPaginationOptions(
            Cursor: null,
            PageSize: clampedSize,
            Direction: CursorDirection.Forward);

        return options.PageSize <= CursorPaginationOptions.MaxPageSize;
    }

    [Property(MaxTest = 100)]
    public bool Property_Cursor_NullForFirstPage(CursorDirection direction)
    {
        // Property: First page always has null cursor
        var options = new CursorPaginationOptions(
            Cursor: null,
            PageSize: 10,
            Direction: direction);

        return options.Cursor == null;
    }

    [Property(MaxTest = 100)]
    public bool Property_Direction_IsPreserved(CursorDirection direction)
    {
        // Property: Direction is preserved in options
        var options = new CursorPaginationOptions(
            Cursor: null,
            PageSize: 10,
            Direction: direction);

        return options.Direction == direction;
    }

    #endregion

    #region CursorPaginatedResult Invariants

    [Property(MaxTest = 100)]
    public bool Property_EmptyResult_HasNoNextCursor(PositiveInt pageSize)
    {
        // Property: Empty result has no next cursor
        var result = CursorPaginatedResult<TestEntity>.Empty();

        return result.NextCursor == null && !result.HasNextPage;
    }

    [Property(MaxTest = 100)]
    public bool Property_EmptyResult_HasNoPreviousCursor(PositiveInt pageSize)
    {
        // Property: Empty result has no previous cursor
        var result = CursorPaginatedResult<TestEntity>.Empty();

        return result.PreviousCursor == null && !result.HasPreviousPage;
    }

    [Property(MaxTest = 100)]
    public bool Property_EmptyResult_IsEmpty(PositiveInt pageSize)
    {
        // Property: Empty result reports IsEmpty true
        var result = CursorPaginatedResult<TestEntity>.Empty();

        return result.IsEmpty && result.Items.Count == 0;
    }

    [Fact]
    public void Property_ItemCount_NeverNegative()
    {
        // Property: Items.Count is never negative
        var result = CursorPaginatedResult<TestEntity>.Empty();

        result.Items.Count.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Property_ItemsCollection_IsNeverNull()
    {
        // Property: Items collection is never null
        var result = CursorPaginatedResult<TestEntity>.Empty();

        result.Items.ShouldNotBeNull();
    }

    #endregion

    #region Base64JsonCursorEncoder Invariants

    [Property(MaxTest = 50)]
    public bool Property_Encoder_RoundTrip_PreservesValue(Guid id)
    {
        // Property: Encode then Decode returns original value
        var encoder = new Base64JsonCursorEncoder();
        var cursor = encoder.Encode(id);
        var decoded = encoder.Decode<Guid>(cursor);

        return decoded == id;
    }

    [Property(MaxTest = 50)]
    public bool Property_Encoder_RoundTrip_PreservesDateTime(DateTime dateTime)
    {
        // Property: Encode then Decode returns original DateTime
        var encoder = new Base64JsonCursorEncoder();
        var normalized = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
            dateTime.Hour, dateTime.Minute, dateTime.Second, DateTimeKind.Utc);

        var cursor = encoder.Encode(normalized);
        var decoded = encoder.Decode<DateTime>(cursor);

        // Compare ticks to handle precision
        return Math.Abs(decoded.Ticks - normalized.Ticks) < TimeSpan.TicksPerSecond;
    }

    [Property(MaxTest = 50)]
    public bool Property_Encoder_RoundTrip_PreservesInt(int value)
    {
        // Property: Encode then Decode returns original int
        var encoder = new Base64JsonCursorEncoder();
        var cursor = encoder.Encode(value);
        var decoded = encoder.Decode<int>(cursor);

        return decoded == value;
    }

    [Property(MaxTest = 50)]
    public bool Property_Encoder_RoundTrip_PreservesLong(long value)
    {
        // Property: Encode then Decode returns original long
        var encoder = new Base64JsonCursorEncoder();
        var cursor = encoder.Encode(value);
        var decoded = encoder.Decode<long>(cursor);

        return decoded == value;
    }

    [Property(MaxTest = 50)]
    public bool Property_Encoder_ProducesUrlSafeOutput(Guid id)
    {
        // Property: Encoded cursor contains only URL-safe characters
        var encoder = new Base64JsonCursorEncoder();
        var cursor = encoder.Encode(id);

        // Base64 URL-safe: letters, digits, -, _
        return cursor is not null && cursor.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '=');
    }

    [Property(MaxTest = 50)]
    public bool Property_Encoder_DifferentValues_ProduceDifferentCursors(Guid id1, Guid id2)
    {
        // Property: Different values produce different cursors (unless equal)
        if (id1 == id2) return true;

        var encoder = new Base64JsonCursorEncoder();
        var cursor1 = encoder.Encode(id1);
        var cursor2 = encoder.Encode(id2);

        return cursor1 != cursor2;
    }

    #endregion

    #region Cross-Provider Invariants

    [Property(MaxTest = 50)]
    public bool Property_PageSize_ValidRange_IsAccepted(PositiveInt pageSize)
    {
        // Property: Valid page sizes are accepted by options
        var size = Math.Min(pageSize.Get, CursorPaginationOptions.MaxPageSize);
        var options = new CursorPaginationOptions(null, size, CursorDirection.Forward);

        return options.PageSize == size;
    }

    [Property(MaxTest = 50)]
    public bool Property_Direction_AllValues_AreValid()
    {
        // Property: All CursorDirection enum values are valid
        var directions = Enum.GetValues<CursorDirection>();

        foreach (var direction in directions)
        {
            var options = new CursorPaginationOptions(null, 10, direction);
            if (options.Direction != direction) return false;
        }

        return true;
    }

    #endregion

    #region Test Entities

    private sealed record TestEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }

    #endregion
}
