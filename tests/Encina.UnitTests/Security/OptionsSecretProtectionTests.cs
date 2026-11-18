using System.Text.Json;

using Amazon;
using Amazon.Runtime;

using Encina.AzureServiceBus;
using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Kafka;
using Encina.DistributedLock.SqlServer;
using Encina.Messaging.ReadWriteSeparation;
using Encina.MongoDB;
using Encina.MQTT;
using Encina.RabbitMQ;
using Encina.Security.AntiTampering;
using Encina.Security.Secrets.AwsSecretsManager;
using Encina.Security.Secrets.HashiCorpVault;
using Encina.Sharding.Configuration;
using Encina.Tenancy;

using Shouldly;
using VaultSharp.V1.AuthMethods.Token;

namespace Encina.UnitTests.Security;

/// <summary>
/// Verifies that Options classes with sensitive properties are protected
/// against accidental secret leakage via serialization and ToString().
/// See: https://github.com/dlrivada/Encina/issues/851
/// </summary>
public sealed class OptionsSecretProtectionTests
{
    private const string SecretConnectionString = "Server=secret-host;Password=SuperSecret123;";
    private const string SecretPassword = "SuperSecret123";
    private const string SecretToken = "eyJhbGciOiJIUzI1NiJ9.secret";

    // ── ToString() must NOT contain secrets ──

    [Fact]
    public void AzureServiceBusOptions_ToString_ExcludesConnectionString()
    {
        var options = new EncinaAzureServiceBusOptions { ConnectionString = SecretConnectionString };
        var result = options.ToString();

        result.ShouldNotContain("secret-host");
        result.ShouldNotContain("SuperSecret");
        result.ShouldContain("EncinaAzureServiceBusOptions");
    }

    [Fact]
    public void RabbitMQOptions_ToString_ExcludesPassword()
    {
        var options = new EncinaRabbitMQOptions { Password = SecretPassword };
        var result = options.ToString();

        result.ShouldNotContain("SuperSecret");
        result.ShouldContain("EncinaRabbitMQOptions");
        result.ShouldContain("localhost"); // safe: host is OK
    }

    [Fact]
    public void MQTTOptions_ToString_ExcludesCredentials()
    {
        var options = new EncinaMQTTOptions { Username = "admin", Password = SecretPassword };
        var result = options.ToString();

        result.ShouldNotContain("admin");
        result.ShouldNotContain("SuperSecret");
        result.ShouldContain("EncinaMQTTOptions");
    }

    [Fact]
    public void MongoDbOptions_ToString_ExcludesConnectionString()
    {
        var options = new EncinaMongoDbOptions { ConnectionString = SecretConnectionString };
        var result = options.ToString();

        result.ShouldNotContain("secret-host");
        result.ShouldContain("EncinaMongoDbOptions");
    }

    [Fact]
    public void DebeziumCdcOptions_ToString_ExcludesBearerToken()
    {
        var options = new DebeziumCdcOptions { BearerToken = SecretToken };
        var result = options.ToString();

        result.ShouldNotContain("eyJhbGci");
        result.ShouldContain("DebeziumCdcOptions");
    }

    [Fact]
    public void DebeziumKafkaOptions_ToString_ExcludesSaslCredentials()
    {
        var options = new DebeziumKafkaOptions
        {
            SaslUsername = "admin",
            SaslPassword = SecretPassword
        };
        var result = options.ToString();

        result.ShouldNotContain("admin");
        result.ShouldNotContain("SuperSecret");
        result.ShouldContain("DebeziumKafkaOptions");
    }

    [Fact]
    public void SqlServerLockOptions_ToString_ExcludesConnectionString()
    {
        var options = new SqlServerLockOptions { ConnectionString = SecretConnectionString };
        var result = options.ToString();

        result.ShouldNotContain("secret-host");
        result.ShouldContain("SqlServerLockOptions");
    }

    [Fact]
    public void TenancyOptions_ToString_ExcludesConnectionString()
    {
        var options = new TenancyOptions { DefaultConnectionString = SecretConnectionString };
        var result = options.ToString();

        result.ShouldNotContain("secret-host");
        result.ShouldContain("TenancyOptions");
    }

    [Fact]
    public void TenantConnectionOptions_ToString_ExcludesConnectionString()
    {
        var options = new TenantConnectionOptions { DefaultConnectionString = SecretConnectionString };
        var result = options.ToString();

        result.ShouldNotContain("secret-host");
        result.ShouldContain("TenantConnectionOptions");
    }

    [Fact]
    public void ReadWriteSeparationOptions_ToString_ExcludesConnectionStrings()
    {
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = SecretConnectionString,
            ReadConnectionStrings = [SecretConnectionString, "Server=replica2;Password=Secret;"]
        };
        var result = options.ToString();

