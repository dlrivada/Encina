using Aspire.Hosting;
using Encina.Aspire.Testing;
using Encina.Messaging.Outbox;
using Encina.Testing.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.GuardTests.Aspire;

/// <summary>
/// Guard tests for the remaining public methods of Encina.Aspire.Testing: null application on
/// the generic and saga/dead-letter overloads, and argument validation that runs after the
/// application guard against a real (never started) <see cref="DistributedApplication"/>.
/// </summary>
[Trait("Category", "Guard")]
public sealed class AspireTestingArgumentGuardTests : IDisposable
{
    private sealed class Marker;

    private readonly DistributedApplication _app;

    public AspireTestingArgumentGuardTests()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(AspireTestingArgumentGuardTests).Assembly.GetName().Name,
            ProjectDirectory = AppContext.BaseDirectory,
            DisableDashboard = true
        });
        builder.Services.AddSingleton(new EncinaTestSupportOptions());
        builder.Services.AddEncinaFakes();
        builder.Services.AddSingleton<EncinaTestContext>();
        _app = builder.Build();
    }

    public void Dispose() => _app.Dispose();

    // ─── Null application on remaining overloads ───

    [Fact]
    public async Task AssertOutboxContainsAsync_Generic_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.AssertOutboxContainsAsync<Marker>(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task AssertInboxProcessedAsync_Generic_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.AssertInboxProcessedAsync<Marker>(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task AssertSagaCompletedAsync_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.AssertSagaCompletedAsync<Marker>(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task AssertSagaCompensatedAsync_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.AssertSagaCompensatedAsync<Marker>(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void GetRunningSagas_NullApp_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationExtensions.GetRunningSagas<Marker>(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task AssertDeadLetterContainsAsync_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.AssertDeadLetterContainsAsync<Marker>(null!));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public async Task WaitForSagaCompletionAsync_NullApp_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => DistributedApplicationExtensions.WaitForSagaCompletionAsync<Marker>(null!, Guid.NewGuid()));
        ex.ParamName.ShouldBe("app");
    }

    [Fact]
    public void WithEncinaTestSupport_NullBuilder_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            DistributedApplicationTestingBuilderExtensions.WithEncinaTestSupport(null!));
        ex.ParamName.ShouldBe("builder");
    }

    // ─── Argument validation with a real application ───

    [Fact]
    public async Task AssertOutboxContainsAsync_Predicate_NullPredicate_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _app.AssertOutboxContainsAsync((Func<IOutboxMessage, bool>)null!));
        ex.ParamName.ShouldBe("predicate");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssertInboxProcessedAsync_InvalidMessageId_Throws(string? messageId)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => _app.AssertInboxProcessedAsync(messageId!));
        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public async Task WaitForOutboxProcessingAsync_NegativeCount_Throws()
    {
        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => _app.WaitForOutboxProcessingAsync(-1));
        ex.ParamName.ShouldBe("expectedMessageCount");
    }

    [Fact]
    public void SimulateOutboxMessageFailure_EmptyGuid_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _app.SimulateOutboxMessageFailure(Guid.Empty));
        ex.ParamName.ShouldBe("messageId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SimulateInboxMessageFailure_InvalidMessageId_Throws(string? messageId)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _app.SimulateInboxMessageFailure(messageId!));
        ex.ParamName.ShouldBe("messageId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SimulateInboxExpiration_InvalidMessageId_Throws(string? messageId)
    {
        var ex = Should.Throw<ArgumentException>(() =>
            _app.SimulateInboxExpiration(messageId!));
        ex.ParamName.ShouldBe("messageId");
    }

    [Theory]
    [InlineData(null, "{}", "requestType")]
    [InlineData("", "{}", "requestType")]
    [InlineData("Type.A", null, "requestContent")]
    [InlineData("Type.A", "  ", "requestContent")]
    public async Task AddToDeadLetterAsync_InvalidArguments_Throws(string? requestType, string? requestContent, string paramName)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => _app.AddToDeadLetterAsync(requestType!, requestContent!));
        ex.ParamName.ShouldBe(paramName);
    }
}
