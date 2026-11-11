using System.Text;
using System.Text.RegularExpressions;
using Shouldly;

namespace Encina.UnitTests.Core;

/// <summary>
/// Completeness guard of the error category contract (ADR-062, #3545): every <c>EncinaErrors.Create(</c> and
/// <c>EncinaErrors.FromException(</c> call under <c>src/</c> passes <c>ErrorCategory.X</c> or a named
/// <c>category:</c> argument, unless its path is still pending in <c>ErrorCategoryPending.txt</c>.
/// </summary>
/// <remarks>
/// <para>
/// The pending file lists path prefixes relative to the repository root, one entry per line and a blank line
/// between entries, each written <c>prefix =&gt; owner</c> where the owner is the migration slice or the issue that
/// categorizes those calls. A slice deletes its own line, so parallel slices merge without conflict.
/// </para>
/// <para>
/// The scan also fails on a stale pending entry (no uncategorized call left under it) and on one code created with
/// two different categories. Known limit: a category passed through a variable must use the named
/// <c>category:</c> argument to be recognised; a code-less error (<see cref="EncinaError.New(string)"/>) is invisible
/// to the scan and is always <see cref="ErrorCategory.Internal"/>.
/// </para>
/// </remarks>
public sealed class ErrorCategoryCompletenessStaticScanTests
{
    private const string PendingFile = "tests/Encina.UnitTests/Core/ErrorCategoryPending.txt";

    [Fact]
    public void Src_EveryEncinaErrorsCallDeclaresACategory_UnlessItsPathIsPending()
    {
        var root = FindRepositoryRoot();
        var pending = PendingList.Parse(File.ReadAllLines(Path.Combine(root, PendingFile)));
        pending.FormatErrors.ShouldBeEmpty($"{PendingFile} is malformed:\n" + string.Join('\n', pending.FormatErrors));

        var calls = ScanSource(root);
        calls.ShouldNotBeEmpty("The scan found no EncinaErrors call under src/; the scanner or the path is broken.");

        var violations = new List<string>();
        var usedEntries = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        foreach (var (path, call) in calls.Where(static c => !c.Call.IsCategorized))
        {
            var entry = pending.Entries.FirstOrDefault(e => path.StartsWith(e.Prefix, StringComparison.Ordinal));
            if (entry is null)
            {
                violations.Add($"{path}:{call.Line}: {call.Method}({call.CodeExpression}, ...) has no ErrorCategory");
                continue;
            }

            usedEntries.Add(entry.Prefix);
        }

        violations.ShouldBeEmpty(
            "Pass ErrorCategory.X (or a named category: argument) to every EncinaErrors.Create/FromException call; "
            + "choose it with the rules of ADR-062:\n" + string.Join('\n', violations));

        var stale = pending.Entries.Where(e => !usedEntries.Contains(e.Prefix)).Select(static e => $"{e.Prefix} => {e.Owner}").ToList();
        stale.ShouldBeEmpty($"Stale entries in {PendingFile} (no uncategorized call is left under them); delete them:\n" + string.Join('\n', stale));
    }

