using Encina.Cli.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Encina.IntegrationTests.Cli.Services;

/// <summary>
/// Verifies that every project template produces compilable C# source.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CLI")]
public sealed class ProjectScaffolderCompilationTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>
    /// Creates an isolated directory for generated projects.
    /// </summary>
    public ProjectScaffolderCompilationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-integration-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Removes the generated project directory.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Generates and compiles the selected project template.
    /// </summary>
    [Theory]
    [InlineData("api")]
    [InlineData("worker")]
    [InlineData("console")]
    public async Task CreateProjectAsync_WithTemplate_GeneratesCompilableProject(string template)
    {
        // Arrange
        var options = new ProjectOptions
        {
            Template = template,
            Name = $"My{template}",
            OutputDirectory = Path.Combine(_tempDir, $"My{template}")
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeTrue();

        var generatedSyntaxTrees = await Task.WhenAll(result.GeneratedFiles
            .Where(path => Path.GetExtension(path) == ".cs")
            .Select(async path => CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(path))));
        var syntaxTrees = generatedSyntaxTrees.Append(CSharpSyntaxTree.ParseText("""
                global using System;
                global using System.Threading;
                global using System.Threading.Tasks;
                global using Microsoft.AspNetCore.Builder;
                global using Microsoft.AspNetCore.Http;
                global using Microsoft.Extensions.DependencyInjection;
                global using Microsoft.Extensions.Hosting;
                global using Microsoft.Extensions.Logging;
                """));
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        trustedPlatformAssemblies.ShouldNotBeNull("TRUSTED_PLATFORM_ASSEMBLIES must be available for compilation references.");
        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            assemblyName: $"My{template}",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.ShouldBeEmpty();
    }
}
