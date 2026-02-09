using Encina.Cdc.Debezium.Kafka;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc.Debezium;

/// <summary>
/// Property-based tests for <see cref="DebeziumKafkaPosition"/> invariants.
/// Verifies serialization round-trips, offset-based ordering, comparison semantics,
/// and string format consistency across randomized inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class DebeziumKafkaPositionPropertyTests
{
    #region Serialization Round-Trip

    /// <summary>
    /// Property: For all valid inputs, FromBytes(ToBytes()) preserves all fields.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_RoundTrip_PreservesAllFields()
    {
        var gen = from offsetJson in GenNonEmptyString()
                  from topic in GenNonEmptyString()
                  from partition in Gen.Choose(0, 99)
                  from offset in Gen.Choose(0, 1000000).Select(o => (long)o)
                  select (offsetJson, topic, partition, offset);

        return Prop.ForAll(
            Arb.From(gen),
            t =>
            {
                var original = new DebeziumKafkaPosition(t.offsetJson, t.topic, t.partition, t.offset);
                var restored = DebeziumKafkaPosition.FromBytes(original.ToBytes());

                return restored.OffsetJson == original.OffsetJson
                    && restored.Topic == original.Topic
                    && restored.Partition == original.Partition
                    && restored.Offset == original.Offset;
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
            Arb.From(GenNonEmptyString()),
            (offsetJson, topic) =>
            {
                var position = new DebeziumKafkaPosition(offsetJson, topic, 0, 42);
                return position.CompareTo(position) == 0;
            });
    }

    #endregion

    #region CompareTo Same Topic+Partition — Offset Ordering

    /// <summary>
    /// Property: Same topic and partition → comparison result matches offset comparison.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_CompareTo_SameTopicPartition_MatchesOffsetComparison(long offset1, long offset2)
    {
        var absOffset1 = Math.Abs(offset1 % 1000000);
        var absOffset2 = Math.Abs(offset2 % 1000000);

        var pos1 = new DebeziumKafkaPosition("{\"x\":1}", "topic", 0, absOffset1);
        var pos2 = new DebeziumKafkaPosition("{\"x\":1}", "topic", 0, absOffset2);

        var posCompare = pos1.CompareTo(pos2);
        var offsetCompare = absOffset1.CompareTo(absOffset2);

        return Math.Sign(posCompare) == Math.Sign(offsetCompare);
    }

    #endregion

    #region CompareTo Antisymmetric

    /// <summary>
    /// Property: For all a, b on same topic+partition → sign(a.CompareTo(b)) == -sign(b.CompareTo(a)).
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_CompareTo_IsAntisymmetric(long offset1, long offset2)
    {
        var absOffset1 = Math.Abs(offset1 % 1000000);
        var absOffset2 = Math.Abs(offset2 % 1000000);

        var a = new DebeziumKafkaPosition("{\"x\":1}", "topic", 0, absOffset1);
        var b = new DebeziumKafkaPosition("{\"x\":1}", "topic", 0, absOffset2);

        var ab = a.CompareTo(b);
        var ba = b.CompareTo(a);

        if (ab > 0) return ba < 0;
        if (ab < 0) return ba > 0;
        return ba == 0;
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
            Arb.From(GenNonEmptyString()),
            (offsetJson, topic) =>
            {
                var position = new DebeziumKafkaPosition(offsetJson, topic, 0, 42);
                return position.CompareTo(null) > 0;
            });
    }

    #endregion

    #region ToString Format

    /// <summary>
    /// Property: For all positions, ToString() matches the Kafka format pattern.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_ToString_MatchesKafkaFormat()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyString()),
            Arb.From(GenNonEmptyString()),
            (offsetJson, topic) =>
            {
                var position = new DebeziumKafkaPosition(offsetJson, topic, 3, 100);
                var str = position.ToString();
                return str == $"Kafka:{topic}[3]@100";
            });
    }

    #endregion

    #region Offset Ordering

    /// <summary>
    /// Property: Same topic+partition, offset1 &lt; offset2 → pos1 &lt; pos2.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_OffsetOrdering_IsPreserved(PositiveInt rawO1, PositiveInt rawO2)
    {
        long offset1 = rawO1.Get;
        long offset2 = rawO2.Get + rawO1.Get + 1; // Ensure offset2 > offset1

        var pos1 = new DebeziumKafkaPosition("{\"x\":1}", "topic", 0, offset1);
        var pos2 = new DebeziumKafkaPosition("{\"x\":1}", "topic", 0, offset2);

        return pos1.CompareTo(pos2) < 0;
    }

    #endregion

    #region Generators

    private static Gen<string> GenNonEmptyString()
    {
        return Gen.Elements("alpha", "beta", "gamma", "delta", "epsilon")
            .SelectMany(prefix =>
                Gen.Choose(1, 10000).Select(n => $"{prefix}-{n}"));
    }

    #endregion
}
