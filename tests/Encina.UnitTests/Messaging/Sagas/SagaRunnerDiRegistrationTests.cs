using System.Data;
using Encina.Messaging;
using Encina.Messaging.Sagas.LowCeremony;
using Microsoft.Extensions.DependencyInjection;

using AdoSqlServer = Encina.ADO.SqlServer.ServiceCollectionExtensions;
using AdoPostgreSql = Encina.ADO.PostgreSQL.ServiceCollectionExtensions;
using AdoMySql = Encina.ADO.MySQL.ServiceCollectionExtensions;
using DapperSqlServer = Encina.Dapper.SqlServer.ServiceCollectionExtensions;
using DapperPostgreSql = Encina.Dapper.PostgreSQL.ServiceCollectionExtensions;
using DapperMySql = Encina.Dapper.MySQL.ServiceCollectionExtensions;

namespace Encina.UnitTests.Messaging.Sagas;

/// <summary>
/// Verifies that every ADO.NET and Dapper provider package can resolve <see cref="ISagaRunner"/>
/// once <see cref="MessagingConfiguration.UseSagas"/> is enabled.
/// </summary>
/// <remarks>
/// Before #1163, <c>SagaRunner</c> took a constructor-injected <c>IRequestContext</c>, which is
/// never registered in DI. Every one of these six provider packages calls the shared
/// <c>AddMessagingServices</c> helper with <c>services.AddScoped&lt;ISagaRunner, SagaRunner&gt;()</c>,
/// so resolving <see cref="IServiceProvider.GetService"/> for it threw
/// <see cref="InvalidOperationException"/> at first use. The fix makes <c>SagaRunner</c> depend on
/// <see cref="IRequestContextAccessor"/> instead, which the shared helper now registers.
/// </remarks>
public sealed class SagaRunnerDiRegistrationTests
{
    [Theory]
    [MemberData(nameof(SagaRunnerCases))]
    public void AddEncinaProvider_WithSagasEnabled_ResolvesSagaRunner(
        Action<IServiceCollection, Action<MessagingConfiguration>> register)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        register(services, config => config.UseSagas = true);
        using var provider = services.BuildServiceProvider();

        // Assert
        var sagaRunner = provider.GetRequiredService<ISagaRunner>();
        sagaRunner.ShouldNotBeNull();
        sagaRunner.ShouldBeOfType<SagaRunner>();
    }

    public static IEnumerable<object[]> SagaRunnerCases()
    {
        yield return new object[]
        {
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                AdoSqlServer.AddEncinaADO(services, configure))
        };
        yield return new object[]
        {
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                AdoPostgreSql.AddEncinaADO(services, configure))
        };
        yield return new object[]
        {
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                AdoMySql.AddEncinaADO(services, configure))
        };
        yield return new object[]
        {
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                DapperSqlServer.AddEncinaDapper(services, configure))
        };
        yield return new object[]
        {
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                DapperPostgreSql.AddEncinaDapper(services, configure))
        };
        yield return new object[]
        {
            (Action<IServiceCollection, Action<MessagingConfiguration>>)((services, configure) =>
                DapperMySql.AddEncinaDapper(services, configure))
        };
    }
}
