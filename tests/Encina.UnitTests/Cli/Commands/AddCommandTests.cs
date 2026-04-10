using System.CommandLine;

using Encina.Cli.Commands;

using Shouldly;

using Xunit;

namespace Encina.UnitTests.Cli.Commands;

/// <summary>
/// Unit tests for <see cref="AddCommand"/> exercising the System.CommandLine subcommand
/// tree, aliases, and provider → package mapping switch statements.
/// </summary>
/// <remarks>
/// The <c>SetAction</c> delegates ultimately call <c>PackageManager.AddPackagesAsync</c>, which
/// tries to locate a <c>.csproj</c> in the current working directory. In the unit test runner
/// process the current directory does contain <c>Encina.UnitTests.csproj</c>, so the execution
/// path reaches <c>dotnet add package ...</c>. We intentionally only exercise the command graph
/// construction for the mapping paths, and use <b>unknown providers</b> to drive the error
/// branches without touching the network / filesystem.
/// </remarks>
public class AddCommandTests
{
    private static RootCommand BuildRoot()
    {
        var root = new RootCommand("test");
        root.Subcommands.Add(AddCommand.Create());
        return root;
    }

    [Fact]
    public void Create_ReturnsCommandWithAllSubcommands()
    {
        var command = AddCommand.Create();

        command.Name.ShouldBe("add");
        command.Subcommands.Count.ShouldBe(6);
        command.Subcommands.ShouldContain(c => c.Name == "caching");
        command.Subcommands.ShouldContain(c => c.Name == "database");
        command.Subcommands.ShouldContain(c => c.Name == "transport");
        command.Subcommands.ShouldContain(c => c.Name == "validation");
        command.Subcommands.ShouldContain(c => c.Name == "resilience");
        command.Subcommands.ShouldContain(c => c.Name == "observability");
    }

    [Fact]
    public void Create_CachingCommand_HasCacheAlias()
    {
        var add = AddCommand.Create();
        var caching = add.Subcommands.First(c => c.Name == "caching");
        caching.Aliases.ShouldContain("cache");
    }

    [Fact]
    public void Create_DatabaseCommand_HasDbAlias()
    {
        var add = AddCommand.Create();
        var db = add.Subcommands.First(c => c.Name == "database");
        db.Aliases.ShouldContain("db");
    }

    [Fact]
    public void Create_TransportCommand_HasMessagingAlias()
    {
        var add = AddCommand.Create();
        var transport = add.Subcommands.First(c => c.Name == "transport");
        transport.Aliases.ShouldContain("messaging");
    }

    [Fact]
    public void Create_ObservabilityCommand_HasOtelAlias()
    {
        var add = AddCommand.Create();
        var otel = add.Subcommands.First(c => c.Name == "observability");
        otel.Aliases.ShouldContain("otel");
    }

    // ─── Error branches: unknown providers drive each switch _ => null path ───

    [Theory]
    [InlineData("add", "caching", "nosuchprovider")]
    [InlineData("add", "database", "nosuchdb")]
    [InlineData("add", "transport", "nosuchtransport")]
    [InlineData("add", "validation", "nosuchvalidator")]
    [InlineData("add", "resilience", "nosuchresilience")]
    public async Task Invoke_UnknownProvider_ReturnsErrorExitCode(params string[] args)
    {
        var root = BuildRoot();
        var parseResult = root.Parse(args);

        var exitCode = await parseResult.InvokeAsync();

        exitCode.ShouldBe(1);
    }

    // ─── Parser-level: valid mapping reaches the execute branch ───

    [Theory]
    [InlineData("add", "caching", "memory")]
    [InlineData("add", "caching", "redis")]
    [InlineData("add", "caching", "valkey")]
    [InlineData("add", "caching", "garnet")]
    [InlineData("add", "caching", "dragonfly")]
    [InlineData("add", "caching", "keydb")]
    [InlineData("add", "caching", "hybrid")]
    public void Parse_CachingProviders_SucceedsAtArgumentLayer(params string[] args)
    {
        var root = BuildRoot();
        var parseResult = root.Parse(args);

        parseResult.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("add", "database", "efcore")]
    [InlineData("add", "database", "entityframeworkcore")]
    [InlineData("add", "database", "dapper-sqlserver")]
    [InlineData("add", "database", "dapper-postgresql")]
    [InlineData("add", "database", "dapper-postgres")]
    [InlineData("add", "database", "dapper-mysql")]
    [InlineData("add", "database", "dapper-sqlite")]
    [InlineData("add", "database", "dapper-oracle")]
    [InlineData("add", "database", "ado-sqlserver")]
    [InlineData("add", "database", "ado-postgresql")]
    [InlineData("add", "database", "ado-postgres")]
    [InlineData("add", "database", "ado-mysql")]
    [InlineData("add", "database", "ado-sqlite")]
    [InlineData("add", "database", "ado-oracle")]
    [InlineData("add", "database", "mongodb")]
    [InlineData("add", "database", "marten")]
    public void Parse_DatabaseProviders_SucceedsAtArgumentLayer(params string[] args)
    {
        var root = BuildRoot();
        var parseResult = root.Parse(args);

        parseResult.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("add", "transport", "rabbitmq")]
    [InlineData("add", "transport", "kafka")]
    [InlineData("add", "transport", "azureservicebus")]
    [InlineData("add", "transport", "servicebus")]
    [InlineData("add", "transport", "sqs")]
    [InlineData("add", "transport", "amazonsqs")]
    [InlineData("add", "transport", "nats")]
    [InlineData("add", "transport", "mqtt")]
    [InlineData("add", "transport", "signalr")]
    [InlineData("add", "transport", "redis")]
    [InlineData("add", "transport", "redis-pubsub")]
    public void Parse_TransportProviders_SucceedsAtArgumentLayer(params string[] args)
    {
        var root = BuildRoot();
        var parseResult = root.Parse(args);

        parseResult.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("add", "validation", "fluentvalidation")]
    [InlineData("add", "validation", "fluent")]
    [InlineData("add", "validation", "dataannotations")]
    [InlineData("add", "validation", "annotations")]
    [InlineData("add", "validation", "minivalidator")]
    [InlineData("add", "validation", "mini")]
    public void Parse_ValidationProviders_SucceedsAtArgumentLayer(params string[] args)
    {
        var root = BuildRoot();
        var parseResult = root.Parse(args);

        parseResult.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("add", "resilience", "polly")]
    [InlineData("add", "resilience", "standard")]
    public void Parse_ResilienceProviders_SucceedsAtArgumentLayer(params string[] args)
    {
        var root = BuildRoot();
        var parseResult = root.Parse(args);

        parseResult.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_Observability_NoSubArgument_SucceedsAtArgumentLayer()
    {
        var root = BuildRoot();
        var parseResult = root.Parse(["add", "observability"]);

        parseResult.Errors.ShouldBeEmpty();
    }
}
