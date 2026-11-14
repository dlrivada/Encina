using System.Text.Json;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.ReplicaSelection;
using Encina.Sharding.TimeBased;
using Microsoft.Extensions.Configuration;

namespace Encina.UnitTests.Sharding;

/// <summary>
/// Regression tests for #3685: <see cref="ShardInfo"/> and <see cref="ShardTierInfo"/> never
/// expose their connection strings through <c>ToString()</c>, string interpolation or
/// System.Text.Json serialization, while configuration binding still reads them.
/// </summary>
public sealed class ShardRecordsSecretMaskingTests
{
    private const string PrimaryConnectionString = "Server=db;User Id=sa;Password=secret;";
    private const string ReplicaConnectionString = "Server=r1;User Id=sa;Password=rsecret;";

    private static readonly DateTime TierCreatedAtUtc = new(2026, 2, 1, 8, 30, 0, DateTimeKind.Utc);

    #region ShardInfo

    private static ShardInfo CreateShardWithReplica() =>
        new("s1", PrimaryConnectionString, ReplicaConnectionStrings: [ReplicaConnectionString]);

    [Fact]
    public void ShardInfo_ToString_WithPrimaryAndReplica_MasksEveryConnectionString()
    {
        // Arrange
        var shard = CreateShardWithReplica();

        // Act
        var text = shard.ToString();

        // Assert
        text.ShouldNotContain("secret");
        text.ShouldNotContain("rsecret");
        text.ShouldNotContain("Server=db");
        text.ShouldNotContain("Server=r1");
        text.ShouldNotContain("Password");
        text.ShouldBe(
            "ShardInfo { ShardId = s1, ConnectionString = ***, Weight = 1, IsActive = True, ReplicaConnectionStrings = [1 masked], ReplicaStrategy =  }");
    }

    [Fact]
    public void ShardInfo_StringInterpolation_WithPrimaryAndReplica_MasksEveryConnectionString()
    {
        // Arrange
        var shard = CreateShardWithReplica();

        // Act
        var text = $"Routing to {shard}";

        // Assert
        text.ShouldNotContain("secret");
        text.ShouldNotContain("Server=db");
        text.ShouldNotContain("Server=r1");
        text.ShouldNotContain("Password");
        text.ShouldContain("ConnectionString = ***");
        text.ShouldContain("ReplicaConnectionStrings = [1 masked]");
    }

    [Fact]
    public void ShardInfo_ToString_WithoutReplicas_ShowsZeroMaskedReplicas()
    {
        // Arrange
        var shard = new ShardInfo("s2", PrimaryConnectionString, Weight: 3, IsActive: false);

        // Act
        var text = shard.ToString();

        // Assert
        text.ShouldBe(
            "ShardInfo { ShardId = s2, ConnectionString = ***, Weight = 3, IsActive = False, ReplicaConnectionStrings = [0 masked], ReplicaStrategy =  }");
    }

    [Fact]
    public void ShardInfo_ToString_WithEmptyReplicaListAndStrategy_ShowsStrategyAndZeroReplicas()
    {
        // Arrange
        var shard = new ShardInfo(
            "s3",
            PrimaryConnectionString,
            ReplicaConnectionStrings: [],
            ReplicaStrategy: ReplicaSelectionStrategy.RoundRobin);

        // Act
        var text = shard.ToString();

        // Assert
        text.ShouldContain("ReplicaConnectionStrings = [0 masked]");
        text.ShouldContain("ReplicaStrategy = RoundRobin");
        text.ShouldNotContain("Password");
    }

    [Fact]
    public void ShardInfo_ToString_WithSeveralReplicas_ShowsOnlyTheReplicaCount()
    {
        // Arrange
        var shard = new ShardInfo(
            "s4",
            PrimaryConnectionString,
            ReplicaConnectionStrings: [ReplicaConnectionString, "Server=r2;Password=r2secret;"]);

        // Act
        var text = shard.ToString();

        // Assert
        text.ShouldContain("ReplicaConnectionStrings = [2 masked]");
        text.ShouldNotContain("Server=r2");
        text.ShouldNotContain("r2secret");
    }

