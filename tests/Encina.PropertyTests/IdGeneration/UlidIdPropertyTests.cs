using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.IdGeneration;

/// <summary>
/// Property-based tests for <see cref="UlidIdGenerator"/> and <see cref="UlidId"/> invariants.
/// </summary>
[Trait("Category", "Property")]
public sealed class UlidIdPropertyTests
{
    #region Uniqueness

    [Property(MaxTest = 200)]
    public bool Property_Generate_ProducesUniqueIds(PositiveInt batchSize)
    {
        var size = Math.Min(batchSize.Get, 5000);
        var generator = new UlidIdGenerator();
        var ids = new System.Collections.Generic.HashSet<string>(size);

        for (var i = 0; i < size; i++)
        {
            var result = generator.Generate();
            if (result.IsLeft) return false;
            result.Match(id => { ids.Add(id.ToString()); }, _ => { });
        }

        return ids.Count == size;
    }

    #endregion

    #region String format

    [Property(MaxTest = 100)]
    public bool Property_ToString_AlwaysReturns26Characters()
    {
        var id = UlidId.NewUlid();
        return id.ToString().Length == 26;
    }

    [Property(MaxTest = 100)]
    public bool Property_ToString_OnlyContainsCrockfordBase32Chars()
    {
        var id = UlidId.NewUlid();
        var str = id.ToString();
        return str.All(c => "0123456789ABCDEFGHJKMNPQRSTVWXYZ".Contains(c));
    }

    #endregion

    #region Parse roundtrip

    [Property(MaxTest = 100)]
    public bool Property_ParseRoundtrip_IsIdentity()
    {
        var original = UlidId.NewUlid();
        var str = original.ToString();
        var parsed = UlidId.Parse(str);
        return original.Equals(parsed);
    }

    #endregion

    #region Timestamp encoding

    [Property(MaxTest = 50)]
    public bool Property_GetTimestamp_ReturnsRecentTimestamp()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var id = UlidId.NewUlid();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var timestamp = id.GetTimestamp();
        return timestamp >= before && timestamp <= after;
    }

    #endregion
}
