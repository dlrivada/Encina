using System.Globalization;
using System.Text;

namespace Encina.Cli.Services;

/// <summary>
/// Options for generating a command handler.
/// </summary>
public sealed class HandlerOptions
{
    public required string Name { get; init; }
    public string ResponseType { get; init; } = "Unit";
    public required string OutputDirectory { get; init; }
    public string? Namespace { get; init; }
}

/// <summary>
/// Options for generating a query handler.
/// </summary>
public sealed class QueryOptions
{
    public required string Name { get; init; }
    public required string ResponseType { get; init; }
    public required string OutputDirectory { get; init; }
    public string? Namespace { get; init; }
}

/// <summary>
/// Options for generating a saga.
/// </summary>
public sealed class SagaOptions
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Steps { get; init; }
    public required string OutputDirectory { get; init; }
    public string? Namespace { get; init; }
}

/// <summary>
/// Options for generating a notification.
/// </summary>
public sealed class NotificationOptions
{
    public required string Name { get; init; }
    public required string OutputDirectory { get; init; }
    public string? Namespace { get; init; }
}

/// <summary>
/// Options for generating a Stryker.NET configuration file.
/// </summary>
public sealed class StrykerOptions
{
    public required string ProjectPath { get; init; }
    public IReadOnlyList<string> TestProjects { get; init; } = [];
    public required string OutputDirectory { get; init; }
    public int ThresholdHigh { get; init; } = 80;
    public int ThresholdLow { get; init; } = 60;
    public int ThresholdBreak { get; init; } = 50;
    public bool UseAdvanced { get; init; }
}

/// <summary>
/// Result of a code generation operation.
/// </summary>
public sealed class GenerationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> GeneratedFiles { get; init; } = [];

    public static GenerationResult Ok(IReadOnlyList<string> files) => new() { Success = true, GeneratedFiles = files };
    public static GenerationResult Error(string message) => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Service for generating Encina code files.
/// </summary>
public static class CodeGenerator
{
    public static Task<GenerationResult> GenerateCommandHandlerAsync(HandlerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        var @namespace = options.Namespace ?? DetectNamespace(options.OutputDirectory);
        var generatedFiles = new List<string>();

        Directory.CreateDirectory(options.OutputDirectory);

        var isUnit = options.ResponseType is "Unit" or "unit";
        var commandContent = GenerateCommand(options.Name, options.ResponseType, @namespace, isUnit);
        var commandPath = Path.Combine(options.OutputDirectory, $"{options.Name}.cs");
        File.WriteAllText(commandPath, commandContent);
        generatedFiles.Add(commandPath);

        var handlerContent = GenerateCommandHandler(options.Name, options.ResponseType, @namespace, isUnit);
        var handlerPath = Path.Combine(options.OutputDirectory, $"{options.Name}Handler.cs");
        File.WriteAllText(handlerPath, handlerContent);
        generatedFiles.Add(handlerPath);

        return Task.FromResult(GenerationResult.Ok(generatedFiles));
    }

    public static Task<GenerationResult> GenerateQueryHandlerAsync(QueryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ResponseType);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        var @namespace = options.Namespace ?? DetectNamespace(options.OutputDirectory);
        var generatedFiles = new List<string>();

        Directory.CreateDirectory(options.OutputDirectory);

        var queryContent = GenerateQuery(options.Name, options.ResponseType, @namespace);
        var queryPath = Path.Combine(options.OutputDirectory, $"{options.Name}.cs");
        File.WriteAllText(queryPath, queryContent);
        generatedFiles.Add(queryPath);

        var handlerContent = GenerateQueryHandler(options.Name, options.ResponseType, @namespace);
        var handlerPath = Path.Combine(options.OutputDirectory, $"{options.Name}Handler.cs");
        File.WriteAllText(handlerPath, handlerContent);
        generatedFiles.Add(handlerPath);