    [Fact]
    public void ShardInfo_Constructor_WithWhitespaceReplica_ThrowsWithoutEchoingTheOtherReplicas()
    {
        // Act
        var ex = Should.Throw<ArgumentException>(() =>
            new ShardInfo("s5", PrimaryConnectionString, ReplicaConnectionStrings: [ReplicaConnectionString, " "]));

        // Assert
        ex.Message.ShouldContain("index 1");
        ex.Message.ShouldNotContain("Password");
    }

    [Fact]
    public void ShardInfo_JsonSerialize_WithPrimaryAndReplica_ExcludesEveryConnectionString()
    {
        // Arrange
        var shard = CreateShardWithReplica();

        // Act
        var json = JsonSerializer.Serialize(shard);

        // Assert
        json.ShouldNotContain("ConnectionString", Case.Insensitive);
        json.ShouldNotContain("Password", Case.Insensitive);
        json.ShouldNotContain("secret");
        json.ShouldNotContain("Server=");
        json.ShouldContain("\"ShardId\":\"s1\"");
        json.ShouldContain("\"ReplicaCount\":1");
    }

    [Fact]
    public void ShardInfo_JsonDeserialize_OfSerializedShard_FailsBecauseTheConnectionStringIsNotSerialized()
    {
        // Arrange
        var json = JsonSerializer.Serialize(CreateShardWithReplica());

        // Act & Assert
        Should.Throw<ArgumentException>(() => JsonSerializer.Deserialize<ShardInfo>(json));
    }

    [Fact]
    public void ShardInfo_ConfigurationBinding_ShardList_StillBindsPrimaryAndReplicaConnectionStrings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Shards:0:ShardId"] = "s1",
                ["Shards:0:ConnectionString"] = PrimaryConnectionString,
                ["Shards:0:Weight"] = "2",
                ["Shards:0:IsActive"] = "true",
                ["Shards:0:ReplicaConnectionStrings:0"] = ReplicaConnectionString,
            })
            .Build();

        // Act
        var shards = configuration.GetSection("Shards").Get<List<ShardInfo>>();

        // Assert
        shards.ShouldNotBeNull();
        shards.Count.ShouldBe(1);
        shards[0].ShardId.ShouldBe("s1");
        shards[0].Weight.ShouldBe(2);
        shards[0].ConnectionString.ShouldBe(PrimaryConnectionString);
        shards[0].ReplicaConnectionStrings.ShouldBe([ReplicaConnectionString]);
    }

    #endregion

    #region ShardTierInfo

    private static ShardTierInfo CreateTierInfo(DateTime? lastTransitionAtUtc = null) =>
        new(
            "orders-2026-02",
            ShardTier.Warm,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 1),
            IsReadOnly: true,
            PrimaryConnectionString,
            TierCreatedAtUtc,
            lastTransitionAtUtc);

    [Fact]
    public void ShardTierInfo_ToString_WithConnectionString_MasksTheConnectionString()
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
    public void ShardTierInfo_ToString_WithLastTransition_PrintsTheTransitionTimestamp()
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
    public void ShardTierInfo_StringInterpolation_WithConnectionString_MasksTheConnectionString()
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
    public void ShardTierInfo_JsonSerialize_WithConnectionString_ExcludesTheConnectionString()
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
    public void ShardTierInfo_JsonSerialize_OfTimeBasedOptions_ExcludesTheInitialShardConnectionStrings()
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
    public void ShardTierInfo_JsonDeserialize_OfSerializedTierInfo_FailsBecauseTheConnectionStringIsNotSerialized()
    {
        // Arrange
        var json = JsonSerializer.Serialize(CreateTierInfo());

        // Act & Assert
        Should.Throw<ArgumentException>(() => JsonSerializer.Deserialize<ShardTierInfo>(json));
    }

    [Fact]
    public void ShardTierInfo_ConfigurationBinding_TimeBasedOptionsInitialShards_StillBindsTheConnectionString()
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
                ["TimeBased:InitialShards:0:ConnectionString"] = PrimaryConnectionString,
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
        options.InitialShards[0].ConnectionString.ShouldBe(PrimaryConnectionString);
    }

    #endregion
}
