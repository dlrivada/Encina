using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.IdGeneration;

/// <summary>
/// Property-based tests for <see cref="UuidV7IdGenerator"/> and <see cref="UuidV7Id"/> invariants.
/// </summary>
[Trait("Category", "Property")]
public sealed class UuidV7IdPropertyTests
{
    #region Uniqueness

    [Property(MaxTest = 200)]
    public bool Property_Generate_ProducesUniqueIds(PositiveInt batchSize)
    {
        var size = Math.Min(batchSize.Get, 5000);
        var generator = new UuidV7IdGenerator();
        var ids = new System.Collections.Generic.HashSet<Guid>(size);

        for (var i = 0; i < size; i++)
        {
            var result = generator.Generate();
            if (result.IsLeft) return false;
            result.Match(id => { ids.Add(id.Value); }, _ => { });
        }

        return ids.Count == size;
    }

    #endregion

    #region RFC 9562 compliance

    [Property(MaxTest = 100)]
    public bool Property_NewUuidV7_AlwaysSetsVersion7()
    {
        var id = UuidV7Id.NewUuidV7();
        Span<byte> bytes = stackalloc byte[16];
        id.Value.TryWriteBytes(bytes, bigEndian: true, out _);
        return (bytes[6] & 0xF0) == 0x70;
    }

    [Property(MaxTest = 100)]
    public bool Property_NewUuidV7_AlwaysSetsRFC4122Variant()
    {
        var id = UuidV7Id.NewUuidV7();
        Span<byte> bytes = stackalloc byte[16];
        id.Value.TryWriteBytes(bytes, bigEndian: true, out _);
        return (bytes[8] & 0xC0) == 0x80;
    }

    #endregion

    #region Timestamp encoding

    [Property(MaxTest = 50)]
    public bool Property_GetTimestamp_ReturnsRecentTimestamp()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var id = UuidV7Id.NewUuidV7();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        var timestamp = id.GetTimestamp();
        return timestamp >= before && timestamp <= after;
    }

    #endregion

    #region Parse roundtrip

    [Property(MaxTest = 100)]
    public bool Property_ParseRoundtrip_IsIdentity()
    {
        var original = UuidV7Id.NewUuidV7();
        var str = original.ToString();
        var parsed = UuidV7Id.Parse(str);
        return original.Equals(parsed);
    }

    #endregion
}
