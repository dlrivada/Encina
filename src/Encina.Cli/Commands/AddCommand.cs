using System.CommandLine;
using Encina.Cli.Services;
using Spectre.Console;

namespace Encina.Cli.Commands;

/// <summary>
/// Command for adding Encina packages to a project.
/// </summary>
internal static class AddCommand
{
    public static Command Create()
    {
        var command = new Command("add", "Add Encina packages to a project")
        {
            CreateCachingCommand(),
            CreateDatabaseCommand(),
            CreateTransportCommand(),
            CreateValidationCommand(),
            CreateResilienceCommand(),
            CreateObservabilityCommand()
        };

        return command;
    }

    private static Command CreateCachingCommand()
    {
        var providerArgument = new Argument<string>("provider")
        {
            Description = "The caching provider (memory, redis, valkey, garnet, dragonfly, keydb, hybrid)"
        };

        var command = new Command("caching", "Add a caching provider")
        {
            providerArgument
        };

        command.Aliases.Add("cache");

        command.SetAction(parseResult =>
        {
            var provider = parseResult.GetValue(providerArgument);
            return ExecuteCachingAsync(provider!);
        });

        return command;
    }

    private static Command CreateDatabaseCommand()
    {
        var providerArgument = new Argument<string>("provider")
        {
            Description = "The database provider (efcore, dapper-sqlserver, dapper-postgresql, dapper-mysql, dapper-sqlite, dapper-oracle, ado-sqlserver, ado-postgresql, mongodb, marten)"
        };

        var command = new Command("database", "Add a database provider")
        {
            providerArgument
        };

        command.Aliases.Add("db");

        command.SetAction(parseResult =>
        {
            var provider = parseResult.GetValue(providerArgument);
            return ExecuteDatabaseAsync(provider!);
        });

        return command;
    }

    private static Command CreateTransportCommand()
    {
        var providerArgument = new Argument<string>("provider")
        {
            Description = "The messaging transport (rabbitmq, kafka, azureservicebus, sqs, nats, mqtt, signalr)"
        };

        var command = new Command("transport", "Add a messaging transport")
        {
            providerArgument
        };

        command.Aliases.Add("messaging");

        command.SetAction(parseResult =>
        {
            var provider = parseResult.GetValue(providerArgument);
            return ExecuteTransportAsync(provider!);
        });

        return command;
    }

    private static Command CreateValidationCommand()
    {
        var providerArgument = new Argument<string>("provider")
        {
            Description = "The validation provider (fluentvalidation, dataannotations, minivalidator)"
        };

        var command = new Command("validation", "Add a validation provider")
        {
            providerArgument
        };

        command.SetAction(parseResult =>
        {
            var provider = parseResult.GetValue(providerArgument);
            return ExecuteValidationAsync(provider!);
        });

        return command;
    }

    private static Command CreateResilienceCommand()
    {
        var providerArgument = new Argument<string>("provider")
        {
            Description = "The resilience provider (polly, standard)"
        };

        var command = new Command("resilience", "Add a resilience provider")
        {
            providerArgument
        };

        command.SetAction(parseResult =>
        {
            var provider = parseResult.GetValue(providerArgument);
            return ExecuteResilienceAsync(provider!);
        });

        return command;
    }

    private static Command CreateObservabilityCommand()
    {
        var command = new Command("observability", "Add OpenTelemetry observability");

        command.Aliases.Add("otel");

        command.SetAction(_ => ExecuteObservabilityAsync());

        return command;
    }

    private static async Task<int> ExecuteCachingAsync(string provider)
    {
        var packageName = provider.ToLowerInvariant() switch
        {
            "memory" => "Encina.Caching.Memory",
            "redis" => "Encina.Caching.Redis",
            "valkey" => "Encina.Caching.Valkey",
            "garnet" => "Encina.Caching.Garnet",
            "dragonfly" => "Encina.Caching.Dragonfly",
            "keydb" => "Encina.Caching.KeyDB",
            "hybrid" => "Encina.Caching.Hybrid",
            _ => null
        };

        if (packageName is null)
        {
            AnsiConsole.MarkupLine($"[red]Unknown caching provider: {provider}[/]");
            AnsiConsole.MarkupLine("[yellow]Available providers: memory, redis, valkey, garnet, dragonfly, keydb, hybrid[/]");
            return 1;
        }

        return await AddPackageAsync(packageName, "Encina.Caching");
    }

