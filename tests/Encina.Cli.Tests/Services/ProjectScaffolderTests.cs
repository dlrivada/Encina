using Encina.Cli.Services;
using FluentAssertions;
using Xunit;

namespace Encina.Cli.Tests.Services;

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
        result.Success.Should().BeTrue();
        result.GeneratedFiles.Should().HaveCountGreaterThanOrEqualTo(2);

        var csprojFile = Path.Combine(outputDir, "MyApi.csproj");
        var programFile = Path.Combine(outputDir, "Program.cs");

        File.Exists(csprojFile).Should().BeTrue();
        File.Exists(programFile).Should().BeTrue();

        var csprojContent = await File.ReadAllTextAsync(csprojFile);
        csprojContent.Should().Contain("Encina.AspNetCore");
        csprojContent.Should().Contain("net10.0");

        var programContent = await File.ReadAllTextAsync(programFile);
        programContent.Should().Contain("WebApplication");
        programContent.Should().Contain("AddEncinaAspNetCore");
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
        result.Success.Should().BeTrue();

        var csprojFile = Path.Combine(outputDir, "MyWorker.csproj");
        var programFile = Path.Combine(outputDir, "Program.cs");

        File.Exists(csprojFile).Should().BeTrue();
        File.Exists(programFile).Should().BeTrue();

        var csprojContent = await File.ReadAllTextAsync(csprojFile);
        csprojContent.Should().Contain("<OutputType>Exe</OutputType>");

        var programContent = await File.ReadAllTextAsync(programFile);
        programContent.Should().Contain("Host.CreateApplicationBuilder");
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
        result.Success.Should().BeTrue();

        var programFile = Path.Combine(outputDir, "Program.cs");
        var programContent = await File.ReadAllTextAsync(programFile);

        programContent.Should().Contain("ServiceCollection");
        programContent.Should().Contain("BuildServiceProvider");
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
        result.Success.Should().BeTrue();

        var csprojContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MyApp.csproj"));
        csprojContent.Should().Contain("Encina.Dapper.SqlServer");
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
        result.Success.Should().BeTrue();

        var csprojContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MyApp.csproj"));
        csprojContent.Should().Contain("Encina.Caching");
        csprojContent.Should().Contain("Encina.Caching.Redis");
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
        result.Success.Should().BeTrue();

        var csprojContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "MyApp.csproj"));
        csprojContent.Should().Contain("Encina.RabbitMQ");
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
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown template");
        result.ErrorMessage.Should().Contain("api, worker, console");
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
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not empty");
        result.ErrorMessage.Should().Contain("--force");
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
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void CreateProjectAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await ProjectScaffolder.CreateProjectAsync(null!);
        act.Should().ThrowAsync<ArgumentNullException>();
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
        var act = async () => await ProjectScaffolder.CreateProjectAsync(options);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
