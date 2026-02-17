using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableConfiguration"/>.
/// </summary>
public sealed class ReferenceTableConfigurationTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Country
    {
        public int Id { get; set; }
    }

    private sealed class Currency
    {
        public int Id { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    //  Record Properties
    // ────────────────────────────────────────────────────────────

    #region Record Properties

    [Fact]
    public void Record_StoresEntityTypeAndOptions()
    {
        // Arrange
        var options = new ReferenceTableOptions { BatchSize = 500 };

        // Act
        var config = new ReferenceTableConfiguration(typeof(Country), options);

        // Assert
        config.EntityType.ShouldBe(typeof(Country));
        config.Options.BatchSize.ShouldBe(500);
    }

    [Fact]
    public void Record_OptionsAreAccessible()
    {
        // Arrange
        var options = new ReferenceTableOptions
        {
            RefreshStrategy = RefreshStrategy.CdcDriven,
            PrimaryShardId = "shard-0",
            PollingInterval = TimeSpan.FromMinutes(10),
            BatchSize = 2000,
            SyncOnStartup = false
        };

        // Act
        var config = new ReferenceTableConfiguration(typeof(Country), options);

        // Assert
        config.Options.RefreshStrategy.ShouldBe(RefreshStrategy.CdcDriven);
        config.Options.PrimaryShardId.ShouldBe("shard-0");
        config.Options.PollingInterval.ShouldBe(TimeSpan.FromMinutes(10));
        config.Options.BatchSize.ShouldBe(2000);
        config.Options.SyncOnStartup.ShouldBeFalse();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Record Equality
    // ────────────────────────────────────────────────────────────

    #region Equality

    [Fact]
    public void Record_Equality_SameValues_AreEqual()
    {
        // Arrange
        var options = new ReferenceTableOptions();
        var config1 = new ReferenceTableConfiguration(typeof(Country), options);
        var config2 = new ReferenceTableConfiguration(typeof(Country), options);

        // Act & Assert
        config1.ShouldBe(config2);
    }

    [Fact]
    public void Record_Equality_DifferentEntityType_AreNotEqual()
    {
        // Arrange
        var options = new ReferenceTableOptions();
        var config1 = new ReferenceTableConfiguration(typeof(Country), options);
        var config2 = new ReferenceTableConfiguration(typeof(Currency), options);

        // Act & Assert
        config1.ShouldNotBe(config2);
    }

    [Fact]
    public void Record_Equality_DifferentOptions_AreNotEqual()
    {
        // Arrange
        var options1 = new ReferenceTableOptions { BatchSize = 100 };
        var options2 = new ReferenceTableOptions { BatchSize = 200 };
        var config1 = new ReferenceTableConfiguration(typeof(Country), options1);
        var config2 = new ReferenceTableConfiguration(typeof(Country), options2);

        // Act & Assert
        config1.ShouldNotBe(config2);
    }

    #endregion
}
