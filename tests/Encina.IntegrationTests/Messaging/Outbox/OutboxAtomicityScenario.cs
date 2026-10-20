using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Encina.DomainModeling;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using Encina.Modules.Isolation;
using Encina.Tenancy;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.IntegrationTests.Messaging.Outbox;

/// <summary>
/// Proves the transactional outbox of the ADO.NET providers (#718) on a real database: the outbox rows of a
/// request's notifications and its business change commit or roll back together, one transaction is shared by
/// every participant, and a failed outbox write or a rollback-only participant fails the request.
/// </summary>
/// <remarks>
/// The request, the handlers and every assertion are provider-neutral; a <see cref="Harness"/> supplies the
/// provider's registration, SQL dialect and connections. The services are the ones <c>AddEncinaADO</c> builds,
/// resolved in one scope per request, as in an application.
/// </remarks>
public static class OutboxAtomicityScenario
{
    /// <summary>What the handler does with the business change.</summary>
    public enum Mode
    {
        /// <summary>Inserts the business row and returns a response.</summary>
        Commit,

        /// <summary>Inserts the business row and returns a business Left.</summary>
        ReturnLeft,

        /// <summary>Inserts the business row and throws.</summary>
        Throw,

        /// <summary>Inserts the business row inside a unit of work that is rolled back, then returns a response.</summary>
        UnitOfWorkRollback
    }

    /// <summary>A notification the handler emits.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Sequence">The position of the notification.</param>
    public sealed record OrderPlaced(int OrderId, int Sequence) : INotification;

