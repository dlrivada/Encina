using System.Reflection;

using Encina.Diagnostics;
using Encina.Testing.Modules;
using Encina.UnitTests.Support;

using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Testing.Modules;

/// <summary>
/// Unit tests for the failure reporting of <see cref="ModuleArchitectureAnalyzer"/>: with a logger the
/// exceptions go through it redacted, without one a diagnostic line is written to the error stream (#1557).
/// </summary>
[Trait("Category", "Unit")]
[Collection("ConsoleError")]
public sealed class ModuleArchitectureAnalyzerLoggingTests
{
    private const string Sentinel = "SENTINEL-LOADER-PATH-6a0";

    private static ReflectionTypeLoadException LoadFailure() =>
        new(
            [typeof(string)],
            [new FileNotFoundException(Sentinel), null],
            Sentinel);

    private static (ModuleArchitectureAnalyzer Sut, FakeLogger<ModuleArchitectureAnalyzer> Logger) WithLogger()
    {
        var logger = new FakeLogger<ModuleArchitectureAnalyzer>();
        return (new ModuleArchitectureAnalyzer(logger, typeof(ModuleArchitectureAnalyzerLoggingTests).Assembly), logger);
    }

    private static ModuleArchitectureAnalyzer WithoutLogger() =>
        new(typeof(ModuleArchitectureAnalyzerLoggingTests).Assembly);

    [Fact]
    public void LogReflectionTypeLoadException_WithLogger_LogsRedactedExceptionsOnly()
    {
        var (sut, logger) = WithLogger();

        sut.LogReflectionTypeLoadException(typeof(string), typeof(string).Assembly, LoadFailure());

        // One record for the load failure plus one per non-null loader exception.
        logger.Collector.Count.ShouldBe(2);
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }

    [Fact]
    public void LogReflectionTypeLoadException_WithoutLogger_WritesLoaderExceptionsToErrorStream()
    {
        var original = Console.Error;
        using var captured = new StringWriter();
        Console.SetError(captured);
        try
        {
            WithoutLogger().LogReflectionTypeLoadException(
                typeof(List<>).GetGenericArguments()[0],
                new UnnamedAssembly(),
                LoadFailure());
        }
        finally
        {
            Console.SetError(original);
        }

        var text = captured.ToString();
        text.ShouldContain("<unknown assembly>");
        text.ShouldContain(nameof(FileNotFoundException));
    }

    [Fact]
    public void LogAnalysisException_WithLogger_LogsRedactedException()
    {
        var (sut, logger) = WithLogger();

        sut.LogAnalysisException(typeof(string), typeof(string).Assembly, new InvalidOperationException(Sentinel).ForLogging());

        logger.Collector.Count.ShouldBe(1);
        RedactedExceptionLogAssert.LoggedOnlyRedacted(logger, Sentinel);
    }

    [Fact]
    public void LogAnalysisException_WithoutLogger_WritesDiagnosticToErrorStream()
    {
        var original = Console.Error;
        using var captured = new StringWriter();
        Console.SetError(captured);
        try
        {
            WithoutLogger().LogAnalysisException(
                typeof(List<>).GetGenericArguments()[0],
                new UnnamedAssembly(),
                new InvalidOperationException("analysis failed"));
        }
        finally
        {
            Console.SetError(original);
        }

        var text = captured.ToString();
        text.ShouldContain("error analyzing type T");
        text.ShouldContain("<unknown assembly>");
        text.ShouldContain(nameof(InvalidOperationException));
    }

    private sealed class UnnamedAssembly : Assembly
    {
        public override AssemblyName GetName() => new();
    }
}
