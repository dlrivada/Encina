using Aspire.Hosting;

using Encina.Aspire.Testing;

using Shouldly;

namespace Encina.GuardTests.Aspire;

/// <summary>
/// Guard tests for Encina.Aspire.Testing covering ThrowIfNull guards on
/// extension methods. The guards fire before any Aspire host logic runs,
/// so passing null is sufficient to verify guard clauses without a real host.
/// </summary>
[Trait("Category", "Guard")]
public sealed class AspireTestingGuardTests
{
    // ─── DistributedApplicationExtensions null guards ───

    [Fact]
    public void GetEncinaTestContext_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetEncinaTestContext(null!));
    }

    [Fact]
    public void GetOutboxStore_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetOutboxStore(null!));
    }

    [Fact]
    public void GetInboxStore_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetInboxStore(null!));
    }

    [Fact]
    public void GetSagaStore_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetSagaStore(null!));
    }

    [Fact]
    public void GetPendingOutboxMessages_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetPendingOutboxMessages(null!));
    }

    [Fact]
    public void GetDeadLetterMessages_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetDeadLetterMessages(null!));
    }

    [Fact]
    public async Task AssertOutboxContainsAsync_NullApp_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await DistributedApplicationExtensions.AssertOutboxContainsAsync(null!, _ => true));
    }

    [Fact]
    public async Task AssertInboxProcessedAsync_NullApp_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await DistributedApplicationExtensions.AssertInboxProcessedAsync(null!, "msg-1"));
    }

    [Fact]
    public async Task WaitForOutboxProcessingAsync_NullApp_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await DistributedApplicationExtensions.WaitForOutboxProcessingAsync(null!));
    }

    // ─── FailureSimulationExtensions null guards ───

    [Fact]
    public void SimulateSagaTimeout_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateSagaTimeout(null!, Guid.NewGuid()));
    }

    [Fact]
    public void SimulateSagaFailure_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateSagaFailure(null!, Guid.NewGuid(), "error"));
    }

    [Fact]
    public void SimulateOutboxMessageFailure_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateOutboxMessageFailure(null!, Guid.NewGuid()));
    }

    [Fact]
    public void SimulateOutboxDeadLetter_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateOutboxDeadLetter(null!, Guid.NewGuid()));
    }

    [Fact]
    public void SimulateInboxMessageFailure_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateInboxMessageFailure(null!, "msg-1"));
    }

    [Fact]
    public void SimulateInboxExpiration_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateInboxExpiration(null!, "msg-1"));
    }

    [Fact]
    public async Task AddToDeadLetterAsync_NullApp_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await FailureSimulationExtensions.AddToDeadLetterAsync(null!, "type", "content"));
    }

    // ─── EncinaTestContext ───

    [Fact]
    public void EncinaTestContext_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new EncinaTestContext(null!));
    }

    [Fact]
    public void EncinaTestContext_ValidOptions_Constructs()
    {
        var sut = new EncinaTestContext(new EncinaTestSupportOptions());
        sut.ShouldNotBeNull();
    }

    // ─── EncinaTestSupportOptions ───

    [Fact]
    public void EncinaTestSupportOptions_Defaults()
    {
        var options = new EncinaTestSupportOptions();
        options.ShouldNotBeNull();
    }
}
