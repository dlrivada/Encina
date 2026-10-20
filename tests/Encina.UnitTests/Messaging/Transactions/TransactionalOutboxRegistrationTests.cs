using System.Data;
using Encina.DomainModeling;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Transactions;
using Encina.Testing.Fakes.Factories;
using Encina.Testing.Fakes.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Encina.UnitTests.Messaging.Transactions;

/// <summary>
/// Registration of the shared transaction and of the outbox write path (#718): ADO.NET registers the transactional
/// behavior, Dapper keeps the post-processor, both register one accessor per scope that also answers as the
/// neutral lease source; EF Core and MongoDB, which have no <see cref="IDbConnection"/>, register no accessor and
/// still build with <c>ValidateOnBuild</c>. Recoverability runs outside the transaction (ADR-050).
/// </summary>
[Trait("Category", "Unit")]
public sealed class TransactionalOutboxRegistrationTests
{
    private const string ConnectionString = "Host=localhost;Database=probe;Username=probe;Password=probe";

    public enum Family
    {
        AdoSqlServer,
        AdoPostgreSql,
        AdoMySql,
        DapperSqlServer,
        DapperPostgreSql,
        DapperMySql,
        EntityFrameworkCore,
        MongoDB
    }

    public static TheoryData<Family> RelationalFamilies =>
    [
        Family.AdoSqlServer, Family.AdoPostgreSql, Family.AdoMySql,
        Family.DapperSqlServer, Family.DapperPostgreSql, Family.DapperMySql
    ];

    public static TheoryData<Family> NonRelationalFamilies => [Family.EntityFrameworkCore, Family.MongoDB];

    private static bool IsAdo(Family family) => family is Family.AdoSqlServer or Family.AdoPostgreSql or Family.AdoMySql;

    [Theory]
    [MemberData(nameof(RelationalFamilies))]
    public void Relational_OutboxAndTransactions_BuildsWithValidationAndSharesOneAccessor(Family family)
    {
        var services = Register(family);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var accessor = sp.GetRequiredService<IDbTransactionAccessor>();
        sp.GetRequiredService<ITransactionLeaseSource>().ShouldBeSameAs(accessor);
        sp.GetRequiredService<IDbTransactionAccessor>().ShouldBeSameAs(accessor);
        sp.GetServices<IPipelineBehavior<ProbeRequest, string>>()
            .ShouldContain(b => b is TransactionPipelineBehavior<ProbeRequest, string>);

        if (IsAdo(family))
        {
            sp.GetServices<IPipelineBehavior<ProbeRequest, string>>()
                .ShouldContain(b => b is TransactionalOutboxPipelineBehavior<ProbeRequest, string>);
            sp.GetServices<IRequestPostProcessor<ProbeRequest, string>>()
                .ShouldNotContain(p => p is OutboxPostProcessor<ProbeRequest, string>);
        }
        else
        {
            sp.GetServices<IRequestPostProcessor<ProbeRequest, string>>()
                .ShouldContain(p => p is OutboxPostProcessor<ProbeRequest, string>);
            sp.GetServices<IPipelineBehavior<ProbeRequest, string>>()
                .ShouldNotContain(b => b is TransactionalOutboxPipelineBehavior<ProbeRequest, string>);
        }
    }

    [Theory]
    [MemberData(nameof(NonRelationalFamilies))]
    public void EfCoreAndMongoDb_RegisterNoAccessor_AndStillBuildWithValidation(Family family)
    {
        var services = Register(family);

        services.ShouldNotContain(d => d.ServiceType == typeof(IDbTransactionAccessor));
        services.ShouldNotContain(d => d.ServiceType == typeof(ITransactionLeaseSource));
        Should.NotThrow(() =>
        {
            using var provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        });
    }

    [Theory]
    [InlineData(Family.AdoSqlServer)]
    [InlineData(Family.AdoPostgreSql)]
    [InlineData(Family.AdoMySql)]
    public async Task AddEncinaUnitOfWork_RegistersTheAccessorOnceAndResolvesTheUnitOfWork(Family family)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IDbConnection>(_ => Substitute.For<IDbConnection>());

