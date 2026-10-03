using Encina.Security.ABAC;
using Encina.Security.ABAC.EEL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// The precompilation failure log carries the error code, never the compiler error message (#1557).
/// </summary>
public sealed class EELExpressionPrecompilationServiceRedactionTests
{
    [Fact]
    public async Task StartAsync_InvalidExpression_LogsTheErrorCodeNotTheCompilerMessage()
    {
        // Arrange
        using var compiler = new EELCompiler();
        var options = new ABACOptions();
        options.ExpressionScanAssemblies.Add(typeof(InvalidConditionRequest).Assembly);
        var logger = new FakeLogger<EELExpressionPrecompilationService>();
        var sut = new EELExpressionPrecompilationService(compiler, Options.Create(options), logger);

        var compileError = (await compiler.CompileAsync(InvalidConditionRequest.Expression))
            .Match(Right: _ => string.Empty, Left: e => e.Message);
        compileError.ShouldNotBeEmpty();

        // Act
        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        // Assert
        var errors = logger.Collector.GetSnapshot().Where(r => r.Level == LogLevel.Error).ToList();
        errors.ShouldNotBeEmpty();
        errors.ShouldAllBe(r => !r.Message.Contains(compileError, StringComparison.Ordinal));
        errors.ShouldContain(r => r.Message.Contains("abac.invalid_condition", StringComparison.Ordinal));
        thrown.Message.ShouldNotContain(compileError);
        thrown.Message.ShouldContain("abac.invalid_condition");
    }

    [RequireCondition(Expression)]
    public sealed class InvalidConditionRequest
    {
        public const string Expression = "this is ((( not a valid expression";
    }
}
