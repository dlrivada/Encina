using System.CommandLine;
using System.Dynamic;
using System.Text.Json;

using Encina.Security.ABAC.EEL;

using Spectre.Console;

namespace Encina.Security.ABAC.Cli.Commands;

/// <summary>
/// CLI command that evaluates a single EEL expression with provided JSON globals.
/// </summary>
/// <remarks>
/// Useful for ad-hoc testing of EEL expressions during development.
/// Exits with code 0 on success, 1 on failure.
/// </remarks>
internal static class EvalCommand
{
    public static Command Create()
    {
        var expressionArgument = new Argument<string>("expression")
        {
            Description = "The EEL expression to evaluate"
        };

        var globalsOption = new Option<string?>("--globals")
        {
            Description = "JSON string with globals (e.g., '{\"user\":{\"department\":\"Finance\"}}')"
        };

        var globalsFileOption = new Option<string?>("--globals-file")
        {
            Description = "Path to a JSON file containing globals"
        };

        var command = new Command("eval", "Evaluate a single EEL expression")
        {
            expressionArgument,
            globalsOption,
            globalsFileOption
        };

        command.SetAction(parseResult =>
        {
            var expression = parseResult.GetValue(expressionArgument)!;
            var globalsJson = parseResult.GetValue(globalsOption);
            var globalsFile = parseResult.GetValue(globalsFileOption);

            return ExecuteAsync(expression, globalsJson, globalsFile);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string expression, string? globalsJson, string? globalsFile)
    {
        using var compiler = new EELCompiler();

        // First, verify it compiles
        var compileResult = await compiler.CompileAsync(expression).ConfigureAwait(false);

        var compileFailed = false;
        compileResult.Match(
            Left: error =>
            {
                AnsiConsole.MarkupLine($"[red]Compilation failed:[/] {Markup.Escape(error.Message)}");
                compileFailed = true;
            },
            Right: _ => AnsiConsole.MarkupLine("[green]Compilation:[/] OK"));

        if (compileFailed)
        {
            return 1;
        }

        // If globals are provided, evaluate
        if (globalsJson is null && globalsFile is null)
        {
            AnsiConsole.MarkupLine("[grey]No globals provided. Use --globals or --globals-file to evaluate.[/]");
            return 0;
        }

        var json = globalsJson ?? File.ReadAllText(globalsFile!);
        var globals = ParseGlobals(json);

        if (globals is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to parse globals JSON.[/]");
            return 1;
        }

        var evalResult = await compiler.EvaluateAsync(expression, globals).ConfigureAwait(false);

        var exitCode = 0;
        evalResult.Match(
            Left: error =>
            {
                AnsiConsole.MarkupLine($"[red]Evaluation failed:[/] {Markup.Escape(error.Message)}");
                exitCode = 1;
            },
            Right: value =>
            {
                var color = value ? "green" : "red";
                AnsiConsole.MarkupLine($"[{color}]Result: {value}[/]");
            });

        return exitCode;
    }

    private static EELGlobals? ParseGlobals(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new EELGlobals
            {
                user = JsonElementToExpando(root, "user"),
                resource = JsonElementToExpando(root, "resource"),
                environment = JsonElementToExpando(root, "environment"),
                action = JsonElementToExpando(root, "action")
            };
        }
        catch (JsonException ex)
        {
            AnsiConsole.MarkupLine($"[red]JSON parse error:[/] {Markup.Escape(ex.Message)}");
            return null;
        }
    }

    private static ExpandoObject JsonElementToExpando(JsonElement root, string propertyName)
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;

        if (root.TryGetProperty(propertyName, out var element) &&
            element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = ConvertJsonValue(prop.Value);
            }
        }

        return expando;
    }

    private static object? ConvertJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var l) => l,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
        JsonValueKind.Object => JsonObjectToExpando(element),
        _ => element.ToString()
    };

    private static ExpandoObject JsonObjectToExpando(JsonElement element)
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;

        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = ConvertJsonValue(prop.Value);
        }

        return expando;
    }
}
