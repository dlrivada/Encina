using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Health;
using Encina.Cdc.Debezium.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for Debezium <see cref="ServiceCollectionExtensions"/>.
/// Verifies service registration for both HTTP and Kafka modes.
/// </summary>
public sealed class ServiceCollectionExtensionsDebeziumTests
{
    #region AddEncinaCdcDebezium

    /// <summary>
    /// Verifies that AddEncinaCdcDebezium registers the DebeziumCdcOptions singleton.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebezium_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcDebezium(opts =>
        {
            opts.ListenPort = 9090;
        });

        // Assert
        services.ShouldContain(
            d => d.ServiceType == typeof(DebeziumCdcOptions) && d.Lifetime == ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Verifies that AddEncinaCdcDebezium registers the DebeziumCdcHealthCheck.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebezium_ShouldRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcDebezium(_ => { });

        // Assert
        services.ShouldContain(d => d.ServiceType == typeof(DebeziumCdcHealthCheck));
    }

    #endregion

    #region AddEncinaCdcDebeziumKafka

    /// <summary>
    /// Verifies that AddEncinaCdcDebeziumKafka registers the DebeziumKafkaOptions singleton.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebeziumKafka_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcDebeziumKafka(opts =>
        {
            opts.BootstrapServers = "broker:9092";
            opts.Topics = ["topic1"];
        });

        // Assert
        services.ShouldContain(
            d => d.ServiceType == typeof(DebeziumKafkaOptions) && d.Lifetime == ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Verifies that AddEncinaCdcDebeziumKafka registers ICdcConnector.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebeziumKafka_ShouldRegisterConnector()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcDebeziumKafka(opts =>
        {
            opts.Topics = ["topic1"];
        });

        // Assert
        services.ShouldContain(d => d.ServiceType == typeof(ICdcConnector));
    }

    /// <summary>
    /// Verifies that AddEncinaCdcDebeziumKafka registers DebeziumKafkaHealthCheck.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebeziumKafka_ShouldRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcDebeziumKafka(opts =>
        {
            opts.Topics = ["topic1"];
        });

        // Assert
        services.ShouldContain(d => d.ServiceType == typeof(DebeziumKafkaHealthCheck));
    }

    #endregion

    #region Mutual Exclusivity

    /// <summary>
    /// Verifies that HTTP registered first takes precedence over Kafka (TryAddSingleton).
    /// Both register ICdcConnector — first wins.
    /// </summary>
    [Fact]
    public void MutualExclusivity_HttpFirst_ShouldNotRegisterKafkaConnector()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act — register HTTP first, then Kafka
        services.AddEncinaCdcDebezium(_ => { });
        services.AddEncinaCdcDebeziumKafka(opts => { opts.Topics = ["topic1"]; });

        // Assert — only one ICdcConnector registration (HTTP wins)
        var connectorRegistrations = services.Where(d => d.ServiceType == typeof(ICdcConnector)).ToList();
        connectorRegistrations.Count.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that Kafka registered first takes precedence over HTTP (TryAddSingleton).
    /// </summary>
    [Fact]
    public void MutualExclusivity_KafkaFirst_ShouldNotRegisterHttpConnector()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act — register Kafka first, then HTTP
        services.AddEncinaCdcDebeziumKafka(opts => { opts.Topics = ["topic1"]; });
        services.AddEncinaCdcDebezium(_ => { });

        // Assert — only one ICdcConnector registration (Kafka wins)
        var connectorRegistrations = services.Where(d => d.ServiceType == typeof(ICdcConnector)).ToList();
        connectorRegistrations.Count.ShouldBe(1);
    }

    #endregion
}
