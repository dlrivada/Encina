using Azure.Messaging.ServiceBus;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// Azure Service Bus emulator fixture using Testcontainers.
/// Provides a throwaway official Azure Service Bus emulator for the Encina.AzureServiceBus integration and contract tests.
/// </summary>
/// <remarks>
/// <para>
/// Pinned images (issue #2699, decision EULA = a, 2026-10-27): the emulator is
/// <c>mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.1</c> (the newest explicit version tag of 2026-10-27)
/// and its SQL Server sidecar is <c>mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04</c> with
/// <c>MSSQL_PID=Developer</c> set explicitly. No <c>:latest</c> tag is used.
/// </para>
/// <para>
/// Running this fixture accepts the Microsoft Azure Service Bus Emulator Software License Terms (development and
/// testing only, see <see cref="LicenseUrl"/>) through <c>WithAcceptLicenseAgreement(true)</c>, and the SQL Server
/// Developer edition terms (non-production use only). See <c>docs/testing/integration-tests.md</c>.
/// </para>
/// <para>
/// The emulator has no management API: every queue, topic and subscription the tests use is declared in the emulator
/// configuration below (<see cref="EmulatorConfig"/>). One container is shared by every test class of the
/// <c>Cloud-ServiceBusEmulator</c> collection (<see cref="CollectionName"/>); each test assembly declares its own
/// <c>[CollectionDefinition]</c> class. Test classes call <see cref="ClearAllDataAsync"/> in <c>InitializeAsync()</c>
/// and never dispose the fixture.
/// </para>
/// </remarks>
public sealed class ServiceBusEmulatorFixture : IAsyncLifetime
{
    /// <summary>
    /// The name of the shared xUnit collection that owns the emulator container.
    /// </summary>
    public const string CollectionName = "Cloud-ServiceBusEmulator";

    /// <summary>
    /// The pinned Service Bus emulator image.
    /// </summary>
    public const string Image = "mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.1";

    /// <summary>
    /// The pinned SQL Server image the emulator uses as its backing store.
    /// </summary>
    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    /// <summary>
    /// The URL of the Azure Service Bus emulator licence terms accepted by running this fixture.
    /// </summary>
    public const string LicenseUrl = "https://github.com/Azure/azure-service-bus-emulator-installer/blob/main/EMULATOR_EULA.txt";

    /// <summary>
    /// The default queue configured in the emulator (the package default <c>DefaultQueueName</c>).
    /// </summary>
    public const string CommandsQueue = "encina-commands";

    /// <summary>
    /// A second queue, so tests can address an explicit queue name.
    /// </summary>
    public const string OrdersQueue = "orders";

    /// <summary>
    /// A queue reserved for scheduled-message tests, so pending scheduled messages never disturb the others.
    /// </summary>
    public const string ScheduledQueue = "scheduled";

    /// <summary>
    /// The default topic configured in the emulator (the package default <c>DefaultTopicName</c>).
    /// </summary>
    public const string EventsTopic = "encina-events";

    /// <summary>
    /// The subscription of <see cref="EventsTopic"/> that matches the package default <c>SubscriptionName</c>.
    /// </summary>
    public const string DefaultSubscription = "default";

    /// <summary>
    /// A second subscription of <see cref="EventsTopic"/>, to prove a topic publish fans out.
    /// </summary>
    public const string AuditSubscription = "audit";

    /// <summary>
    /// The emulator configuration: the queues, topics and subscriptions the tests need.
    /// </summary>
    public const string EmulatorConfig = """
        {
          "UserConfig": {
            "Namespaces": [
              {
                "Name": "sbemulatorns",
                "Queues": [
                  { "Name": "encina-commands", "Properties": { "MaxDeliveryCount": 10, "LockDuration": "PT1M" } },
                  { "Name": "orders", "Properties": { "MaxDeliveryCount": 10, "LockDuration": "PT1M" } },
                  { "Name": "scheduled", "Properties": { "MaxDeliveryCount": 10, "LockDuration": "PT1M" } }
                ],
                "Topics": [
                  {
                    "Name": "encina-events",
                    "Properties": {},
                    "Subscriptions": [
                      { "Name": "default", "Properties": { "MaxDeliveryCount": 10, "LockDuration": "PT1M" } },
                      { "Name": "audit", "Properties": { "MaxDeliveryCount": 10, "LockDuration": "PT1M" } }
                    ]
                  }
                ]
              }
            ],
            "Logging": { "Type": "File" }
          }
        }
        """;

    private INetwork? _network;
    private MsSqlContainer? _sqlContainer;
    private ServiceBusContainer? _container;
    private ServiceBusClient? _client;
    private string? _configPath;

    /// <summary>
    /// Gets the emulator connection string (AMQP endpoint, development emulator mode).
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the emulator container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && !string.IsNullOrEmpty(ConnectionString);

    /// <summary>
    /// Gets the Service Bus client shared by the collection. The fixture owns it: do not dispose it.
    /// </summary>
    public ServiceBusClient Client => _client ?? throw new InvalidOperationException("The fixture is not initialized.");

    /// <summary>
    /// Creates a Service Bus client configured to use the emulator.
    /// </summary>
    /// <returns>A <see cref="ServiceBusClient"/> instance the caller disposes.</returns>
    public ServiceBusClient CreateClient() => new(ConnectionString);

    /// <summary>
    /// Drains (receive-and-delete) every configured queue and subscription so the next test starts empty.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all entities are empty.</returns>
    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        foreach (var queue in new[] { CommandsQueue, OrdersQueue, ScheduledQueue })
        {
            await DrainAsync(Client.CreateReceiver(queue, ReceiveAndDelete), cancellationToken).ConfigureAwait(false);
        }

        foreach (var subscription in new[] { DefaultSubscription, AuditSubscription })
        {
            await DrainAsync(Client.CreateReceiver(EventsTopic, subscription, ReceiveAndDelete), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _configPath = Path.Combine(Path.GetTempPath(), $"encina-servicebus-emulator-{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(_configPath, EmulatorConfig);

            _network = new NetworkBuilder().Build();
            _sqlContainer = new MsSqlBuilder(SqlServerImage)
                .WithNetwork(_network)
                .WithNetworkAliases(ServiceBusBuilder.DatabaseNetworkAlias)
                .WithEnvironment("MSSQL_PID", "Developer")
                .Build();

            _container = new ServiceBusBuilder(Image)
                .WithAcceptLicenseAgreement(true)
                .WithConfig(_configPath)
                .WithMsSqlContainer(_network, _sqlContainer, ServiceBusBuilder.DatabaseNetworkAlias)
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();

            _client = CreateClient();
        }
        catch
        {
            // A failed start must not leak the temp config file or half-started containers.
            await DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        if (_sqlContainer is not null)
        {
            await _sqlContainer.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DisposeAsync();
        }

        if (_configPath is not null && File.Exists(_configPath))
        {
            File.Delete(_configPath);
        }
    }

    private static ServiceBusReceiverOptions ReceiveAndDelete { get; } =
        new() { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete };

    private static async Task DrainAsync(ServiceBusReceiver receiver, CancellationToken cancellationToken)
    {
        await using (receiver.ConfigureAwait(false))
        {
            while ((await receiver.ReceiveMessagesAsync(100, TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false)).Count > 0)
            {
            }
        }
    }
}
