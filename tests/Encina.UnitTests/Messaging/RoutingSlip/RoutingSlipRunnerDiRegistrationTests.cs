using System.Data;
using Encina.Messaging;
using Encina.Messaging.RoutingSlip;
using AdoMySql = Encina.ADO.MySQL.ServiceCollectionExtensions;
using AdoPostgreSql = Encina.ADO.PostgreSQL.ServiceCollectionExtensions;
using AdoSqlServer = Encina.ADO.SqlServer.ServiceCollectionExtensions;
using DapperMySql = Encina.Dapper.MySQL.ServiceCollectionExtensions;
using DapperPostgreSql = Encina.Dapper.PostgreSQL.ServiceCollectionExtensions;
using DapperSqlServer = Encina.Dapper.SqlServer.ServiceCollectionExtensions;

namespace Encina.UnitTests.Messaging.RoutingSlip;

/// <summary>
/// Verifies that every ADO.NET and Dapper provider package can resolve <see cref="IRoutingSlipRunner"/>
/// once <see cref="MessagingConfiguration.UseRoutingSlips"/> is enabled.
/// </summary>
/// <remarks>
/// Before #1163, <c>RoutingSlipRunner</c> took a constructor-injected <c>IRequestContext</c>, which is
/// never registered in DI. Every one of these six provider packages calls the shared
/// <c>AddMessagingServices</c> helper with <c>services.AddScoped&lt;IRoutingSlipRunner, RoutingSlipRunner&gt;()</c>,
/// so resolving <see cref="IServiceProvider.GetService"/> for it threw
/// <see cref="InvalidOperationException"/> at first use. The fix makes <c>RoutingSlipRunner</c> depend on
/// <see cref="IRequestContextAccessor"/> instead, matching <c>SagaRunner</c>.
/// </remarks>
/// <remarks>
/// <c>Encina.EntityFrameworkCore</c> and <c>Encina.MongoDB</c> are not in this suite because neither
/// package registers <see cref="IRoutingSlipRunner"/> today - routing slips are only wired through the
/// shared ADO.NET/Dapper <c>AddMessagingServices</c> helper.
/// </remarks>
public sealed class RoutingSlipRunnerDiRegistrationTests
{
    [Theory]
    [MemberData(nameof(RoutingSlipRunnerCases))]
    public void AddEncinaProvider_WithRoutingSlipsEnabled_ResolvesRoutingSlipRunner(
        Action<IServiceCollection, Action<MessagingConfiguration>> register)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        register(services, config => config.UseRoutingSlips = true);
        using var provider = services.BuildServiceProvider();

        // Assert
        var routingSlipRunner = provider.GetRequiredService<IRoutingSlipRunner>();
        routingSlipRunner.ShouldNotBeNull();
        routingSlipRunner.ShouldBeOfType<RoutingSlipRunner>();
    }

    public static IEnumerable<object[]> RoutingSlipRunnerCases()
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
