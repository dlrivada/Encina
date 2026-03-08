using System.CommandLine;
using System.Reflection;
using System.Text.Json;

using Encina.Security.ABAC.EEL;

using Spectre.Console;

namespace Encina.Security.ABAC.Cli.Commands;

/// <summary>
/// CLI command that lists all <c>[RequireCondition]</c> expressions found in assemblies
/// without compiling them.
/// </summary>
/// <remarks>
/// Useful for auditing and documenting which EEL expressions exist in the codebase.
/// </remarks>
internal static class ListCommand
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    public static Command Create()
    {
        var assemblyArgument = new Argument<string[]>("assemblies")
        {
            Description = "Paths to .NET assemblies to scan",
            Arity = ArgumentArity.OneOrMore
        };

        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output as JSON array"
        };

        var command = new Command("list", "List all EEL expressions found in assemblies")
        {
            assemblyArgument,
            jsonOption
        };

        command.SetAction(parseResult =>
        {
            var assemblyPaths = parseResult.GetValue(assemblyArgument)!;
            var json = parseResult.GetValue(jsonOption);

            return Execute(assemblyPaths, json);
        });

        return command;
    }

    private static int Execute(string[] assemblyPaths, bool json)
    {
        var assemblies = new List<Assembly>();

        foreach (var path in assemblyPaths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                assemblies.Add(Assembly.LoadFrom(fullPath));
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
            if (json)
            {
                Console.WriteLine("[]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No [RequireCondition] expressions found.[/]");
            }

            return 0;
        }

        if (json)
        {
            var items = expressions.Select(e => new
            {
                type = e.RequestType.FullName,
                expression = e.Expression
            });

            Console.WriteLine(JsonSerializer.Serialize(items, IndentedJsonOptions));
        }
        else
        {
            var table = new Table();
            table.AddColumn("Type");
            table.AddColumn("Expression");

            foreach (var (requestType, expression) in expressions)
            {
                table.AddRow(
                    Markup.Escape(requestType.Name),
                    Markup.Escape(expression));
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[blue]Total: {expressions.Count} expression(s)[/]");
        }

        return 0;
    }
}