        return Task.FromResult(GenerationResult.Ok(generatedFiles));
    }

    public static Task<GenerationResult> GenerateSagaAsync(SagaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        if (options.Steps.Count == 0)
        {
            return Task.FromResult(GenerationResult.Error("At least one step is required."));
        }

        var @namespace = options.Namespace ?? DetectNamespace(options.OutputDirectory);
        var generatedFiles = new List<string>();

        Directory.CreateDirectory(options.OutputDirectory);

        var dataContent = GenerateSagaData(options.Name, @namespace);
        var dataPath = Path.Combine(options.OutputDirectory, $"{options.Name}Data.cs");
        File.WriteAllText(dataPath, dataContent);
        generatedFiles.Add(dataPath);

        var sagaContent = GenerateSagaDefinition(options.Name, options.Steps, @namespace);
        var sagaPath = Path.Combine(options.OutputDirectory, $"{options.Name}Saga.cs");
        File.WriteAllText(sagaPath, sagaContent);
        generatedFiles.Add(sagaPath);

        return Task.FromResult(GenerationResult.Ok(generatedFiles));
    }

    public static Task<GenerationResult> GenerateNotificationAsync(NotificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        var @namespace = options.Namespace ?? DetectNamespace(options.OutputDirectory);
        var generatedFiles = new List<string>();

        Directory.CreateDirectory(options.OutputDirectory);

        var notificationContent = GenerateNotification(options.Name, @namespace);
        var notificationPath = Path.Combine(options.OutputDirectory, $"{options.Name}.cs");
        File.WriteAllText(notificationPath, notificationContent);
        generatedFiles.Add(notificationPath);

        var handlerContent = GenerateNotificationHandler(options.Name, @namespace);
        var handlerPath = Path.Combine(options.OutputDirectory, $"{options.Name}Handler.cs");
        File.WriteAllText(handlerPath, handlerContent);
        generatedFiles.Add(handlerPath);

        return Task.FromResult(GenerationResult.Ok(generatedFiles));
    }

    /// <summary>
    /// Generates a Stryker.NET configuration file.
    /// </summary>
    /// <param name="options">The generation options.</param>
    /// <returns>The result of the generation.</returns>
    public static async Task<GenerationResult> GenerateStrykerConfigAsync(StrykerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ProjectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);

        var tb = options.ThresholdBreak;
        var tl = options.ThresholdLow;
        var th = options.ThresholdHigh;
        if (!(0 <= tb && tb <= tl && tl <= th && th <= 100))
        {
            return GenerationResult.Error(
                $"Invalid thresholds: Break={tb}, Low={tl}, High={th}. Required: 0 <= Break <= Low <= High <= 100");
        }

        Directory.CreateDirectory(options.OutputDirectory);

        var content = options.UseAdvanced
            ? GenerateAdvancedStrykerConfig(options)
            : GenerateBasicStrykerConfig(options);

        var configPath = Path.Combine(options.OutputDirectory, "stryker-config.json");
        await File.WriteAllTextAsync(configPath, content);

        return GenerationResult.Ok([configPath]);
    }

    private static string BuildTestProjectsJson(StrykerOptions options)
    {
        if (options.TestProjects.Count > 0)
        {
            return string.Join(",\n            ", options.TestProjects.Select(p => $"\"{p}\""));
        }

        var proj = options.ProjectPath;
        var normalized = proj.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var dir = Path.GetDirectoryName(normalized) ?? string.Empty;
        var parts = dir.Split(Path.DirectorySeparatorChar).Where(s => !string.IsNullOrEmpty(s)).ToList();

        var srcIndex = -1;
        for (var i = parts.Count - 1; i >= 0; i--)
        {
            if (parts[i].Equals("src", StringComparison.OrdinalIgnoreCase))
            {
                srcIndex = i;
                break;
            }
        }

        if (srcIndex < 0)
        {
            return $"\"{proj.Replace(Path.DirectorySeparatorChar, '/')}\"";
        }

        parts[srcIndex] = "tests";
        var root = Path.GetPathRoot(normalized) ?? string.Empty;
        var testDir = Path.Combine(root, Path.Combine(parts.ToArray()));
        var projectName = Path.GetFileNameWithoutExtension(normalized);
        var testProjPath = Path.Combine(testDir, projectName + ".Tests", projectName + ".Tests.csproj");
        return $"\"{testProjPath.Replace(Path.DirectorySeparatorChar, '/')}\"";
    }

    private static string GenerateBasicStrykerConfig(StrykerOptions options)
    {
        var concurrency = Environment.ProcessorCount;
        var testProjects = BuildTestProjectsJson(options);

        return $$"""
            {
                "$schema": "https://stryker-mutator.io/schema/stryker.schema.json",
                "stryker-config": {
                    "project": "{{options.ProjectPath}}",
                    "test-projects": [
                        {{testProjects}}
                    ],
                    "mutation-level": "Standard",
                    "thresholds": {
                        "high": {{options.ThresholdHigh}},
                        "low": {{options.ThresholdLow}},
                        "break": {{options.ThresholdBreak}}
                    },
                    "reporters": [
                        "html",
                        "progress",
                        "json"
                    ],
                    "mutate": [
                        "!**/Program.cs",
                        "!**/Startup.cs",
                        "!**/Migrations/**",
                        "!**/obj/**",
                        "!**/*.Designer.cs"
                    ],
                    "ignore-mutations": [
                        "string"
                    ],
                    "concurrency": {{concurrency}},
                    "log-level": "Info"
                }
            }
            """;
    }

    private static string GenerateAdvancedStrykerConfig(StrykerOptions options)
    {
        var concurrency = Environment.ProcessorCount;
        var testProjects = BuildTestProjectsJson(options);

        return $$"""
            {
                "$schema": "https://stryker-mutator.io/schema/stryker.schema.json",
                "stryker-config": {
                    "project": "{{options.ProjectPath}}",
                    "test-projects": [
                        {{testProjects}}
                    ],
                    "mutation-level": "Standard",
                    "thresholds": {
                        "high": {{options.ThresholdHigh}},
                        "low": {{options.ThresholdLow}},
                        "break": {{options.ThresholdBreak}}
                    },
                    "reporters": [
                        "html",
                        "progress",
                        "json",
                        "cleartext"
                    ],
                    "mutate": [
                        "src/**/*.cs",
                        "!**/Program.cs",
                        "!**/Startup.cs",
                        "!**/Migrations/**",
                        "!**/obj/**",
                        "!**/*.Designer.cs",
                        "!**/PublicAPI.*.txt"
                    ],
                    "ignore-mutations": [
                        "string"
                    ],
                    "ignore-methods": [
                        "Dispose",
                        "DisposeAsync",
                        "ToString",
                        "GetHashCode",
                        "Equals"
                    ],
                    "concurrency": {{concurrency}},
                    "baseline": {
                        "enabled": true,
                        "provider": "disk"
                    },
                    "since": {
                        "enabled": true,
                        "target": "main"
                    },
                    "log-level": "Info"
                }
            }
            """;
    }

    private static string DetectNamespace(string directory)
    {
        var csprojFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);
        if (csprojFiles.Length > 0)
        {
            return Path.GetFileNameWithoutExtension(csprojFiles[0]);
        }

        var parent = Directory.GetParent(directory);
        if (parent is not null)
        {
            csprojFiles = Directory.GetFiles(parent.FullName, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length > 0)
            {
                var projectName = Path.GetFileNameWithoutExtension(csprojFiles[0]);
                var folderName = new DirectoryInfo(directory).Name;
                return $"{projectName}.{folderName}";
            }
        }

        return "MyApp";
    }

    private static string GenerateCommand(string name, string responseType, string @namespace, bool isUnit)
    {
        var baseInterface = isUnit ? "ICommand" : $"ICommand<{responseType}>";

        return $$"""
            using Encina;

            namespace {{@namespace}};

            /// <summary>
            /// Command to {{ToDescription(name)}}.
            /// </summary>
            public sealed record {{name}} : {{baseInterface}}
            {
                // TODO: Add command properties
            }
            """;
    }

    private static string GenerateCommandHandler(string name, string responseType, string @namespace, bool isUnit)
    {
        var handlerInterface = isUnit
            ? $"ICommandHandler<{name}>"
            : $"ICommandHandler<{name}, {responseType}>";

        var returnType = isUnit ? "Unit" : responseType;

        return $$"""
            using Encina;
            using LanguageExt;
            using static LanguageExt.Prelude;

            namespace {{@namespace}};

            /// <summary>
            /// Handler for <see cref="{{name}}"/>.
            /// </summary>
            public sealed class {{name}}Handler : {{handlerInterface}}
            {
                public Task<Either<EncinaError, {{returnType}}>> Handle(
                    {{name}} request,
                    CancellationToken cancellationToken)
                {
                    // TODO: Implement command handling logic
                    {{(isUnit ? "return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));" : $"throw new NotImplementedException();")}}
                }
            }
            """;
    }

    private static string GenerateQuery(string name, string responseType, string @namespace)
    {
        return $$"""
            using Encina;

            namespace {{@namespace}};

            /// <summary>
            /// Query to {{ToDescription(name)}}.
            /// </summary>
            public sealed record {{name}} : IQuery<{{responseType}}>
            {
                // TODO: Add query parameters
            }
            """;
    }

    private static string GenerateQueryHandler(string name, string responseType, string @namespace)
    {
        return $$"""
            using Encina;
            using LanguageExt;
            using static LanguageExt.Prelude;

            namespace {{@namespace}};

            /// <summary>
            /// Handler for <see cref="{{name}}"/>.
            /// </summary>
            public sealed class {{name}}Handler : IQueryHandler<{{name}}, {{responseType}}>
            {
                public Task<Either<EncinaError, {{responseType}}>> Handle(
                    {{name}} request,
                    CancellationToken cancellationToken)
                {
                    // TODO: Implement query handling logic
                    throw new NotImplementedException();
                }
            }
            """;
    }

    private static string GenerateSagaData(string name, string @namespace)
    {
        return $$"""
            namespace {{@namespace}};

            /// <summary>
            /// Data for the {{name}} saga.
            /// </summary>
            public sealed class {{name}}Data
            {
                // TODO: Add saga data properties

                /// <summary>
                /// The correlation ID for this saga instance.
                /// </summary>
                public Guid CorrelationId { get; set; } = Guid.NewGuid();
            }
            """;
    }

    private static string GenerateSagaDefinition(string name, IReadOnlyList<string> steps, string @namespace)
    {
        var sb = new StringBuilder();

        sb.AppendLine(CultureInfo.InvariantCulture, $$"""
            using Encina;
            using Encina.Messaging.Sagas;
            using LanguageExt;
            using static LanguageExt.Prelude;

            namespace {{@namespace}};

            /// <summary>
            /// Saga definition for {{name}}.
            /// </summary>
            public static class {{name}}Saga
            {
                public static BuiltSagaDefinition<{{name}}Data> Create()
                {
                    return SagaDefinition.Create<{{name}}Data>("{{name}}")
            """);

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            sb.AppendLine(CultureInfo.InvariantCulture, $$"""
                        .Step("{{step}}")
                            .Execute(async (data, ct) =>
                            {
                                // TODO: Implement {{step}} logic
                                return Right<EncinaError, {{name}}Data>(data);
                            })
                            .Compensate(async (data, ct) =>
                            {
                                // TODO: Implement {{step}} compensation logic
                            })
            """);
        }

        sb.AppendLine("""
                    .Build();
            }
        }
        """);

        return sb.ToString();
    }

    private static string GenerateNotification(string name, string @namespace)
    {
        return $$"""
            using Encina;

            namespace {{@namespace}};

            /// <summary>
            /// Notification for when {{ToDescription(name)}}.
            /// </summary>
            public sealed record {{name}} : INotification
            {
                // TODO: Add notification properties
            }
            """;
    }

    private static string GenerateNotificationHandler(string name, string @namespace)
    {
        return $$"""
            using Encina;
            using LanguageExt;
            using static LanguageExt.Prelude;

            namespace {{@namespace}};

            /// <summary>
            /// Handler for <see cref="{{name}}"/>.
            /// </summary>
            public sealed class {{name}}Handler : INotificationHandler<{{name}}>
            {
                public Task<Either<EncinaError, Unit>> Handle(
                    {{name}} notification,
                    CancellationToken cancellationToken)
                {
                    // TODO: Implement notification handling logic
                    return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
                }
            }
            """;
    }

    private static string ToDescription(string name)
    {
        var result = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c))
            {
                result.Append(' ');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
