using System.Text.RegularExpressions;

namespace Encina.UnitTests.Messaging.Encryption;

/// <summary>
/// Static regression guard for the #1259 / #1274 review: <c>EncinaError.Message</c>
/// (and any identifier conventionally holding an <c>EncinaError</c>) must never reach a logger call, an
/// activity/tag call or a health-check result, because the message can carry personal data such as a
/// data-subject id. <see cref="ErrorMessageLeakTests"/> covers the behavioural side (what actually ends up
/// in logs/results); this test scans the source of the packages touched by that review so a new leak
/// cannot slip back in without either being fixed or explicitly allowlisted here.
/// </summary>
public sealed partial class ErrorMessageLeakStaticScanTests
{
    /// <summary>
    /// Identifiers that conventionally hold an <c>EncinaError</c> in this codebase. Deliberately
    /// conservative: a generic name like "e" or "ex" is not included because it usually holds an
    /// <see cref="Exception"/>, not an <c>EncinaError</c>, and would produce false positives.
    /// </summary>
    [GeneratedRegex(
        @"\b(error|err|storeError|left|failure|encinaError)\.Message\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex ErrorMessageAccessRegex();

    [GeneratedRegex(@"\bLog\.|\bLogError\b|\bLogWarning\b|_logger\.", RegexOptions.IgnoreCase)]
    private static partial Regex LoggerCallRegex();

    [GeneratedRegex(@"\bSetTag\b|\bSetStatus\b|\bAddTag\b|\bActivitySource\b", RegexOptions.IgnoreCase)]
    private static partial Regex ActivityOrTagCallRegex();

    [GeneratedRegex(@"HealthCheckResult\.|\bdata\[", RegexOptions.IgnoreCase)]
    private static partial Regex HealthCheckResultRegex();

    /// <summary>
    /// Matches a <c>Left: error =&gt; error.Message</c>-shaped projection (the error-like identifier bound
    /// by the lambda parameter and immediately projected to its own <c>.Message</c>). This shape is a sink
    /// on its own: whatever the caller does with the projected value (store it in a result field, log it,
    /// pass it to a health check) happens on a later line the per-line scan below cannot see.
    /// </summary>
    [GeneratedRegex(@"\bLeft\s*:\s*(\w+)\s*=>\s*\1\.Message\b", RegexOptions.IgnoreCase)]
    private static partial Regex LeftProjectsMessageRegex();

    /// <summary>
    /// Legitimate uses of "&lt;error-like identifier&gt;.Message" next to a logger/tag/health-check call,
    /// each with a one-line reason. Keyed by the path relative to the repository root (forward slashes)
    /// and the exact source line (trimmed), so a change to the surrounding code re-triggers review.
    /// </summary>
    private static readonly IReadOnlyList<(string RelativePath, string TrimmedLine, string Reason)> Allowlist =
    [
        // No entries yet: every current match under the four scanned packages is inside a `//`/`///`
        // comment (doc examples, explanatory notes) and is already excluded by the comment skip below.
        // Add a (path, line, reason) tuple here - never a real leak - if a future change needs one.
    ];

    private static readonly string[] ScannedPackages =
    [
        "src/Encina.Messaging",
        "src/Encina.Hangfire",
        "src/Encina.Quartz",
        "src/Encina.Cdc"
    ];

    [Fact]
    public void ScannedPackages_NeverLogOrExposeTheRawErrorMessage()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var package in ScannedPackages)
        {
            var packageDir = Path.Combine(repositoryRoot, package.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(packageDir))
            {
                throw new InvalidOperationException($"Expected package directory not found: {packageDir}");
            }

            foreach (var file in Directory.EnumerateFiles(packageDir, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                var lines = File.ReadAllLines(file);

                for (var i = 0; i < lines.Length; i++)
                {
                    var trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("//", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var projectsMessage = LeftProjectsMessageRegex().IsMatch(trimmed);
                    if (!projectsMessage && !ErrorMessageAccessRegex().IsMatch(trimmed))
                    {
                        continue;
                    }

                    var isSink = projectsMessage
                        || LoggerCallRegex().IsMatch(trimmed)
                        || ActivityOrTagCallRegex().IsMatch(trimmed)
                        || HealthCheckResultRegex().IsMatch(trimmed);

                    if (!isSink)
                    {
                        continue;
                    }

                    if (Allowlist.Any(a => a.RelativePath == relativePath && a.TrimmedLine == trimmed))
                    {
                        continue;
                    }

                    violations.Add($"{relativePath}:{i + 1}: {trimmed}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "EncinaError.Message can carry personal data and must not reach a logger, activity/tag or " +
            "health-check result. Fix the leak, or add an allowlist entry with a reason if this is a " +
            "verified true negative:\n" + string.Join('\n', violations));
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
