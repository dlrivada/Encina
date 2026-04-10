using Encina.Cli.Services;

using Shouldly;

using Xunit;

namespace Encina.GuardTests.CLI;

/// <summary>
/// Guard tests that exercise happy-path branches of <see cref="ProjectScaffolder"/> to
/// generate real line coverage for the guard flag.
/// </summary>
public class ProjectScaffolderHappyPathGuardTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectScaffolderHappyPathGuardTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-ps-hp-{Guid.NewGuid()}");
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

    private async Task<string> ScaffoldAsync(string name, string template, string? db = null, string? cache = null, string? transport = null, bool force = false)
    {
        var outputDir = Path.Combine(_tempDir, name);
        var options = new ProjectOptions
        {
            Template = template,
            Name = name,
            OutputDirectory = outputDir,
            Database = db,
            Caching = cache,
            Transport = transport,
            Force = force
        };

        var result = await ProjectScaffolder.CreateProjectAsync(options);
        result.Success.ShouldBeTrue();
        return await File.ReadAllTextAsync(Path.Combine(outputDir, $"{name}.csproj"));
    }

    // ─── Templates ───

    [Fact]
    public async Task CreateProjectAsync_ApiTemplate_EmitsAspNetCoreAndHandler()
    {
        var outputDir = Path.Combine(_tempDir, "ApiApp");
        var result = await ProjectScaffolder.CreateProjectAsync(new ProjectOptions
        {
            Template = "api",
            Name = "ApiApp",
            OutputDirectory = outputDir
        });

        result.Success.ShouldBeTrue();
        File.Exists(Path.Combine(outputDir, "ApiApp.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(outputDir, "Program.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(outputDir, "Handlers", "SampleCommandHandler.cs")).ShouldBeTrue();

        var csproj = await File.ReadAllTextAsync(Path.Combine(outputDir, "ApiApp.csproj"));
        csproj.ShouldContain("Encina.AspNetCore");
        csproj.ShouldContain("net10.0");

        var programContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "Program.cs"));
        programContent.ShouldContain("WebApplication.CreateBuilder");
        programContent.ShouldContain("AddEncinaAspNetCore");
    }

    [Fact]
    public async Task CreateProjectAsync_WorkerTemplate_EmitsExeOutput()
    {
        var csproj = await ScaffoldAsync("WorkerApp", "worker");

        csproj.ShouldContain("<OutputType>Exe</OutputType>");
        csproj.ShouldNotContain("Encina.AspNetCore");
    }

    [Fact]
    public async Task CreateProjectAsync_ConsoleTemplate_EmitsServiceCollection()
    {
        var outputDir = Path.Combine(_tempDir, "ConsoleApp");
        await ProjectScaffolder.CreateProjectAsync(new ProjectOptions
        {
            Template = "console",
            Name = "ConsoleApp",
            OutputDirectory = outputDir
        });

        var program = await File.ReadAllTextAsync(Path.Combine(outputDir, "Program.cs"));
        program.ShouldContain("ServiceCollection");
        program.ShouldContain("BuildServiceProvider");
    }

    // ─── Database providers ───

    [Theory]
    [InlineData("sqlserver", "Encina.Dapper.SqlServer")]
    [InlineData("postgresql", "Encina.Dapper.PostgreSQL")]
    [InlineData("postgres", "Encina.Dapper.PostgreSQL")]
    [InlineData("mysql", "Encina.Dapper.MySQL")]
    [InlineData("sqlite", "Encina.Dapper.Sqlite")]
    [InlineData("mongodb", "Encina.MongoDB")]
    [InlineData("efcore", "Encina.EntityFrameworkCore")]
    public async Task CreateProjectAsync_Database_EmitsExpectedPackage(string db, string expected)
    {
        var csproj = await ScaffoldAsync($"DbApp{db}", "api", db: db);
        csproj.ShouldContain(expected);
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownDatabase_DoesNotAddPackage()
    {
        var csproj = await ScaffoldAsync("UnknownDb", "api", db: "neverheard");
        csproj.ShouldNotContain("neverheard");
    }

    // ─── Caching providers ───

    [Theory]
    [InlineData("memory", "Encina.Caching.Memory")]
    [InlineData("redis", "Encina.Caching.Redis")]
    [InlineData("hybrid", "Encina.Caching.Hybrid")]
    public async Task CreateProjectAsync_Caching_EmitsExpectedPackage(string cache, string expected)
    {
        var csproj = await ScaffoldAsync($"CacheApp{cache}", "api", cache: cache);
        csproj.ShouldContain("Encina.Caching");
        csproj.ShouldContain(expected);
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownCaching_DoesNotAddPackage()
    {
        var csproj = await ScaffoldAsync("UnknownCache", "api", cache: "nocache");
        csproj.ShouldNotContain("Encina.Caching.");
    }

    // ─── Transport providers ───

    [Theory]
    [InlineData("rabbitmq", "Encina.RabbitMQ")]
    [InlineData("kafka", "Encina.Kafka")]
    [InlineData("azureservicebus", "Encina.AzureServiceBus")]
    [InlineData("sqs", "Encina.AmazonSQS")]
    public async Task CreateProjectAsync_Transport_EmitsExpectedPackage(string transport, string expected)
    {
        var csproj = await ScaffoldAsync($"TxApp{transport}", "api", transport: transport);
        csproj.ShouldContain(expected);
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownTransport_DoesNotAddPackage()
    {
        var csproj = await ScaffoldAsync("UnknownTx", "api", transport: "notx");
        csproj.ShouldNotContain("notx");
    }

    // ─── Directory + force behavior ───

    [Fact]
    public async Task CreateProjectAsync_NonEmptyDir_WithoutForce_ReturnsError()
    {
        var outputDir = Path.Combine(_tempDir, "NonEmpty");
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "marker.txt"), "x");

        var result = await ProjectScaffolder.CreateProjectAsync(new ProjectOptions
        {
            Template = "api",
            Name = "NonEmpty",
            OutputDirectory = outputDir
        });

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("not empty");
        result.ErrorMessage!.ShouldContain("--force");
    }

    [Fact]
    public async Task CreateProjectAsync_NonEmptyDir_WithForce_Succeeds()
    {
        var outputDir = Path.Combine(_tempDir, "Forced");
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "marker.txt"), "x");

        var result = await ProjectScaffolder.CreateProjectAsync(new ProjectOptions
        {
            Template = "api",
            Name = "Forced",
            OutputDirectory = outputDir,
            Force = true
        });

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateProjectAsync_EmptyExistingDir_Succeeds()
    {
        var outputDir = Path.Combine(_tempDir, "EmptyExisting");
        Directory.CreateDirectory(outputDir);

        var result = await ProjectScaffolder.CreateProjectAsync(new ProjectOptions
        {
            Template = "console",
            Name = "EmptyExisting",
            OutputDirectory = outputDir
        });

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateProjectAsync_InvalidTemplate_ReturnsError()
    {
        var result = await ProjectScaffolder.CreateProjectAsync(new ProjectOptions
        {
            Template = "bogus",
            Name = "Bad",
            OutputDirectory = Path.Combine(_tempDir, "Bad")
        });

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Unknown template");
    }

    // ─── Combined options on all templates ───

    [Fact]
    public async Task CreateProjectAsync_AllOptionsCombined_EmitsAllPackages()
    {
        var csproj = await ScaffoldAsync("FullApp", "api", db: "postgresql", cache: "redis", transport: "kafka");
        csproj.ShouldContain("Encina.Dapper.PostgreSQL");
        csproj.ShouldContain("Encina.Caching.Redis");
        csproj.ShouldContain("Encina.Kafka");
        csproj.ShouldContain("Encina.AspNetCore");
    }
}
