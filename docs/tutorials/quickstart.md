---
title: "Quickstart: your first command"
layout: default
parent: "Tutorials"
nav_order: 1
---

# Quickstart: your first command

This tutorial is for a .NET 10 developer who has never used Encina. By the end you will have a console app that sends one command through `IEncina` and prints both the success and the error track of the result. If you want to know why Encina is built this way first, read [the introduction](../introduction.md); this page teaches by doing.

Encina is pre-1.0 and is not published to nuget.org yet — packages ship only to GitHub Packages, which requires authentication, so `dotnet add package Encina` from the README does not work today. This tutorial builds a local package feed from a clone instead; that is the supported path before 1.0.

## 1. Clone the repository

```bash
git clone https://github.com/dlrivada/Encina.git
```

## 2. Pack the core library into a local feed

```bash
mkdir feed
dotnet pack Encina/src/Encina/Encina.csproj -c Release -o feed
```

You should see a confirmation line naming the package file it produced, for example `feed/Encina.0.14.0-dev.nupkg`. The exact version depends on the commit you cloned; note the `-dev` prerelease suffix, you will need it in the next step.

## 3. Create a console app

```bash
dotnet new console -n QuickstartDemo
cd QuickstartDemo
```

## 4. Add Encina from the local feed

```bash
dotnet add package Encina --source ../feed --prerelease
```

`--prerelease` is required because the packed version carries the `-dev` suffix.

## 5. Write the command, the handler and the caller

Replace the contents of `Program.cs` with:

```csharp
using Encina;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEncina(typeof(Program).Assembly);

await using var provider = services.BuildServiceProvider();
var encina = provider.GetRequiredService<IEncina>();

var success = await encina.Send(new Greet("World"));
success.Match(
    Left: error => Console.WriteLine($"Error: {error.Message}"),
    Right: message => Console.WriteLine(message));

var failure = await encina.Send(new Greet(""));
failure.Match(
    Left: error => Console.WriteLine($"Error: {error.Message}"),
    Right: message => Console.WriteLine(message));

public sealed record Greet(string Name) : ICommand<string>;

public sealed class GreetHandler : ICommandHandler<Greet, string>
{
    public Task<Either<EncinaError, string>> Handle(Greet request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Task.FromResult<Either<EncinaError, string>>(
                EncinaErrors.Create("greet.name_required", "Name must not be empty."));

        return Task.FromResult<Either<EncinaError, string>>($"Hello, {request.Name}!");
    }
}
```

`AddEncina` scans the given assembly, finds `GreetHandler` and wires it to `IEncina` for you. `Send` always returns `Either<EncinaError, TResponse>`: the first call carries a name and takes the success (`Right`) track; the second sends an empty name and takes the error (`Left`) track. `Match` runs exactly one of the two branches, so your code never has to check for `null` or catch an exception to know which track it is on.

## 6. Run it

```bash
dotnet run
```

You should see:

```text
Hello, World!
Error: Name must not be empty.
```

The first line is the success track from `Send(new Greet("World"))`. The second line is the error track from `Send(new Greet(""))`, where `GreetHandler` returned `EncinaErrors.Create(...)` instead of a greeting.

## If something goes wrong

- **`error NETSDK1045` or the console app fails to restore**: you need the .NET 10 SDK; `dotnet --version` must report `10.x`.
- **`error NU1102: Unable to find package Encina with version (>= ...)`**: you forgot `--prerelease`, or the `--source` path does not point at the `feed` folder from step 2.

## Next steps

Browse the [features overview](../features/index.md) to see the pipeline behaviours, messaging patterns and providers you can add next.
