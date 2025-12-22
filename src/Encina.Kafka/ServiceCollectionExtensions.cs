using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Kafka;

/// <summary>
/// Extension methods for configuring Encina Kafka integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Kafka integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaKafka(
        this IServiceCollection services,
        Action<EncinaKafkaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaKafkaOptions();
        configure?.Invoke(options);

        services.Configure<EncinaKafkaOptions>(opt =>
        {
            opt.BootstrapServers = options.BootstrapServers;
            opt.GroupId = options.GroupId;
            opt.DefaultCommandTopic = options.DefaultCommandTopic;
            opt.DefaultEventTopic = options.DefaultEventTopic;
            opt.AutoOffsetReset = options.AutoOffsetReset;
            opt.EnableAutoCommit = options.EnableAutoCommit;
            opt.Acks = options.Acks;
            opt.EnableIdempotence = options.EnableIdempotence;
            opt.MessageTimeoutMs = options.MessageTimeoutMs;
        });

        services.TryAddSingleton<IProducer<string, byte[]>>(sp =>
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                Acks = options.Acks switch
                {
                    "all" => Confluent.Kafka.Acks.All,
                    "none" => Confluent.Kafka.Acks.None,
                    "leader" => Confluent.Kafka.Acks.Leader,
                    _ => Confluent.Kafka.Acks.All
                },
                EnableIdempotence = options.EnableIdempotence,
                MessageTimeoutMs = options.MessageTimeoutMs
            };

            return new ProducerBuilder<string, byte[]>(producerConfig).Build();
        });

        services.TryAddScoped<IKafkaMessagePublisher, KafkaMessagePublisher>();

        return services;
    }
}
