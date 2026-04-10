using System.CommandLine;

using Encina.Cli.Commands;

using Shouldly;

using Xunit;

namespace Encina.UnitTests.Cli.Commands;

/// <summary>
/// Unit tests for <see cref="GenerateCommand"/> subcommand graph and end-to-end
/// invocations through <see cref="CodeGenerator"/>.
/// </summary>
public class GenerateCommandTests : IDisposable
{
    private readonly string _tempDir;

    public GenerateCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-gen-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private static RootCommand BuildRoot()
    {
        var root = new RootCommand("test");
        root.Subcommands.Add(GenerateCommand.Create());
        return root;
    }

    [Fact]
    public void Create_HasExpectedSubcommandsAndAlias()
    {
        var command = GenerateCommand.Create();

        command.Name.ShouldBe("generate");
        command.Aliases.ShouldContain("g");
        command.Subcommands.Count.ShouldBe(5);
        command.Subcommands.ShouldContain(c => c.Name == "handler");
        command.Subcommands.ShouldContain(c => c.Name == "query");
        command.Subcommands.ShouldContain(c => c.Name == "saga");
        command.Subcommands.ShouldContain(c => c.Name == "notification");
        command.Subcommands.ShouldContain(c => c.Name == "stryker");
    }

    [Fact]
    public void Create_HandlerCommand_HasAliases()
    {
        var gen = GenerateCommand.Create();
        var handler = gen.Subcommands.First(c => c.Name == "handler");
        handler.Aliases.ShouldContain("h");
        handler.Aliases.ShouldContain("command");
    }

    [Fact]
    public void Create_NotificationCommand_HasAliases()
    {
        var gen = GenerateCommand.Create();
        var n = gen.Subcommands.First(c => c.Name == "notification");
        n.Aliases.ShouldContain("n");
        n.Aliases.ShouldContain("event");
    }

    [Fact]
    public async Task Invoke_Handler_DefaultResponse_GeneratesFiles()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "handler", "CreateOrder",
            "--output", _tempDir,
            "--namespace", "MyApp.Commands"
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDir, "CreateOrder.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDir, "CreateOrderHandler.cs")).ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_Handler_WithResponse_GeneratesTypedHandler()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "handler", "CreateOrder",
            "--response", "OrderId",
            "--output", _tempDir,
            "--namespace", "MyApp.Commands"
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "CreateOrder.cs"));
        content.ShouldContain("ICommand<OrderId>");
    }

    [Fact]
    public async Task Invoke_Query_GeneratesFiles()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "query", "GetOrderById",
            "--response", "OrderDto",
            "--output", _tempDir,
            "--namespace", "MyApp.Queries"
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDir, "GetOrderById.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDir, "GetOrderByIdHandler.cs")).ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_Saga_GeneratesFiles()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "saga", "OrderProcessing",
            "--steps", "Create,Pay,Ship",
            "--output", _tempDir,
            "--namespace", "MyApp.Sagas"
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDir, "OrderProcessingSaga.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDir, "OrderProcessingData.cs")).ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_Saga_InvalidSteps_ReturnsOne()
    {
        var root = BuildRoot();
        // Comma only → split with RemoveEmptyEntries yields 0 steps → generator error
        var exitCode = await root.Parse([
            "generate", "saga", "Bad",
            "--steps", ",",
            "--output", _tempDir
        ]).InvokeAsync();

        exitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Invoke_Notification_GeneratesFiles()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "notification", "OrderCreated",
            "--output", _tempDir,
            "--namespace", "MyApp.Events"
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDir, "OrderCreated.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDir, "OrderCreatedHandler.cs")).ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_Stryker_Basic_GeneratesConfig()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "stryker",
            "--project", "src/MyApp/MyApp.csproj",
            "--output", _tempDir
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDir, "stryker-config.json")).ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_Stryker_Advanced_GeneratesConfig()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "stryker",
            "--project", "src/MyApp/MyApp.csproj",
            "--output", _tempDir,
            "--advanced"
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("baseline");
    }

    [Fact]
    public async Task Invoke_Stryker_CustomThresholds_Succeed()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "stryker",
            "--project", "src/MyApp/MyApp.csproj",
            "--output", _tempDir,
            "--threshold-high", "95",
            "--threshold-low", "85",
            "--threshold-break", "70"
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("\"high\": 95");
        content.ShouldContain("\"low\": 85");
        content.ShouldContain("\"break\": 70");
    }

    [Fact]
    public async Task Invoke_Stryker_InvalidThresholds_ReturnsOne()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "stryker",
            "--project", "src/MyApp/MyApp.csproj",
            "--output", _tempDir,
            "--threshold-high", "50",
            "--threshold-low", "80",
            "--threshold-break", "30"
        ]).InvokeAsync();

        exitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Invoke_Stryker_ExplicitTestProjects_Succeed()
    {
        var root = BuildRoot();
        var exitCode = await root.Parse([
            "generate", "stryker",
            "--project", "src/MyApp/MyApp.csproj",
            "--test-projects", "tests/MyApp.Tests/MyApp.Tests.csproj",
            "--output", _tempDir
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "stryker-config.json"));
        content.ShouldContain("MyApp.Tests.csproj");
    }

    [Fact]
    public async Task Invoke_Handler_InvalidName_ReturnsOneAndPrintsError()
    {
        var root = BuildRoot();
        // An empty name triggers ArgumentException inside CodeGenerator which is caught
        // and converted into an error exit code by the command handler.
        var exitCode = await root.Parse([
            "generate", "handler", "  ",
            "--output", _tempDir
        ]).InvokeAsync();

        exitCode.ShouldBe(1);
    }
}
