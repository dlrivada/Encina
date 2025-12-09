using System.Collections;
using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SimpleMediator.Tests.Fixtures;
using static LanguageExt.Prelude;

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
    public void RegisterServicesFromAssembly_ThrowsWhenAssemblyIsNull()
    {
        var configuration = new SimpleMediatorConfiguration();

        var exception = Should.Throw<ArgumentNullException>(() => configuration.RegisterServicesFromAssembly(null!));
        exception.ParamName.ShouldBe("assembly");
    }

    [Fact]
    public void RegisterServicesFromAssemblies_ReturnsSameInstanceWhenArrayIsNull()
    {
        var configuration = new SimpleMediatorConfiguration();

        var result = configuration.RegisterServicesFromAssemblies(null!);

        ReferenceEquals(configuration, result).ShouldBeTrue();
    }

    [Fact]
    public void AddPipelineBehavior_ThrowsForInvalidType()
    {
        var configuration = new SimpleMediatorConfiguration();

        var invalidImplementation = Should.Throw<ArgumentException>(() => configuration.AddPipelineBehavior(typeof(NotABehavior)));
        invalidImplementation.ParamName.ShouldBe("pipelineBehaviorType");
        invalidImplementation.Message.ShouldContain("does not implement IPipelineBehavior<,>.");

        var abstractType = Should.Throw<ArgumentException>(() => configuration.AddPipelineBehavior(typeof(AbstractBehavior)));
        abstractType.ParamName.ShouldBe("pipelineBehaviorType");
        abstractType.Message.ShouldContain("must be a concrete class type.");
    }

    [Fact]
    public void AddPipelineBehavior_ThrowsForNullType()
    {
        var configuration = new SimpleMediatorConfiguration();

        Should.Throw<ArgumentNullException>(() => configuration.AddPipelineBehavior(null!));
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
    public void AddPipelineBehavior_RegistersQueryPipelineInterfaces()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddPipelineBehavior(typeof(QueryActivityPipelineBehavior<,>));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredPipelineBehaviors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IPipelineBehavior<,>) && d.ImplementationType == typeof(QueryActivityPipelineBehavior<,>));
        services.ShouldContain(d => d.ServiceType == typeof(IQueryPipelineBehavior<,>) && d.ImplementationType == typeof(QueryActivityPipelineBehavior<,>));
    }

    [Fact]
    public void AddPipelineBehavior_RegistersOpenGenericServiceType()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddPipelineBehavior(typeof(OpenGenericPipelineBehavior<,>));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredPipelineBehaviors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IPipelineBehavior<,>) && d.ImplementationType == typeof(OpenGenericPipelineBehavior<,>));
    }

    [Fact]
    public void AddPipelineBehavior_DoesNotDuplicateRegistrations()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddPipelineBehavior(typeof(OpenGenericPipelineBehavior<,>));
        configuration.AddPipelineBehavior(typeof(OpenGenericPipelineBehavior<,>));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredPipelineBehaviors", services);

        services.Count(d => d.ImplementationType == typeof(OpenGenericPipelineBehavior<,>)).ShouldBe(1);
    }

    [Fact]
    public void RegisterConfiguredPipelineBehaviors_ThrowsWhenServicesIsNull()
    {
        var configuration = new SimpleMediatorConfiguration();
        var method = typeof(SimpleMediatorConfiguration)
            .GetMethod("RegisterConfiguredPipelineBehaviors", BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();

        var exception = Should.Throw<TargetInvocationException>(() => method!.Invoke(configuration, new object?[] { null }));
        var argumentNull = exception.InnerException.ShouldBeOfType<ArgumentNullException>();
        argumentNull.ParamName.ShouldBe("services");
    }

    [Fact]
    public void RegisterConfiguredPipelineBehaviors_UsesGenericFallbackWhenInterfaceNotFound()
    {
        var configuration = new SimpleMediatorConfiguration();
        GetMutablePipelineTypes(configuration).Add(typeof(NotABehavior));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredPipelineBehaviors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IPipelineBehavior<,>) && d.ImplementationType == typeof(NotABehavior));
    }

    [Fact]
    public void RegisterConfiguredPipelineBehaviors_IgnoresUnrelatedGenericInterfaces()
    {
        var configuration = new SimpleMediatorConfiguration();
        GetMutablePipelineTypes(configuration).Add(typeof(DifferentGenericComponent));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredPipelineBehaviors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IPipelineBehavior<,>) && d.ImplementationType == typeof(DifferentGenericComponent));
    }

    [Fact]
    public void AddRequestPreProcessor_ThrowsForInvalidType()
    {
        var configuration = new SimpleMediatorConfiguration();

        var invalidImplementation = Should.Throw<ArgumentException>(() => configuration.AddRequestPreProcessor(typeof(NotAPreProcessor)));
        invalidImplementation.ParamName.ShouldBe("processorType");
        invalidImplementation.Message.ShouldContain("does not implement IRequestPreProcessor<>.");

        var abstractType = Should.Throw<ArgumentException>(() => configuration.AddRequestPreProcessor(typeof(AbstractPreProcessor)));
        abstractType.ParamName.ShouldBe("processorType");
        abstractType.Message.ShouldContain("must be a concrete class type.");
    }

    [Fact]
    public void AddRequestPreProcessor_ThrowsForNullType()
    {
        var configuration = new SimpleMediatorConfiguration();

        Should.Throw<ArgumentNullException>(() => configuration.AddRequestPreProcessor(null!));
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
    public void AddRequestPreProcessor_RegistersOpenGenericInterface()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddRequestPreProcessor(typeof(OpenGenericPreProcessor<>));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredRequestPreProcessors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IRequestPreProcessor<>) && d.ImplementationType == typeof(OpenGenericPreProcessor<>));
    }

    [Fact]
    public void AddRequestPreProcessor_DoesNotDuplicateRegistrations()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddRequestPreProcessor(typeof(OpenGenericPreProcessor<>));
        configuration.AddRequestPreProcessor(typeof(OpenGenericPreProcessor<>));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredRequestPreProcessors", services);

        services.Count(d => d.ImplementationType == typeof(OpenGenericPreProcessor<>)).ShouldBe(1);
    }

    [Fact]
    public void RegisterConfiguredRequestPreProcessors_ThrowsWhenServicesIsNull()
    {
        var configuration = new SimpleMediatorConfiguration();
        var method = typeof(SimpleMediatorConfiguration)
            .GetMethod("RegisterConfiguredRequestPreProcessors", BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();

        var exception = Should.Throw<TargetInvocationException>(() => method!.Invoke(configuration, new object?[] { null }));
        var argumentNull = exception.InnerException.ShouldBeOfType<ArgumentNullException>();
        argumentNull.ParamName.ShouldBe("services");
    }

    [Fact]
    public void RegisterConfiguredRequestPreProcessors_UsesGenericFallbackForOpenNestedTypes()
    {
        var configuration = new SimpleMediatorConfiguration();
        GetMutablePreProcessorTypes(configuration).Add(typeof(GenericContainer<>.NestedPreProcessor));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredRequestPreProcessors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IRequestPreProcessor<>) && d.ImplementationType == typeof(GenericContainer<>.NestedPreProcessor));
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
    public void AddRequestPostProcessor_ThrowsForInvalidType()
    {
        var configuration = new SimpleMediatorConfiguration();

        var invalidImplementation = Should.Throw<ArgumentException>(() => configuration.AddRequestPostProcessor(typeof(NotAPostProcessor)));
        invalidImplementation.ParamName.ShouldBe("processorType");
        invalidImplementation.Message.ShouldContain("does not implement IRequestPostProcessor<,>.");

        var abstractType = Should.Throw<ArgumentException>(() => configuration.AddRequestPostProcessor(typeof(AbstractPostProcessor)));
        abstractType.ParamName.ShouldBe("processorType");
        abstractType.Message.ShouldContain("must be a concrete class type.");
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

    [Fact]
    public void RegisterConfiguredRequestPostProcessors_ThrowsWhenServicesIsNull()
    {
        var configuration = new SimpleMediatorConfiguration();
        var method = typeof(SimpleMediatorConfiguration)
            .GetMethod("RegisterConfiguredRequestPostProcessors", BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();

        var exception = Should.Throw<TargetInvocationException>(() => method!.Invoke(configuration, new object?[] { null }));
        var argumentNull = exception.InnerException.ShouldBeOfType<ArgumentNullException>();
        argumentNull.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddRequestPostProcessor_RegistersOpenGenericInterface()
    {
        var configuration = new SimpleMediatorConfiguration();
        configuration.AddRequestPostProcessor(typeof(OpenGenericPostProcessor<,>));
        var services = new ServiceCollection();

        InvokeInternal(configuration, "RegisterConfiguredRequestPostProcessors", services);

        services.ShouldContain(d => d.ServiceType == typeof(IRequestPostProcessor<,>) && d.ImplementationType == typeof(OpenGenericPostProcessor<,>));
    }

    [Fact]
    public void AddRequestPostProcessor_ThrowsForNullType()
    {
        var configuration = new SimpleMediatorConfiguration();

        Should.Throw<ArgumentNullException>(() => configuration.AddRequestPostProcessor(null!));
    }

    [Fact]
    public void RegisterServicesFromAssemblyContaining_AddsAssembly()
    {
        var configuration = new SimpleMediatorConfiguration();

        configuration.RegisterServicesFromAssemblyContaining<SimpleMediatorConfigurationTests>();

        GetAssemblies(configuration).ShouldContain(typeof(SimpleMediatorConfigurationTests).Assembly);
    }

    [Fact]
    public void ResolveServiceType_ReturnsGenericInterfaceWhenNoMatchingInterface()
    {
        var resolved = InvokeResolveServiceType(typeof(NotABehavior), typeof(IPipelineBehavior<,>));

        resolved.ShouldBe(typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void ResolveServiceType_IgnoresNonGenericInterfaces()
    {
        var resolved = InvokeResolveServiceType(typeof(DisposablePipelineBehavior), typeof(IPipelineBehavior<,>));

        resolved.ShouldBe(typeof(IPipelineBehavior<PingCommand, string>));
    }

    [Fact]
    public void ResolveServiceType_ReturnsGenericInterfaceWhenInterfaceIsOpen()
    {
        var resolved = InvokeResolveServiceType(typeof(GenericContainer<>.NestedPreProcessor), typeof(IRequestPreProcessor<>));

        resolved.ShouldBe(typeof(IRequestPreProcessor<>));
    }

    [Fact]
    public void NestedPreProcessor_ExposesInterfaceWithGenericParameters()
    {
        var interfaces = typeof(GenericContainer<>.NestedPreProcessor)
            .GetInterfaces()
            .Where(i => i.IsGenericType)
            .ToArray();

        interfaces.ShouldContain(i => i.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>));
        var interfaceType = interfaces.Single(i => i.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>));
        interfaceType.ContainsGenericParameters.ShouldBeTrue();
        interfaceType.ShouldNotBe(typeof(IRequestPreProcessor<>));
    }

    [Fact]
    public void IsAssignableFromGeneric_ReturnsTrueForOpenGenericDefinitions()
    {
        var result = typeof(IPipelineBehavior<,>).IsAssignableFromGeneric(typeof(OpenGenericPipelineBehavior<,>));

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAssignableFromGeneric_IgnoresNonGenericInterfaces()
    {
        var result = typeof(IPipelineBehavior<,>).IsAssignableFromGeneric(typeof(DisposablePipelineBehavior));

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAssignableFromGeneric_ReturnsFalseForUnrelatedTypes()
    {
        var result = typeof(IRequestPreProcessor<>).IsAssignableFromGeneric(typeof(NotAPostProcessor));

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAssignableFromGeneric_ReturnsFalseWhenTypeImplementsDifferentGenericInterface()
    {
        var result = typeof(IPipelineBehavior<,>).IsAssignableFromGeneric(typeof(DifferentGenericComponent));

        result.ShouldBeFalse();
    }

    private static Type InvokeResolveServiceType(Type implementationType, Type genericInterface)
    {
        var method = typeof(SimpleMediatorConfiguration)
            .GetMethod("ResolveServiceType", BindingFlags.Static | BindingFlags.NonPublic);
        method.ShouldNotBeNull();

        return (Type)method!.Invoke(null, new object[] { implementationType, genericInterface })!;
    }

    private static IReadOnlyCollection<Assembly> GetAssemblies(SimpleMediatorConfiguration configuration)
    {
        var property = typeof(SimpleMediatorConfiguration)
            .GetProperty("Assemblies", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return (IReadOnlyCollection<Assembly>)(property?.GetValue(configuration) ?? System.Array.Empty<Assembly>());
    }

    private static IList<Type> GetMutablePipelineTypes(SimpleMediatorConfiguration configuration)
    {
        var field = typeof(SimpleMediatorConfiguration)
            .GetField("_pipelineBehaviorTypes", BindingFlags.Instance | BindingFlags.NonPublic);
        field.ShouldNotBeNull();

        return (IList<Type>)field!.GetValue(configuration)!;
    }

    private static IList<Type> GetMutablePreProcessorTypes(SimpleMediatorConfiguration configuration)
    {
        var field = typeof(SimpleMediatorConfiguration)
            .GetField("_requestPreProcessorTypes", BindingFlags.Instance | BindingFlags.NonPublic);
        field.ShouldNotBeNull();

        return (IList<Type>)field!.GetValue(configuration)!;
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
        public ValueTask<Either<MediatorError, string>> Handle(PingCommand request, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
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
        public Task Process(PingCommand request, Either<MediatorError, string> response, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class ConfiguredPreProcessor : IRequestPreProcessor<PingCommand>
    {
        public Task Process(PingCommand request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class NotAPostProcessor
    {
    }

    private abstract class AbstractPostProcessor : IRequestPostProcessor<PingCommand, string>
    {
        public Task Process(PingCommand request, Either<MediatorError, string> response, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class OpenGenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class OpenGenericPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    {
        public Task Process(TRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class OpenGenericPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    {
        public Task Process(TRequest request, Either<MediatorError, TResponse> response, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class DisposablePipelineBehavior : IPipelineBehavior<PingCommand, string>, IDisposable
    {
        public ValueTask<Either<MediatorError, string>> Handle(PingCommand request, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();

        public void Dispose()
        {
        }
    }

    private sealed class GenericContainer<T>
    {
        internal sealed class NestedPreProcessor : IRequestPreProcessor<T>
        {
            public Task Process(T request, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }
    }

    private sealed class DifferentGenericComponent : IEnumerable<string>
    {
        public IEnumerator<string> GetEnumerator()
            => Enumerable.Empty<string>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
