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
        var ex = Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetEncinaTestContext(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void GetOutboxStore_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetOutboxStore(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void GetInboxStore_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetInboxStore(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void GetSagaStore_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetSagaStore(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void GetPendingOutboxMessages_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetPendingOutboxMessages(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void GetDeadLetterMessages_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetDeadLetterMessages(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task AssertOutboxContainsAsync_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.AssertOutboxContainsAsync(null!, _ => true));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task AssertInboxProcessedAsync_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.AssertInboxProcessedAsync(null!, "msg-1"));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task WaitForOutboxProcessingAsync_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.WaitForOutboxProcessingAsync(null!));
        ex.ParamName.ShouldBe("app");
    }

    // ─── FailureSimulationExtensions null guards ───

    [Fact]
    public void SimulateSagaTimeout_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateSagaTimeout(null!, Guid.NewGuid()));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void SimulateSagaFailure_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateSagaFailure(null!, Guid.NewGuid(), "error"));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void SimulateOutboxMessageFailure_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateOutboxMessageFailure(null!, Guid.NewGuid()));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void SimulateOutboxDeadLetter_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateOutboxDeadLetter(null!, Guid.NewGuid()));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void SimulateInboxMessageFailure_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateInboxMessageFailure(null!, "msg-1"));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void SimulateInboxExpiration_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            FailureSimulationExtensions.SimulateInboxExpiration(null!, "msg-1"));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task AddToDeadLetterAsync_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FailureSimulationExtensions.AddToDeadLetterAsync(null!, "type", "content"));
        ex.ParamName.ShouldBe("app");
    }

    // ─── EncinaTestContext ───

    [Fact]
    public void EncinaTestContext_NullOptions_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new EncinaTestContext(null!));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void EncinaTestContext_ValidOptions_Constructs()
    {
        var options = new EncinaTestSupportOptions();

        var sut = new EncinaTestContext(options);

        sut.Options.ShouldBeSameAs(options);
    }

    // ─── EncinaTestSupportOptions ───

    [Fact]
    public void EncinaTestSupportOptions_Defaults()
    {
        var options = new EncinaTestSupportOptions();

        options.ClearOutboxBeforeTest.ShouldBeTrue();
        options.ClearInboxBeforeTest.ShouldBeTrue();
        options.ResetSagasBeforeTest.ShouldBeTrue();
        options.ClearScheduledMessagesBeforeTest.ShouldBeTrue();
        options.ClearDeadLetterBeforeTest.ShouldBeTrue();
        options.DefaultWaitTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.PollingInterval.ShouldBe(TimeSpan.FromMilliseconds(100));
    }
}
