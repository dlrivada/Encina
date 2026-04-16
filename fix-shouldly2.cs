#!/usr/bin/env dotnet-script
using System.Text.RegularExpressions;

var testDir = @"D:\Proyectos\Encina\tests\Encina.UnitTests";
var files = Directory.GetFiles(testDir, "*.cs", SearchOption.AllDirectories);
var fixedCount = 0;

foreach (var file in files)
{
    var content = File.ReadAllText(file);
    var original = content;

    // Fix: .Which. after ShouldHaveSingleItem() - Shouldly returns the item directly
    // e.g. .ShouldHaveSingleItem()\n            .Which.ShouldBeSameAs(x) -> .ShouldHaveSingleItem()\n            .ShouldBeSameAs(x)
    content = Regex.Replace(content, @"(\.ShouldHaveSingleItem\(\))\s*\n(\s*)\.Which\.", "$1\n$2.");

    // Inline case
    content = Regex.Replace(content, @"(\.ShouldHaveSingleItem\(\))\.Which\.", "$1.");

    // Fix: .Which. after any Shouldly assertion result on same line
    // e.g. ShouldContain(...).Which.PropertyName -> need different approach (access item then assert)
    // For now, just handle the simple .Which. removal after ShouldHaveSingleItem

    // Fix: .ShouldBeOfType<T>().Subject -> .ShouldBeOfType<T>() (was already done but do again as failsafe)
    content = Regex.Replace(content, @"(\.ShouldBeOfType<[^>]+>\(\))\.Subject\b", "$1");

    // Fix .Which. after .ShouldBeOfType<T>()
    content = Regex.Replace(content, @"(\.ShouldBeOfType<[^>]+>\(\))\s*\n(\s*)\.Which\.", "$1\n$2.");
    content = Regex.Replace(content, @"(\.ShouldBeOfType<[^>]+>\(\))\.Which\.", "$1.");

    // Generic: remove .Which. after any ) that's a Shouldly call
    // This is the pattern: xxx.ShouldXxx()\n    .Which.Property
    // In Shouldly, the call returns the item itself, so .Which is not needed
    content = Regex.Replace(content, @"(\.\w+\([^)]*\))\s*\n(\s*)\.Which\.", "$1\n$2.");
    content = Regex.Replace(content, @"(\.\w+\([^)]*\))\.Which\.", "$1.");

    if (content != original)
    {
        File.WriteAllText(file, content);
        fixedCount++;
        Console.WriteLine($"Fixed: {Path.GetRelativePath(testDir, file)}");
    }
}

Console.WriteLine($"\nTotal files fixed: {fixedCount}");
