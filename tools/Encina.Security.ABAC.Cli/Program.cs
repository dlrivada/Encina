using System.CommandLine;

using Encina.Security.ABAC.Cli.Commands;

var rootCommand = new RootCommand(
    "Encina EEL (Encina Expression Language) validator — " +
    "validates [RequireCondition] expressions in .NET assemblies");

rootCommand.Subcommands.Add(VerifyCommand.Create());
rootCommand.Subcommands.Add(EvalCommand.Create());
rootCommand.Subcommands.Add(ListCommand.Create());

return await rootCommand.Parse(args).InvokeAsync();