    [Fact]
    public void Src_NoCodeIsCreatedWithTwoDifferentCategories()
    {
        var conflicts = ScanSource(FindRepositoryRoot())
            .Where(static c => c.Call.CategoryName is not null)
            .GroupBy(static c => CodeKey(c.Path, c.Call.CodeExpression), StringComparer.Ordinal)
            .Where(static g => g.Select(static c => c.Call.CategoryName).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(static g => $"{g.Key}: " + string.Join(", ", g.Select(static c => $"{c.Call.CategoryName} at {c.Path}:{c.Call.Line}")))
            .ToList();

        conflicts.ShouldBeEmpty("One error code must always carry the same category:\n" + string.Join('\n', conflicts));
    }

    [Theory]
    [InlineData("return EncinaErrors.Create(Codes.NotFound, ErrorCategory.NotFound, \"missing\");", "NotFound")]
    [InlineData("return EncinaErrors.Create(\"x.y\", Encina.ErrorCategory.Conflict, $\"conflict {id}\");", "Conflict")]
    [InlineData("return EncinaErrors.FromException(Codes.Failed, ErrorCategory.Internal, ex);", "Internal")]
    [InlineData("return EncinaErrors.Create(code: Codes.Bad, message: \"m\", category: ErrorCategory.InvalidRequest);", "InvalidRequest")]
    [InlineData("return EncinaErrors.Create(\n    Codes.Gone,\n    ErrorCategory.NotFound,\n    \"m\",\n    details: new Dictionary<string, object?> { [\"k\"] = 1 });", "NotFound")]
    [InlineData("return EncinaErrors.Create(Codes.X, ErrorCategory.NotFound, $\"{string.Join(\", \", xs)} missing\");", "NotFound")]
    [InlineData("return EncinaErrors.Create(Codes.X, ErrorCategory.Timeout, $\"\"\"took {elapsed} \"\"\");", "Timeout")]
    public void Scanner_RecognisesALiteralCategory(string statement, string expectedCategory)
    {
        var call = ErrorCategoryCallScanner.Analyze(Wrap(statement)).ShouldHaveSingleItem();

        call.IsCategorized.ShouldBeTrue();
        call.CategoryName.ShouldBe(expectedCategory);
    }

    [Fact]
    public void Scanner_AcceptsANamedCategoryVariable_WithoutAName()
    {
        var call = ErrorCategoryCallScanner.Analyze(Wrap("return EncinaErrors.Create(code, category: category, message);")).ShouldHaveSingleItem();

        call.IsCategorized.ShouldBeTrue();
        call.CategoryName.ShouldBeNull();
    }

    [Theory]
    [InlineData("return EncinaErrors.Create(Codes.NotFound, \"missing\");")]
    [InlineData("return EncinaErrors.FromException(Codes.Failed, ex, \"ErrorCategory.NotFound\");")]
    [InlineData("return EncinaErrors.Create(Codes.X, category, \"positional variable is not recognised\");")]
    [InlineData("return EncinaErrors.Create(Codes.X, \"m\", details: new Dictionary<string, object?> { [\"category\"] = 1 });")]
    public void Scanner_FlagsACallWithoutACategory(string statement)
    {
        ErrorCategoryCallScanner.Analyze(Wrap(statement)).ShouldHaveSingleItem().IsCategorized.ShouldBeFalse();
    }

    [Theory]
    [InlineData("// return EncinaErrors.Create(Codes.X, \"m\");")]
    [InlineData("/* EncinaErrors.Create(Codes.X, \"m\"); */")]
    [InlineData("var text = \"EncinaErrors.Create(Codes.X, m)\";")]
    [InlineData("/// <see cref=\"EncinaErrors.Create(string, string)\"/>")]
    public void Scanner_IgnoresCommentsAndStrings(string statement)
    {
        ErrorCategoryCallScanner.Analyze(Wrap(statement)).ShouldBeEmpty();
    }

    [Fact]
    public void Scanner_DropsCommentsInsideTheArguments()
    {
        var call = ErrorCategoryCallScanner.Analyze(Wrap("return EncinaErrors.Create( // NOSONAR\n    code: Codes.X, /* why */ ErrorCategory.Forbidden, \"m\");")).ShouldHaveSingleItem();

        call.CodeExpression.ShouldBe("Codes.X");
        call.CategoryName.ShouldBe("Forbidden");
    }

    [Fact]
    public void Scanner_ReportsTheLineAndTheCodeOfEachCall()
    {
        const string Source = "class C\n{\n    E A() => EncinaErrors.Create(\"a.b\", \"m\");\n\n    E B() =>\n        EncinaErrors.FromException(\n            Codes.B, ErrorCategory.Timeout, ex);\n}\n";

        var calls = ErrorCategoryCallScanner.Analyze(Source);

        calls.Count.ShouldBe(2);
        calls[0].Line.ShouldBe(3);
        calls[0].Method.ShouldBe("Create");
        calls[0].CodeExpression.ShouldBe("\"a.b\"");
        calls[1].Line.ShouldBe(6);
        calls[1].Method.ShouldBe("FromException");
        calls[1].CodeExpression.ShouldBe("Codes.B");
    }

    [Fact]
    public void PendingList_ParsesEntriesSeparatedByBlankLines()
    {
        var list = PendingList.Parse(["# header", "", "src/Encina.A/ => P-A", "", "src/Encina.B/X.cs => #3420"]);

        list.FormatErrors.ShouldBeEmpty();
        list.Entries.Select(static e => e.Prefix).ShouldBe(["src/Encina.A/", "src/Encina.B/X.cs"]);
        list.Entries[1].Owner.ShouldBe("#3420");
    }

    [Fact]
    public void PendingList_RejectsAdjacentEntries()
    {
        PendingList.Parse(["src/Encina.A/ => P-A", "src/Encina.B/ => P-B"]).FormatErrors.ShouldHaveSingleItem().ShouldContain("blank line");
    }

    [Theory]
    [InlineData("src/Encina.A/")]
    [InlineData("src/Encina.A/ => ")]
    [InlineData(" => P-A")]
    public void PendingList_RejectsAnEntryWithoutPrefixOrOwner(string line)
    {
        PendingList.Parse([line]).FormatErrors.ShouldHaveSingleItem().ShouldContain("expected");
    }

    [Fact]
    public void PendingList_RejectsADuplicateEntry()
    {
        PendingList.Parse(["src/Encina.A/ => P-A", "", "src/Encina.A/ => P-B"]).FormatErrors.ShouldHaveSingleItem().ShouldContain("duplicate");
    }

    [Theory]
    [InlineData("src/Encina.Caching/X.cs", "\"orders.not_found\"", "\"orders.not_found\"")]
    [InlineData("src/Encina.Caching/X.cs", "EncinaErrorCodes.RequestNull", "EncinaErrorCodes.RequestNull")]
    [InlineData("src/Encina.Caching/X.cs", "Codes.Missing", "src/Encina.Caching/:Codes.Missing")]
    [InlineData("src/Encina.Caching/X.cs", "MissingCode", "src/Encina.Caching/X.cs:MissingCode")]
    public void CodeKey_ScopesNonGlobalCodesToTheirPackageOrFile(string path, string codeExpression, string expected)
    {
        CodeKey(path, codeExpression).ShouldBe(expected);
    }

    /// <summary>
    /// The key under which two calls are "the same code": a string literal or an <c>EncinaErrorCodes</c> constant is
    /// global; a qualified constant is scoped to its package (two packages may each have a <c>Codes.NotFound</c>);
    /// a bare identifier is scoped to its file.
    /// </summary>
    private static string CodeKey(string path, string codeExpression)
    {
        if (codeExpression.StartsWith('"') || codeExpression.StartsWith("EncinaErrorCodes.", StringComparison.Ordinal))
        {
            return codeExpression;
        }

        if (codeExpression.Contains('.', StringComparison.Ordinal))
        {
            var package = path.IndexOf('/', "src/".Length);
            return $"{path[..(package + 1)]}:{codeExpression}";
        }

        return $"{path}:{codeExpression}";
    }

    private static string Wrap(string statement) => $"class C {{ object M() {{ {statement}\n }} }}";

    private static List<(string Path, ErrorCategoryCallScanner.Call Call)> ScanSource(string root)
    {
        var result = new List<(string, ErrorCategoryCallScanner.Call)>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relativePath.Contains("/obj/", StringComparison.Ordinal) || relativePath.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            result.AddRange(ErrorCategoryCallScanner.Analyze(File.ReadAllText(file)).Select(call => (relativePath, call)));
        }

        return result;
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Encina.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException($"Encina.slnx not found above {AppContext.BaseDirectory}.");
    }
}

