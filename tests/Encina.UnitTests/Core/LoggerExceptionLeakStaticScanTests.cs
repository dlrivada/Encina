using System.Text;
using System.Text.RegularExpressions;

namespace Encina.UnitTests.Core;

/// <summary>
/// Static regression guard for AGENTS.md section 3: no logger call in <c>src/</c> receives a raw exception, an
/// exception or <c>EncinaError</c> message, or an exception rendered to text. Exceptions reach loggers through
/// <c>ex.ForLogging()</c> (type and stack trace, never the message); errors are logged by code or exception type.
/// The scan works per statement (multi-line calls included) over every <c>.cs</c> file under <c>src/</c>.
/// </summary>
/// <remarks>
/// Known limits: it recognises an exception by its conventional name (<c>ex</c>, <c>e</c>, <c>exception</c>,
/// <c>*Exception</c>, <c>*Ex</c>, <c>.Exception</c>), and it does not follow a value through a local variable
/// or a helper method (a helper that receives an exception must redact it itself, see the allowlist).
/// </remarks>
public sealed partial class LoggerExceptionLeakStaticScanTests
{
    /// <summary>
    /// Statements that are verified true negatives or are fixed in another change; each entry is the path
    /// relative to the repository root, the start of the trimmed statement and the reason.
    /// </summary>
    private static readonly IReadOnlyList<(string RelativePath, string StatementPrefix, string Reason)> Allowlist =
    [
        ("src/Encina.Security.Secrets.AwsSecretsManager/AwsSecretsManagerProvider.cs", "return LogAndReturn", "private helper that logs through ForLogging() itself"),
        ("src/Encina.Security.Secrets.AzureKeyVault/AzureKeyVaultSecretProvider.cs", "404 => LogAndReturnNotFound", "private helpers that log through ForLogging() themselves"),
        ("src/Encina.Security.Secrets.GoogleCloudSecretManager/GoogleCloudSecretManagerProvider.cs", "return LogAndReturn", "private helper that logs through ForLogging() itself"),
        ("src/Encina.Security.Secrets.HashiCorpVault/HashiCorpVaultSecretProvider.cs", "return LogAndReturn", "private helper that logs through ForLogging() itself"),
        ("src/Encina.Testing.Respawn/RespawnerFactory.cs", "LogParseFailure(", "opt-in verbose test diagnostic written to Debug output and a callback, not an ILogger"),
    ];

