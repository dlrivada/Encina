using System.CommandLine;
using Encina.Cli.Commands;

var rootCommand = new RootCommand("Encina CLI - Scaffolding tool for Encina-based projects");

rootCommand.Subcommands.Add(NewCommand.Create());
rootCommand.Subcommands.Add(GenerateCommand.Create());
rootCommand.Subcommands.Add(AddCommand.Create());

return rootCommand.Parse(args).Invoke();
