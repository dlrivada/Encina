using Encina.Security.ABAC.EEL;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.EEL;

/// <summary>
/// Guard clause tests for <see cref="EELCompiler"/>.
/// </summary>
public class EELCompilerGuardTests
{
    #region CompileAsync Guards

    [Fact]
    public async Task CompileAsync_NullExpression_ThrowsArgumentException()
    {
        using var compiler = new EELCompiler();
        var act = () => compiler.CompileAsync(null!).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CompileAsync_EmptyExpression_ThrowsArgumentException()
    {
        using var compiler = new EELCompiler();
        var act = () => compiler.CompileAsync("").AsTask();
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompileAsync_WhitespaceExpression_ThrowsArgumentException()
    {
        using var compiler = new EELCompiler();
        var act = () => compiler.CompileAsync("   ").AsTask();
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompileAsync_ValidExpression_ReturnsRight()
    {
        using var compiler = new EELCompiler();
        var result = await compiler.CompileAsync("true");
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CompileAsync_InvalidExpression_ReturnsLeft()
    {
        using var compiler = new EELCompiler();
        var result = await compiler.CompileAsync("this is not valid C#!!!!");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task CompileAsync_SameExpressionTwice_ReturnsCached()
    {
        using var compiler = new EELCompiler();
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
        using var compiler = new EELCompiler();
        var act = () => compiler.EvaluateAsync(null!, new EELGlobals()).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_EmptyExpression_ThrowsArgumentException()
    {
        using var compiler = new EELCompiler();
        var act = () => compiler.EvaluateAsync("", new EELGlobals()).AsTask();
        await act.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EvaluateAsync_NullGlobals_ThrowsArgumentNullException()
    {
        using var compiler = new EELCompiler();
        var act = () => compiler.EvaluateAsync("true", null!).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_TrueExpression_ReturnsTrue()
    {
        using var compiler = new EELCompiler();
        var globals = new EELGlobals
        {
            user = new System.Dynamic.ExpandoObject(),
            resource = new System.Dynamic.ExpandoObject(),
            environment = new System.Dynamic.ExpandoObject(),
            action = new System.Dynamic.ExpandoObject()
        };
        var result = await compiler.EvaluateAsync("true", globals);
        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => false, Right: v => v).ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_FalseExpression_ReturnsFalse()
    {
        using var compiler = new EELCompiler();
        var globals = new EELGlobals
        {
            user = new System.Dynamic.ExpandoObject(),
            resource = new System.Dynamic.ExpandoObject(),
            environment = new System.Dynamic.ExpandoObject(),
            action = new System.Dynamic.ExpandoObject()
        };
        var result = await compiler.EvaluateAsync("false", globals);
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
