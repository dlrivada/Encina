using Encina.IdGeneration.Generators;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.IdGeneration.Generators;

/// <summary>
/// Unit tests for <see cref="UuidV7IdGenerator"/>.
/// </summary>
public sealed class UuidV7IdGeneratorTests
{
    // ────────────────────────────────────────────────────────────
    //  StrategyName
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StrategyName_ReturnsUUIDv7()
    {
        var generator = new UuidV7IdGenerator();
        generator.StrategyName.ShouldBe("UUIDv7");
    }

    // ────────────────────────────────────────────────────────────
    //  Generate
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_ReturnsRight_WithNonEmptyId()
    {
        var generator = new UuidV7IdGenerator();

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
        result.Match(id => id.IsEmpty.ShouldBeFalse(), _ => { });
    }

    [Fact]
    public void Generate_BatchOf1000_ProducesUniqueIds()
    {
        var generator = new UuidV7IdGenerator();
        var ids = new System.Collections.Generic.HashSet<Guid>();

        for (var i = 0; i < 1000; i++)
        {
            var result = generator.Generate();
            result.IsRight.ShouldBeTrue();
            result.Match(id => ids.Add(id.Value).ShouldBeTrue(), _ => { });
        }

        ids.Count.ShouldBe(1000);
    }

    [Fact]
    public void Generate_ProducesRFC9562CompliantUuid()
    {
        var generator = new UuidV7IdGenerator();

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                var guid = id.Value;
                Span<byte> bytes = stackalloc byte[16];
                guid.TryWriteBytes(bytes, bigEndian: true, out _);

                // Version nibble (byte 6, top 4 bits) should be 0x70 (version 7)
                (bytes[6] & 0xF0).ShouldBe(0x70);

                // Variant bits (byte 8, top 2 bits) should be 0x80 (RFC 4122)
                (bytes[8] & 0xC0).ShouldBe(0x80);
            },
            _ => { });
    }

    [Fact]
    public void Generate_EncodesTimestamp_FromTimeProvider()
    {
        var fixedTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var generator = new UuidV7IdGenerator(timeProvider);

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                var extractedTimestamp = id.GetTimestamp();
                extractedTimestamp.ToUnixTimeMilliseconds()
                    .ShouldBe(fixedTime.ToUnixTimeMilliseconds());
            },
            _ => { });
    }

    [Fact]
    public void Generate_WithAdvancingTime_ProducesSortableIds()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var generator = new UuidV7IdGenerator(timeProvider);

        var result1 = generator.Generate();
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        var result2 = generator.Generate();

        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        var id1 = result1.Match(id => id, _ => default);
        var id2 = result2.Match(id => id, _ => default);

        // UUIDv7s with later timestamps should sort after earlier ones
        id2.CompareTo(id1).ShouldBeGreaterThan(0);
    }
}
