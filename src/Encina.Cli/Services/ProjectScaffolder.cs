using System.Globalization;
using System.Text;

namespace Encina.Cli.Services;

/// <summary>
/// Options for creating a new project.
/// </summary>
public sealed class ProjectOptions
{
    public required string Template { get; init; }
    public required string Name { get; init; }
    public required string OutputDirectory { get; init; }
    public string? Database { get; init; }
    public string? Caching { get; init; }
    public string? Transport { get; init; }
    public bool Force { get; init; }
}

/// <summary>
/// Result of a scaffolding operation.
/// </summary>
public sealed class ScaffoldResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> GeneratedFiles { get; init; } = [];

    public static ScaffoldResult Ok(IReadOnlyList<string> files) => new() { Success = true, GeneratedFiles = files };
    public static ScaffoldResult Error(string message) => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Service for scaffolding new Encina projects.
/// </summary>
public static class ProjectScaffolder
{
    private static readonly string[] ValidTemplates = ["api", "worker", "console"];

    public static Task<ScaffoldResult> CreateProjectAsync(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Name, nameof(options.Name));
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Template, nameof(options.Template));
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory, nameof(options.OutputDirectory));

        var template = options.Template.ToLowerInvariant();
        if (!ValidTemplates.Contains(template))
        {
            return Task.FromResult(ScaffoldResult.Error($"Unknown template '{options.Template}'. Valid templates: {string.Join(", ", ValidTemplates)}"));
        }

        if (Directory.Exists(options.OutputDirectory) && !options.Force)
        {
            var files = Directory.GetFileSystemEntries(options.OutputDirectory);
            if (files.Length > 0)
            {
                return Task.FromResult(ScaffoldResult.Error($"Directory '{options.OutputDirectory}' is not empty. Use --force to overwrite."));
            }
        }

        Directory.CreateDirectory(options.OutputDirectory);

        var generatedFiles = new List<string>();

        var csprojContent = GenerateCsproj(options);
        var csprojPath = Path.Combine(options.OutputDirectory, $"{options.Name}.csproj");
        File.WriteAllText(csprojPath, csprojContent);
        generatedFiles.Add(csprojPath);

        var programContent = template switch
        {
            "api" => GenerateApiProgram(options),
            "worker" => GenerateWorkerProgram(options),
            "console" => GenerateConsoleProgram(options),
            _ => GenerateConsoleProgram(options)
        };
        var programPath = Path.Combine(options.OutputDirectory, "Program.cs");
        File.WriteAllText(programPath, programContent);
        generatedFiles.Add(programPath);

        if (template == "api")
        {
            var sampleHandler = GenerateSampleCommandHandler(options.Name);
            var handlersDir = Path.Combine(options.OutputDirectory, "Handlers");
            Directory.CreateDirectory(handlersDir);
            var handlerPath = Path.Combine(handlersDir, "SampleCommandHandler.cs");
            File.WriteAllText(handlerPath, sampleHandler);
            generatedFiles.Add(handlerPath);
        }

        return Task.FromResult(ScaffoldResult.Ok(generatedFiles));
    }

    private static string GenerateCsproj(ProjectOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk.Web\">");
        sb.AppendLine();
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");

        if (options.Template == "worker")
        {
            sb.AppendLine("    <OutputType>Exe</OutputType>");
        }

        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Encina\" Version=\"*\" />");

        if (!string.IsNullOrEmpty(options.Database))
        {
            var dbPackage = GetDatabasePackage(options.Database);
            if (dbPackage is not null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <PackageReference Include=\"{dbPackage}\" Version=\"*\" />");
            }
        }

        if (!string.IsNullOrEmpty(options.Caching))
        {
            var cachePackage = GetCachingPackage(options.Caching);
            if (cachePackage is not null)
            {
                sb.AppendLine("    <PackageReference Include=\"Encina.Caching\" Version=\"*\" />");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <PackageReference Include=\"{cachePackage}\" Version=\"*\" />");
            }
        }

        if (!string.IsNullOrEmpty(options.Transport))
        {
            var transportPackage = GetTransportPackage(options.Transport);
            if (transportPackage is not null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <PackageReference Include=\"{transportPackage}\" Version=\"*\" />");
            }
        }

        if (options.Template == "api")
        {
            sb.AppendLine("    <PackageReference Include=\"Encina.AspNetCore\" Version=\"*\" />");
        }

        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("</Project>");

        return sb.ToString();
    }

    private static string? GetDatabasePackage(string database) => database.ToLowerInvariant() switch
    {
        "sqlserver" => "Encina.Dapper.SqlServer",
        "postgresql" or "postgres" => "Encina.Dapper.PostgreSQL",
        "mysql" => "Encina.Dapper.MySQL",
        "sqlite" => "Encina.Dapper.Sqlite",
        "mongodb" => "Encina.MongoDB",
        "efcore" => "Encina.EntityFrameworkCore",
        _ => null
    };

    private static string? GetCachingPackage(string caching) => caching.ToLowerInvariant() switch
    {
        "memory" => "Encina.Caching.Memory",
        "redis" => "Encina.Caching.Redis",
        "hybrid" => "Encina.Caching.Hybrid",
        _ => null
    };

    private static string? GetTransportPackage(string transport) => transport.ToLowerInvariant() switch
    {
        "rabbitmq" => "Encina.RabbitMQ",
        "kafka" => "Encina.Kafka",
        "azureservicebus" => "Encina.AzureServiceBus",
        "sqs" => "Encina.AmazonSQS",
        _ => null
    };

    private static string GenerateApiProgram(ProjectOptions options)
    {
        return $$"""
            using Encina;
            using Encina.AspNetCore;
            using {{options.Name}}.Handlers;

            var builder = WebApplication.CreateBuilder(args);

            // Add Encina
            builder.Services.AddEncina(typeof(Program).Assembly);
            builder.Services.AddEncinaAspNetCore();

            var app = builder.Build();

            app.UseEncinaContext();

            // Sample endpoint using Encina
            app.MapPost("/api/sample", async (SampleCommand command, IEncina encina) =>
            {
                var result = await encina.Send<SampleCommand, SampleResponse>(command);
                return result.Match(
                    Right: response => Results.Ok(response),
                    Left: error => Results.Problem(error.Message));
            });

            app.Run();
            """;
    }

    private static string GenerateWorkerProgram(ProjectOptions _) // NOSONAR S1172: Signature required for delegate consistency
    {
        return $$"""
            using Encina;

            var builder = Host.CreateApplicationBuilder(args);

            // Add Encina
            builder.Services.AddEncina(typeof(Program).Assembly);

            var host = builder.Build();

            host.Run();
            """;
    }

    private static string GenerateConsoleProgram(ProjectOptions _) // NOSONAR S1172: Signature required for delegate consistency
    {
        return $$"""
            using Encina;
            using Microsoft.Extensions.DependencyInjection;

            // Build service provider
            var services = new ServiceCollection();
            services.AddEncina(typeof(Program).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            // Get Encina and send a command
            var encina = serviceProvider.GetRequiredService<IEncina>();

            Console.WriteLine("Encina Console Application");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            """;
    }

    private static string GenerateSampleCommandHandler(string projectName)
    {
        return $$"""
            using Encina;
            using LanguageExt;
            using static LanguageExt.Prelude;

            namespace {{projectName}}.Handlers;

            public sealed record SampleCommand(string Message) : ICommand<SampleResponse>;

            public sealed record SampleResponse(string Result);

            public sealed class SampleCommandHandler : ICommandHandler<SampleCommand, SampleResponse>
            {
                public Task<Either<EncinaError, SampleResponse>> Handle(
                    SampleCommand request,
                    CancellationToken cancellationToken)
                {
                    var response = new SampleResponse($"Processed: {request.Message}");
                    return Task.FromResult(Right<EncinaError, SampleResponse>(response));
                }
            }
            """;
    }
}
