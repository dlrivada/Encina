using Encina.RabbitMQ;
using RabbitMQ.Client;

namespace Encina.UnitTests.RabbitMQ.Publishing;

/// <summary>
/// Regression tests for #1608: ending a scope that resolved the scoped publisher must not close
/// the DI-singleton RabbitMQ channel or connection.
/// </summary>
public sealed class RabbitMQPublisherLifetimeTests
{
    [Fact]
    public async Task ResolvingAndDisposingPublisherScope_LeavesSingletonChannelAndConnectionUntouched()
    {
        // Arrange
        var connection = Substitute.For<IConnection>();
        var channel = Substitute.For<IChannel>();
        await using var provider = BuildProvider(connection, channel);

        // Act
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IRabbitMQMessagePublisher>().ShouldNotBeNull();
        }

        // Assert
        await channel.Received(0).CloseAsync(Arg.Any<CancellationToken>());
        await channel.Received(0).DisposeAsync();
        channel.Received(0).Dispose();
        await connection.Received(0).CloseAsync(Arg.Any<CancellationToken>());
        await connection.Received(0).DisposeAsync();
        connection.Received(0).Dispose();
        using var secondScope = provider.CreateScope();
        secondScope.ServiceProvider.GetRequiredService<IChannel>().ShouldBeSameAs(channel);
        secondScope.ServiceProvider.GetRequiredService<IConnection>().ShouldBeSameAs(connection);
        secondScope.ServiceProvider.GetRequiredService<IRabbitMQMessagePublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaRabbitMQ_BuildsWithScopeAndBuildValidation()
    {
        // Arrange
        var connection = Substitute.For<IConnection>();
        var channel = Substitute.For<IChannel>();

        // Act
        var act = () => BuildProvider(connection, channel).Dispose();

        // Assert
        Should.NotThrow(act);
    }

    private static ServiceProvider BuildProvider(IConnection connection, IChannel channel)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(connection);
        services.AddSingleton(channel);
        services.AddEncinaRabbitMQ();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
