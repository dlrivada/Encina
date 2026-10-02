using System.Text.RegularExpressions;

namespace Encina.UnitTests.Messaging.Health;

/// <summary>
/// Static regression guard for #1301: a health check or a database health monitor must never put an
/// exception message, an <c>EncinaError.Message</c> or an exception object into a result that can reach a
/// health endpoint (the description, the <c>data</c> dictionary or the <c>exception:</c> argument), because
/// the text can carry host names, credentials or personal data. Only the exception type name or the error
/// code may travel; details go to a structured log.
/// </summary>
/// <remarks>
/// This is the health-only sibling of <c>ErrorMessageLeakStaticScanTests</c> (#1259, #1274), which is
/// deliberately narrow (it leaves out <c>ex</c> and scans five packages). Here every <c>*HealthCheck*.cs</c>
/// and <c>*HealthMonitor*.cs</c> file under <c>src/</c> is scanned, and ANY <c>.Message</c> access in such a
/// file is a violation: those files have no legitimate reason to read one. <see cref="Scan"/> is separate
/// from the repository walk so its rules are themselves tested against the leaking shapes found in the
/// code before the fix.
/// </remarks>
public sealed partial class HealthCheckExceptionMessageLeakStaticScanTests
{
    /// <summary>
    /// Any member access named <c>Message</c> (exception, <c>EncinaError</c> or lookalike), also through
    /// <c>?.</c>, plus the other members that print an exception's or an error's text: <c>InnerException</c>,
    /// <c>StackTrace</c>, <c>Error.Reason</c> (a free-text driver message) and <c>.ToString()</c> on an
    /// exception or error identifier (the generated <c>EncinaError.ToString()</c> prints its message).
    /// </summary>
    [GeneratedRegex(
        @"\b\w+\??\.(Message|InnerException|StackTrace)\b|\.Error\.Reason\b|\b(ex|exception|err|error|\w*Error|\w*Exception)\??\.ToString\(")]
    private static partial Regex MessageAccessRegex();

    /// <summary>
    /// An exception identifier interpolated whole into a string: <c>{ex}</c>. Identifiers ending in
    /// <c>Error</c> are not matched here: in health files they hold string error codes (<c>encryptError</c>).
    /// </summary>
    [GeneratedRegex(@"\{(ex|exception|\w*Exception)(:[^}]*)?\}")]
    private static partial Regex InterpolatedErrorRegex();

    /// <summary>The <c>exception:</c> named argument that attaches an exception object to a result.</summary>
    [GeneratedRegex(@"(^|[(,])\s*exception\s*:(?>\s*)(?!null\b)")]
    private static partial Regex ExceptionNamedArgumentRegex();

    /// <summary>
    /// An exception passed positionally to a <c>HealthCheckResult</c>, <c>DatabaseHealthResult</c> or
    /// <c>ShardHealthResult</c> factory (<c>Unhealthy(description, ex, data)</c>).
    /// </summary>
    [GeneratedRegex(
        @"(\.(Unhealthy|Degraded)|new\s+(HealthCheckResult|DatabaseHealthResult|ShardHealthResult))\([^;]*?,\s*(ex|exception|\w*Exception|\w+\.Exception)\s*[,)]",
        RegexOptions.Singleline)]
    private static partial Regex PositionalExceptionRegex();

