using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Health;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Messaging.Encryption;

/// <summary>
/// Extension methods for registering message encryption services in DI.
/// </summary>
/// <remarks>
/// <para>
/// Call <see cref="AddEncinaMessageEncryption"/> after registering the base messaging services
/// (e.g., <c>AddEncinaEntityFrameworkCore</c>, <c>AddEncinaDapper</c>, etc.) to enable
/// transparent payload-level encryption for outbox/inbox messages.
/// </para>
/// <para>
/// This method decorates the existing <see cref="IMessageSerializer"/> registration
/// with <see cref="EncryptingMessageSerializer"/>, so plain JSON serialization is
/// transparently wrapped with encryption when enabled.
/// </para>
/// <para>
/// <strong>Registration pattern</strong>:
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config => { ... });
/// services.AddEncinaMessageEncryption(options =>
/// {
///     options.EncryptAllMessages = true;
///     options.DefaultKeyId = "msg-key-2024";
/// });
/// </code>
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers message encryption services and decorates the existing
    /// <see cref="IMessageSerializer"/> with <see cref="EncryptingMessageSerializer"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for <see cref="MessageEncryptionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><see cref="MessageEncryptionOptions"/> via <c>IOptions&lt;MessageEncryptionOptions&gt;</c></item>
    ///   <item><see cref="IMessageEncryptionProvider"/> → <see cref="DefaultMessageEncryptionProvider"/> (TryAdd)</item>
    ///   <item><see cref="ITenantKeyResolver"/> → <see cref="DefaultTenantKeyResolver"/> (TryAdd)</item>
    ///   <item><see cref="IMessageSerializer"/> → decorated with <see cref="EncryptingMessageSerializer"/></item>
    ///   <item>Optionally: <see cref="MessageEncryptionHealthCheck"/> when <c>AddHealthCheck = true</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaMessageEncryption(
        this IServiceCollection services,
        Action<MessageEncryptionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddEncinaMessageEncryptionCore<DefaultMessageEncryptionProvider>(services, configure);
    }

    /// <summary>
    /// Registers message encryption services with a custom <see cref="IMessageEncryptionProvider"/>
    /// and decorates the existing <see cref="IMessageSerializer"/> with <see cref="EncryptingMessageSerializer"/>.
    /// </summary>
    /// <typeparam name="TProvider">The custom encryption provider type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for <see cref="MessageEncryptionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMessageEncryption<TProvider>(
        this IServiceCollection services,
        Action<MessageEncryptionOptions>? configure = null)
        where TProvider : class, IMessageEncryptionProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddEncinaMessageEncryptionCore<TProvider>(services, configure);
    }

    private static IServiceCollection AddEncinaMessageEncryptionCore<TProvider>(
        IServiceCollection services,
        Action<MessageEncryptionOptions>? configure)
        where TProvider : class, IMessageEncryptionProvider
    {
        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<MessageEncryptionOptions>(_ => { });
        }

        // Register encryption provider and tenant key resolver (TryAdd — user can replace)
        services.TryAddSingleton<IMessageEncryptionProvider, TProvider>();
        services.TryAddSingleton<ITenantKeyResolver, DefaultTenantKeyResolver>();

        // Decorate IMessageSerializer with EncryptingMessageSerializer.
        // Captures the existing registration (typically JsonMessageSerializer) as the inner serializer.
        DecorateMessageSerializer(services);

        // Register health check if enabled
        var optionsInstance = new MessageEncryptionOptions();
        configure?.Invoke(optionsInstance);

        if (optionsInstance.AddHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<MessageEncryptionHealthCheck>(
                    MessageEncryptionHealthCheck.DefaultName,
                    tags: MessageEncryptionHealthCheck.Tags);
        }

        return services;
    }

    private static void DecorateMessageSerializer(IServiceCollection services)
    {
        // Find existing IMessageSerializer registration to use as inner
        var existingDescriptor = FindDescriptor(services, typeof(IMessageSerializer));

        if (existingDescriptor is not null)
        {
            services.Remove(existingDescriptor);
        }

        services.AddSingleton<IMessageSerializer>(sp =>
        {
            // Resolve the inner serializer (either from existing registration or default)
            var inner = existingDescriptor is not null
                ? ResolveFromDescriptor(sp, existingDescriptor)
                : new Messaging.Serialization.JsonMessageSerializer();

            var provider = sp.GetRequiredService<IMessageEncryptionProvider>();
            var options = sp.GetRequiredService<IOptions<MessageEncryptionOptions>>();
            var logger = sp.GetRequiredService<ILogger<EncryptingMessageSerializer>>();

            return new EncryptingMessageSerializer(inner, provider, options, logger);
        });
    }

    private static ServiceDescriptor? FindDescriptor(IServiceCollection services, Type serviceType)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == serviceType)
            {
                return services[i];
            }
        }

        return null;
    }

    private static IMessageSerializer ResolveFromDescriptor(
        IServiceProvider serviceProvider,
        ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IMessageSerializer instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (IMessageSerializer)descriptor.ImplementationFactory(serviceProvider);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (IMessageSerializer)ActivatorUtilities.CreateInstance(
                serviceProvider, descriptor.ImplementationType);
        }

        // Fallback to default JSON serializer
        return new Messaging.Serialization.JsonMessageSerializer();
    }
}