/// <summary>
/// The parsed <c>ErrorCategoryPending.txt</c>: entries <c>prefix =&gt; owner</c>, one per line, a blank line between
/// entries, <c>#</c> lines are comments.
/// </summary>
internal sealed record PendingList(IReadOnlyList<PendingList.Entry> Entries, IReadOnlyList<string> FormatErrors)
{
    internal sealed record Entry(string Prefix, string Owner);

    internal static PendingList Parse(IReadOnlyList<string> lines)
    {
        var entries = new List<Entry>();
        var errors = new List<string>();
        var previousWasEntry = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                previousWasEntry = false;
                continue;
            }

            if (previousWasEntry)
            {
                errors.Add($"line {i + 1}: entries must be separated by a blank line");
            }

            previousWasEntry = true;
            var parts = line.Split(" => ", 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
            {
                errors.Add($"line {i + 1}: expected 'path/prefix => owner', got '{line}'");
                continue;
            }

            if (entries.Exists(e => e.Prefix == parts[0]))
            {
                errors.Add($"line {i + 1}: duplicate entry '{parts[0]}'");
                continue;
            }

            entries.Add(new Entry(parts[0], parts[1]));
        }

        return new PendingList(entries, errors);
    }
}

/// <summary>
/// Finds the <c>EncinaErrors.Create(</c> and <c>EncinaErrors.FromException(</c> calls of a C# source and tells
/// whether each one declares a category. Comments and the contents of string literals are blanked first, so a call
/// inside either is never reported.
/// </summary>
/// <remarks>
/// A lexical scan, not a parser. Two literal forms can desynchronise it: a character literal holding a quote inside
/// an interpolation hole (<c>$"{'"'}"</c>) and a raw string delimited by four or more quotes. Neither occurs in
/// <c>src/</c> today; if one is added, the scan may miss calls after it in that file. An interpolation hole with a
/// paired string (<c>{string.Join(", ", xs)}</c>) only adds a bogus argument, which can never look like a category.
/// </remarks>
internal static partial class ErrorCategoryCallScanner
{
    internal sealed record Call(int Line, string Method, string CodeExpression, bool IsCategorized, string? CategoryName);

