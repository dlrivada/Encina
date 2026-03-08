using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

using Encina.Security.ABAC.EEL;

using Spectre.Console;

namespace Encina.Security.ABAC.Cli.Commands;

/// <summary>
/// CLI command that scans one or more assemblies for <c>[RequireCondition]</c> attributes,
/// compiles each EEL expression, and reports results.
/// </summary>
/// <remarks>
/// Exits with code 0 on success (all expressions compile), 1 on failure.
/// Supports SARIF output format for CI/CD integration.
/// </remarks>
internal static class VerifyCommand
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    public static Command Create()
    {
        var assemblyArgument = new Argument<string[]>("assemblies")
        {
            Description = "Paths to .NET assemblies to scan for [RequireCondition] attributes",
            Arity = ArgumentArity.OneOrMore
        };

        var sarifOption = new Option<string?>("--sarif")
        {
            Description = "Output SARIF file path for CI/CD integration"
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Show detailed output including expression text"
        };

        var command = new Command("verify", "Validate all EEL expressions in the specified assemblies")
        {
            assemblyArgument,
            sarifOption,
            verboseOption
        };

        command.SetAction(parseResult =>
        {
            var assemblyPaths = parseResult.GetValue(assemblyArgument)!;
            var sarifPath = parseResult.GetValue(sarifOption);
            var verbose = parseResult.GetValue(verboseOption);

            return ExecuteAsync(assemblyPaths, sarifPath, verbose);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string[] assemblyPaths, string? sarifPath, bool verbose)
    {
        var assemblies = new List<Assembly>();

        foreach (var path in assemblyPaths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var assembly = Assembly.LoadFrom(fullPath);
                assemblies.Add(assembly);

                if (verbose)
                {
                    AnsiConsole.MarkupLine($"[grey]Loaded: {fullPath}[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error loading '{path}': {ex.Message}[/]");
                return 1;
            }
        }

        var expressions = EELExpressionDiscovery.Discover(assemblies);

        if (expressions.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No [RequireCondition] expressions found.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine(
            $"[blue]Found {expressions.Count} EEL expression(s) in {assemblies.Count} assembly(ies)[/]");

        using var compiler = new EELCompiler();
        var stopwatch = Stopwatch.StartNew();

        var failures = new List<(Type RequestType, string Expression, string ErrorMessage)>();
        var successes = 0;

        foreach (var (requestType, expression) in expressions)
        {
            var result = await compiler.CompileAsync(expression).ConfigureAwait(false);

            result.Match(
                Left: error =>
                {
                    failures.Add((requestType, expression, error.Message));
                    AnsiConsole.MarkupLine(
                        $"  [red]✗[/] [white]{requestType.Name}[/]: {Markup.Escape(expression)}");

                    if (verbose)
                    {
                        AnsiConsole.MarkupLine($"    [red]{Markup.Escape(error.Message)}[/]");
                    }
                },
                Right: _ =>
                {
                    successes++;
                    if (verbose)
                    {
                        AnsiConsole.MarkupLine(
                            $"  [green]✓[/] [white]{requestType.Name}[/]: {Markup.Escape(expression)}");
                    }
                });
        }

        stopwatch.Stop();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[blue]Results:[/] {successes} passed, {failures.Count} failed ({stopwatch.ElapsedMilliseconds}ms)");

        if (sarifPath is not null)
        {
            WriteSarifReport(sarifPath, expressions, failures);
            AnsiConsole.MarkupLine($"[grey]SARIF report written to: {sarifPath}[/]");
        }

        return failures.Count > 0 ? 1 : 0;
    }

    private static void WriteSarifReport(
        string sarifPath,
        IReadOnlyList<(Type RequestType, string Expression)> expressions,
        List<(Type RequestType, string Expression, string ErrorMessage)> failures)
    {
        var sarif = new
        {
            version = "2.1.0",
            schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/main/sarif-2.1/schema/sarif-schema-2.1.0.json",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "encina-eel",
                            version = typeof(VerifyCommand).Assembly.GetName().Version?.ToString() ?? "0.0.0",
                            informationUri = "https://github.com/dlrivada/Encina",
                            rules = new[]
                            {
                                new
                                {
                                    id = "EEL001",
                                    name = "InvalidEELExpression",
                                    shortDescription = new { text = "EEL expression failed to compile" },
                                    defaultConfiguration = new { level = "error" }
                                }
                            }
                        }
                    },
                    results = failures.Select(f => new
                    {
                        ruleId = "EEL001",
                        level = "error",
                        message = new
                        {
                            text = $"Expression \"{f.Expression}\" on type {f.RequestType.FullName} failed: {f.ErrorMessage}"
                        },
                        locations = new[]
                        {
                            new
                            {
                                logicalLocations = new[]
                                {
                                    new
                                    {
                                        fullyQualifiedName = f.RequestType.FullName,
                                        kind = "type"
                                    }
                                }
                            }
                        }
                    }).ToArray()
                }
            }
        };

        var json = JsonSerializer.Serialize(sarif, IndentedJsonOptions);
        File.WriteAllText(sarifPath, json);
    }
}
