using System.CommandLine;
using Encina.Cli.Services;
using Spectre.Console;

namespace Encina.Cli.Commands;

/// <summary>
/// Command for generating Encina components (handlers, sagas, etc.).
/// </summary>
internal static class GenerateCommand
{
    private const string OutputOptionName = "--output";
    private const string OutputOptionAlias = "-o";
    private const string OutputOptionDescription = "The output directory for the generated files";
    private const string NamespaceOptionName = "--namespace";
    private const string NamespaceOptionAlias = "-n";
    private const string NamespaceOptionDescription = "The namespace for the generated files";

    public static Command Create()
    {
        var command = new Command("generate", "Generate Encina components")
        {
            CreateHandlerCommand(),
            CreateQueryCommand(),
            CreateSagaCommand(),
            CreateNotificationCommand(),
            CreateStrykerCommand()
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

        var outputOption = new Option<string?>(OutputOptionName, OutputOptionAlias)
        {
            Description = OutputOptionDescription
        };

        var namespaceOption = new Option<string?>(NamespaceOptionName, NamespaceOptionAlias)
        {
            Description = NamespaceOptionDescription
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

        var outputOption = new Option<string?>(OutputOptionName, OutputOptionAlias)
        {
            Description = OutputOptionDescription
        };

        var namespaceOption = new Option<string?>(NamespaceOptionName, NamespaceOptionAlias)
        {
            Description = NamespaceOptionDescription
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

        var outputOption = new Option<string?>(OutputOptionName, OutputOptionAlias)
        {
            Description = OutputOptionDescription
        };

        var namespaceOption = new Option<string?>(NamespaceOptionName, NamespaceOptionAlias)
        {
            Description = NamespaceOptionDescription
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

        var outputOption = new Option<string?>(OutputOptionName, OutputOptionAlias)
        {
            Description = OutputOptionDescription
        };

        var namespaceOption = new Option<string?>(NamespaceOptionName, NamespaceOptionAlias)
        {
            Description = NamespaceOptionDescription
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

    private static Command CreateStrykerCommand()
    {
        var projectOption = new Option<string>("--project", "-p")
        {
            Description = "The project file to mutate (e.g., src/MyApp/MyApp.csproj)",
            Required = true
        };

        var testProjectsOption = new Option<string[]?>("--test-projects", "-t")
        {
            Description = "The test project files (comma-separated or multiple -t entries)",
            AllowMultipleArgumentsPerToken = true
        };

        var outputOption = new Option<string?>(OutputOptionName, OutputOptionAlias)
        {
            Description = "The output directory for the configuration file"
        };

        var thresholdHighOption = new Option<int?>("--threshold-high")
        {
            Description = "The threshold for high (green) score (default: 80)"
        };

        var thresholdLowOption = new Option<int?>("--threshold-low")
        {
            Description = "The threshold for low (yellow) score (default: 60)"
        };

        var thresholdBreakOption = new Option<int?>("--threshold-break")
        {
            Description = "The threshold at which the build fails (default: 50)"
        };

        var advancedOption = new Option<bool>("--advanced", "-a")
        {
            Description = "Generate advanced configuration with baseline and since options"
        };

        var command = new Command("stryker", "Generate a Stryker.NET configuration file for mutation testing")
        {
            projectOption,
            testProjectsOption,
            outputOption,
            thresholdHighOption,
            thresholdLowOption,
            thresholdBreakOption,
            advancedOption
        };

        command.SetAction(parseResult =>
        {
            var project = parseResult.GetValue(projectOption);
            var rawTestProjects = parseResult.GetValue(testProjectsOption) ?? [];

            var testProjects = rawTestProjects
                .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToArray();

            var output = parseResult.GetValue(outputOption);
            var thresholdHigh = parseResult.GetValue(thresholdHighOption) ?? 80;
            var thresholdLow = parseResult.GetValue(thresholdLowOption) ?? 60;
            var thresholdBreak = parseResult.GetValue(thresholdBreakOption) ?? 50;
            var advanced = parseResult.GetValue(advancedOption);

            return ExecuteStrykerAsync(project!, testProjects, output, thresholdHigh, thresholdLow, thresholdBreak, advanced);
        });

        return command;
    }

    private static async Task<int> ExecuteStrykerAsync(
        string project,
        string[] testProjects,
        string? output,
        int thresholdHigh,
        int thresholdLow,
        int thresholdBreak,
        bool advanced)
    {
        var options = new StrykerOptions
        {
            ProjectPath = project,
            TestProjects = testProjects,
            OutputDirectory = output ?? Directory.GetCurrentDirectory(),
            ThresholdHigh = thresholdHigh,
            ThresholdLow = thresholdLow,
            ThresholdBreak = thresholdBreak,
            UseAdvanced = advanced
        };

        try
        {
            var result = await CodeGenerator.GenerateStrykerConfigAsync(options);

            if (result.Success)
            {
                AnsiConsole.MarkupLine("[green]Generated Stryker configuration[/]");
                foreach (var file in result.GeneratedFiles)
                {
                    AnsiConsole.MarkupLine($"  [dim]Created: {file}[/]");
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]To run mutation testing:[/]");
                AnsiConsole.MarkupLine("[cyan]  dotnet tool install -g dotnet-stryker[/]");
                AnsiConsole.MarkupLine("[cyan]  dotnet stryker[/]");

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
