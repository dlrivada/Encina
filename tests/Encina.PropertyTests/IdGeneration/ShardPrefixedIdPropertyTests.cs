using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.IdGeneration;

/// <summary>
/// Property-based tests for <see cref="ShardPrefixedIdGenerator"/> and <see cref="ShardPrefixedId"/> invariants.
/// </summary>
[Trait("Category", "Property")]
public sealed class ShardPrefixedIdPropertyTests
{
    #region Shard extraction roundtrip

    [Property(MaxTest = 200)]
    public bool Property_ExtractShardId_Roundtrip_ReturnsOriginalShard(NonEmptyString shardPrefix)
    {
        var shardId = shardPrefix.Get
            .Replace(":", "")
            .Replace("\0", "")
            .Trim();

        if (string.IsNullOrWhiteSpace(shardId)) return true; // Skip degenerate cases

        var options = new ShardPrefixedOptions { Format = ShardPrefixedFormat.Ulid };
        var generator = new ShardPrefixedIdGenerator(options);

        var generateResult = generator.Generate(shardId);
        if (generateResult.IsLeft) return true; // Invalid input is expected to fail

        var id = generateResult.Match(x => x, _ => default);
        var extractResult = generator.ExtractShardId(id);

        return extractResult.Match(s => s == shardId, _ => false);
    }

    #endregion

    #region Uniqueness

    [Property(MaxTest = 200)]
    public bool Property_Generate_ProducesUniqueIdsPerShard(PositiveInt batchSize)
    {
        var size = Math.Min(batchSize.Get, 2000);
        var generator = new ShardPrefixedIdGenerator(new ShardPrefixedOptions());
        var ids = new System.Collections.Generic.HashSet<string>(size);

        for (var i = 0; i < size; i++)
        {
            var result = generator.Generate("test-shard");
            if (result.IsLeft) return false;
            result.Match(id => { ids.Add(id.ToString()); }, _ => { });
        }

        return ids.Count == size;
    }

    #endregion

    #region Format consistency

    [Property(MaxTest = 50)]
    public bool Property_Generate_UlidFormat_Sequence26Chars()
    {
        var generator = new ShardPrefixedIdGenerator(
            new ShardPrefixedOptions { Format = ShardPrefixedFormat.Ulid });

        var result = generator.Generate("shard");
        if (result.IsLeft) return false;

        return result.Match(id => id.Sequence.Length == 26, _ => false);
    }

    [Property(MaxTest = 50)]
    public bool Property_Generate_UuidV7Format_SequenceIsValidGuid()
    {
        var generator = new ShardPrefixedIdGenerator(
            new ShardPrefixedOptions { Format = ShardPrefixedFormat.UuidV7 });

        var result = generator.Generate("shard");
        if (result.IsLeft) return false;

        return result.Match(id => Guid.TryParse(id.Sequence, out _), _ => false);
    }

    #endregion

    #region Parse roundtrip

    [Property(MaxTest = 100)]
    public bool Property_ParseRoundtrip_IsIdentity()
    {
        var generator = new ShardPrefixedIdGenerator(new ShardPrefixedOptions());
        var genResult = generator.Generate("test-shard");
        if (genResult.IsLeft) return false;

        var original = genResult.Match(x => x, _ => default);
        var str = original.ToString();
        var parsed = ShardPrefixedId.Parse(str);

        return original.Equals(parsed);
    }

    #endregion
}