    [Fact]
    public void Src_NeverPassesARawExceptionOrAMessageToALogger()
    {
        var root = FindRepositoryRoot();
        var violations = new List<string>();
        var usedAllowlist = new System.Collections.Generic.HashSet<int>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relativePath.Contains("/obj/", StringComparison.Ordinal) || relativePath.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var finding in LoggerLeakScanner.Analyze(File.ReadAllText(file)))
            {
                var allowed = FindAllowlistEntry(relativePath, finding.Statement);
                if (allowed >= 0)
                {
                    usedAllowlist.Add(allowed);
                    continue;
                }

                violations.Add($"{relativePath}:{finding.Line}: [{finding.Rule}] {Truncate(finding.Statement)}");
            }
        }

        violations.ShouldBeEmpty(
            "A logger call must receive exceptions as ex.ForLogging() and errors as a code or exception type, never a message "
            + "(AGENTS.md section 3). Fix the call:\n" + string.Join('\n', violations));

        for (var i = 0; i < Allowlist.Count; i++)
        {
            usedAllowlist.ShouldContain(i, $"Stale allowlist entry (nothing matches it any more): {Allowlist[i].RelativePath} / {Allowlist[i].StatementPrefix}");
        }
    }

    [Theory]
    [InlineData("_logger.LogError(ex, \"failed\");", "raw-exception")]
    [InlineData("_logger.LogWarning(exception: ex, \"failed\");", "raw-exception")]
    [InlineData("Log.StepFailed(_logger, id, ex.GetType().Name, ex);", "raw-exception")]
    [InlineData("Log.ConnectionFailed(_logger, eventData.Exception);", "raw-exception")]
    [InlineData("LogCacheError(_logger, key, innerException);", "raw-exception")]
    [InlineData("_logger.LogWarning(\"failed: {Error}\", error.Message);", "message")]
    [InlineData("_logger.LogWarning($\"failed: {ex.Message}\");", "message")]
    [InlineData("Left: error => Log.Failed(_logger, id,\n    error.Message)", "message")]
    [InlineData("_logger.LogError(\"failed {Details}\", ex.ToString());", "exception-to-string")]
    [InlineData("_logger.LogError($\"failed {ex}\");", "exception-interpolated")]
    [InlineData("LogApiException(requestType, apiClient, apiEx.Message, string.Empty);", "message")]
    public void Scanner_FlagsLeakingLoggerCalls(string statement, string expectedRule)
    {
        var findings = LoggerLeakScanner.Analyze($"class C {{ void M() {{ {statement}\n}} }}");

        findings.ShouldContain(f => f.Rule == expectedRule);
    }

    [Theory]
    [InlineData("_logger.LogError(ex.ForLogging(), \"failed\");")]
    [InlineData("Log.StepFailed(_logger, id, ex.GetType().Name, ex.ForLogging());")]
    [InlineData("_logger.LogWarning(\"failed: {ErrorCode}\", error.GetCode().IfNone(\"encina.unknown\"));")]
    [InlineData("var message = ex.Message;")]
    [InlineData("return EncinaErrors.Create(\"code\", ex.Message);")]
    [InlineData("LogCacheError(_logger, key, ex.ForLogging());")]
    [InlineData("// _logger.LogError(ex, \"commented out\");")]
    [InlineData("var text = \"_logger.LogError(ex, x)\";")]
    [InlineData("private static partial void LogApiException(string requestType, Exception exception, string correlationId);")]
    [InlineData("internal static void Failed(this ILogger logger, Exception? exception = null) => Def(logger, exception)")]
    public void Scanner_AcceptsRedactedAndNonLoggerStatements(string statement)
    {
        var findings = LoggerLeakScanner.Analyze($"class C {{ void M() {{ {statement}\n}} }}");

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void Scanner_ReportsTheLineOfAMultiLineCall()
    {
        const string Source = "class C\n{\n    void M()\n    {\n        _logger.LogWarning(\n            \"failed {Id}\",\n            id,\n            ex);\n    }\n}\n";

        var finding = LoggerLeakScanner.Analyze(Source).ShouldHaveSingleItem();

        finding.Line.ShouldBe(5);
        finding.Rule.ShouldBe("raw-exception");
    }

    private static int FindAllowlistEntry(string relativePath, string statement)
    {
        var trimmed = Regex.Replace(statement, @"\s+", " ").Trim();
        for (var i = 0; i < Allowlist.Count; i++)
        {
            if (Allowlist[i].RelativePath == relativePath && trimmed.StartsWith(Allowlist[i].StatementPrefix, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static string Truncate(string statement)
    {
        var collapsed = Regex.Replace(statement, @"\s+", " ").Trim();
        return collapsed.Length <= 180 ? collapsed : collapsed[..180];
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
/// Finds logger calls that receive a raw exception, a message or an exception rendered to text.
/// </summary>
internal static partial class LoggerLeakScanner
{
    internal readonly record struct Finding(int Line, string Rule, string Statement);

    // A call on a logger variable: _logger.LogX(...), logger?.LogX(...), _logger.ExtensionMethod(...)
    [GeneratedRegex(@"\b_?[lL]ogger\s*[?!]?\.\s*\w+\s*\(")]
    private static partial Regex CallOnLoggerRegex();

    // A call that receives the logger as its first argument: Log.X(_logger, ...), _definedAction(_logger, ...)
    [GeneratedRegex(@"\b\w+\s*\(\s*(?:this\s+)?_?[lL]ogger!?\s*[,)]")]
    private static partial Regex CallWithLoggerArgumentRegex();

    // An unqualified call to a LoggerMessage partial method or helper: LogCacheError(...)
    [GeneratedRegex(@"(?<![\w.])Log[A-Z]\w*\s*\(")]
    private static partial Regex UnqualifiedLogMethodRegex();

    [GeneratedRegex(@"^\s*(?:\[[^\]]*\]\s*)*(?:public|internal|private|protected)\b")]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"\.Message\b")]
    private static partial Regex MessageAccessRegex();

    [GeneratedRegex(@"^(?:\w+\s*:\s*)?(?:\w+\??\.)*(?:ex|e|exception|innerException|\w+Exception|\w+Ex|Exception)$")]
    private static partial Regex BareExceptionRegex();

    [GeneratedRegex(@"\b(?:ex|e|exception|\w+Exception|\w+Ex)\s*\??\.\s*ToString\s*\(")]
    private static partial Regex ExceptionToStringRegex();

    [GeneratedRegex(@"\{(?:ex|e|exception|\w+Exception|\w+Ex)\}")]
    private static partial Regex ExceptionInterpolationRegex();

    internal static IReadOnlyList<Finding> Analyze(string source)
    {
        var findings = new List<Finding>();

        foreach (var (statement, line) in Statements(source))
        {
            // A method declaration only forwards its own parameters to the logger; the rule applies to the calls of that method.
            if (DeclarationRegex().IsMatch(statement))
            {
                continue;
            }

            var rule = Evaluate(statement);
            if (rule is not null)
            {
                findings.Add(new Finding(line, rule, statement));
            }
        }

        return findings;
    }

    private static string? Evaluate(string statement)
    {
        // Call sites are located on a copy with the literal contents blanked, so text inside a string is never a call.
        var masked = MaskLiterals(statement);
        var openParens = new SortedSet<int>();
        foreach (var regex in new[] { CallOnLoggerRegex(), CallWithLoggerArgumentRegex(), UnqualifiedLogMethodRegex() })
        {
            foreach (Match match in regex.Matches(masked))
            {
                openParens.Add(masked.IndexOf('(', match.Index));
            }
        }

        foreach (var open in openParens)
        {
            var rule = EvaluateArguments(SplitArguments(statement, open + 1));
            if (rule is not null)
            {
                return rule;
            }
        }

        return null;
    }

    private static string MaskLiterals(string statement)
    {
        var masked = new StringBuilder(statement);
        for (var i = 0; i < statement.Length; i++)
        {
            if (statement[i] is '"' or '\'')
            {
                var end = SkipLiteral(statement, i);
                for (var j = i + 1; j < end - 1 && j < statement.Length; j++)
                {
                    masked[j] = ' ';
                }

                i = end - 1;
            }
        }

        return masked.ToString();
    }

    private static string? EvaluateArguments(IEnumerable<string> arguments)
    {
        foreach (var raw in arguments)
        {
            var argument = raw.Trim();

            if (MessageAccessRegex().IsMatch(argument))
            {
                return "message";
            }

            if (BareExceptionRegex().IsMatch(argument))
            {
                return "raw-exception";
            }

            if (ExceptionToStringRegex().IsMatch(argument))
            {
                return "exception-to-string";
            }

            if (ExceptionInterpolationRegex().IsMatch(argument))
            {
                return "exception-interpolated";
            }
        }

        return null;
    }

    /// <summary>Splits the arguments of a call at depth zero, starting right after its opening parenthesis.</summary>
    private static IEnumerable<string> SplitArguments(string text, int start)
    {
        var depth = 0;
        var current = new StringBuilder();

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (c is '"' or '\'')
            {
                var end = SkipLiteral(text, i);
                current.Append(text, i, end - i);
                i = end - 1;
                continue;
            }

            if (c is '(' or '[')
            {
                depth++;
            }
            else if (c is ')' or ']')
            {
                if (depth == 0)
                {
                    yield return current.ToString();
                    yield break;
                }

                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                yield return current.ToString();
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        yield return current.ToString();
    }

    /// <summary>Returns the index just after the string or character literal that starts at <paramref name="start"/>.</summary>
    private static int SkipLiteral(string text, int start)
    {
        var quote = text[start];
        var verbatim = quote == '"' && start > 0 && (text[start - 1] == '@' || (start > 1 && text[start - 1] == '$' && text[start - 2] == '@'));

        if (quote == '"' && text.AsSpan(start).StartsWith("\"\"\"", StringComparison.Ordinal))
        {
            var close = text.IndexOf("\"\"\"", start + 3, StringComparison.Ordinal);
            return close < 0 ? text.Length : close + 3;
        }

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

    /// <summary>
    /// Splits C# source into statements at <c>;</c>, <c>{</c> and <c>}</c> outside string literals, dropping comments,
    /// and reports the line on which each statement starts.
    /// </summary>
    private static IEnumerable<(string Statement, int Line)> Statements(string text)
    {
        var current = new StringBuilder();
        var line = 1;
        var startLine = 1;
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];

            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/'))
                {
                    if (text[i] == '\n')
                    {
                        line++;
                    }

                    i++;
                }

                i += 2;
                continue;
            }

            if (c is '"' or '\'')
            {
                var end = SkipLiteral(text, i);
                if (current.Length == 0)
                {
                    startLine = line;
                }

                current.Append(text, i, end - i);
                line += text.AsSpan(i, end - i).Count('\n');
                i = end;
                continue;
            }

            if (c is ';' or '{' or '}')
            {
                if (current.ToString().Trim().Length > 0)
                {
                    yield return (current.ToString(), startLine);
                }

                current.Clear();
                i++;
                continue;
            }

            if (c == '\n')
            {
                line++;
            }

            if (current.Length == 0 && char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (current.Length == 0)
            {
                startLine = line;
            }

            current.Append(c);
            i++;
        }
    }
}
