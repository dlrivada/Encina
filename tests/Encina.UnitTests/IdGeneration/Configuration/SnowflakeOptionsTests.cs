using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;

namespace Encina.UnitTests.IdGeneration.Configuration;

/// <summary>
/// Unit tests for <see cref="SnowflakeOptions"/>.
/// </summary>
public sealed class SnowflakeOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new SnowflakeOptions();

        options.MachineId.ShouldBe(0L);
        options.TimestampBits.ShouldBe(41);
        options.ShardBits.ShouldBe(10);
        options.SequenceBits.ShouldBe(12);
        options.ClockDriftToleranceMs.ShouldBe(5L);
        options.EpochStart.ShouldBe(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void MaxMachineId_WithDefaultBits_Returns1023()
    {
        var options = new SnowflakeOptions();
        options.MaxMachineId.ShouldBe(1023L);
    }

    [Fact]
    public void MaxSequence_WithDefaultBits_Returns4095()
    {
        var options = new SnowflakeOptions();
        options.MaxSequence.ShouldBe(4095L);
    }

    [Fact]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        var options = new SnowflakeOptions();
        Should.NotThrow(() =>
        {
            // Validate is internal, we test via generator construction
            var _ = new SnowflakeIdGenerator(options);
        });
    }

    [Fact]
    public void Validate_InvalidBitSum_ThrowsArgumentException()
    {
        var options = new SnowflakeOptions
        {
            TimestampBits = 41,
            ShardBits = 10,
            SequenceBits = 10 // Sum = 61, not 63
        };

        Should.Throw<ArgumentException>(() =>
            new SnowflakeIdGenerator(options));
    }

    [Fact]
    public void Validate_MachineIdExceedsMax_ThrowsArgumentException()
    {
        var options = new SnowflakeOptions
        {
            MachineId = 2000, // Exceeds max for 10 bits (1023)
            ShardBits = 10
        };

        Should.Throw<ArgumentException>(() =>
            new SnowflakeIdGenerator(options));
    }

    [Fact]
    public void Validate_NegativeMachineId_ThrowsArgumentException()
    {
        var options = new SnowflakeOptions { MachineId = -1 };

        Should.Throw<ArgumentException>(() =>
            new SnowflakeIdGenerator(options));
    }

    [Fact]
    public void Validate_ZeroTimestampBits_ThrowsArgumentException()
    {
        var options = new SnowflakeOptions
        {
            TimestampBits = 0,
            ShardBits = 51,
            SequenceBits = 12
        };

        Should.Throw<ArgumentException>(() =>
            new SnowflakeIdGenerator(options));
    }
}
