using System;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: dotnet run --file scripts/show-lines.cs <path> [start] [end]");
    return;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return;
}

var lines = File.ReadAllLines(path);
var start = 1;
var end = lines.Length;

if (args.Length > 1 && int.TryParse(args[1], out var parsedStart))
{
    start = Math.Max(1, parsedStart);
}

if (args.Length > 2 && int.TryParse(args[2], out var parsedEnd))
{
    end = Math.Min(lines.Length, parsedEnd);
}

for (var index = start; index <= end; index++)
{
    var line = lines[index - 1];
    Console.WriteLine($"{index,4}: {line}");
}
