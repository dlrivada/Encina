#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern

using System.Security.Claims;
using System.Text.Json;
using Encina.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.SignalR;

/// <summary>
/// Tests for the <see cref="EncinaHub"/> allow-list (#3677): deny-all default, lookup before
/// deserialization, one indistinguishable denial, bounded state and logs, and the fail-closed
/// authentication gate.
/// </summary>
public sealed class EncinaHubAllowListTests
{
    private static readonly JsonElement Payload = JsonSerializer.SerializeToElement(new { Value = "x" });

    [Fact]
    public async Task UnregisteredType_IsNeverDeserialized()
    {
        using var hub = EncinaHubTestFactory.CreateHub(new SignalROptions(), out var encina);
        var before = ProbeCommand.Constructions;

        var response = EncinaHubTestFactory.Parse(await hub.SendCommand(typeof(ProbeCommand).FullName!, Payload));

        ProbeCommand.Constructions.ShouldBe(before);
        response.error!.code.ShouldBe("command.type_not_found");
        encina.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task WrongKindAndAnonymousCalls_NeverDeserialize()
    {
        var options = new SignalROptions().AllowRequest<ProbeCommand>().AllowNotification<ProbeNotification>();
        using var authenticatedHub = EncinaHubTestFactory.CreateHub(options, out _);
        using var anonymousHub = EncinaHubTestFactory.CreateHub(options, out _, authenticated: false);
        var commandsBefore = ProbeCommand.Constructions;
        var notificationsBefore = ProbeNotification.Constructions;

        await authenticatedHub.SendQuery(typeof(ProbeCommand).FullName!, Payload);
        await authenticatedHub.PublishNotification(typeof(ProbeCommand).FullName!, Payload);
        await authenticatedHub.SendCommand(typeof(ProbeNotification).FullName!, Payload);
        await anonymousHub.SendCommand(typeof(ProbeCommand).FullName!, Payload);
        await anonymousHub.PublishNotification(typeof(ProbeNotification).FullName!, Payload);

        ProbeCommand.Constructions.ShouldBe(commandsBefore);
        ProbeNotification.Constructions.ShouldBe(notificationsBefore);
    }

    [Fact]
    public async Task RegisteredProbe_IsDeserializedForAnAuthenticatedCaller()
    {
        // Positive control: the probe counts a construction once the call passes the gate.
        var options = new SignalROptions().AllowRequest<ProbeCommand>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out _);
        var before = ProbeCommand.Constructions;

        var response = EncinaHubTestFactory.Parse(await hub.SendCommand(typeof(ProbeCommand).FullName!, Payload));

        ProbeCommand.Constructions.ShouldBe(before + 1);
        response.error!.code.ShouldBe("command.execution_failed");
    }

    [Fact]
    public async Task UnknownNotExposedWrongKindAndAnonymous_CommandResponsesAreByteIdentical()
    {
        var options = new SignalROptions().AllowRequest<AllowedCommand>().AllowRequest<AllowedQuery>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out _);
        using var anonymousHub = EncinaHubTestFactory.CreateHub(options, out _, authenticated: false);

        string[] responses =
        [
            JsonSerializer.Serialize(await hub.SendCommand("No.Such.Type.Anywhere", Payload)),
            JsonSerializer.Serialize(await hub.SendCommand(typeof(Uri).FullName!, Payload)),
            JsonSerializer.Serialize(await hub.SendCommand(typeof(AllowedQuery).FullName!, Payload)),
            JsonSerializer.Serialize(await hub.SendCommand(nameof(AllowedCommand), Payload)),
            JsonSerializer.Serialize(await hub.SendCommand(typeof(AllowedCommand).FullName!.ToUpperInvariant(), Payload)),
            JsonSerializer.Serialize(await hub.SendCommand(typeof(AllowedCommand).AssemblyQualifiedName!, Payload)),
            JsonSerializer.Serialize(await hub.SendCommand(null!, Payload)),
            JsonSerializer.Serialize(await anonymousHub.SendCommand(typeof(AllowedCommand).FullName!, Payload)),
        ];

