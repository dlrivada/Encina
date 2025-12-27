using System.CommandLine;
using Encina.Cli.Services;
using Spectre.Console;

namespace Encina.Cli.Commands;

/// <summary>
/// Command for generating Encina components (handlers, sagas, etc.).
/// </summary>
internal static class GenerateCommand
{
    public static Command Create()
    {
        var command = new Command("generate", "Generate Encina components")
        {
            CreateHandlerCommand(),
            CreateQueryCommand(),
            CreateSagaCommand(),
            CreateNotificationCommand()
        };

        command.Aliases.Add("g");

        return command;
    }

    private static Command CreateHandlerCommand()
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "The name of the command (e.g., CreateOrder)"
        };

        var responseOption = new Option<string?>("--response", "-r")
        {
            Description = "The response type (e.g., OrderId). Defaults to Unit."
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "The output directory for the generated files"
        };

        var namespaceOption = new Option<string?>("--namespace", "-n")
        {
            Description = "The namespace for the generated files"
        };

        var command = new Command("handler", "Generate a command handler")
        {
            nameArgument,
            responseOption,
            outputOption,
            namespaceOption
        };

        command.Aliases.Add("h");
        command.Aliases.Add("command");

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            var response = parseResult.GetValue(responseOption);
            var output = parseResult.GetValue(outputOption);
            var ns = parseResult.GetValue(namespaceOption);

            return ExecuteHandlerAsync(name!, response, output, ns);
        });

        return command;
    }

    private static Command CreateQueryCommand()
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "The name of the query (e.g., GetOrderById)"
        };

        var responseOption = new Option<string>("--response", "-r")
        {
            Description = "The response type (e.g., Order)",
            Required = true
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "The output directory for the generated files"
        };

        var namespaceOption = new Option<string?>("--namespace", "-n")
        {
            Description = "The namespace for the generated files"
        };

        var command = new Command("query", "Generate a query handler")
        {
            nameArgument,
            responseOption,
            outputOption,
            namespaceOption
        };

        command.Aliases.Add("q");

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            var response = parseResult.GetValue(responseOption);
            var output = parseResult.GetValue(outputOption);
            var ns = parseResult.GetValue(namespaceOption);

            return ExecuteQueryAsync(name!, response!, output, ns);
        });

        return command;
    }

    private static Command CreateSagaCommand()
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "The name of the saga (e.g., OrderProcessing)"
        };

        var stepsOption = new Option<string>("--steps", "-s")
        {
            Description = "Comma-separated list of saga steps (e.g., Create,Pay,Ship)",
            Required = true
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "The output directory for the generated files"
        };

        var namespaceOption = new Option<string?>("--namespace", "-n")
        {
            Description = "The namespace for the generated files"
        };

        var command = new Command("saga", "Generate a saga definition")
        {
            nameArgument,
            stepsOption,
            outputOption,
            namespaceOption
        };

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            var steps = parseResult.GetValue(stepsOption);
            var output = parseResult.GetValue(outputOption);
            var ns = parseResult.GetValue(namespaceOption);

            return ExecuteSagaAsync(name!, steps!, output, ns);
        });

        return command;
    }

    private static Command CreateNotificationCommand()
    {
        var nameArgument = new Argument<string>("name")
        {
            Description = "The name of the notification (e.g., OrderCreated)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "The output directory for the generated files"
        };

        var namespaceOption = new Option<string?>("--namespace", "-n")
        {
            Description = "The namespace for the generated files"
        };

        var command = new Command("notification", "Generate a notification and handler")
        {
            nameArgument,
            outputOption,
            namespaceOption
        };

        command.Aliases.Add("n");
        command.Aliases.Add("event");

        command.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            var output = parseResult.GetValue(outputOption);
            var ns = parseResult.GetValue(namespaceOption);

            return ExecuteNotificationAsync(name!, output, ns);
        });

        return command;
    }

    private static async Task<int> ExecuteHandlerAsync(
        string name,
        string? response,
        string? output,
        string? @namespace)
    {
        var options = new HandlerOptions
        {
            Name = name,
            ResponseType = response ?? "Unit",
            OutputDirectory = output ?? Directory.GetCurrentDirectory(),
            Namespace = @namespace
        };

        try
        {
            var result = await CodeGenerator.GenerateCommandHandlerAsync(options);

            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]Generated command handler '[bold]{name}[/]'[/]");
                foreach (var file in result.GeneratedFiles)
                {
                    AnsiConsole.MarkupLine($"  [dim]Created: {file}[/]");
                }
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static async Task<int> ExecuteQueryAsync(
        string name,
        string response,
        string? output,
        string? @namespace)
    {
        var options = new QueryOptions
        {
            Name = name,
            ResponseType = response,
            OutputDirectory = output ?? Directory.GetCurrentDirectory(),
            Namespace = @namespace
        };

        try
        {
            var result = await CodeGenerator.GenerateQueryHandlerAsync(options);

            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]Generated query handler '[bold]{name}[/]'[/]");
                foreach (var file in result.GeneratedFiles)
                {
                    AnsiConsole.MarkupLine($"  [dim]Created: {file}[/]");
                }
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static async Task<int> ExecuteSagaAsync(
        string name,
        string steps,
        string? output,
        string? @namespace)
    {
        var stepList = steps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var options = new SagaOptions
        {
            Name = name,
            Steps = stepList,
            OutputDirectory = output ?? Directory.GetCurrentDirectory(),
            Namespace = @namespace
        };

        try
        {
            var result = await CodeGenerator.GenerateSagaAsync(options);

            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]Generated saga '[bold]{name}[/]' with {stepList.Length} steps[/]");
                foreach (var file in result.GeneratedFiles)
                {
                    AnsiConsole.MarkupLine($"  [dim]Created: {file}[/]");
                }
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static async Task<int> ExecuteNotificationAsync(
        string name,
        string? output,
        string? @namespace)
    {
        var options = new NotificationOptions
        {
            Name = name,
            OutputDirectory = output ?? Directory.GetCurrentDirectory(),
            Namespace = @namespace
        };

        try
        {
            var result = await CodeGenerator.GenerateNotificationAsync(options);

            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]Generated notification '[bold]{name}[/]'[/]");
                foreach (var file in result.GeneratedFiles)
                {
                    AnsiConsole.MarkupLine($"  [dim]Created: {file}[/]");
                }
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.ErrorMessage}[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}
