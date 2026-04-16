#!/usr/bin/env dotnet-script
using System.Text.RegularExpressions;

var testDir = @"D:\Proyectos\Encina\tests\Encina.UnitTests";
var files = Directory.GetFiles(testDir, "*.cs", SearchOption.AllDirectories);
var fixedCount = 0;

foreach (var file in files)
{
    var content = File.ReadAllText(file);
    var original = content;

    // Fix 1: Remove .Subject after ShouldBeOfType<T>()
    // e.g. .ShouldBeOfType<BreachAssessed>().Subject -> .ShouldBeOfType<BreachAssessed>()
    content = Regex.Replace(content, @"(\.ShouldBeOfType<[^>]+>\(\))\.Subject\b", "$1");

    // Fix 2: Remove .Which. before ShouldBeOfType
    // e.g. .Which.ShouldBeOfType<T>() -> .ShouldBeOfType<T>()
    content = Regex.Replace(content, @"\.Which\.ShouldBeOfType<", ".ShouldBeOfType<");

    // Fix 3: Remove .And. after Should.Throw results
    // e.g. Should.Throw<ArgumentException>(act).And.ParamName -> Should.Throw<ArgumentException>(act).ParamName
    content = Regex.Replace(content, @"(Should\.Throw<[^>]+>\([^)]+\))\.And\.", "$1.");

    // Also handle multiline: Should.Throw<T>(act)\n            .And.ParamName
    content = Regex.Replace(content, @"(Should\.Throw<[^>]+>\([^)]+\))\s*\n(\s*)\.And\.", "$1\n$2.");

    if (content != original)
    {
        File.WriteAllText(file, content);
        fixedCount++;
        Console.WriteLine($"Fixed: {Path.GetRelativePath(testDir, file)}");
    }
}

Console.WriteLine($"\nTotal files fixed: {fixedCount}");
