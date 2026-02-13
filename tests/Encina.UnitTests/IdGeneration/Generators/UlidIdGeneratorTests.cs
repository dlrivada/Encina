using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.IdGeneration.Generators;

/// <summary>
/// Unit tests for <see cref="UlidIdGenerator"/>.
/// </summary>
public sealed class UlidIdGeneratorTests
{
    // ────────────────────────────────────────────────────────────
    //  StrategyName
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StrategyName_ReturnsULID()
    {
        var generator = new UlidIdGenerator();
        generator.StrategyName.ShouldBe("ULID");
    }

    // ────────────────────────────────────────────────────────────
    //  Generate
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_ReturnsRight_WithNonEmptyId()
    {
        var generator = new UlidIdGenerator();

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
        result.Match(id => id.IsEmpty.ShouldBeFalse(), _ => { });
    }

    [Fact]
    public void Generate_BatchOf1000_ProducesUniqueIds()
    {
        var generator = new UlidIdGenerator();
        var ids = new System.Collections.Generic.HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            var result = generator.Generate();
            result.IsRight.ShouldBeTrue();
            result.Match(id => ids.Add(id.ToString()).ShouldBeTrue(), _ => { });
        }

        ids.Count.ShouldBe(1000);
    }

    [Fact]
    public void Generate_ProducesValidCrockfordBase32String()
    {
        var generator = new UlidIdGenerator();

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                var str = id.ToString();
                str.Length.ShouldBe(UlidId.StringLength);
                // Crockford Base32 uses uppercase alphanumeric minus I, L, O, U
                str.ShouldMatch("^[0-9A-HJKMNP-TV-Z]{26}$");
            },
            _ => { });
    }

    [Fact]
    public void Generate_EncodesTimestamp_FromTimeProvider()
    {
        var fixedTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var generator = new UlidIdGenerator(timeProvider);

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                var extractedTimestamp = id.GetTimestamp();
                // Timestamps should match at millisecond precision
                extractedTimestamp.ToUnixTimeMilliseconds()
                    .ShouldBe(fixedTime.ToUnixTimeMilliseconds());
            },
            _ => { });
    }

    [Fact]
    public void Generate_WithAdvancingTime_ProducesSortableIds()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var generator = new UlidIdGenerator(timeProvider);

        var result1 = generator.Generate();
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        var result2 = generator.Generate();

        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        var id1 = result1.Match(id => id, _ => default);
        var id2 = result2.Match(id => id, _ => default);

        // ULIDs with later timestamps should sort after earlier ones
        id2.CompareTo(id1).ShouldBeGreaterThan(0);
    }
}
