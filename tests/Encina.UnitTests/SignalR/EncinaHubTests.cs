#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern

using System.Security.Claims;
using System.Text.Json;
using Encina.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.SignalR;

/// <summary>
/// Tests for the <see cref="EncinaHub"/> dispatch paths of registered types; the allow-list,
/// the authentication gate and the uniform denial are covered by <see cref="EncinaHubAllowListTests"/>.
/// </summary>
public sealed class EncinaHubTests
{
    private static readonly JsonElement Payload = JsonSerializer.SerializeToElement(new { Message = "test", Query = "test" });

    [Fact]
    public void Constructor_SetsEncinaProperty()
    {
        using var hub = EncinaHubTestFactory.CreateHub(new SignalROptions(), out _);

        Assert.NotNull(hub.GetEncina());
    }

    [Fact]
    public async Task PublishNotification_WithUnknownType_DoesNotPublish()
    {
        using var hub = EncinaHubTestFactory.CreateHub(new SignalROptions(), out var encina);

        await hub.PublishNotification("NonExistent.Notification", Payload);

        encina.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task SendCommand_WithRegisteredFullName_SendsCommandAndReturnsData()
    {
        var options = new SignalROptions().AllowRequest<TestCommand>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);
        encina.Send(Arg.Any<IRequest<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, string>("done")));

        var json = JsonSerializer.Serialize(await hub.SendCommand(typeof(TestCommand).FullName!, Payload));

        json.ShouldBe("""{"success":true,"data":"done"}""");
        await encina.Received(1).Send(Arg.Is<IRequest<string>>(r => ((TestCommand)r).Message == "test"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendQuery_WhenEncinaReturnsError_ReturnsErrorCodeAndMessage()
    {
        var options = new SignalROptions().AllowRequest<TestQuery>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);
        encina.Send(Arg.Any<IRequest<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("orders.missing", "Order missing"))));

        var response = EncinaHubTestFactory.Parse(await hub.SendQuery(typeof(TestQuery).FullName!, Payload));

        response.success.ShouldBeFalse();
        response.error!.code.ShouldBe("orders.missing");
    }

    [Fact]
    public async Task SendCommand_WhenEncinaThrows_ReturnsExecutionFailedWithGenericMessage()
    {
        var options = new SignalROptions().AllowRequest<TestCommand>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);
        encina.Send(Arg.Any<IRequest<string>>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, string>>>(_ => throw new InvalidOperationException("boom"));

        var response = EncinaHubTestFactory.Parse(await hub.SendCommand(typeof(TestCommand).FullName!, Payload));

        response.error!.code.ShouldBe("command.execution_failed");
        response.error.message.ShouldBe("An error occurred while processing your request.");
    }

    [Fact]
    public async Task SendQuery_WhenEncinaThrowsWithDetailedErrors_ReturnsExceptionDetails()
    {
        var options = new SignalROptions { IncludeDetailedErrors = true }.AllowRequest<TestQuery>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);
        encina.Send(Arg.Any<IRequest<string>>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, string>>>(_ => throw new InvalidOperationException("boom"));

        var response = EncinaHubTestFactory.Parse(await hub.SendQuery(typeof(TestQuery).FullName!, Payload));

        response.error!.code.ShouldBe("query.execution_failed");
        response.error.message!.ShouldContain("boom");
    }

    [Fact]
    public async Task SendCommand_WithDetailedErrorsEnabled_DoesNotEchoUnknownTypeName()
    {
        using var hub = EncinaHubTestFactory.CreateHub(new SignalROptions { IncludeDetailedErrors = true }, out _);

        var response = EncinaHubTestFactory.Parse(await hub.SendCommand("NonExistent.CommandType", Payload));

        response.error!.code.ShouldBe("command.type_not_found");
        response.error.message!.ShouldNotContain("NonExistent");
    }

    [Fact]
    public async Task SendCommand_WithNullPayload_ReturnsDeserializationError()
    {
        var options = new SignalROptions().AllowRequest<TestCommand>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out _);

        var response = EncinaHubTestFactory.Parse(
            await hub.SendCommand(typeof(TestCommand).FullName!, JsonSerializer.SerializeToElement<object?>(null)));

        response.error!.code.ShouldBe("command.deserialization_failed");
    }

    [Fact]
    public async Task SendQuery_WithNullPayload_ReturnsDeserializationError()
    {
        var options = new SignalROptions().AllowRequest<TestQuery>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out _);

        var response = EncinaHubTestFactory.Parse(
            await hub.SendQuery(typeof(TestQuery).FullName!, JsonSerializer.SerializeToElement<object?>(null)));

        response.error!.code.ShouldBe("query.deserialization_failed");
    }

    [Fact]
    public async Task PublishNotification_WithRegisteredType_PublishesNotification()
    {
        var options = new SignalROptions().AllowNotification<TestHubNotification>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);

        await hub.PublishNotification(typeof(TestHubNotification).FullName!, Payload);

        await encina.Received(1).Publish(Arg.Is<TestHubNotification>(n => n.Message == "test"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishNotification_WithNullPayload_DoesNotPublish()
    {
        var options = new SignalROptions().AllowNotification<TestHubNotification>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);

        await hub.PublishNotification(typeof(TestHubNotification).FullName!, JsonSerializer.SerializeToElement<object?>(null));

        encina.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task PublishNotification_WhenEncinaThrows_DoesNotThrow()
    {
        var options = new SignalROptions().AllowNotification<TestHubNotification>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);
        encina.Publish(Arg.Any<TestHubNotification>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("boom"));

        var exception = await Record.ExceptionAsync(() => hub.PublishNotification(typeof(TestHubNotification).FullName!, Payload));

        exception.ShouldBeNull();
    }

    // Test types - public for NSubstitute proxy generation.
    public sealed record TestCommand(string Message) : ICommand<string>;

    public sealed record TestQuery(string Query) : IQuery<string>;

    public sealed record TestHubNotification(string Message) : INotification;
}

/// <summary>Builds hubs over a substitute <see cref="IEncina"/> and a caller of the test's choosing.</summary>
internal static class EncinaHubTestFactory
{
    internal static TestEncinaHub CreateHub(
        SignalROptions options,
        out IEncina encina,
        ClaimsPrincipal? user = null,
        ILogger? logger = null,
        bool authenticated = true)
    {
        encina = Substitute.For<IEncina>();
        var hub = new TestEncinaHub(encina, Options.Create(options), logger ?? NullLogger.Instance);
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("test-connection-id");
        context.ConnectionAborted.Returns(CancellationToken.None);
        context.User.Returns(user ?? (authenticated ? AuthenticatedUser() : null));
        hub.Context = context;
        return hub;
    }

    internal static ClaimsPrincipal AuthenticatedUser()
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], authenticationType: "Test"));

    internal static HubResponse Parse(object result)
        => JsonSerializer.Deserialize<HubResponse>(JsonSerializer.Serialize(result))
           ?? throw new InvalidOperationException("Failed to parse hub response");

#pragma warning disable IDE1006, SA1300 // Lower-case names mirror the hub's JSON response shape.
    internal sealed record HubResponse(bool success, ErrorInfo? error);

    internal sealed record ErrorInfo(string code, string? message);
#pragma warning restore IDE1006, SA1300
}

/// <summary>A concrete hub over the abstract <see cref="EncinaHub"/>; public for NSubstitute.</summary>
public sealed class TestEncinaHub : EncinaHub
{
    public TestEncinaHub(IEncina encina, IOptions<SignalROptions> options, ILogger logger)
        : base(encina, options, logger)
    {
    }

    public IEncina GetEncina() => Encina;
}
