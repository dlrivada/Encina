using System.Text.Json;
using Encina.Sharding.Configuration;
using Encina.Sharding.TimeBased;
using Microsoft.Extensions.Configuration;

namespace Encina.UnitTests.Sharding.TimeBased;

/// <summary>
/// Unit tests proving that <see cref="ShardTierInfo"/> never exposes its connection string
/// through <see cref="ShardTierInfo.ToString"/>, string interpolation or System.Text.Json
/// serialization (#3685).
/// </summary>
public sealed class ShardTierInfoSecretMaskingTests
{
    private const string SecretConnectionString = "Server=db;User Id=sa;Password=secret;";

    private static readonly DateTime CreatedAtUtc = new(2026, 2, 1, 8, 30, 0, DateTimeKind.Utc);

    private static ShardTierInfo CreateTierInfo(DateTime? lastTransitionAtUtc = null) =>
        new(
            "orders-2026-02",
            ShardTier.Warm,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 1),
            IsReadOnly: true,
            SecretConnectionString,
            CreatedAtUtc,
            lastTransitionAtUtc);

    [Fact]
    public void ToString_WithConnectionString_MasksTheConnectionString()
    {
        // Arrange
        var tierInfo = CreateTierInfo();

        // Act
        var text = tierInfo.ToString();

        // Assert
        text.ShouldNotContain("secret");
        text.ShouldNotContain("Server=db");
        text.ShouldNotContain("Password");
        text.ShouldBe(
            "ShardTierInfo { ShardId = orders-2026-02, CurrentTier = Warm, PeriodStart = 2026-02-01, PeriodEnd = 2026-03-01, IsReadOnly = True, ConnectionString = ***, CreatedAtUtc = 2026-02-01T08:30:00.0000000Z, LastTransitionAtUtc =  }");
    }

    [Fact]
    public void ToString_WithLastTransition_PrintsTheTransitionTimestamp()
    {
        // Arrange
        var tierInfo = CreateTierInfo(new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var text = tierInfo.ToString();

        // Assert
        text.ShouldContain("LastTransitionAtUtc = 2026-03-02T00:00:00.0000000Z");
        text.ShouldContain("ConnectionString = ***");
        text.ShouldNotContain("Password");
    }

    [Fact]
    public void StringInterpolation_WithConnectionString_MasksTheConnectionString()
    {
        // Arrange
        var tierInfo = CreateTierInfo();

        // Act
        var text = $"Transitioning {tierInfo}";

        // Assert
        text.ShouldNotContain("secret");
        text.ShouldNotContain("Server=db");
        text.ShouldNotContain("Password");
        text.ShouldContain("ConnectionString = ***");
    }

    [Fact]
    public void JsonSerialize_WithConnectionString_ExcludesTheConnectionString()
    {
        // Arrange
        var tierInfo = CreateTierInfo();

        // Act
        var json = JsonSerializer.Serialize(tierInfo);

        // Assert
        json.ShouldNotContain("ConnectionString", Case.Insensitive);
        json.ShouldNotContain("Password", Case.Insensitive);
        json.ShouldNotContain("secret");
        json.ShouldNotContain("Server=");
        json.ShouldContain("\"ShardId\":\"orders-2026-02\"");
    }

    [Fact]
    public void JsonSerialize_OfTimeBasedOptions_ExcludesTheInitialShardConnectionStrings()
    {
        // Arrange
        var options = new TimeBasedShardRouterOptions { InitialShards = [CreateTierInfo()] };

        // Act
        var json = JsonSerializer.Serialize(options);

        // Assert
        json.ShouldNotContain("Password", Case.Insensitive);
        json.ShouldNotContain("secret");
        json.ShouldContain("orders-2026-02");
    }

    [Fact]
    public void JsonDeserialize_OfSerializedTierInfo_FailsBecauseTheConnectionStringIsNotSerialized()
    {
        // Arrange
        var json = JsonSerializer.Serialize(CreateTierInfo());

        // Act & Assert
        Should.Throw<ArgumentException>(() => JsonSerializer.Deserialize<ShardTierInfo>(json));
    }

    [Fact]
    public void ConfigurationBinding_TimeBasedOptionsInitialShards_StillBindsTheConnectionString()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TimeBased:InitialShards:0:ShardId"] = "orders-2026-02",
                ["TimeBased:InitialShards:0:CurrentTier"] = "Hot",
                ["TimeBased:InitialShards:0:PeriodStart"] = "2026-02-01",
                ["TimeBased:InitialShards:0:PeriodEnd"] = "2026-03-01",
                ["TimeBased:InitialShards:0:IsReadOnly"] = "false",
                ["TimeBased:InitialShards:0:ConnectionString"] = SecretConnectionString,
                ["TimeBased:InitialShards:0:CreatedAtUtc"] = "2026-02-01T00:00:00Z",
            })
            .Build();
        var options = new TimeBasedShardRouterOptions();

        // Act
        configuration.GetSection("TimeBased").Bind(options);

        // Assert
        options.InitialShards.Count.ShouldBe(1);
        options.InitialShards[0].ShardId.ShouldBe("orders-2026-02");
        options.InitialShards[0].CurrentTier.ShouldBe(ShardTier.Hot);
        options.InitialShards[0].PeriodStart.ShouldBe(new DateOnly(2026, 2, 1));
        options.InitialShards[0].ConnectionString.ShouldBe(SecretConnectionString);
    }
}
