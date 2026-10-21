using Xunit;

namespace Encina.IntegrationTests.Messaging.Outbox;

/// <summary>
/// The transactional outbox scenarios of #718, identical for the three ADO.NET providers: each provider's
/// <c>OutboxAtomicityTests</c> supplies its <see cref="OutboxAtomicityScenario.Harness"/> and its shared
/// <c>ADO-*</c> collection fixture.
/// </summary>
public abstract class OutboxAtomicityTestsBase : IAsyncLifetime
{
    /// <summary>Gets the provider under test.</summary>
    protected abstract OutboxAtomicityScenario.Harness Harness { get; }

    /// <inheritdoc />
    public ValueTask InitializeAsync() => new(Harness.PrepareAsync());

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [InlineData(true, 1)]
    [InlineData(true, 3)]
    [InlineData(false, 3)]
    [InlineData(false, 0)]
    public Task Commit_WritesTheBusinessRowAndOneOutboxRowPerNotification(bool transactions, int notifications) =>
        OutboxAtomicityScenario.AssertCommitWritesBusinessRowAndOutboxRowsAsync(Harness, transactions, notifications);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task HandlerLeft_LeavesNoBusinessRowAndNoOutboxRow(bool transactions) =>
        OutboxAtomicityScenario.AssertHandlerLeftLeavesNothingAsync(Harness, transactions);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task HandlerException_LeavesNoBusinessRowAndNoOutboxRow(bool transactions) =>
        OutboxAtomicityScenario.AssertHandlerExceptionLeavesNothingAsync(Harness, transactions);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task FailedOutboxWrite_FailsTheRequestAndRollsTheBusinessChangeBack(bool transactions) =>
        OutboxAtomicityScenario.AssertFailedOutboxWriteRollsBusinessChangeBackAsync(Harness, transactions);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task TransactionBehaviorAndOutbox_BeginExactlyOneTransactionPerRequest(bool transactions) =>
        OutboxAtomicityScenario.AssertOneTransactionIsSharedByTheBehaviorsAsync(Harness, transactions);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task UnitOfWorkRollbackInsideTheRequest_FailsWithRollbackOnlyAndLeavesNoRow(bool transactions) =>
        OutboxAtomicityScenario.AssertUnitOfWorkRollbackFailsTheRequestAsync(Harness, transactions);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task InboxAndOutbox_ShareTheBusinessTransaction(bool transactions) =>
        OutboxAtomicityScenario.AssertInboxAndOutboxShareTheBusinessTransactionAsync(Harness, transactions);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task ModuleIsolation_WritesTheOutboxRowThroughTheSchemaValidatingConnection(bool transactions) =>
        OutboxAtomicityScenario.AssertModuleIsolationWritesTheOutboxRowAsync(Harness, transactions);

    [Fact]
    public Task DatabasePerTenant_BusinessRowAndOutboxRowsLandInTheTenantStore() =>
        OutboxAtomicityScenario.AssertTenantStoreReceivesBusinessAndOutboxRowsAsync(Harness);
}
