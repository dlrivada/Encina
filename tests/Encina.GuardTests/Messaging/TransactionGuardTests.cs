using System.Data;
using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionPipelineBehavior = Encina.Messaging.TransactionPipelineBehavior<Encina.GuardTests.Messaging.TransactionGuardTests.TestRequest, string>;

namespace Encina.GuardTests.Messaging;

/// <summary>
/// Guard tests for the shared-transaction types (#718): the accessor, the Messaging transaction behavior, the
/// transactional outbox behavior and the lease helpers reject null arguments with the right parameter name.
/// </summary>
[Trait("Category", "Guard")]
public sealed class TransactionGuardTests
{
    public sealed record TestRequest : IRequest<string>, IHasNotifications
    {
        public IEnumerable<INotification> GetNotifications() => [];
    }

    private static RequestHandlerCallback<string> Next() =>
        () => ValueTask.FromResult<Either<EncinaError, string>>("ok");

    [Fact]
    public void DbTransactionAccessor_Constructor_RejectsNulls()
    {
        Should.Throw<ArgumentNullException>(() => new DbTransactionAccessor(null!, NullLogger<DbTransactionAccessor>.Instance))
            .ParamName.ShouldBe("connection");
        Should.Throw<ArgumentNullException>(() => new DbTransactionAccessor(Substitute.For<IDbConnection>(), null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void TransactionPipelineBehavior_Constructor_RejectsNulls()
    {
        var logger = NullLogger<TransactionPipelineBehavior>.Instance;
        var accessor = Substitute.For<IDbTransactionAccessor>();

        Should.Throw<ArgumentNullException>(() => new TransactionPipelineBehavior(null!, logger))
            .ParamName.ShouldBe("transactionAccessor");
        Should.Throw<ArgumentNullException>(() => new TransactionPipelineBehavior(accessor, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task TransactionPipelineBehavior_Handle_RejectsNulls()
    {
        var behavior = new TransactionPipelineBehavior(
            Substitute.For<IDbTransactionAccessor>(), NullLogger<TransactionPipelineBehavior>.Instance);
        var context = Substitute.For<IRequestContext>();

        (await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(null!, context, Next(), CancellationToken.None)))
            .ParamName.ShouldBe("request");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(new TestRequest(), null!, Next(), CancellationToken.None)))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(new TestRequest(), context, null!, CancellationToken.None)))
            .ParamName.ShouldBe("nextStep");
    }

    [Fact]
    public void TransactionalOutboxPipelineBehavior_Constructor_RejectsNulls()
    {
        var source = Substitute.For<ITransactionLeaseSource>();
        var store = Substitute.For<IOutboxStore>();
        var factory = Substitute.For<IOutboxMessageFactory>();
        var serializer = Substitute.For<IMessageSerializer>();
        var logger = NullLogger<TransactionalOutboxPipelineBehavior<TestRequest, string>>.Instance;

        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<TestRequest, string>(null!, store, factory, serializer, logger))
            .ParamName.ShouldBe("transactionSource");
        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<TestRequest, string>(source, null!, factory, serializer, logger))
            .ParamName.ShouldBe("outboxStore");
        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<TestRequest, string>(source, store, null!, serializer, logger))
            .ParamName.ShouldBe("messageFactory");
        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<TestRequest, string>(source, store, factory, null!, logger))
            .ParamName.ShouldBe("messageSerializer");
        Should.Throw<ArgumentNullException>(() => new TransactionalOutboxPipelineBehavior<TestRequest, string>(source, store, factory, serializer, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task TransactionalOutboxPipelineBehavior_Handle_RejectsNulls()
    {
        var behavior = new TransactionalOutboxPipelineBehavior<TestRequest, string>(
            Substitute.For<ITransactionLeaseSource>(), Substitute.For<IOutboxStore>(), Substitute.For<IOutboxMessageFactory>(),
            Substitute.For<IMessageSerializer>(), NullLogger<TransactionalOutboxPipelineBehavior<TestRequest, string>>.Instance);
        var context = Substitute.For<IRequestContext>();

        (await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(null!, context, Next(), CancellationToken.None)))
            .ParamName.ShouldBe("request");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(new TestRequest(), null!, Next(), CancellationToken.None)))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await behavior.Handle(new TestRequest(), context, null!, CancellationToken.None)))
            .ParamName.ShouldBe("nextStep");
    }

    [Fact]
    public void TransactionErrors_NotSupported_RejectsNull()
    {
        Should.Throw<ArgumentNullException>(() => TransactionErrors.NotSupported(null!)).ParamName.ShouldBe("connectionType");
    }

    [Fact]
    public async Task DbLease_Factories_RejectNullConnection()
    {
        Should.Throw<ArgumentNullException>(() => DbLease.Enlisted(null!, null)).ParamName.ShouldBe("connection");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await DbLease.IndependentAsync(null!, null, CancellationToken.None)))
            .ParamName.ShouldBe("connection");
    }
}