        result.ShouldNotContain("secret-host");
        result.ShouldNotContain("SuperSecret");
        result.ShouldContain("ReadWriteSeparationOptions");
        result.ShouldContain("Replicas=2");
    }

    [Fact]
    public void ReadWriteSeparationOptions_ToString_HandlesNullReadConnectionStrings()
    {
        var options = new ReadWriteSeparationOptions { ReadConnectionStrings = null! };
        var result = options.ToString();

        result.ShouldContain("Replicas=0");
    }

    [Fact]
    public void TimeBasedShardRouterOptions_ToString_ExcludesTierConnectionStrings()
    {
        var options = new TimeBasedShardRouterOptions
        {
            HotTierConnectionString = SecretConnectionString,
            WarmTierConnectionString = SecretConnectionString,
            ColdTierConnectionString = SecretConnectionString,
            ArchivedTierConnectionString = SecretConnectionString
        };
        var result = options.ToString();

        result.ShouldNotContain("secret-host");
        result.ShouldNotContain("SuperSecret");
        result.ShouldContain("TimeBasedShardRouterOptions");
    }

    [Fact]
    public void AntiTamperingOptions_ToString_ExcludesKeyMaterial()
    {
        var options = new AntiTamperingOptions();
        options.AddKey("key-1", "super-secret-key-material");
        var result = options.ToString();

        result.ShouldNotContain("super-secret");
        result.ShouldContain("AntiTamperingOptions");
        result.ShouldContain("Keys=1");
    }

    [Fact]
    public void AwsSecretsManagerOptions_ToString_ExcludesCredentials()
    {
        var options = new AwsSecretsManagerOptions
        {
            Region = RegionEndpoint.USEast1,
            Credentials = new BasicAWSCredentials("AKIA1234567890", SecretPassword)
        };
        var result = options.ToString();

        result.ShouldNotContain("AKIA1234567890");
        result.ShouldNotContain(SecretPassword);
        result.ShouldContain("AwsSecretsManagerOptions");
        result.ShouldContain("us-east-1");
    }

    [Fact]
    public void HashiCorpVaultOptions_ToString_ExcludesAuthMethod()
    {
        var options = new HashiCorpVaultOptions
        {
            VaultAddress = "https://vault.example.com:8200",
            AuthMethod = new TokenAuthMethodInfo("hvs.super-secret-token")
        };
        var result = options.ToString();

        result.ShouldNotContain("super-secret");
        result.ShouldContain("HashiCorpVaultOptions");
        result.ShouldContain("AuthMethod=***");
    }

    // ── JsonSerializer must NOT include [JsonIgnore] properties ──

    [Fact]
    public void RabbitMQOptions_JsonSerialize_ExcludesPassword()
    {
        var options = new EncinaRabbitMQOptions { Password = SecretPassword };
        var json = JsonSerializer.Serialize(options);

        json.ShouldNotContain("Password");
        json.ShouldNotContain("SuperSecret");
        json.ShouldContain("HostName"); // safe property is present
    }

    [Fact]
    public void AzureServiceBusOptions_JsonSerialize_ExcludesConnectionString()
    {
        var options = new EncinaAzureServiceBusOptions { ConnectionString = SecretConnectionString };
        var json = JsonSerializer.Serialize(options);

        json.ShouldNotContain("ConnectionString");
        json.ShouldNotContain("secret-host");
        json.ShouldContain("DefaultQueueName"); // safe property is present
    }

    [Fact]
    public void ReadWriteSeparationOptions_JsonSerialize_ExcludesConnectionStrings()
    {
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = SecretConnectionString,
            ReadConnectionStrings = [SecretConnectionString]
        };
        var json = JsonSerializer.Serialize(options);

        json.ShouldNotContain("WriteConnectionString");
        json.ShouldNotContain("ReadConnectionStrings");
        json.ShouldNotContain("secret-host");
        json.ShouldContain("ReplicaStrategy"); // safe property is present
    }

    [Fact]
    public void TimeBasedShardRouterOptions_JsonSerialize_ExcludesTierConnections()
    {
        var options = new TimeBasedShardRouterOptions
        {
            HotTierConnectionString = SecretConnectionString,
            WarmTierConnectionString = SecretConnectionString,
            ColdTierConnectionString = SecretConnectionString,
            ArchivedTierConnectionString = SecretConnectionString
        };
        var json = JsonSerializer.Serialize(options);

        json.ShouldNotContain("HotTierConnectionString");
        json.ShouldNotContain("WarmTierConnectionString");
        json.ShouldNotContain("ColdTierConnectionString");
        json.ShouldNotContain("ArchivedTierConnectionString");
        json.ShouldNotContain("secret-host");
        json.ShouldContain("ShardIdPrefix"); // safe property is present
    }

    [Fact]
    public void MongoDbOptions_JsonSerialize_ExcludesConnectionString()
    {
        var options = new EncinaMongoDbOptions { ConnectionString = SecretConnectionString };
        var json = JsonSerializer.Serialize(options);

        json.ShouldNotContain("ConnectionString");
        json.ShouldNotContain("secret-host");
        json.ShouldContain("DatabaseName"); // safe property is present
    }

    [Fact]
    public void AwsSecretsManagerOptions_JsonSerialize_ExcludesCredentials()
    {
        var options = new AwsSecretsManagerOptions
        {
            Region = RegionEndpoint.USEast1,
            Credentials = new BasicAWSCredentials("AKIA1234567890", SecretPassword)
        };
        var json = JsonSerializer.Serialize(options);

        json.ShouldNotContain("Credentials");
        json.ShouldNotContain("AKIA1234567890");
        json.ShouldNotContain(SecretPassword);
        json.ShouldContain("Region");
    }

    [Fact]
    public void HashiCorpVaultOptions_JsonSerialize_ExcludesAuthMethod()
    {
        var options = new HashiCorpVaultOptions
        {
            VaultAddress = "https://vault.example.com:8200",
            AuthMethod = new TokenAuthMethodInfo("hvs.super-secret-token")
        };
        var json = JsonSerializer.Serialize(options);

        json.ShouldNotContain("AuthMethod");
        json.ShouldNotContain("super-secret");
        json.ShouldContain("VaultAddress");
    }
}