    /// <summary>A request that changes a business row and emits notifications.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Notifications">How many notifications it emits.</param>
    /// <param name="Mode">What the handler does.</param>
    public sealed record PlaceOrder(int OrderId, int Notifications, Mode Mode) : IRequest<int>, IHasNotifications
    {
        /// <inheritdoc />
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new OrderPlaced(OrderId, i));
    }

    /// <summary>The same request, deduplicated by the inbox.</summary>
    /// <param name="OrderId">The business key.</param>
    /// <param name="Notifications">How many notifications it emits.</param>
    /// <param name="Mode">What the handler does.</param>
    public sealed record PlaceIdempotentOrder(int OrderId, int Notifications, Mode Mode)
        : IRequest<int>, IHasNotifications, IIdempotentRequest
    {
        /// <inheritdoc />
        public IEnumerable<INotification> GetNotifications() =>
            Enumerable.Range(0, Notifications).Select(i => (INotification)new OrderPlaced(OrderId, i));
    }

    /// <summary>Handler of <see cref="PlaceOrder"/>.</summary>
    public sealed class PlaceOrderHandler(IDbTransactionAccessor accessor, IServiceProvider services, Harness harness)
        : IRequestHandler<PlaceOrder, int>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, int>> Handle(PlaceOrder request, CancellationToken cancellationToken) =>
            Run(harness, accessor, services, request.OrderId, request.Mode, cancellationToken);
    }

    /// <summary>Handler of <see cref="PlaceIdempotentOrder"/>.</summary>
    public sealed class PlaceIdempotentOrderHandler(IDbTransactionAccessor accessor, IServiceProvider services, Harness harness)
        : IRequestHandler<PlaceIdempotentOrder, int>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, int>> Handle(PlaceIdempotentOrder request, CancellationToken cancellationToken) =>
            Run(harness, accessor, services, request.OrderId, request.Mode, cancellationToken);
    }

    /// <summary>Replaces the id of every outbox message with one value, so a second write violates the key.</summary>
    private sealed class FixedIdOutboxMessageFactory(IOutboxMessageFactory inner, Guid id) : IOutboxMessageFactory
    {
        public IOutboxMessage Create(Guid _, string notificationType, string content, DateTime createdAtUtc) =>
            inner.Create(id, notificationType, content, createdAtUtc);
    }

    /// <summary>How the scenario wires and reads the provider under test.</summary>
    public sealed class Harness
    {
        /// <summary>Registers the scoped connection and the provider's <c>AddEncinaADO</c> with the given configuration.</summary>
        public required Action<IServiceCollection, Action<MessagingConfiguration>> Register { get; init; }

        /// <summary>Same as <see cref="Register"/>, with the connection wrapped by the module isolation decorator.</summary>
        public required Action<IServiceCollection, Action<MessagingConfiguration>> RegisterWithModuleIsolation { get; init; }

        /// <summary>Registers the provider's <c>AddEncinaADOWithTenancy</c> with the given default connection string and unit of work.</summary>
        public required Action<IServiceCollection, Action<MessagingConfiguration>, string> RegisterWithTenancy { get; init; }

        /// <summary>
        /// Creates the provider's unit of work over the scope's accessor. The container is not used: Encina disposes
        /// its request scope synchronously and the units of work implement only <see cref="IAsyncDisposable"/>.
        /// </summary>
        public required Func<IDbTransactionAccessor, IServiceProvider, IUnitOfWork> CreateUnitOfWork { get; init; }

        /// <summary>The provider's own outbox message factory.</summary>
        public required Func<IOutboxMessageFactory> CreateInnerOutboxFactory { get; init; }

        /// <summary>Opens a new connection to the primary database.</summary>
        public required Func<IDbConnection> CreateConnection { get; init; }

        /// <summary>Creates a database with the outbox, inbox and business tables and returns its connection string.</summary>
        public required Func<string, Task<string>> CreateTenantDatabaseAsync { get; init; }

        /// <summary>Opens a connection from a connection string.</summary>
        public required Func<string, IDbConnection> OpenConnection { get; init; }

        /// <summary>The connection string of the primary database.</summary>
        public required string ConnectionString { get; init; }

        /// <summary>Clears every table of the primary database and (re)creates the business table.</summary>
        public required Func<Task> PrepareAsync { get; init; }

        /// <summary>Reads the persisted inbox row of a message, or null.</summary>
        public required Func<string, Task<InboxPipelineScenario.InboxRow?>> ReadInboxRow { get; init; }

        /// <summary>SQL that inserts a business row from the parameters <c>@id</c> and <c>@name</c>.</summary>
        public required string InsertBusinessSql { get; init; }

        /// <summary>SQL that counts the business rows.</summary>
        public required string CountBusinessSql { get; init; }

        /// <summary>SQL that counts the outbox rows.</summary>
        public required string CountOutboxSql { get; init; }
    }

    private static async Task<Either<EncinaError, int>> Run(
        Harness harness,
        IDbTransactionAccessor accessor,
        IServiceProvider services,
        int orderId,
        Mode mode,
        CancellationToken cancellationToken)
    {
        if (mode == Mode.UnitOfWorkRollback)
        {
            await using var unitOfWork = harness.CreateUnitOfWork(accessor, services);
            (await unitOfWork.BeginTransactionAsync(cancellationToken)).ShouldBeRight();
            await InsertAsync(harness, accessor, orderId, cancellationToken);
            await unitOfWork.RollbackAsync(cancellationToken);
            return orderId;
        }

        await InsertAsync(harness, accessor, orderId, cancellationToken);

        return mode switch
        {
            Mode.Throw => throw new InvalidOperationException("boom"),
            Mode.ReturnLeft => Left<EncinaError, int>(EncinaErrors.Create("biz.rule", "business rule")),
            _ => Right<EncinaError, int>(orderId)
        };
    }

    // The handler enlists in the scope's transaction the way application code does: its command uses the
    // accessor's connection and Current transaction.
    private static async Task InsertAsync(Harness harness, IDbTransactionAccessor accessor, int orderId, CancellationToken cancellationToken)
    {
        using var command = accessor.Connection.CreateCommand();
        command.Transaction = accessor.Current;
        command.CommandText = harness.InsertBusinessSql;
        AddParameter(command, "@id", orderId);
        AddParameter(command, "@name", "order-" + orderId);
        await ((DbCommand)command).ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    /// <summary>Counts rows with a statement on a fresh connection.</summary>
    public static async Task<long> CountAsync(IDbConnection connection, string sql)
    {
        if (connection.State != ConnectionState.Open)
        {
            await ((DbConnection)connection).OpenAsync();
        }

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await ((DbCommand)command).ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<(long Business, long Outbox)> CountRowsAsync(Harness harness)
    {
        using var connection = harness.CreateConnection();
        return (await CountAsync(connection, harness.CountBusinessSql), await CountAsync(connection, harness.CountOutboxSql));
    }

    private static ServiceProvider Build(Harness harness, Action<IServiceCollection, Action<MessagingConfiguration>> register, Action<MessagingConfiguration> configure, Action<IServiceCollection>? customize = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddSingleton(harness);
        services.AddScoped<IRequestHandler<PlaceOrder, int>, PlaceOrderHandler>();
        services.AddScoped<IRequestHandler<PlaceIdempotentOrder, int>, PlaceIdempotentOrderHandler>();
        register(services, configure);
        customize?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static Action<MessagingConfiguration> Configure(bool transactions, bool inbox = false) => config =>
    {
        config.UseOutbox = true;
        config.UseTransactions = transactions;
        config.UseInbox = inbox;
    };

    private static async Task<Either<EncinaError, int>> SendAsync<TRequest>(ServiceProvider provider, TRequest request, string? idempotencyKey = null)
        where TRequest : IRequest<int>
    {
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        return idempotencyKey is null
            ? await encina.Send(request)
            : await encina.Send(request, RequestContext.CreateForTest(idempotencyKey: idempotencyKey));
    }

    private static Task<Either<EncinaError, int>> SendSafelyAsync(ServiceProvider provider, PlaceOrder request) => SendAsync(provider, request);

    // True when the request did not succeed: the pipeline may surface a throwing handler as a Left or as the exception.
    private static async Task<bool> SendFailsAsync(ServiceProvider provider, IRequest<int> request, string? idempotencyKey = null)
    {
        try
        {
            await using var scope = provider.CreateAsyncScope();
            var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var result = idempotencyKey is null
                ? await encina.Send(request)
                : await encina.Send(request, RequestContext.CreateForTest(idempotencyKey: idempotencyKey));
            return result.IsLeft;
        }
        catch (Exception)
        {
            return true;
        }
    }

    /// <summary>n notifications leave n outbox rows and the business row, with and without the transaction behavior.</summary>
    public static async Task AssertCommitWritesBusinessRowAndOutboxRowsAsync(Harness harness, bool transactions, int notifications)
    {
        await using var provider = Build(harness, harness.Register, Configure(transactions));

        var result = await SendSafelyAsync(provider, new PlaceOrder(1, notifications, Mode.Commit));

        result.ShouldBeRight().ShouldBe(1);
        var (business, outbox) = await CountRowsAsync(harness);
        business.ShouldBe(1);
        outbox.ShouldBe(notifications);
    }

    /// <summary>A business Left leaves no business row and no outbox row.</summary>
    public static async Task AssertHandlerLeftLeavesNothingAsync(Harness harness, bool transactions)
    {
        await using var provider = Build(harness, harness.Register, Configure(transactions));

        var result = await SendSafelyAsync(provider, new PlaceOrder(2, 3, Mode.ReturnLeft));

        result.ShouldBeErrorWithCode("biz.rule");
        (await CountRowsAsync(harness)).ShouldBe((0L, 0L));
    }

    /// <summary>A throwing handler reaches the caller and leaves no business row and no outbox row.</summary>
    public static async Task AssertHandlerExceptionLeavesNothingAsync(Harness harness, bool transactions)
    {
        await using var provider = Build(harness, harness.Register, Configure(transactions));

        // The transaction behavior turns a handler exception into a Left; without it the outbox behavior
        // rolls back and rethrows the original exception.
        if (transactions)
        {
            (await SendAsync(provider, new PlaceOrder(3, 2, Mode.Throw))).ShouldBeErrorWithCode("transaction.failed");
        }
        else
        {
            var thrown = await Should.ThrowAsync<InvalidOperationException>(async () => await SendAsync(provider, new PlaceOrder(3, 2, Mode.Throw)));
            thrown.Message.ShouldBe("boom");
        }

        (await CountRowsAsync(harness)).ShouldBe((0L, 0L));
    }

    /// <summary>A failed outbox write fails the request and rolls the business change back.</summary>
    public static async Task AssertFailedOutboxWriteRollsBusinessChangeBackAsync(Harness harness, bool transactions)
    {
        var fixedId = Guid.NewGuid();
        await using var provider = Build(
            harness,
            harness.Register,
            Configure(transactions),
            services => services.AddScoped<IOutboxMessageFactory>(_ => new FixedIdOutboxMessageFactory(harness.CreateInnerOutboxFactory(), fixedId)));

        var result = await SendSafelyAsync(provider, new PlaceOrder(4, 2, Mode.Commit));

        result.IsLeft.ShouldBeTrue();
        (await CountRowsAsync(harness)).ShouldBe((0L, 0L));
    }

    /// <summary>With the transaction behavior and the outbox, one transaction is begun per request.</summary>
    public static async Task AssertOneTransactionIsSharedByTheBehaviorsAsync(Harness harness, bool transactions)
    {
        var owners = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "Encina.Messaging.Transactions" or SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.Source.Name == "Encina.Messaging.Transactions")
                {
                    lock (owners) { owners.Add(activity); }
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        await using var provider = Build(harness, harness.Register, Configure(transactions));
        await using (var probe = provider.CreateAsyncScope())
        {
            probe.ServiceProvider.GetServices<IPipelineBehavior<PlaceOrder, int>>()
                .Any(b => b.GetType().Name.StartsWith("TransactionPipelineBehavior", StringComparison.Ordinal))
                .ShouldBe(transactions);
        }

        using var root = Source.StartActivity("scenario");
        root.ShouldNotBeNull();

        var result = await SendSafelyAsync(provider, new PlaceOrder(5, 2, Mode.Commit));

        result.ShouldBeRight();
        Activity[] mine;
        lock (owners) { mine = owners.Where(a => a.TraceId == root.TraceId).ToArray(); }
        mine.Length.ShouldBe(1);
        mine[0].GetTagItem("encina.transaction.owner").ShouldBe(true);
        mine[0].GetTagItem("encina.transaction.outcome").ShouldBe("committed");
        (await CountRowsAsync(harness)).ShouldBe((1L, 2L));
    }

    /// <summary>A unit of work inside the request joins; its rollback fails the request and leaves no row.</summary>
    public static async Task AssertUnitOfWorkRollbackFailsTheRequestAsync(Harness harness, bool transactions)
    {
        await using var provider = Build(harness, harness.Register, Configure(transactions));

        var result = await SendSafelyAsync(provider, new PlaceOrder(6, 2, Mode.UnitOfWorkRollback));

        result.ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        (await CountRowsAsync(harness)).ShouldBe((0L, 0L));
    }

    /// <summary>Inbox and outbox share the business transaction: a success commits all, a Left or exception leaves no outbox row.</summary>
    public static async Task AssertInboxAndOutboxShareTheBusinessTransactionAsync(Harness harness, bool transactions)
    {
        await using var provider = Build(harness, harness.Register, Configure(transactions, inbox: true));

        var okKey = Guid.NewGuid().ToString();
        (await SendAsync(provider, new PlaceIdempotentOrder(8, 2, Mode.Commit), okKey)).ShouldBeRight();
        (await CountRowsAsync(harness)).ShouldBe((1L, 2L));
        (await harness.ReadInboxRow(okKey)).ShouldNotBeNull().IsProcessed.ShouldBeTrue();

        var leftKey = Guid.NewGuid().ToString();
        (await SendAsync(provider, new PlaceIdempotentOrder(9, 2, Mode.ReturnLeft), leftKey)).IsLeft.ShouldBeTrue();
        var leftRow = (await harness.ReadInboxRow(leftKey)).ShouldNotBeNull();
        leftRow.IsProcessed.ShouldBeTrue();
        leftRow.Response.ShouldNotBeNull();

        var throwKey = Guid.NewGuid().ToString();
        (await SendFailsAsync(provider, new PlaceIdempotentOrder(10, 2, Mode.Throw), throwKey)).ShouldBeTrue();
        var thrownRow = (await harness.ReadInboxRow(throwKey)).ShouldNotBeNull();
        thrownRow.RetryCount.ShouldBe(1);
        thrownRow.IsProcessed.ShouldBeFalse();

        // Only the successful request left business and outbox rows.
        (await CountRowsAsync(harness)).ShouldBe((1L, 2L));
    }

    /// <summary>With module isolation the outbox row is written through the schema validating connection.</summary>
    public static async Task AssertModuleIsolationWritesTheOutboxRowAsync(Harness harness, bool transactions)
    {
        var configure = Configure(transactions);
        await using var provider = Build(
            harness,
            harness.RegisterWithModuleIsolation,
            config =>
            {
                configure(config);
                config.ModuleIsolationOptions.Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly;
            });
        await using (var probe = provider.CreateAsyncScope())
        {
            probe.ServiceProvider.GetRequiredService<IDbConnection>().GetType().Name.ShouldBe("SchemaValidatingConnection");
        }

        var result = await SendSafelyAsync(provider, new PlaceOrder(11, 2, Mode.Commit));

        result.ShouldBeRight();
        (await CountRowsAsync(harness)).ShouldBe((1L, 2L));

        var failed = await SendSafelyAsync(provider, new PlaceOrder(12, 2, Mode.ReturnLeft));
        failed.IsLeft.ShouldBeTrue();
        (await CountRowsAsync(harness)).ShouldBe((1L, 2L));
    }

    /// <summary>With database-per-tenant the business row and the outbox rows land in the tenant's own database.</summary>
    public static async Task AssertTenantStoreReceivesBusinessAndOutboxRowsAsync(Harness harness)
    {
        var tenantConnectionString = await harness.CreateTenantDatabaseAsync("t" + Guid.NewGuid().ToString("N")[..12]);

        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns("tenant-a");
        var tenantStore = Substitute.For<ITenantStore>();
        var tenant = new TenantInfo("tenant-a", "Tenant A", TenantIsolationStrategy.DatabasePerTenant, tenantConnectionString);
        tenantStore.GetTenantAsync("tenant-a", Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<TenantInfo?>(tenant));

        await using var provider = Build(
            harness,
            (services, configure) => harness.RegisterWithTenancy(services, configure, harness.ConnectionString),
            Configure(transactions: true),
            services =>
            {
                services.AddSingleton(tenantProvider);
                services.AddSingleton(tenantStore);
            });

        (await SendSafelyAsync(provider, new PlaceOrder(13, 2, Mode.Commit))).ShouldBeRight();
        (await SendSafelyAsync(provider, new PlaceOrder(14, 2, Mode.ReturnLeft))).IsLeft.ShouldBeTrue();

        using var tenantConnection = harness.OpenConnection(tenantConnectionString);
        (await CountAsync(tenantConnection, harness.CountBusinessSql)).ShouldBe(1);
        (await CountAsync(tenantConnection, harness.CountOutboxSql)).ShouldBe(2);
        (await CountRowsAsync(harness)).ShouldBe((0L, 0L));
    }

    private const string SourceName = "Encina.IntegrationTests.OutboxAtomicity";

    private static readonly ActivitySource Source = new(SourceName);
}
