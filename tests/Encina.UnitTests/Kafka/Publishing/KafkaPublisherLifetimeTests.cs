using Confluent.Kafka;
using Encina.Kafka;

namespace Encina.UnitTests.Kafka.Publishing;

/// <summary>
/// Regression tests for #1608: ending a scope that resolved the scoped publisher must not flush,
/// dispose or otherwise touch the DI-singleton producer.
/// </summary>
public sealed class KafkaPublisherLifetimeTests
{
    [Fact]
    public void ResolvingAndDisposingPublisherScope_LeavesSingletonProducerUntouched()
    {
        // Arrange
        var producer = Substitute.For<IProducer<string, byte[]>>();
        using var provider = BuildProvider(producer);

        // Act
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IKafkaMessagePublisher>().ShouldNotBeNull();
        }

        // Assert
        producer.Received(0).Flush(Arg.Any<TimeSpan>());
        producer.Received(0).Dispose();
        using var secondScope = provider.CreateScope();
        secondScope.ServiceProvider.GetRequiredService<IProducer<string, byte[]>>().ShouldBeSameAs(producer);
        secondScope.ServiceProvider.GetRequiredService<IKafkaMessagePublisher>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaKafka_BuildsWithScopeAndBuildValidation()
    {
        // Arrange
        var producer = Substitute.For<IProducer<string, byte[]>>();

        // Act
        var act = () => BuildProvider(producer).Dispose();

        // Assert
        Should.NotThrow(act);
    }

    private static ServiceProvider BuildProvider(IProducer<string, byte[]> producer)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(producer);
        services.AddEncinaKafka();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
