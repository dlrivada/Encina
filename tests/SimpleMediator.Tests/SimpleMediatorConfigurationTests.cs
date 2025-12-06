using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SimpleMediator;
using SimpleMediator.Tests.Fixtures;

namespace SimpleMediator.Tests;

public sealed class SimpleMediatorConfigurationTests
{
    [Fact]
    public void WithHandlerLifetime_UpdatesConfiguration()
    {
        var configuration = new SimpleMediatorConfiguration();

        configuration.WithHandlerLifetime(ServiceLifetime.Singleton);

        configuration.HandlerLifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegisterServicesFromAssemblies_IgnoresNullEntries()
    {
        var configuration = new SimpleMediatorConfiguration();

        configuration.RegisterServicesFromAssemblies(null!, typeof(PingCommand).Assembly, null!);

        var assemblies = GetAssemblies(configuration);
        assemblies.ShouldContain(typeof(PingCommand).Assembly);
        assemblies.Count.ShouldBe(1);
    }

    [Fact]
    public void AddPipelineBehavior_ThrowsForInvalidType()
    {
        var configuration = new SimpleMediatorConfiguration();

        Should.Throw<ArgumentException>(() => configuration.AddPipelineBehavior(typeof(NotABehavior)));
        Should.Throw<ArgumentException>(() => configuration.AddPipelineBehavior(typeof(AbstractBehavior)));
    }

    [Fact]
    public void AddPipelineBehavior_RegistersSpecializedInterfaces()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddPipelineBehavior(typeof(CommandActivityPipelineBehavior<,>));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredPipelineBehaviors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IPipelineBehavior<,>) && d.ImplementationType == typeof(CommandActivityPipelineBehavior<,>));
        services.ShouldContain(d => d.ServiceType == typeof(ICommandPipelineBehavior<,>) && d.ImplementationType == typeof(CommandActivityPipelineBehavior<,>));
    }

    [Fact]
    public void AddRequestPreProcessor_ThrowsForInvalidType()
    {
        var configuration = new SimpleMediatorConfiguration();

        Should.Throw<ArgumentException>(() => configuration.AddRequestPreProcessor(typeof(NotAPreProcessor)));
        Should.Throw<ArgumentException>(() => configuration.AddRequestPreProcessor(typeof(AbstractPreProcessor)));
    }

    [Fact]
    public void AddRequestPreProcessor_RegistersConcreteInterface()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddRequestPreProcessor(typeof(ConfiguredPreProcessor));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredRequestPreProcessors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IRequestPreProcessor<PingCommand>) && d.ImplementationType == typeof(ConfiguredPreProcessor));
    }

    [Fact]
    public void AddRequestPostProcessor_RegistersConcreteInterfaces()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddRequestPostProcessor(typeof(ConfiguredPostProcessor));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredRequestPostProcessors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IRequestPostProcessor<PingCommand, string>) && d.ImplementationType == typeof(ConfiguredPostProcessor));
    }

    [Fact]
    public void AddRequestPostProcessor_DoesNotDuplicateRegistrations()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddRequestPostProcessor(typeof(ConfiguredPostProcessor));
        configuration.AddRequestPostProcessor(typeof(ConfiguredPostProcessor));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredRequestPostProcessors", services);

        services.Count(d => d.ImplementationType == typeof(ConfiguredPostProcessor)).ShouldBe(1);
    }

    private static IReadOnlyCollection<Assembly> GetAssemblies(SimpleMediatorConfiguration configuration)
    {
        var property = typeof(SimpleMediatorConfiguration)
            .GetProperty("Assemblies", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return (IReadOnlyCollection<Assembly>)(property?.GetValue(configuration) ?? Array.Empty<Assembly>());
    }

    private static void InvokeInternal(SimpleMediatorConfiguration configuration, string methodName, IServiceCollection services)
    {
        var method = typeof(SimpleMediatorConfiguration)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();
        method.Invoke(configuration, new object[] { services });
    }

    private sealed class NotABehavior
    {
    }

    private abstract class AbstractBehavior : IPipelineBehavior<PingCommand, string>
    {
        public Task<string> Handle(PingCommand request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
            => next();
    }

    private sealed class NotAPreProcessor
    {
    }

    private abstract class AbstractPreProcessor : IRequestPreProcessor<PingCommand>
    {
        public Task Process(PingCommand request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class ConfiguredPostProcessor : IRequestPostProcessor<PingCommand, string>
    {
        public Task Process(PingCommand request, string response, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class ConfiguredPreProcessor : IRequestPreProcessor<PingCommand>
    {
        public Task Process(PingCommand request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
