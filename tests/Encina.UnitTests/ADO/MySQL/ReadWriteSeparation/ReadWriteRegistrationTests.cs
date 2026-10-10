using System.Data;
using System.Data.Common;
using Encina.ADO.MySQL;
using Encina.ADO.MySQL.ReadWriteSeparation;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Testing.Shouldly;

namespace Encina.UnitTests.ADO.MySQL.ReadWriteSeparation;

/// <summary>
/// Verifies that <c>UseReadWriteSeparation</c> registers the routing behavior and the read/write services
/// on the ADO.NET MySQL provider, built with <c>ValidateOnBuild</c> and <c>ValidateScopes</c>
/// (#2029, AGENTS.md section 3).
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReadWriteRegistrationTests
{
    private const string WriteConnectionString = "Server=primary;Database=test;";
    private const string ReadConnectionString = "Server=replica;Database=test;";

    private static ServiceProvider Build(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDbConnection>(Substitute.For<DbConnection>());
        return services;
    }

    [Fact]
    public void AddEncinaADO_WithReadWriteSeparation_ResolvesBehaviorAndServicesFromAScope()
    {
        var services = NewServices();

        services.AddEncinaADO(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
            config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(ReadConnectionString);
        });

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IReadWriteConnectionFactory>().ShouldBeOfType<ReadWriteConnectionFactory>();
        sp.GetRequiredService<IReadWriteConnectionSelector>().ShouldBeOfType<ReadWriteConnectionSelector>();
        sp.GetRequiredService<IReplicaSelector>().ShouldBeOfType<RoundRobinReplicaSelector>();
        sp.GetRequiredService<ReadWriteSeparationOptions>().WriteConnectionString.ShouldBe(WriteConnectionString);
        sp.GetServices<IPipelineBehavior<RwCommand, string>>()
            .ShouldContain(b => b is ReadWriteRoutingPipelineBehavior<RwCommand, string>);
        sp.GetServices<IPipelineBehavior<RwQuery, string>>()
            .ShouldContain(b => b is ReadWriteRoutingPipelineBehavior<RwQuery, string>);
        sp.GetServices<IEncinaHealthCheck>().ShouldContain(h => h is ReadWriteSeparationHealthCheck);
    }

    [Fact]
    public void AddEncinaADO_WithReadWriteSeparationAndNoReplicas_FallsBackToThePrimaryWithoutAReplicaSelector()
    {
        var services = NewServices();

        services.AddEncinaADO(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
        });

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IReadWriteConnectionSelector>().ShouldBeOfType<ReadWriteConnectionSelector>();
        sp.GetService<IReplicaSelector>().ShouldBeNull();
        sp.GetRequiredService<IReadWriteConnectionFactory>().GetReadConnectionString().ShouldBeRight()
            .ShouldBe(WriteConnectionString);
    }

    [Fact]
    public void AddEncinaADO_WithoutReadWriteSeparation_RegistersNothing()
    {
        var services = NewServices();

        services.AddEncinaADO(config => config.UseOutbox = true);

        using var provider = Build(services);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetService<IReadWriteConnectionFactory>().ShouldBeNull();
        sp.GetService<IReadWriteConnectionSelector>().ShouldBeNull();
        sp.GetService<ReadWriteSeparationOptions>().ShouldBeNull();
        sp.GetServices<IPipelineBehavior<RwCommand, string>>()
            .ShouldNotContain(b => b is ReadWriteRoutingPipelineBehavior<RwCommand, string>);
        sp.GetServices<IEncinaHealthCheck>().ShouldNotContain(h => h is ReadWriteSeparationHealthCheck);
    }

    [Fact]
    public void AddEncinaADO_CalledTwice_RegistersTheBehaviorAndTheHealthCheckOnce()
    {
        var services = NewServices();
        Action<MessagingConfiguration> configure = config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
        };

        services.AddEncinaADO(configure);
        services.AddEncinaADO(configure);

        services.Count(d => d.ServiceType == typeof(IPipelineBehavior<,>)
                && d.ImplementationType == typeof(ReadWriteRoutingPipelineBehavior<,>)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IEncinaHealthCheck)
                && d.ImplementationType == typeof(ReadWriteSeparationHealthCheck)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IReadWriteConnectionFactory)).ShouldBe(1);
    }

    private sealed record RwCommand : ICommand<string>;

    private sealed record RwQuery : IQuery<string>;
}
