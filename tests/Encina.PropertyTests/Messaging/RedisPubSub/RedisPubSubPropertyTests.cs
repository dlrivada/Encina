using Encina.Redis.PubSub;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Messaging.RedisPubSub;

/// <summary>
/// Property-based tests for <see cref="EncinaRedisPubSubOptions"/> invariants.
/// Verifies channel naming determinism and ToString safety.
/// </summary>
[Trait("Category", "Property")]
public sealed class RedisPubSubPropertyTests
{
    #region Channel Naming Determinism

    /// <summary>
    /// Property: Channel naming with a prefix is deterministic (same input = same channel).
    /// Setting the same ChannelPrefix always produces the same ToString output.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_ChannelPrefix_IsDeterministic()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyAlphanumericString()),
            prefix =>
            {
                var options1 = new EncinaRedisPubSubOptions { ChannelPrefix = prefix };
                var options2 = new EncinaRedisPubSubOptions { ChannelPrefix = prefix };

                return options1.ToString() == options2.ToString();
            });
    }

    /// <summary>
    /// Property: Different prefixes produce different ToString representations.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_DifferentPrefixes_ProduceDifferentToString()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyAlphanumericString()),
            Arb.From(GenNonEmptyAlphanumericString()),
            (prefixA, prefixB) =>
            {
                if (prefixA == prefixB) return true;

                var optionsA = new EncinaRedisPubSubOptions { ChannelPrefix = prefixA };
                var optionsB = new EncinaRedisPubSubOptions { ChannelPrefix = prefixB };

                return optionsA.ToString() != optionsB.ToString();
            });
    }

    /// <summary>
    /// Property: ChannelPrefix round-trip (set then get returns same value).
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_ChannelPrefix_SetGetRoundTrip()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyAlphanumericString()),
            prefix =>
            {
                var options = new EncinaRedisPubSubOptions { ChannelPrefix = prefix };

                return options.ChannelPrefix == prefix;
            });
    }

    #endregion

    #region ToString Safety

    /// <summary>
    /// Property: ToString never throws for any valid configuration.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_ToString_NeverThrows()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyAlphanumericString()),
            Arb.From(GenNonEmptyAlphanumericString()),
            Arb.From(GenNonEmptyAlphanumericString()),
            (prefix, commandChannel, eventChannel) =>
            {
                var options = new EncinaRedisPubSubOptions
                {
                    ChannelPrefix = prefix,
                    CommandChannel = commandChannel,
                    EventChannel = eventChannel
                };

                try
                {
                    var result = options.ToString();
                    return !string.IsNullOrEmpty(result);
                }
                catch
                {
                    return false;
                }
            });
    }

    /// <summary>
    /// Property: ToString output contains the configured prefix.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_ToString_ContainsPrefix()
    {
        return Prop.ForAll(
            Arb.From(GenNonEmptyAlphanumericString()),
            prefix =>
            {
                var options = new EncinaRedisPubSubOptions { ChannelPrefix = prefix };
                var str = options.ToString();

                return str!.Contains(prefix, StringComparison.Ordinal);
            });
    }

    #endregion

    #region Default Values

    /// <summary>
    /// Property: Default options always have non-null, non-empty channel values.
    /// </summary>
    [Fact]
    public void DefaultOptions_HaveValidDefaults()
    {
        var options = new EncinaRedisPubSubOptions();

        options.ChannelPrefix.ShouldNotBeNullOrEmpty();
        options.CommandChannel.ShouldNotBeNullOrEmpty();
        options.EventChannel.ShouldNotBeNullOrEmpty();
        options.ConnectionString.ShouldNotBeNullOrEmpty();
        options.ConnectTimeout.ShouldBeGreaterThan(0);
        options.SyncTimeout.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generates non-empty alphanumeric strings suitable for channel names.
    /// </summary>
    private static Gen<string> GenNonEmptyAlphanumericString()
    {
        return Gen.Elements("alpha", "beta", "gamma", "delta", "epsilon", "zeta")
            .SelectMany(prefix =>
                Gen.Choose(1, 9999).Select(n => $"{prefix}{n}"));
    }

    #endregion
}