    [GeneratedRegex(@"\bEncinaErrors\s*\.\s*(?<method>Create|FromException)\s*\(")]
    private static partial Regex CallRegex();

    [GeneratedRegex(@"^(?:category\s*:\s*)?(?:global::)?(?:Encina\.)?ErrorCategory\.(?<name>\w+)$")]
    private static partial Regex LiteralCategoryRegex();

    [GeneratedRegex(@"^category\s*:")]
    private static partial Regex NamedCategoryRegex();

    [GeneratedRegex(@"^code\s*:\s*")]
    private static partial Regex NamedCodeRegex();

    internal static IReadOnlyList<Call> Analyze(string source)
    {
        var (withoutComments, masked) = Mask(source);
        var calls = new List<Call>();

        foreach (Match match in CallRegex().Matches(masked))
        {
            var open = match.Index + match.Length - 1;
            var arguments = SplitArguments(withoutComments, masked, open + 1);
            calls.Add(Describe(match.Groups["method"].Value, LineOf(source, match.Index), arguments));
        }

        return calls;
    }

    private static Call Describe(string method, int line, List<string> arguments)
    {
        var code = arguments.Count > 0 ? NamedCodeRegex().Replace(arguments[0], string.Empty) : string.Empty;
        var categorized = false;

        foreach (var argument in arguments)
        {
            var literal = LiteralCategoryRegex().Match(argument);
            if (literal.Success)
            {
                return new Call(line, method, code, true, literal.Groups["name"].Value);
            }

            categorized |= NamedCategoryRegex().IsMatch(argument);
        }

        return new Call(line, method, code, categorized, CategoryName: null);
    }

