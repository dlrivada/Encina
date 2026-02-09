using Encina.Cdc.Debezium;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc.Debezium;

/// <summary>
/// Property-based tests for <see cref="DebeziumCdcPosition"/> invariants.
/// Verifies serialization round-trips, comparison semantics, and null handling
/// across randomized inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class DebeziumCdcPositionPropertyTests
{
    #region Serialization Round-Trip

    /// <summary>
    /// Property: For all non-empty offset strings, FromBytes(ToBytes()) preserves OffsetJson.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_RoundTrip_PreservesOffsetJson()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyString()),
            offsetJson =>
            {
                var original = new DebeziumCdcPosition(offsetJson);
                var restored = DebeziumCdcPosition.FromBytes(original.ToBytes());
                return restored.OffsetJson == original.OffsetJson;
            });
    }

    #endregion

    #region CompareTo Reflexive

    /// <summary>
    /// Property: For all positions, pos.CompareTo(pos) == 0.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_CompareTo_IsReflexive()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyString()),
            offsetJson =>
            {
                var position = new DebeziumCdcPosition(offsetJson);
                return position.CompareTo(position) == 0;
            });
    }

    #endregion

    #region CompareTo Antisymmetric

    /// <summary>
    /// Property: For all a, b → sign(a.CompareTo(b)) == -sign(b.CompareTo(a)).
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_CompareTo_IsAntisymmetric()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyString()),
            Arb.From(GenNonEmptyString()),
            (offsetA, offsetB) =>
            {
                var a = new DebeziumCdcPosition(offsetA);
                var b = new DebeziumCdcPosition(offsetB);

                var ab = a.CompareTo(b);
                var ba = b.CompareTo(a);

                if (ab > 0) return ba < 0;
                if (ab < 0) return ba > 0;
                return ba == 0;
            });
    }

    #endregion

    #region CompareTo Null

    /// <summary>
    /// Property: For all positions, pos.CompareTo(null) > 0.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_CompareTo_NullReturnsPositive()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyString()),
            offsetJson =>
            {
                var position = new DebeziumCdcPosition(offsetJson);
                return position.CompareTo(null) > 0;
            });
    }

    #endregion

    #region ToString Non-Empty

    /// <summary>
    /// Property: For all positions, ToString() is non-null and non-empty.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_ToString_IsNonEmpty()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyString()),
            offsetJson =>
            {
                var position = new DebeziumCdcPosition(offsetJson);
                var str = position.ToString();
                return !string.IsNullOrWhiteSpace(str);
            });
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generates non-empty, non-whitespace strings for use as offset JSON.
    /// </summary>
    private static Gen<string> GenNonEmptyString()
    {
        return Gen.Elements("a", "b", "c", "d", "e")
            .SelectMany(prefix =>
                Gen.Choose(1, 10000).Select(n => $"{{\"{prefix}\":{n}}}"));
    }

    #endregion
}
