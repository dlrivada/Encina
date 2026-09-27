using System.Data;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using AdoMySql = Encina.ADO.MySQL.ServiceCollectionExtensions;
using AdoPostgreSql = Encina.ADO.PostgreSQL.ServiceCollectionExtensions;
using AdoSqlServer = Encina.ADO.SqlServer.ServiceCollectionExtensions;
using DapperMySql = Encina.Dapper.MySQL.ServiceCollectionExtensions;
using DapperPostgreSql = Encina.Dapper.PostgreSQL.ServiceCollectionExtensions;
using DapperSqlServer = Encina.Dapper.SqlServer.ServiceCollectionExtensions;

namespace Encina.UnitTests.Messaging.Sagas;

/// <summary>
/// Verifies that every ADO.NET and Dapper provider package registers the shared messaging
/// patterns completely, proven by <see cref="ServiceProviderOptions.ValidateOnBuild"/> and
/// <see cref="ServiceProviderOptions.ValidateScopes"/> rather than by resolving a single service
/// with the lenient default provider.
/// </summary>
/// <remarks>
/// <para>
/// AGENTS.md §3 requires that an <c>AddEncina*</c> registration method is proven by a DI test
/// that builds the provider under <see cref="ServiceProviderOptions.ValidateOnBuild"/> and
/// <see cref="ServiceProviderOptions.ValidateScopes"/>. The EF Core and MongoDB packages got such
/// tests when their registrations were fixed to go through the shared
/// <c>AddOutboxInboxSagaSchedulingServices</c> helper (#1333). The six ADO.NET and Dapper
/// packages already call that helper (indirectly, through <c>AddMessagingServices</c>) but their
/// only DI regression test, <c>SagaRunnerDiRegistrationTests</c>, resolved <see cref="ISagaRunner"/>
/// against a default (non-validating) provider and checked nothing else. This class supersedes
/// it: one shared, provider-parameterised theory covers all six providers with the full
/// validation contract (#1398).
/// </para>
/// </remarks>
public sealed class AdoDapperMessagingDiRegistrationTests
{
    /// <summary>
    /// Registers the fake infrastructure every ADO.NET/Dapper provider registration expects the
    /// caller to have already registered: a fake <see cref="IDbConnection"/> (no real database or
    /// Docker involved) and logging (required by <c>SagaRunner</c>, <c>SagaNotFoundDispatcher</c>
    /// and the outbox/scheduled message processors).
    /// </summary>
    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDbConnection>());
        return services;
    }

    [Theory]
    [MemberData(nameof(ProviderCases))]
    public void AddEncinaProvider_WithAllMessagingPatternsEnabled_ValidatesAndResolvesEveryService(
        string providerName,
        Action<IServiceCollection, Action<MessagingConfiguration>> register)
    {
        // Arrange
        var services = CreateServices();

        // Act
        register(services, config =>
        {
            config.UseTransactions = true;
            config.UseOutbox = true;
            config.UseInbox = true;
            config.UseSagas = true;
            config.UseScheduling = true;
        });

        // Assert - the provider builds under ValidateOnBuild/ValidateScopes (proving the DI graph
        // for every enabled messaging pattern is complete for this provider) and every service the
        // shared helper registers is resolvable.
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        provider.GetService<OutboxOptions>().ShouldNotBeNull(providerName);
        provider.GetService<InboxOptions>().ShouldNotBeNull(providerName);
        provider.GetService<SagaOptions>().ShouldNotBeNull(providerName);
        provider.GetService<SchedulingOptions>().ShouldNotBeNull(providerName);

        scope.ServiceProvider.GetRequiredService<IOutboxStore>().ShouldNotBeNull(providerName);
        scope.ServiceProvider.GetRequiredService<IInboxStore>().ShouldNotBeNull(providerName);
        scope.ServiceProvider.GetRequiredService<IScheduledMessageStore>().ShouldNotBeNull(providerName);
        scope.ServiceProvider.GetRequiredService<ISagaRunner>().ShouldNotBeNull(providerName);
        scope.ServiceProvider.GetRequiredService<ISagaNotFoundDispatcher>().ShouldNotBeNull(providerName);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(InboxPipelineBehavior<,>));
    }

    [Theory]
    [MemberData(nameof(ProviderCases))]
    public void AddEncinaProvider_WithUseInboxTrue_RegistersInboxPipelineBehavior(
        string providerName,
        Action<IServiceCollection, Action<MessagingConfiguration>> register)
    {
        // Arrange
        var services = CreateServices();

        // Act
        register(services, config => config.UseInbox = true);

        // Assert - InboxPipelineBehavior is registered as an open generic IPipelineBehavior<,>.
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(InboxPipelineBehavior<,>));
        descriptor.ShouldNotBeNull(providerName);
    }

    [Theory]
    [MemberData(nameof(ProviderCases))]
    public void AddEncinaProvider_WithUseInboxFalse_DoesNotRegisterInboxPipelineBehavior(
        string providerName,
        Action<IServiceCollection, Action<MessagingConfiguration>> register)
    {
        // Arrange
        var services = CreateServices();

        // Act
        register(services, config => config.UseInbox = false);

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(InboxPipelineBehavior<,>));
        descriptor.ShouldBeNull(providerName);
    }

    [Theory]
    [MemberData(nameof(ProviderCases))]
    public void AddEncinaProvider_WithUseSagasTrue_ResolvesSagaRunnerAndNotFoundDispatcher(
        string providerName,
        Action<IServiceCollection, Action<MessagingConfiguration>> register)
    {
        // Arrange
        var services = CreateServices();

        // Act
        register(services, config => config.UseSagas = true);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        // Assert
        var runner = scope.ServiceProvider.GetRequiredService<ISagaRunner>();
        runner.ShouldNotBeNull(providerName);
        runner.ShouldBeOfType<SagaRunner>(providerName);
        scope.ServiceProvider.GetRequiredService<ISagaNotFoundDispatcher>().ShouldNotBeNull(providerName);
    }

    [Theory]
    [MemberData(nameof(ProviderCases))]
    public void AddEncinaProvider_WithUseSagasFalse_DoesNotRegisterSagaRunner(
        string providerName,
        Action<IServiceCollection, Action<MessagingConfiguration>> register)
    {
        // Arrange
        var services = CreateServices();

        // Act
        register(services, config => config.UseSagas = false);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ISagaRunner>().ShouldBeNull(providerName);
        provider.GetService<ISagaNotFoundDispatcher>().ShouldBeNull(providerName);
    }

    public static IEnumerable<object[]> ProviderCases()
    {
        yield return new object[]
        {
            "ADO-SqlServer",
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                AdoSqlServer.AddEncinaADO(services, configure))
        };
        yield return new object[]
        {
            "ADO-PostgreSQL",
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                AdoPostgreSql.AddEncinaADO(services, configure))
        };
        yield return new object[]
        {
            "ADO-MySQL",
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                AdoMySql.AddEncinaADO(services, configure))
        };
        yield return new object[]
        {
            "Dapper-SqlServer",
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                DapperSqlServer.AddEncinaDapper(services, configure))
        };
        yield return new object[]
        {
            "Dapper-PostgreSQL",
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                DapperPostgreSql.AddEncinaDapper(services, configure))
        };
        yield return new object[]
        {
            "Dapper-MySQL",
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                DapperMySql.AddEncinaDapper(services, configure))
        };
    }
}
