using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.IdGeneration;

/// <summary>
/// Property-based tests for <see cref="SnowflakeIdGenerator"/> invariants.
/// Verifies uniqueness, monotonicity, and shard extraction across generated inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class SnowflakeIdGeneratorPropertyTests
{
    private static SnowflakeOptions DefaultOptions(long machineId = 1) => new()
    {
        MachineId = machineId
    };

    #region Uniqueness

    [Property(MaxTest = 500)]
    public bool Property_Generate_ProducesUniqueIds(PositiveInt batchSize)
    {
        var size = Math.Min(batchSize.Get, 5000);
        var options = DefaultOptions();
        var generator = new SnowflakeIdGenerator(options);
        var ids = new System.Collections.Generic.HashSet<long>(size);

        for (var i = 0; i < size; i++)
        {
            var result = generator.Generate();
            if (result.IsLeft) return false;
            result.Match(id => { if (!ids.Add(id.Value)) return; }, _ => { });
        }

        return ids.Count == size;
    }

    #endregion

    #region Monotonicity

    [Property(MaxTest = 200)]
    public bool Property_Generate_IdsAreMonotonicallyIncreasing(PositiveInt count)
    {
        var n = Math.Min(count.Get, 1000);
        var generator = new SnowflakeIdGenerator(DefaultOptions());
        long previous = 0;

        for (var i = 0; i < n; i++)
        {
            var result = generator.Generate();
            if (result.IsLeft) return false;

            var current = result.Match(id => id.Value, _ => 0L);
            if (current <= previous) return false;
            previous = current;
        }

        return true;
    }

    #endregion

    #region Shard extraction roundtrip

    [Property(MaxTest = 100)]
    public bool Property_ExtractShardId_Roundtrip_ReturnsOriginalShard(PositiveInt shardValue)
    {
        var shardId = (shardValue.Get % 1023) + 0; // Keep within 10-bit range
        var shardStr = shardId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var options = DefaultOptions();
        var generator = new SnowflakeIdGenerator(options);

        var generateResult = generator.Generate(shardStr);
        if (generateResult.IsLeft) return false;

        var id = generateResult.Match(x => x, _ => default);
        var extractResult = generator.ExtractShardId(id);
        if (extractResult.IsLeft) return false;

        return extractResult.Match(s => s == shardStr, _ => false);
    }

    #endregion

    #region Machine ID embedding

    [Property(MaxTest = 50)]
    public bool Property_Generate_AlwaysEmbedsMachineId(PositiveInt machineValue)
    {
        var machineId = machineValue.Get % 1024; // Keep within 10-bit range
        var options = DefaultOptions(machineId);
        var generator = new SnowflakeIdGenerator(options);

        var result = generator.Generate();
        if (result.IsLeft) return false;

        var id = result.Match(x => x.Value, _ => 0L);
        var extractedMachineId = (id >> options.SequenceBits) & ((1L << options.ShardBits) - 1);

        return extractedMachineId == machineId;
    }

    #endregion
}
