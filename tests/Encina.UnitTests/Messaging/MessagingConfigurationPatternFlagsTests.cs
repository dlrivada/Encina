using Encina.Messaging;

namespace Encina.UnitTests.Messaging;

/// <summary>
/// Verifies that <see cref="MessagingConfiguration.IsAnyPatternEnabled"/> reports true for every
/// pattern flag it covers, and false when none is set (#1633).
/// </summary>
[Trait("Category", "Unit")]
public sealed class MessagingConfigurationPatternFlagsTests
{
    public static TheoryData<string> PatternFlagNames =>
    [
        nameof(MessagingConfiguration.UseTransactions),
        nameof(MessagingConfiguration.UseOutbox),
        nameof(MessagingConfiguration.UseInbox),
        nameof(MessagingConfiguration.UseSagas),
        nameof(MessagingConfiguration.UseRoutingSlips),
        nameof(MessagingConfiguration.UseScheduling),
        nameof(MessagingConfiguration.UseRecoverability),
        nameof(MessagingConfiguration.UseDeadLetterQueue),
        nameof(MessagingConfiguration.UseContentRouter),
        nameof(MessagingConfiguration.UseScatterGather),
        nameof(MessagingConfiguration.UseTenancy),
        nameof(MessagingConfiguration.UseModuleIsolation),
        nameof(MessagingConfiguration.UseReadWriteSeparation),
        nameof(MessagingConfiguration.UseDomainEvents),
        nameof(MessagingConfiguration.UseAuditing),
        nameof(MessagingConfiguration.UseAuditLogStore),
        nameof(MessagingConfiguration.UseOperationAuditStore),
        nameof(MessagingConfiguration.UseReadAuditStore),
        nameof(MessagingConfiguration.UseSoftDelete),
        nameof(MessagingConfiguration.UseTemporalTables),
        nameof(MessagingConfiguration.UseQueryCache),
        nameof(MessagingConfiguration.UseAnonymization),
        nameof(MessagingConfiguration.UseRetention),
        nameof(MessagingConfiguration.UseDataResidency),
        nameof(MessagingConfiguration.UseCrossBorderTransfer),
        nameof(MessagingConfiguration.UseBreachNotification)
    ];

    [Fact]
    public void IsAnyPatternEnabled_WithDefaultConfiguration_ReturnsFalse()
    {
        new MessagingConfiguration().IsAnyPatternEnabled.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(PatternFlagNames))]
    public void IsAnyPatternEnabled_WhenOnlyOneFlagIsSet_ReturnsTrue(string flagName)
    {
        var configuration = new MessagingConfiguration();
        typeof(MessagingConfiguration).GetProperty(flagName)!.SetValue(configuration, true);

        configuration.IsAnyPatternEnabled.ShouldBeTrue();
    }
}
