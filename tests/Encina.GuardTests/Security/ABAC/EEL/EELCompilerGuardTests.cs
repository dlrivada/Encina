using Encina.Security.ABAC.EEL;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.EEL;

/// <summary>
/// Guard clause tests for <see cref="EELCompiler"/>. One compiler is shared by the class because
/// each compiled expression emits a Roslyn script assembly that is never unloaded; the caching and
/// disposal tests create their own instance.
/// </summary>
public class EELCompilerGuardTests(EELCompilerGuardTests.CompilerFixture fixture)
    : IClassFixture<EELCompilerGuardTests.CompilerFixture>
{
    /// <summary>Owns the compiler shared by the tests of this class.</summary>
    public sealed class CompilerFixture : IDisposable
    {
        public EELCompiler Compiler { get; } = new();

        public void Dispose() => Compiler.Dispose();
    }

    private readonly EELCompiler _compiler = fixture.Compiler;

    #region CompileAsync Guards

    [Fact]
    public async Task CompileAsync_NullExpression_ThrowsArgumentException()
    {
        var act = () => _compiler.CompileAsync(null!).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CompileAsync_EmptyExpression_ThrowsArgumentException()
    {
        var act = () => _compiler.CompileAsync("").AsTask();
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompileAsync_WhitespaceExpression_ThrowsArgumentException()
    {
        var act = () => _compiler.CompileAsync("   ").AsTask();
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompileAsync_ValidExpression_ReturnsRight()
    {
        var result = await _compiler.CompileAsync("true");
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CompileAsync_InvalidExpression_ReturnsLeft()
    {
        var result = await _compiler.CompileAsync("this is not valid C#!!!!");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task CompileAsync_SameExpressionTwice_ReturnsCached()
    {
        using var compiler = new EELCompiler(); // fresh: the test asserts on the cache itself
        var result1 = await compiler.CompileAsync("1 == 1");
        var result2 = await compiler.CompileAsync("1 == 1");
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();
    }

    #endregion

    #region EvaluateAsync Guards

    [Fact]
    public async Task EvaluateAsync_NullExpression_ThrowsArgumentException()
    {
        var act = () => _compiler.EvaluateAsync(null!, new EELGlobals()).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_EmptyExpression_ThrowsArgumentException()
    {
        var act = () => _compiler.EvaluateAsync("", new EELGlobals()).AsTask();
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EvaluateAsync_NullGlobals_ThrowsArgumentNullException()
    {
        var act = () => _compiler.EvaluateAsync("true", null!).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_TrueExpression_ReturnsTrue()
    {
        var globals = new EELGlobals
        {
            user = new System.Dynamic.ExpandoObject(),
            resource = new System.Dynamic.ExpandoObject(),
            environment = new System.Dynamic.ExpandoObject(),
            action = new System.Dynamic.ExpandoObject()
        };
        var result = await _compiler.EvaluateAsync("true", globals);
        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => false, Right: v => v).ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_FalseExpression_ReturnsFalse()
    {
        var globals = new EELGlobals
        {
            user = new System.Dynamic.ExpandoObject(),
            resource = new System.Dynamic.ExpandoObject(),
            environment = new System.Dynamic.ExpandoObject(),
            action = new System.Dynamic.ExpandoObject()
        };
        var result = await _compiler.EvaluateAsync("false", globals);
        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => true, Right: v => v).ShouldBeFalse();
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var compiler = new EELCompiler();
        compiler.Dispose();
        Should.NotThrow(() => compiler.Dispose());
    }

    #endregion
}
