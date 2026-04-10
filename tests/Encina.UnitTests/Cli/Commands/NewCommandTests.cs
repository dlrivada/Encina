using System.CommandLine;

using Encina.Cli.Commands;

using Shouldly;

using Xunit;

namespace Encina.UnitTests.Cli.Commands;

/// <summary>
/// Unit tests for <see cref="NewCommand"/> — verifies the command graph, options, and
/// drives end-to-end invocation through <see cref="ProjectScaffolder.CreateProjectAsync"/>
/// via the System.CommandLine parser.
/// </summary>
public class NewCommandTests : IDisposable
{
    private readonly string _tempDir;

    public NewCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-new-tests-{Guid.NewGuid()}");
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
        root.Subcommands.Add(NewCommand.Create());
        return root;
    }

    [Fact]
    public void Create_ReturnsCommandNamedNew()
    {
        var command = NewCommand.Create();

        command.Name.ShouldBe("new");
        command.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Create_HasExpectedArgumentsAndOptions()
    {
        var command = NewCommand.Create();

        command.Arguments.Count.ShouldBe(2);
        command.Options.ShouldContain(o => o.Name == "--output");
        command.Options.ShouldContain(o => o.Name == "--database");
        command.Options.ShouldContain(o => o.Name == "--caching");
        command.Options.ShouldContain(o => o.Name == "--transport");
        command.Options.ShouldContain(o => o.Name == "--force");
    }

    [Fact]
    public async Task Invoke_ApiTemplate_CreatesProjectAndReturnsZero()
    {
        var root = BuildRoot();
        var outputDir = Path.Combine(_tempDir, "MyApi");

        var exitCode = await root.Parse(["new", "api", "MyApi", "--output", outputDir]).InvokeAsync();

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(outputDir, "MyApi.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(outputDir, "Program.cs")).ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_WorkerTemplate_CreatesProject()
    {
        var root = BuildRoot();
        var outputDir = Path.Combine(_tempDir, "MyWorker");

        var exitCode = await root.Parse(["new", "worker", "MyWorker", "--output", outputDir]).InvokeAsync();

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(outputDir, "MyWorker.csproj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_ConsoleTemplate_CreatesProject()
    {
        var root = BuildRoot();
        var outputDir = Path.Combine(_tempDir, "MyConsole");

        var exitCode = await root.Parse(["new", "console", "MyConsole", "--output", outputDir]).InvokeAsync();

        exitCode.ShouldBe(0);
        File.Exists(Path.Combine(outputDir, "MyConsole.csproj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_WithAllOptions_IncludesPackages()
    {
        var root = BuildRoot();
        var outputDir = Path.Combine(_tempDir, "Full");

        var exitCode = await root.Parse([
            "new", "api", "Full",
            "--output", outputDir,
            "--database", "postgresql",
            "--caching", "redis",
            "--transport", "kafka"
        ]).InvokeAsync();

        exitCode.ShouldBe(0);
        var csproj = await File.ReadAllTextAsync(Path.Combine(outputDir, "Full.csproj"));
        csproj.ShouldContain("Encina.Dapper.PostgreSQL");
        csproj.ShouldContain("Encina.Caching.Redis");
        csproj.ShouldContain("Encina.Kafka");
    }

    [Fact]
    public async Task Invoke_InvalidTemplate_ReturnsOne()
    {
        var root = BuildRoot();
        var outputDir = Path.Combine(_tempDir, "Bad");

        var exitCode = await root.Parse(["new", "bogus-template", "Bad", "--output", outputDir]).InvokeAsync();

        exitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Invoke_ExistingNonEmptyDirWithoutForce_ReturnsOne()
    {
        var outputDir = Path.Combine(_tempDir, "Existing");
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "existing.txt"), "data");

        var root = BuildRoot();
        var exitCode = await root.Parse(["new", "api", "Existing", "--output", outputDir]).InvokeAsync();

        exitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Invoke_ExistingNonEmptyDirWithForce_ReturnsZero()
    {
        var outputDir = Path.Combine(_tempDir, "ExistingForce");
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "existing.txt"), "data");

        var root = BuildRoot();
        var exitCode = await root.Parse(["new", "api", "ExistingForce", "--output", outputDir, "--force"]).InvokeAsync();

        exitCode.ShouldBe(0);
    }
}
