using Encina.NATS;
using Encina.Testing;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Encina.UnitTests.NATS.Publishing;

/// <summary>
/// Regression tests for #1608: ending a scope that resolved the scoped publisher must not dispose
/// the DI-singleton NATS connection.
/// </summary>
public sealed class NATSPublisherLifetimeTests
{
    [Fact]
    public async Task ResolvingAndDisposingPublisherScope_LeavesSingletonConnectionUntouched()
    {
        // Arrange
        var connection = Substitute.For<INatsConnection>();
        await using var provider = BuildProvider(connection);

        // Act
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<INATSMessagePublisher>().ShouldNotBeNull();
        }

        // Assert
        await connection.Received(0).DisposeAsync();
        using var secondScope = provider.CreateScope();
        secondScope.ServiceProvider.GetRequiredService<INatsConnection>().ShouldBeSameAs(connection);
        secondScope.ServiceProvider.GetRequiredService<INATSMessagePublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaNATS_BuildsWithScopeAndBuildValidation()
    {
        // Arrange
        var connection = Substitute.For<INatsConnection>();

        // Act
        var act = () => BuildProvider(connection).Dispose();

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public async Task AddEncinaNATS_WithoutJetStream_ResolvesPublisherWithoutJetStreamContext()
    {
        // Arrange
        await using var provider = BuildProvider(Substitute.For<INatsConnection>());
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<INATSMessagePublisher>();

        // Act
        var result = await publisher.JetStreamPublishAsync(new { Value = 1 });

        // Assert
        result.ShouldBeErrorWithCode("NATS_JETSTREAM_NOT_ENABLED");
    }

    [Fact]
    public async Task AddEncinaNATS_WithJetStream_ResolvesPublisherWithTheRegisteredContext()
    {
        // Arrange
        await using var provider = BuildProvider(Substitute.For<INatsConnection>(), useJetStream: true);
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<INATSMessagePublisher>();

        // Act
        var result = await publisher.JetStreamPublishAsync(new { Value = 1 });

        // Assert
        result.Match(_ => string.Empty, error => error.Message).ShouldNotContain("not enabled");
    }

    private static ServiceProvider BuildProvider(INatsConnection connection, bool useJetStream = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(connection);
        if (useJetStream)
        {
            services.AddSingleton(Substitute.For<INatsJSContext>());
        }

        services.AddEncinaNATS(opt =>
        {
            opt.AllowLocalEndpoints = true;
            opt.UseJetStream = useJetStream;
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
