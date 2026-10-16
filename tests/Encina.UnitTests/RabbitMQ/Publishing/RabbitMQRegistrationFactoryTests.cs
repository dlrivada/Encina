using Encina.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Encina.UnitTests.RabbitMQ.Publishing;

/// <summary>
/// Tests the connection and channel factories registered by <c>AddEncinaRabbitMQ</c> without a broker.
/// </summary>
public sealed class RabbitMQRegistrationFactoryTests
{
    [Fact]
    public void AddEncinaRabbitMQ_ConnectionFactory_WithNoBroker_FailsWhenResolved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRabbitMQ(o =>
        {
            o.HostName = "127.0.0.1";
            o.Port = 1;
        });

        using var provider = services.BuildServiceProvider();

        Should.Throw<BrokerUnreachableException>(() => provider.GetRequiredService<IConnection>());
    }

    [Fact]
    public async Task AddEncinaRabbitMQ_ChannelFactory_CreatesTheChannelFromTheRegisteredConnection()
    {
        var channel = Substitute.For<IChannel>();
        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(channel));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(connection);
        services.AddEncinaRabbitMQ();

        await using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IChannel>().ShouldBeSameAs(channel);
    }
}
