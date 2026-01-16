using Encina.Cli.Services;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Cli.Services;

public class ProjectScaffolderTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectScaffolderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-tests-{Guid.NewGuid()}");
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

    [Fact]
    public async Task CreateProjectAsync_WithApiTemplate_GeneratesApiProject()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDir, "MyApi");
        var options = new ProjectOptions
        {
            Template = "api",
            Name = "MyApi",
            OutputDirectory = outputDir
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBeGreaterThanOrEqualTo(2);

        var csprojFile = Path.Combine(outputDir, "MyApi.csproj");
        var programFile = Path.Combine(outputDir, "Program.cs");

        File.Exists(csprojFile).ShouldBeTrue();
        File.Exists(programFile).ShouldBeTrue();

        var csprojContent = await File.ReadAllTextAsync(csprojFile);
        csprojContent.ShouldContain("Encina.AspNetCore");
        csprojContent.ShouldContain("net10.0");

        var programContent = await File.ReadAllTextAsync(programFile);
        programContent.ShouldContain("WebApplication");
        programContent.ShouldContain("AddEncinaAspNetCore");
    }

    [Fact]
    public async Task CreateProjectAsync_WithWorkerTemplate_GeneratesWorkerProject()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDir, "MyWorker");
        var options = new ProjectOptions
        {
            Template = "worker",
            Name = "MyWorker",
            OutputDirectory = outputDir
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeTrue();

        var csprojFile = Path.Combine(outputDir, "MyWorker.csproj");
        var programFile = Path.Combine(outputDir, "Program.cs");

        File.Exists(csprojFile).ShouldBeTrue();
        File.Exists(programFile).ShouldBeTrue();

        var csprojContent = await File.ReadAllTextAsync(csprojFile);
        csprojContent.ShouldContain("<OutputType>Exe</OutputType>");

        var programContent = await File.ReadAllTextAsync(programFile);
        programContent.ShouldContain("Host.CreateApplicationBuilder");
    }

    [Fact]
    public async Task CreateProjectAsync_WithConsoleTemplate_GeneratesConsoleProject()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDir, "MyConsole");
        var options = new ProjectOptions
        {
            Template = "console",
            Name = "MyConsole",
            OutputDirectory = outputDir
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeTrue();

        var programFile = Path.Combine(outputDir, "Program.cs");
        var programContent = await File.ReadAllTextAsync(programFile);

        programContent.ShouldContain("ServiceCollection");
        programContent.ShouldContain("BuildServiceProvider");
    }

    [Fact]
    public async Task CreateProjectAsync_WithDatabaseOption_AddsDatabasePackage()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDir, "MyApp");
        var options = new ProjectOptions
        {
            Template = "api",
            Name = "MyApp",
            OutputDirectory = outputDir,
            Database = "sqlserver"
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeTrue();

        var csprojContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MyApp.csproj"));
        csprojContent.ShouldContain("Encina.Dapper.SqlServer");
    }

    [Fact]
    public async Task CreateProjectAsync_WithCachingOption_AddsCachingPackages()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDir, "MyApp");
        var options = new ProjectOptions
        {
            Template = "api",
            Name = "MyApp",
            OutputDirectory = outputDir,
            Caching = "redis"
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeTrue();

        var csprojContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MyApp.csproj"));
        csprojContent.ShouldContain("Encina.Caching");
        csprojContent.ShouldContain("Encina.Caching.Redis");
    }

    [Fact]
    public async Task CreateProjectAsync_WithTransportOption_AddsTransportPackage()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDir, "MyApp");
        var options = new ProjectOptions
        {
            Template = "api",
            Name = "MyApp",
            OutputDirectory = outputDir,
            Transport = "rabbitmq"
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeTrue();

        var csprojContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MyApp.csproj"));
        csprojContent.ShouldContain("Encina.RabbitMQ");
    }

    [Fact]
    public async Task CreateProjectAsync_WithInvalidTemplate_ReturnsError()
    {
        // Arrange
        var options = new ProjectOptions
        {
            Template = "invalid",
            Name = "MyApp",
            OutputDirectory = Path.Combine(_tempDir, "MyApp")
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldContain("Unknown template");
        result.ErrorMessage!.ShouldContain("api, worker, console");
    }

    [Fact]
    public async Task CreateProjectAsync_WithNonEmptyDirectory_WithoutForce_ReturnsError()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDir, "ExistingDir");
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "existing.txt"), "content");

        var options = new ProjectOptions
        {
            Template = "api",
            Name = "MyApp",
            OutputDirectory = outputDir,
            Force = false
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldContain("not empty");
        result.ErrorMessage!.ShouldContain("--force");
    }

    [Fact]
    public async Task CreateProjectAsync_WithNonEmptyDirectory_WithForce_Succeeds()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDir, "ExistingDir");
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "existing.txt"), "content");

        var options = new ProjectOptions
        {
            Template = "api",
            Name = "MyApp",
            OutputDirectory = outputDir,
            Force = true
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateProjectAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Func<Task> act = () => ProjectScaffolder.CreateProjectAsync(null!);
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProjectAsync_WithInvalidName_ThrowsArgumentException(string name)
    {
        // Arrange
        var options = new ProjectOptions
        {
            Template = "api",
            Name = name,
            OutputDirectory = _tempDir
        };

        // Act & Assert
        Func<Task> act = () => ProjectScaffolder.CreateProjectAsync(options);
        await Should.ThrowAsync<ArgumentException>(act);
    }
}