    private static int LineOf(string source, int index) => source.AsSpan(0, index).Count('\n') + 1;

    /// <summary>
    /// Splits the arguments of a call at depth zero, starting right after its opening parenthesis; the structure is
    /// read from the masked text and each argument is cut from the original, trimmed and with whitespace collapsed.
    /// </summary>
    private static List<string> SplitArguments(string source, string masked, int start)
    {
        var arguments = new List<string>();
        var depth = 0;
        var argumentStart = start;

        for (var i = start; i < masked.Length; i++)
        {
            var c = masked[i];
            if (c is '(' or '[' or '{')
            {
                depth++;
            }
            else if (c is ')' or ']' or '}' && depth-- == 0)
            {
                arguments.Add(Collapse(source[argumentStart..i]));
                return arguments;
            }
            else if (c == ',' && depth == 0)
            {
                arguments.Add(Collapse(source[argumentStart..i]));
                argumentStart = i + 1;
            }
        }

        arguments.Add(Collapse(source[argumentStart..]));
        return arguments;
    }

    private static string Collapse(string text) => WhitespaceRegex().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    /// <summary>
    /// Returns two copies of the source of the same length, so indices and line numbers still match the original:
    /// one where comments are spaces, and one where string and character literal contents are spaces too (the quotes
    /// stay). Structure is read from the second; argument text is cut from the first.
    /// </summary>
    private static (string WithoutComments, string Masked) Mask(string source)
    {
        var withoutComments = new StringBuilder(source);
        var masked = new StringBuilder(source);
        var i = 0;

        while (i < source.Length)
        {
            var end = CommentEnd(source, i);
            if (end > i)
            {
                Blank(withoutComments, i, end);
                Blank(masked, i, end);
                i = end;
                continue;
            }

            if (source[i] is '"' or '\'')
            {
                end = SkipLiteral(source, i);
                Blank(masked, i + 1, end - 1);
                i = end;
                continue;
            }

            i++;
        }

        return (withoutComments.ToString(), masked.ToString());
    }

    /// <summary>Returns the index just after the comment that starts at <paramref name="start"/>, or <paramref name="start"/> when none does.</summary>
    private static int CommentEnd(string text, int start)
    {
        if (text[start] != '/' || start + 1 >= text.Length)
        {
            return start;
        }

        if (text[start + 1] == '/')
        {
            var newline = text.IndexOf('\n', start);
            return newline < 0 ? text.Length : newline;
        }

        if (text[start + 1] == '*')
        {
            var close = text.IndexOf("*/", start + 2, StringComparison.Ordinal);
            return close < 0 ? text.Length : close + 2;
        }

        return start;
    }

    private static void Blank(StringBuilder masked, int from, int to)
    {
        for (var j = from; j < to; j++)
        {
            if (masked[j] != '\n')
            {
                masked[j] = ' ';
            }
        }
    }

    /// <summary>Returns the index just after the string or character literal that starts at <paramref name="start"/>.</summary>
    private static int SkipLiteral(string text, int start)
    {
        var quote = text[start];
        if (quote == '"' && text.AsSpan(start).StartsWith("\"\"\"", StringComparison.Ordinal))
        {
            var close = text.IndexOf("\"\"\"", start + 3, StringComparison.Ordinal);
            return close < 0 ? text.Length : close + 3;
        }

        var verbatim = quote == '"' && start > 0 && (text[start - 1] == '@' || (start > 1 && text[start - 1] == '$' && text[start - 2] == '@'));
        var i = start + 1;
        while (i < text.Length)
        {
            if (text[i] == '\\' && !verbatim)
            {
                i += 2;
                continue;
            }

            if (text[i] == quote)
            {
                if (verbatim && i + 1 < text.Length && text[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            i++;
        }

        return text.Length;
    }
}