    [Fact]
    public void HealthChecksAndMonitors_NeverExposeAnExceptionMessageOrObject()
    {
        var violations = Scan(FindRepositoryRoot());

        violations.ShouldBeEmpty(
            "A health result can reach a health endpoint: report only the exception type name " +
            "(ex.GetType().Name) or the error code (error.GetCode().IfNone(\"encina.unknown\")), never " +
            ".Message or the exception object:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void Scan_VisitsTheHealthFilesOfSrc()
    {
        var healthFiles = EnumerateHealthFiles(Path.Combine(FindRepositoryRoot(), "src")).ToList();

        // A guard against the scan silently matching nothing (a renamed folder or file pattern).
        healthFiles.Count.ShouldBeGreaterThan(40);
        healthFiles.ShouldContain(f => f.EndsWith("DatabaseHealthMonitorBase.cs", StringComparison.Ordinal));
        healthFiles.ShouldContain(f => f.EndsWith("MartenAuditHealthCheck.cs", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("return HealthCheckResult.Unhealthy($\"{Name} error: {ex.Message}\");")]
    [InlineData("return HealthCheckResult.Unhealthy($\"{Name} error: {ex.ErrorCode} - {ex.Message}\");")]
    [InlineData("return (false, ex.Message);")]
    [InlineData("data[\"error\"] = ex.Message;")]
    [InlineData("Left: error => HealthCheckResult.Unhealthy($\"failed: {error.Message}\"),")]
    [InlineData("return ShardHealthResult.Unhealthy(shardId, ex.Message, ex);")]
    [InlineData("    $\"Database health check failed for provider '{ProviderName}': {ex.Message}\",")]
    [InlineData("    exception: ex);")]
    [InlineData("data[\"store_error\"] = error.ToString();")]
    [InlineData("return HealthCheckResult.Unhealthy($\"failed: {ex}\");")]
    [InlineData("return HealthCheckResult.Unhealthy($\"failed: {ex.InnerException?.Message}\");")]
    [InlineData("return HealthCheckResult.Unhealthy($\"{Name} error: {ex.Error.Reason}\");")]
    [InlineData("return new HealthCheckResult(HealthStatus.Unhealthy, description, lastException, data);")]
    [InlineData("return AspNetHealthCheckResult.Unhealthy(description, lastException, results);")]
    [InlineData("return HealthCheckResult.Unhealthy(healthResult.Description, healthResult.Exception, healthResult.Data);")]
    public void Scan_FlagsEveryLeakingShapeFoundBeforeTheFix(string source)
    {
        ScanSource("Sample.cs", source).ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("return HealthCheckResult.Unhealthy($\"{Name} error: {ex.GetType().Name}\");")]
    [InlineData("Left: error => HealthCheckResult.Unhealthy(error.GetCode().IfNone(\"encina.unknown\")),")]
    [InlineData("// ex.Message may carry personal data, so only the type travels.")]
    [InlineData("/// <c>exception: ex</c> is not attached either.")]
    [InlineData("$\"Health check failed with exception: {ex.GetType().Name}\");")]
    [InlineData("    exception: null,")]
    public void Scan_AllowsTheSanitizedShapes(string source)
    {
        ScanSource("Sample.cs", source).ShouldBeEmpty();
    }

    private static List<string> Scan(string repositoryRoot)
    {
        var violations = new List<string>();

        foreach (var file in EnumerateHealthFiles(Path.Combine(repositoryRoot, "src")))
        {
            var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
            violations.AddRange(ScanSource(relativePath, File.ReadAllText(file)));
        }

        return violations;
    }

    private static List<string> ScanSource(string relativePath, string source)
    {
        var violations = new List<string>();
        var lines = source.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var codeLines = lines.Select(l => IsComment(l) ? string.Empty : l).ToArray();

        for (var i = 0; i < codeLines.Length; i++)
        {
            if (MessageAccessRegex().IsMatch(codeLines[i])
                || InterpolatedErrorRegex().IsMatch(codeLines[i])
                || ExceptionNamedArgumentRegex().IsMatch(codeLines[i]))
            {
                violations.Add($"{relativePath}:{i + 1}: {lines[i].Trim()}");
            }
        }

        var code = string.Join('\n', codeLines);
        foreach (Match match in PositionalExceptionRegex().Matches(code))
        {
            var line = code.AsSpan(0, match.Index).Count('\n') + 1;
            violations.Add($"{relativePath}:{line}: exception passed positionally to a health result factory");
        }

        return violations;
    }

    private static bool IsComment(string line)
        => line.TrimStart().StartsWith("//", StringComparison.Ordinal);

    private static IEnumerable<string> EnumerateHealthFiles(string srcDirectory)
    {
        var separator = Path.DirectorySeparatorChar;

        return Directory.EnumerateFiles(srcDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{separator}obj{separator}") && !f.Contains($"{separator}bin{separator}"))
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return name.Contains("HealthCheck", StringComparison.Ordinal)
                    || name.Contains("HealthMonitor", StringComparison.Ordinal);
            });
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