        responses.Distinct().ShouldHaveSingleItem().ShouldBe("""{"success":false,"error":{"code":"command.type_not_found","message":"Command type not found."}}""");
    }

    [Fact]
    public async Task UnknownNotExposedWrongKindAndAnonymous_QueryResponsesAreByteIdentical()
    {
        var options = new SignalROptions().AllowRequest<AllowedCommand>().AllowRequest<AllowedQuery>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out _);
        using var anonymousHub = EncinaHubTestFactory.CreateHub(
            options, out _, user: new ClaimsPrincipal(new ClaimsIdentity()));

        string[] responses =
        [
            JsonSerializer.Serialize(await hub.SendQuery("No.Such.Type.Anywhere", Payload)),
            JsonSerializer.Serialize(await hub.SendQuery(typeof(Uri).FullName!, Payload)),
            JsonSerializer.Serialize(await hub.SendQuery(typeof(AllowedCommand).FullName!, Payload)),
            JsonSerializer.Serialize(await anonymousHub.SendQuery(typeof(AllowedQuery).FullName!, Payload)),
        ];

        responses.Distinct().ShouldHaveSingleItem().ShouldBe("""{"success":false,"error":{"code":"query.type_not_found","message":"Query type not found."}}""");
    }

    [Fact]
    public async Task TenThousandDistinctNames_LeaveAllowListUnchanged()
    {
        var options = new SignalROptions().AllowRequest<AllowedQuery>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);
        var allowListBefore = options.GetAllowList();

        for (var i = 0; i < 10_000; i++)
        {
            await hub.SendQuery($"Probe.Type{i}", Payload);
        }

        var allowListAfter = options.GetAllowList();
        allowListAfter.ShouldBeSameAs(allowListBefore);
        allowListAfter.Count.ShouldBe(1);
        allowListAfter.Keys.ShouldBe(new[] { typeof(AllowedQuery).FullName! });
        encina.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task FourMegabyteName_IsNeverLogged()
    {
        var logger = new FakeLogger();
        using var hub = EncinaHubTestFactory.CreateHub(new SignalROptions(), out _, logger: logger);
        var hugeName = new string('A', 4 * 1024 * 1024);

        await hub.SendCommand(hugeName, Payload);
        await hub.SendQuery(hugeName, Payload);
        await hub.PublishNotification(hugeName, Payload);

        var records = logger.Collector.GetSnapshot();
        records.Count.ShouldBe(3);
        records.ShouldAllBe(record => record.Level == LogLevel.Debug && record.Id.Id == 4608);
        records.ShouldAllBe(record => record.Message.Length < 256);
        records.ShouldAllBe(record => record.GetStructuredStateValue("TypeName") == new string('A', 64));
    }

    [Fact]
    public async Task ControlCharactersInName_AreStrippedFromLogs()
    {
        var logger = new FakeLogger();
        using var hub = EncinaHubTestFactory.CreateHub(new SignalROptions(), out _, logger: logger);

        const char RightToLeftOverride = (char)0x202E;
        const char Escape = (char)0x1B;

        await hub.SendCommand($"Evil\r\nFAKE ENTRY{Escape}[31m{RightToLeftOverride}gnp.exe", Payload);

        logger.Collector.LatestRecord.GetStructuredStateValue("TypeName").ShouldBe("EvilFAKE ENTRY[31mgnp.exe");
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("Orders.GetOrder", "Orders.GetOrder")]
    [InlineData("A\tB", "AB")]
    public void ClientTypeName_ForLog_StripsUnsafeCharacters(string? value, string expected)
        => ClientTypeName.ForLog(value).ShouldBe(expected);

    [Fact]
    public void ClientTypeName_ForLog_StripsSeparatorsAndSurrogates()
    {
        // Built from code points so no invisible character is stored in this source file.
        var value = string.Concat("A", (char)0x2028, "B", (char)0x2029, "C", char.ConvertFromUtf32(0x1F600), "D");

        ClientTypeName.ForLog(value).ShouldBe("ABCD");
    }

    [Fact]
    public void ClientTypeName_ForLog_TruncatesBeforeStripping()
    {
        var value = new string('\n', 64) + "Visible";

        ClientTypeName.ForLog(value).ShouldBeEmpty();
        ClientTypeName.ForLog(new string('B', 65)).Length.ShouldBe(ClientTypeName.MaxLoggedLength);
    }

    [Fact]
    public async Task AnonymousCaller_IsDeniedUnlessAllowAnonymous()
    {
        var options = new SignalROptions().AllowRequest<AllowedCommand>();
        using var noUserHub = EncinaHubTestFactory.CreateHub(options, out var noUserEncina, authenticated: false);
        using var unauthenticatedHub = EncinaHubTestFactory.CreateHub(
            options, out var unauthenticatedEncina, user: new ClaimsPrincipal(new ClaimsIdentity()));

        var first = EncinaHubTestFactory.Parse(await noUserHub.SendCommand(typeof(AllowedCommand).FullName!, Payload));
        var second = EncinaHubTestFactory.Parse(await unauthenticatedHub.SendCommand(typeof(AllowedCommand).FullName!, Payload));

        first.error!.code.ShouldBe("command.type_not_found");
        second.error!.code.ShouldBe("command.type_not_found");
        noUserEncina.ReceivedCalls().ShouldBeEmpty();
        unauthenticatedEncina.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task AllowAnonymousEntry_RunsForAnonymousCaller()
    {
        var options = new SignalROptions()
            .AllowRequest<AllowedQuery>(allowAnonymous: true)
            .AllowNotification<AllowedNotification>(allowAnonymous: true);
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina, authenticated: false);
        encina.Send(Arg.Any<IRequest<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("pong")));

        var json = JsonSerializer.Serialize(await hub.SendQuery(typeof(AllowedQuery).FullName!, Payload));
        await hub.PublishNotification(typeof(AllowedNotification).FullName!, Payload);

        json.ShouldBe("""{"success":true,"data":"pong"}""");
        await encina.Received(1).Publish(Arg.Any<AllowedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisteredCommand_CannotBeInvokedThroughSendQuery_AndViceVersa()
    {
        var options = new SignalROptions().AllowRequest<AllowedCommand>().AllowRequest<AllowedQuery>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina);

        var commandAsQuery = EncinaHubTestFactory.Parse(await hub.SendQuery(typeof(AllowedCommand).FullName!, Payload));
        var queryAsCommand = EncinaHubTestFactory.Parse(await hub.SendCommand(typeof(AllowedQuery).FullName!, Payload));

        commandAsQuery.error!.code.ShouldBe("query.type_not_found");
        queryAsCommand.error!.code.ShouldBe("command.type_not_found");
        encina.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task NotificationPath_DeniesUnknownWrongKindAndAnonymousTheSameWay()
    {
        var options = new SignalROptions().AllowRequest<AllowedCommand>().AllowNotification<AllowedNotification>();
        var logger = new FakeLogger();
        using var hub = EncinaHubTestFactory.CreateHub(options, out var encina, logger: logger);
        using var anonymousHub = EncinaHubTestFactory.CreateHub(options, out var anonymousEncina, logger: logger, authenticated: false);

        await hub.PublishNotification("No.Such.Notification", Payload);
        await hub.PublishNotification(typeof(AllowedCommand).FullName!, Payload);
        await anonymousHub.PublishNotification(typeof(AllowedNotification).FullName!, Payload);

        encina.ReceivedCalls().ShouldBeEmpty();
        anonymousEncina.ReceivedCalls().ShouldBeEmpty();
        logger.Collector.GetSnapshot().Select(record => (record.Id.Id, record.Level)).Distinct()
            .ShouldHaveSingleItem().ShouldBe((4608, LogLevel.Debug));
    }

    [Fact]
    public void DuplicateRegistration_Throws()
    {
        var options = new SignalROptions().AllowRequest<AllowedCommand>();

        Should.Throw<InvalidOperationException>(() => options.AllowRequest<AllowedCommand>(allowAnonymous: true))
            .Message.ShouldContain(typeof(AllowedCommand).FullName!);
        Should.Throw<InvalidOperationException>(() => options.AllowNotification(typeof(DualNotificationCommand)).AllowRequest(typeof(DualNotificationCommand)));
    }

    [Fact]
    public void Registration_RecordsKindKeyAndAnonymousFlag()
    {
        var options = new SignalROptions();

        var returned = options
            .AllowRequest(typeof(AllowedCommand))
            .AllowRequest<UnitCommand>()
            .AllowRequest<AllowedQuery>(allowAnonymous: true)
            .AllowNotification(typeof(AllowedNotification));

        returned.ShouldBeSameAs(options);
        var allowList = options.GetAllowList();
        allowList[typeof(AllowedCommand).FullName!].ShouldBe(new HubTypeRegistration(typeof(AllowedCommand), HubMessageKind.Command, false));
        allowList[typeof(UnitCommand).FullName!].Kind.ShouldBe(HubMessageKind.Command);
        allowList[typeof(AllowedQuery).FullName!].ShouldBe(new HubTypeRegistration(typeof(AllowedQuery), HubMessageKind.Query, true));
        allowList[typeof(AllowedNotification).FullName!].Kind.ShouldBe(HubMessageKind.Notification);
    }

    [Theory]
    [InlineData(typeof(PlainRequest))]
    [InlineData(typeof(CommandAndQuery))]
    [InlineData(typeof(AbstractCommand))]
    [InlineData(typeof(ICommand<string>))]
    [InlineData(typeof(GenericCommand<>))]
    [InlineData(typeof(AllowedNotification))]
    public void AllowRequest_WithTypeThatIsNotExactlyOneConcreteCommandOrQuery_Throws(Type type)
        => Should.Throw<ArgumentException>(() => new SignalROptions().AllowRequest(type)).ParamName.ShouldBe("requestType");

    [Theory]
    [InlineData(typeof(AllowedCommand))]
    [InlineData(typeof(INotification))]
    public void AllowNotification_WithTypeThatIsNotAConcreteNotification_Throws(Type type)
        => Should.Throw<ArgumentException>(() => new SignalROptions().AllowNotification(type)).ParamName.ShouldBe("notificationType");

    [Fact]
    public void AllowRequestAndAllowNotification_WithNullType_ThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new SignalROptions().AllowRequest(null!)).ParamName.ShouldBe("requestType");
        Should.Throw<ArgumentNullException>(() => new SignalROptions().AllowNotification(null!)).ParamName.ShouldBe("notificationType");
    }

    [Fact]
    public void Registration_AfterAHubReadTheAllowList_Throws()
    {
        var options = new SignalROptions().AllowRequest<AllowedCommand>();
        using var hub = EncinaHubTestFactory.CreateHub(options, out _);

        Should.Throw<InvalidOperationException>(() => options.AllowRequest<AllowedQuery>());
        options.GetAllowList().Count.ShouldBe(1);
    }

    [Fact]
    public async Task StartupCheck_LogsEachAnonymousEntryOnce_AndFreezesTheAllowList()
    {
        var options = new SignalROptions()
            .AllowRequest<AllowedCommand>()
            .AllowRequest<AllowedQuery>(allowAnonymous: true)
            .AllowNotification<AllowedNotification>(allowAnonymous: true);
        var logger = new FakeLogger<SignalRAllowListStartupCheck>();
        var check = new SignalRAllowListStartupCheck(Options.Create(options), logger, [MappedHub(typeof(TestEncinaHub))]);

        await check.StartingAsync(CancellationToken.None);
        await check.StartAsync(CancellationToken.None);
        await check.StartedAsync(CancellationToken.None);
        await check.StoppingAsync(CancellationToken.None);
        await check.StopAsync(CancellationToken.None);
        await check.StoppedAsync(CancellationToken.None);

        var records = logger.Collector.GetSnapshot();
        records.ShouldAllBe(record => record.Id.Id == 4612 && record.Level == LogLevel.Information);
        records.Select(record => record.GetStructuredStateValue("TypeName")).Order(StringComparer.Ordinal).ShouldBe(
            new[] { typeof(AllowedNotification).FullName, typeof(AllowedQuery).FullName }.Order(StringComparer.Ordinal));
        Should.Throw<InvalidOperationException>(() => options.AllowRequest<UnitCommand>());
    }

    [Fact]
    public async Task StartupCheck_WithEmptyAllowListAndMappedEncinaHub_Warns()
    {
        var logger = new FakeLogger<SignalRAllowListStartupCheck>();
        var check = new SignalRAllowListStartupCheck(
            Options.Create(new SignalROptions()), logger, [MappedHub(typeof(PlainHub)), MappedHub(typeof(TestEncinaHub))]);

        await check.StartedAsync(CancellationToken.None);

        logger.Collector.LatestRecord.Id.Id.ShouldBe(4613);
        logger.Collector.LatestRecord.Level.ShouldBe(LogLevel.Warning);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task StartupCheck_WithoutMappedEncinaHubOrWithRegistrations_DoesNotWarn(bool mapPlainHub, bool registerType)
    {
        var options = new SignalROptions();
        if (registerType)
        {
            options.AllowRequest<AllowedCommand>();
        }

        EndpointDataSource[] sources = mapPlainHub
            ? [MappedHub(typeof(PlainHub)), new DefaultEndpointDataSource(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "plain"))]
            : registerType ? [MappedHub(typeof(TestEncinaHub))] : [];
        var logger = new FakeLogger<SignalRAllowListStartupCheck>();

        await new SignalRAllowListStartupCheck(Options.Create(options), logger, sources).StartedAsync(CancellationToken.None);

        logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public void StartupCheck_WithNullArguments_Throws()
    {
        var options = Options.Create(new SignalROptions());
        var logger = NullLogger<SignalRAllowListStartupCheck>.Instance;

        Should.Throw<ArgumentNullException>(() => new SignalRAllowListStartupCheck(null!, logger, []));
        Should.Throw<ArgumentNullException>(() => new SignalRAllowListStartupCheck(options, null!, []));
        Should.Throw<ArgumentNullException>(() => new SignalRAllowListStartupCheck(options, logger, null!));
    }

    [Fact]
    public void AddEncinaSignalR_RegistersEverythingItResolves_ValidateOnBuild()
    {
        var services = new ServiceCollection();
        services.AddEncinaSignalR(options => options.AllowRequest<AllowedCommand>());
        services.AddEncinaSignalR();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetServices<IHostedService>().ShouldHaveSingleItem().ShouldBeOfType<SignalRAllowListStartupCheck>();
        provider.GetRequiredService<ISignalRNotificationBroadcaster>().ShouldNotBeNull();
        provider.GetRequiredService<IOptions<SignalROptions>>().Value.GetAllowList().Count.ShouldBe(1);
    }

    private static DefaultEndpointDataSource MappedHub(Type hubType)
        => new(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new HubMetadata(hubType)), hubType.Name));

    // Test types - public for NSubstitute proxy generation and JSON deserialization.
    public sealed class ProbeCommand : ICommand<string>
    {
        private static int _constructions;

        public ProbeCommand()
        {
            Interlocked.Increment(ref _constructions);
            throw new InvalidOperationException("Constructed: the hub deserialized this type.");
        }

        public static int Constructions => Volatile.Read(ref _constructions);
    }

    public sealed class ProbeNotification : INotification
    {
        private static int _constructions;

        public ProbeNotification()
        {
            Interlocked.Increment(ref _constructions);
            throw new InvalidOperationException("Constructed: the hub deserialized this type.");
        }

        public static int Constructions => Volatile.Read(ref _constructions);
    }

    public sealed record AllowedCommand(string? Value) : ICommand<string>;

    public sealed record AllowedQuery(string? Value) : IQuery<string>;

    public sealed record AllowedNotification(string? Value) : INotification;

    public sealed record UnitCommand : ICommand;

    public sealed record PlainRequest : IRequest<string>;

    public sealed record CommandAndQuery : ICommand<string>, IQuery<string>;

    public abstract record AbstractCommand : ICommand<string>;

    public sealed record GenericCommand<T> : ICommand<T>;

    public sealed record DualNotificationCommand : ICommand<string>, INotification;

    public sealed class PlainHub : Hub;
}
