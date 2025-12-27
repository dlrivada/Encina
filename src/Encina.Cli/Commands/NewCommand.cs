using System.CommandLine;
using Encina.Cli.Services;
using Spectre.Console;

namespace Encina.Cli.Commands;

/// <summary>
/// Command for creating new Encina projects.
/// </summary>
internal static class NewCommand
{
    public static Command Create()
    {
        var templateArgument = new Argument<string>("template")
        {
            Description = "The project template to use (api, worker, console)"
        };

        var nameArgument = new Argument<string>("name")
        {
            Description = "The name of the project to create"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "The output directory for the project"
        };

        var databaseOption = new Option<string?>("--database", "-d")
        {
            Description = "The database provider to use (sqlserver, postgresql, mysql, sqlite, mongodb)"
        };

        var cachingOption = new Option<string?>("--caching", "-c")
        {
            Description = "The caching provider to use (memory, redis, hybrid)"
        };

        var transportOption = new Option<string?>("--transport", "-t")
        {
            Description = "The messaging transport to use (rabbitmq, kafka, azureservicebus, sqs)"
        };

        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Overwrite existing files"
        };

        var command = new Command("new", "Create a new Encina project")
        {
            templateArgument,
            nameArgument,
            outputOption,
            databaseOption,
            cachingOption,
            transportOption,
            forceOption
        };

        command.SetAction(parseResult =>
        {
            var template = parseResult.GetValue(templateArgument);
            var name = parseResult.GetValue(nameArgument);
            var output = parseResult.GetValue(outputOption);
            var database = parseResult.GetValue(databaseOption);
            var caching = parseResult.GetValue(cachingOption);
            var transport = parseResult.GetValue(transportOption);
            var force = parseResult.GetValue(forceOption);

            return ExecuteAsync(template!, name!, output, database, caching, transport, force);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string template,
        string name,
        string? output,
        string? database,
        string? caching,
        string? transport,
        bool force)
    {
        var options = new ProjectOptions
        {
            Template = template,
            Name = name,
            OutputDirectory = output ?? Path.Combine(Directory.GetCurrentDirectory(), name),
            Database = database,
            Caching = caching,
            Transport = transport,
            Force = force
        };

        try
        {
            var result = await ProjectScaffolder.CreateProjectAsync(options);

            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]Successfully created project '[bold]{name}[/]' using template '[bold]{template}[/]'[/]");
                AnsiConsole.MarkupLine($"[dim]Location: {options.OutputDirectory}[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Next steps:[/]");
                AnsiConsole.MarkupLine($"  cd {name}");
                AnsiConsole.MarkupLine("  dotnet restore");
                AnsiConsole.MarkupLine("  dotnet run");
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
