using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.ContractTests;

public sealed class ConfigurationEdgeCaseContracts
{
    [Fact]
    public void AddEncina_WithExplicitAssemblies_RegistersHandlersFromAllSources()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(global::Encina.Encina).Assembly, typeof(ConfigurationEdgeCaseContracts).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::Encina.IRequestHandler<TestCommand, string>)
            && ImplementationMatches(d, typeof(TestCommandHandler))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncina_IgnoresDuplicateAssemblies()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(ConfigurationEdgeCaseContracts).Assembly, typeof(ConfigurationEdgeCaseContracts).Assembly);

        var descriptors = services
            .Where(d => d.ServiceType == typeof(global::Encina.IRequestHandler<TestCommand, string>))
            .ToList();

        descriptors.Count.ShouldBe(1, "Handlers should not be registered multiple times when assemblies repeat.");
    }

    [Fact]
    public void AddEncina_WithNoAssemblies_FallsBackToDefaults()
    {
        var services = new ServiceCollection();

        services.AddEncina(System.Array.Empty<Assembly>());

        var pipelineDescriptors = services.Where(IsPipelineDescriptor).ToList();
        pipelineDescriptors.Count.ShouldBe(4, "Default pipeline behaviors should remain intact when no assemblies are provided.");
    }

    private static bool ImplementationMatches(ServiceDescriptor descriptor, Type candidate)
    {
        return descriptor.ImplementationType == candidate
               || descriptor.ImplementationInstance?.GetType() == candidate;
    }

    private static bool IsPipelineDescriptor(ServiceDescriptor descriptor)
    {
        return descriptor.ServiceType.IsGenericType
               && descriptor.ServiceType.GetGenericTypeDefinition() == typeof(global::Encina.IPipelineBehavior<,>);
    }

    private sealed record TestCommand(string Payload) : global::Encina.ICommand<string>;

    private sealed class TestCommandHandler : global::Encina.ICommandHandler<TestCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>(request.Payload));
        }
    }
}