    private static async Task<int> ExecuteDatabaseAsync(string provider)
    {
        var packageName = provider.ToLowerInvariant() switch
        {
            "efcore" or "entityframeworkcore" => "Encina.EntityFrameworkCore",
            "dapper-sqlserver" => "Encina.Dapper.SqlServer",
            "dapper-postgresql" or "dapper-postgres" => "Encina.Dapper.PostgreSQL",
            "dapper-mysql" => "Encina.Dapper.MySQL",
            "dapper-sqlite" => "Encina.Dapper.Sqlite",
            "dapper-oracle" => "Encina.Dapper.Oracle",
            "ado-sqlserver" => "Encina.ADO.SqlServer",
            "ado-postgresql" or "ado-postgres" => "Encina.ADO.PostgreSQL",
            "ado-mysql" => "Encina.ADO.MySQL",
            "ado-sqlite" => "Encina.ADO.Sqlite",
            "ado-oracle" => "Encina.ADO.Oracle",
            "mongodb" => "Encina.MongoDB",
            "marten" => "Encina.Marten",
            _ => null
        };

        if (packageName is null)
        {
            AnsiConsole.MarkupLine($"[red]Unknown database provider: {provider}[/]");
            AnsiConsole.MarkupLine("[yellow]Available providers: efcore, dapper-sqlserver, dapper-postgresql, dapper-mysql, dapper-sqlite, dapper-oracle, ado-sqlserver, ado-postgresql, mongodb, marten[/]");
            return 1;
        }

        return await AddPackageAsync(packageName);
    }

    private static async Task<int> ExecuteTransportAsync(string provider)
    {
        var packageName = provider.ToLowerInvariant() switch
        {
            "rabbitmq" => "Encina.RabbitMQ",
            "kafka" => "Encina.Kafka",
            "azureservicebus" or "servicebus" => "Encina.AzureServiceBus",
            "sqs" or "amazonsqs" => "Encina.AmazonSQS",
            "nats" => "Encina.NATS",
            "mqtt" => "Encina.MQTT",
            "signalr" => "Encina.SignalR",
            "redis" or "redis-pubsub" => "Encina.Redis.PubSub",
            _ => null
        };

        if (packageName is null)
        {
            AnsiConsole.MarkupLine($"[red]Unknown transport: {provider}[/]");
            AnsiConsole.MarkupLine("[yellow]Available transports: rabbitmq, kafka, azureservicebus, sqs, nats, mqtt, signalr, redis[/]");
            return 1;
        }

        return await AddPackageAsync(packageName);
    }

    private static async Task<int> ExecuteValidationAsync(string provider)
    {
        var packageName = provider.ToLowerInvariant() switch
        {
            "fluentvalidation" or "fluent" => "Encina.FluentValidation",
            "dataannotations" or "annotations" => "Encina.DataAnnotations",
            "minivalidator" or "mini" => "Encina.MiniValidator",
            _ => null
        };

        if (packageName is null)
        {
            AnsiConsole.MarkupLine($"[red]Unknown validation provider: {provider}[/]");
            AnsiConsole.MarkupLine("[yellow]Available providers: fluentvalidation, dataannotations, minivalidator[/]");
            return 1;
        }

        return await AddPackageAsync(packageName);
    }

    private static async Task<int> ExecuteResilienceAsync(string provider)
    {
        var packageName = provider.ToLowerInvariant() switch
        {
            "polly" => "Encina.Polly",
            "standard" => "Encina.Extensions.Resilience",
            _ => null
        };

        if (packageName is null)
        {
            AnsiConsole.MarkupLine($"[red]Unknown resilience provider: {provider}[/]");
            AnsiConsole.MarkupLine("[yellow]Available providers: polly, standard[/]");
            return 1;
        }

        return await AddPackageAsync(packageName);
    }

    private static Task<int> ExecuteObservabilityAsync()
    {
        return AddPackageAsync("Encina.OpenTelemetry");
    }

    private static async Task<int> AddPackageAsync(string packageName, string? additionalPackage = null)
    {
        try
        {
            var packages = additionalPackage is not null
                ? new[] { additionalPackage, packageName }
                : [packageName];

            var result = await PackageManager.AddPackagesAsync(packages);

            if (result.Success)
            {
                foreach (var package in packages)
                {
                    AnsiConsole.MarkupLine($"[green]Added package '[bold]{package}[/]'[/]");
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