        switch (family)
        {
            case Family.AdoSqlServer:
                global::Encina.ADO.SqlServer.ServiceCollectionExtensions.AddEncinaUnitOfWork(services);
                global::Encina.ADO.SqlServer.ServiceCollectionExtensions.AddEncinaUnitOfWork(services);
                break;
            case Family.AdoPostgreSql:
                global::Encina.ADO.PostgreSQL.ServiceCollectionExtensions.AddEncinaUnitOfWork(services);
                global::Encina.ADO.PostgreSQL.ServiceCollectionExtensions.AddEncinaUnitOfWork(services);
                break;
            default:
                global::Encina.ADO.MySQL.ServiceCollectionExtensions.AddEncinaUnitOfWork(services);
                global::Encina.ADO.MySQL.ServiceCollectionExtensions.AddEncinaUnitOfWork(services);
                break;
        }

        services.Count(d => d.ServiceType == typeof(IDbTransactionAccessor)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(ITransactionLeaseSource)).ShouldBe(1);
        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ShouldNotBeNull();
    }

    [Fact]
    public void ApplicationsOwnAccessorRegistration_Wins()
    {
        var services = new ServiceCollection();
        var custom = Substitute.For<IDbTransactionAccessor>();
        services.AddScoped(_ => custom);

        MessagingServiceCollectionExtensions.AddDbTransactionAccessor(services);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITransactionLeaseSource>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void AddDbTransactionAccessor_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() => MessagingServiceCollectionExtensions.AddDbTransactionAccessor(null!));
    }

    [Fact]
    public void AddMessagingServices_TransactionalPath_OrdersRecoverabilityThenTransactionThenOutboxThenInbox()
    {
        var services = new ServiceCollection();
        services.AddMessagingServices<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            FakeSagaStore, StubSagaFactory,
            FakeScheduledMessageStore, StubScheduledFactory,
            StubOutboxProcessor>(AllBehaviors(), OutboxWritePath.TransactionalBehavior);

        BehaviorOrder(services).ShouldBe(
        [
            typeof(RecoverabilityPipelineBehavior<,>),
            typeof(TransactionPipelineBehavior<,>),
            typeof(TransactionalOutboxPipelineBehavior<,>),
            typeof(InboxPipelineBehavior<,>)
        ]);
    }

    [Fact]
    public void AddMessagingServices_PostProcessorPath_KeepsRecoverabilityOutsideTheTransaction()
    {
        var services = new ServiceCollection();
        services.AddMessagingServices<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            FakeSagaStore, StubSagaFactory,
            FakeScheduledMessageStore, StubScheduledFactory,
            StubOutboxProcessor>(AllBehaviors());

        BehaviorOrder(services).ShouldBe(
        [
            typeof(RecoverabilityPipelineBehavior<,>),
            typeof(TransactionPipelineBehavior<,>),
            typeof(InboxPipelineBehavior<,>)
        ]);
        services.ShouldContain(d => d.ServiceType == typeof(IRequestPostProcessor<,>));
    }

    [Fact]
    public void AddMessagingServicesCore_TransactionalPath_OrdersRecoverabilityThenTransactionThenOutboxThenInbox()
    {
        var services = new ServiceCollection();
        services.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            StubOutboxProcessor>(AllBehaviors(), OutboxWritePath.TransactionalBehavior);

        BehaviorOrder(services).ShouldBe(
        [
            typeof(RecoverabilityPipelineBehavior<,>),
            typeof(TransactionPipelineBehavior<,>),
            typeof(TransactionalOutboxPipelineBehavior<,>),
            typeof(InboxPipelineBehavior<,>)
        ]);
        services.ShouldContain(d => d.ServiceType == typeof(IDbTransactionAccessor));
    }

    [Fact]
    public void AddOutboxInboxSagaSchedulingServices_Public_RegistersNoAccessorAndTheInterimPostProcessor()
    {
        var services = new ServiceCollection();

        services.AddOutboxInboxSagaSchedulingServices<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            FakeSagaStore, StubSagaFactory,
            FakeScheduledMessageStore, StubScheduledFactory,
            StubOutboxProcessor>(
            useOutbox: true, new OutboxOptions(),
            useInbox: false, new InboxOptions(),
            useSagas: false, new SagaOptions(),
            useScheduling: false, new SchedulingOptions());

        services.ShouldNotContain(d => d.ServiceType == typeof(IDbTransactionAccessor));
        services.ShouldContain(d => d.ServiceType == typeof(IRequestPostProcessor<,>));
        services.ShouldNotContain(d => d.ImplementationType == typeof(TransactionalOutboxPipelineBehavior<,>));
    }

    [Fact]
    public void AddMessagingServices_NullArguments_Throw()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() => services.AddMessagingServices<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            FakeSagaStore, StubSagaFactory,
            FakeScheduledMessageStore, StubScheduledFactory,
            StubOutboxProcessor>(null!, OutboxWritePath.TransactionalBehavior));
        Should.Throw<ArgumentNullException>(() => services.AddMessagingServicesCore<
            FakeOutboxStore, FakeOutboxMessageFactory,
            FakeInboxStore, StubInboxFactory,
            StubOutboxProcessor>(null!, OutboxWritePath.TransactionalBehavior));
    }

    private static MessagingConfiguration AllBehaviors()
    {
        var config = new MessagingConfiguration
        {
            UseTransactions = true,
            UseOutbox = true,
            UseInbox = true,
            UseRecoverability = true
        };
        return config;
    }

    private static List<Type?> BehaviorOrder(IServiceCollection services) =>
        services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();

    private static ServiceCollection Register(Family family)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();

        static void OutboxAndTransactions(MessagingConfiguration config)
        {
            config.UseOutbox = true;
            config.UseTransactions = true;
        }

        switch (family)
        {
            case Family.AdoSqlServer:
                global::Encina.ADO.SqlServer.ServiceCollectionExtensions.AddEncinaADO(services, "Server=localhost;Database=probe;User Id=probe;Password=probe;TrustServerCertificate=True", OutboxAndTransactions);
                break;
            case Family.AdoPostgreSql:
                global::Encina.ADO.PostgreSQL.ServiceCollectionExtensions.AddEncinaADO(services, ConnectionString, OutboxAndTransactions);
                break;
            case Family.AdoMySql:
                global::Encina.ADO.MySQL.ServiceCollectionExtensions.AddEncinaADO(services, "Server=localhost;Database=probe;User=probe;Password=probe", OutboxAndTransactions);
                break;
            case Family.DapperSqlServer:
                global::Encina.Dapper.SqlServer.ServiceCollectionExtensions.AddEncinaDapper(services, "Server=localhost;Database=probe;User Id=probe;Password=probe;TrustServerCertificate=True", OutboxAndTransactions);
                break;
            case Family.DapperPostgreSql:
                global::Encina.Dapper.PostgreSQL.ServiceCollectionExtensions.AddEncinaDapper(services, ConnectionString, OutboxAndTransactions);
                break;
            case Family.DapperMySql:
                global::Encina.Dapper.MySQL.ServiceCollectionExtensions.AddEncinaDapper(services, "Server=localhost;Database=probe;User=probe;Password=probe", OutboxAndTransactions);
                break;
            case Family.EntityFrameworkCore:
                services.AddDbContext<ProbeDbContext>(o => o.UseInMemoryDatabase($"tx-registration-{Guid.NewGuid():N}"));
                global::Encina.EntityFrameworkCore.ServiceCollectionExtensions.AddEncinaEntityFrameworkCore<ProbeDbContext>(
                    services, config => config.UseOutbox = true);
                break;
            default:
                global::Encina.MongoDB.ServiceCollectionExtensions.AddEncinaMongoDB(services, options =>
                {
                    options.ConnectionString = "mongodb://localhost:27017";
                    options.DatabaseName = "probe";
                    options.UseOutbox = true;
                    options.CreateIndexes = false;
                });
                break;
        }

        return services;
    }

    public sealed record ProbeRequest(string Value) : IRequest<string>;

    public sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options);

    private sealed class StubInboxFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => Substitute.For<IInboxMessage>();
    }

    private sealed class StubSagaFactory : ISagaStateFactory
    {
        public ISagaState Create(Guid sagaId, string sagaType, string data, string status, int currentStep, DateTime startedAtUtc, DateTime? timeoutAtUtc = null)
            => Substitute.For<ISagaState>();
    }

    private sealed class StubScheduledFactory : IScheduledMessageFactory
    {
        public IScheduledMessage Create(Guid id, string requestType, string content, DateTime scheduledAtUtc, DateTime createdAtUtc, bool isRecurring, string? cronExpression)
            => Substitute.For<IScheduledMessage>();
    }

    private sealed class StubOutboxProcessor : IHostedService
    {
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
