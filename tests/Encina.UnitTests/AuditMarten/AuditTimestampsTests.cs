using Encina.Audit.Marten.Projections;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

public sealed class AuditTimestampsTests
{
    private static readonly DateTime Instant = new(2026, 10, 9, 8, 30, 0);

    [Fact]
    public void ToUtcOffset_UtcKind_KeepsTheInstantWithZeroOffset()
    {
        var result = DateTime.SpecifyKind(Instant, DateTimeKind.Utc).ToUtcOffset();

        result.Offset.ShouldBe(TimeSpan.Zero);
        result.UtcDateTime.ShouldBe(DateTime.SpecifyKind(Instant, DateTimeKind.Utc));
    }

    [Fact]
    public void ToUtcOffset_UnspecifiedKind_IsReadAsUtc()
    {
        var result = DateTime.SpecifyKind(Instant, DateTimeKind.Unspecified).ToUtcOffset();

        result.Offset.ShouldBe(TimeSpan.Zero);
        result.UtcDateTime.ShouldBe(DateTime.SpecifyKind(Instant, DateTimeKind.Utc));
    }

    [Fact]
    public void ToUtcOffset_LocalKind_ConvertsToTheSameUtcInstant()
    {
        var local = DateTime.SpecifyKind(Instant, DateTimeKind.Local);

        var result = local.ToUtcOffset();

        result.Offset.ShouldBe(TimeSpan.Zero);
        result.UtcDateTime.ShouldBe(local.ToUniversalTime());
    }
}
