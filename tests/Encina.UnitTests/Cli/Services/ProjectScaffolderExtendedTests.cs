using Encina.Cli.Services;

using Shouldly;

using Xunit;

namespace Encina.UnitTests.Cli.Services;

/// <summary>
/// Additional unit tests for <see cref="ProjectScaffolder"/> covering the remaining
/// database / caching / transport mapping switch arms, the "postgres" alias, and the
/// behavior when an unrecognized provider name is given (branch: returns null → skipped).
/// </summary>
public class ProjectScaffolderExtendedTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectScaffolderExtendedTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-scaf-ext-{Guid.NewGuid()}");
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

    private async Task<string> ScaffoldWithAsync(string name, string template, string? db = null, string? cache = null, string? transport = null)
    {
        var outputDir = Path.Combine(_tempDir, name);
        var options = new ProjectOptions
        {
            Template = template,
            Name = name,
            OutputDirectory = outputDir,
            Database = db,
            Caching = cache,
            Transport = transport
        };

        var result = await ProjectScaffolder.CreateProjectAsync(options);
        result.Success.ShouldBeTrue();

        return await File.ReadAllTextAsync(Path.Combine(outputDir, $"{name}.csproj"));
    }

    // ─── Database providers: exercise all switch arms ───

    [Theory]
    [InlineData("postgresql", "Encina.Dapper.PostgreSQL")]
    [InlineData("postgres", "Encina.Dapper.PostgreSQL")]
    [InlineData("mysql", "Encina.Dapper.MySQL")]
    [InlineData("sqlite", "Encina.Dapper.Sqlite")]
    [InlineData("mongodb", "Encina.MongoDB")]
    [InlineData("efcore", "Encina.EntityFrameworkCore")]
    public async Task CreateProjectAsync_Database_EmitsExpectedPackage(string db, string expectedPackage)
    {
        var csproj = await ScaffoldWithAsync($"App{db}", "api", db: db);
        csproj.ShouldContain(expectedPackage);
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownDatabase_DoesNotAddDatabasePackage()
    {
        var csproj = await ScaffoldWithAsync("AppUnknownDb", "api", db: "futuredb");

        csproj.ShouldContain("Encina"); // core package still there
        csproj.ShouldNotContain("futuredb");
    }

    // ─── Caching providers ───

    [Theory]
    [InlineData("memory", "Encina.Caching.Memory")]
    [InlineData("redis", "Encina.Caching.Redis")]
    [InlineData("hybrid", "Encina.Caching.Hybrid")]
    public async Task CreateProjectAsync_Caching_EmitsExpectedPackage(string cache, string expectedPackage)
    {
        var csproj = await ScaffoldWithAsync($"AppCache{cache}", "api", cache: cache);
        csproj.ShouldContain("Encina.Caching");
        csproj.ShouldContain(expectedPackage);
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownCaching_DoesNotAddCachingPackage()
    {
        var csproj = await ScaffoldWithAsync("AppUnknownCache", "api", cache: "futurecache");

        csproj.ShouldNotContain("Encina.Caching.");
        csproj.ShouldNotContain("futurecache");
    }

    // ─── Transport providers ───

    [Theory]
    [InlineData("rabbitmq", "Encina.RabbitMQ")]
    [InlineData("kafka", "Encina.Kafka")]
    [InlineData("azureservicebus", "Encina.AzureServiceBus")]
    [InlineData("sqs", "Encina.AmazonSQS")]
    public async Task CreateProjectAsync_Transport_EmitsExpectedPackage(string transport, string expectedPackage)
    {
        var csproj = await ScaffoldWithAsync($"AppTx{transport}", "api", transport: transport);
        csproj.ShouldContain(expectedPackage);
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownTransport_DoesNotAddTransportPackage()
    {
        var csproj = await ScaffoldWithAsync("AppUnknownTx", "api", transport: "futuretx");

        csproj.ShouldNotContain("futuretx");
    }

    // ─── Templates: worker vs console must NOT include AspNetCore ───

    [Fact]
    public async Task CreateProjectAsync_WorkerTemplate_DoesNotIncludeAspNetCore()
    {
        var csproj = await ScaffoldWithAsync("BgWorker", "worker");
        csproj.ShouldNotContain("Encina.AspNetCore");
        csproj.ShouldContain("<OutputType>Exe</OutputType>");
    }

    [Fact]
    public async Task CreateProjectAsync_ConsoleTemplate_DoesNotIncludeAspNetCore()
    {
        var csproj = await ScaffoldWithAsync("Cli", "console");
        csproj.ShouldNotContain("Encina.AspNetCore");
    }

    [Fact]
    public async Task CreateProjectAsync_ApiTemplate_IncludesSampleHandler()
    {
        var outputDir = Path.Combine(_tempDir, "ApiWithHandler");
        var options = new ProjectOptions
        {
            Template = "api",
            Name = "ApiWithHandler",
            OutputDirectory = outputDir
        };

        var result = await ProjectScaffolder.CreateProjectAsync(options);

        result.Success.ShouldBeTrue();
        File.Exists(Path.Combine(outputDir, "Handlers", "SampleCommandHandler.cs")).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateProjectAsync_NullOutputDirectory_Throws()
    {
        // Validates the OutputDirectory null-or-whitespace guard branch.
        var options = new ProjectOptions
        {
            Template = "api",
            Name = "Test",
            OutputDirectory = "   "
        };

        await Should.ThrowAsync<ArgumentException>(() => ProjectScaffolder.CreateProjectAsync(options));
    }

    [Fact]
    public async Task CreateProjectAsync_EmptyTemplate_Throws()
    {
        var options = new ProjectOptions
        {
            Template = "   ",
            Name = "Test",
            OutputDirectory = _tempDir
        };

        await Should.ThrowAsync<ArgumentException>(() => ProjectScaffolder.CreateProjectAsync(options));
    }
}
