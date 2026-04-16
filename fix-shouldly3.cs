#!/usr/bin/env dotnet-script
using System.Text.RegularExpressions;

var testDir = @"D:\Proyectos\Encina\tests\Encina.UnitTests";
var files = Directory.GetFiles(testDir, "*.cs", SearchOption.AllDirectories);
var fixedCount = 0;

foreach (var file in files)
{
    var content = File.ReadAllText(file);
    var original = content;

    // Fix CS0119: .Count.ShouldBe -> .Count().ShouldBe (for IEnumerable types)
    // Pattern: .Count.ShouldBe( where Count is used as expression (not a list property)
    // Only fix if the current context suggests it's used as a method call attempt
    // We can detect this by checking if the error says 'Enumerable.Count' or 'MemoryExtensions.Count'
    // The pattern is: xxx.Count.ShouldBe -> xxx.Count().ShouldBe
    // But we must NOT fix xxx.Count.ShouldBe when Count IS a property (like List<T>.Count)
    // The way to tell: if it's generating an Enumerable.Count error, the type doesn't have a .Count property

    // Fix .Count.ShouldBe( -> .Count().ShouldBe(
    // Conservative: only fix when preceded by specific patterns that are IEnumerable
    // Based on the errors, these are: Failures.Count, items.Count in various contexts
    // Let's do it for all .Count.ShouldBe since if it's a property it should also work with ()
    // Actually .Count() works for both ICollection.Count (property via extension) and LINQ Count()
    // So safe to change .Count.ShouldBe to .Count().ShouldBe
    content = Regex.Replace(content, @"\.Count\.Should", ".Count().Should");

    // Fix CS8604: nullable string? passed to ShouldContain - add ! to null-forgive
    // result.Description.ShouldContain -> result.Description!.ShouldContain
    // This is tricky - we need to know which ones are nullable
    // The common pattern: result.Description.ShouldContain where result is HealthCheckResult
    content = Regex.Replace(content, @"\bresult\.Description\.ShouldContain\b", "result.Description!.ShouldContain");
    content = Regex.Replace(content, @"\bresult\.FailureMessage\.ShouldContain\b", "result.FailureMessage!.ShouldContain");
    content = Regex.Replace(content, @"\bresult\.Failures\.Count\(\)", "result.Failures.Count()");

    // Fix CS0411: ShouldNotBeEmpty type inference
    // .ShouldNotBeEmpty() -> .ShouldNotBeEmpty<string>() or cast to list
    // The issue is when called on non-generic types - let's use ToList() approach
    // Pattern: someCollection.ShouldNotBeEmpty() where type can't be inferred
    // Better approach: (someList).ToList().ShouldNotBeEmpty()
    // But this is contextual. Let's look at specific patterns:
    // result.Specifications.ShouldNotBeEmpty() etc.

    if (content != original)
    {
        File.WriteAllText(file, content);
        fixedCount++;
        Console.WriteLine($"Fixed: {Path.GetRelativePath(testDir, file)}");
    }
}

Console.WriteLine($"\nTotal files fixed: {fixedCount}");
