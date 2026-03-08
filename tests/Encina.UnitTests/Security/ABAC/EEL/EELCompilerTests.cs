using System.Dynamic;

using Encina.Security.ABAC;
using Encina.Security.ABAC.EEL;

using FluentAssertions;

using LanguageExt;

namespace Encina.UnitTests.Security.ABAC.EEL;

/// <summary>
/// Unit tests for <see cref="EELCompiler"/>: compilation, caching, evaluation,
/// and error handling of Encina Expression Language expressions.
/// </summary>
public sealed class EELCompilerTests : IDisposable
{
    private readonly EELCompiler _compiler = new();

    public void Dispose() => _compiler.Dispose();

    private static EELGlobals MakeGlobals(
        Action<IDictionary<string, object?>>? configureUser = null,
        Action<IDictionary<string, object?>>? configureResource = null,
        Action<IDictionary<string, object?>>? configureEnvironment = null,
        Action<IDictionary<string, object?>>? configureAction = null)
    {
        var user = new ExpandoObject();
        var resource = new ExpandoObject();
        var environment = new ExpandoObject();
        var action = new ExpandoObject();

        configureUser?.Invoke((IDictionary<string, object?>)user);
        configureResource?.Invoke((IDictionary<string, object?>)resource);
        configureEnvironment?.Invoke((IDictionary<string, object?>)environment);
        configureAction?.Invoke((IDictionary<string, object?>)action);

        return new EELGlobals
        {
            user = user,
            resource = resource,
            environment = environment,
            action = action
        };
    }

    /// <summary>
    /// Extracts the Right value from an Either, asserting it is Right.
    /// </summary>
    private static T AssertRight<T>(Either<EncinaError, T> either, string context = "")
    {
        either.IsRight.Should().BeTrue($"expected Right but got Left{(context.Length > 0 ? $": {context}" : "")}");
        return either.Match(Left: _ => default!, Right: v => v);
    }

    /// <summary>
    /// Extracts the Left value from an Either, asserting it is Left.
    /// </summary>
    private static EncinaError AssertLeft<T>(Either<EncinaError, T> either, string context = "")
    {
        either.IsLeft.Should().BeTrue($"expected Left but got Right{(context.Length > 0 ? $": {context}" : "")}");
        return either.Match(Left: e => e, Right: _ => default);
    }

    #region CompileAsync

    [Fact]
    public async Task CompileAsync_ValidExpression_ReturnsRight()
    {
        var result = await _compiler.CompileAsync("true");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task CompileAsync_InvalidExpression_ReturnsLeft()
    {
        var result = await _compiler.CompileAsync("invalid >>>syntax");

        var error = AssertLeft(result);
        error.GetCode().IfNone("").Should().Be(ABACErrors.InvalidConditionCode);
    }

    [Fact]
    public async Task CompileAsync_CachesCompiledExpressions()
    {
        var result1 = await _compiler.CompileAsync("1 == 1");
        var result2 = await _compiler.CompileAsync("1 == 1");

        // Both should succeed
        result1.IsRight.Should().BeTrue();
        result2.IsRight.Should().BeTrue();

        // Should return the same cached instance
        var runner1 = result1.Match(Left: _ => null!, Right: r => r);
        var runner2 = result2.Match(Left: _ => null!, Right: r => r);
        runner1.Should().BeSameAs(runner2);
    }

    [Fact]
    public async Task CompileAsync_NullExpression_ThrowsArgumentException()
    {
        var act = () => _compiler.CompileAsync(null!).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompileAsync_EmptyExpression_ThrowsArgumentException()
    {
        var act = () => _compiler.CompileAsync("").AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompileAsync_WhitespaceExpression_ThrowsArgumentException()
    {
        var act = () => _compiler.CompileAsync("   ").AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CompileAsync_NonBoolExpression_ReturnsLeft()
    {
        // A string expression cannot be implicitly converted to bool
        var result = await _compiler.CompileAsync("\"hello\"");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region EvaluateAsync

    [Fact]
    public async Task EvaluateAsync_TrueLiteral_ReturnsTrue()
    {
        var globals = MakeGlobals();

        var result = await _compiler.EvaluateAsync("true", globals);

        AssertRight(result).Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_FalseLiteral_ReturnsFalse()
    {
        var globals = MakeGlobals();

        var result = await _compiler.EvaluateAsync("false", globals);

        AssertRight(result).Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_StringComparison_Works()
    {
        var globals = MakeGlobals(
            configureUser: u => u["department"] = "Finance");

        var result = await _compiler.EvaluateAsync("user.department == \"Finance\"", globals);

        AssertRight(result).Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_IntegerComparison_Works()
    {
        var globals = MakeGlobals(
            configureResource: r => r["amount"] = 50000);

        var result = await _compiler.EvaluateAsync("resource.amount > 10000", globals);

        AssertRight(result).Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_BooleanProperty_Works()
    {
        var globals = MakeGlobals(
            configureEnvironment: e => e["isBusinessHours"] = true);

        var result = await _compiler.EvaluateAsync("environment.isBusinessHours == true", globals);

        AssertRight(result).Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_CrossCategoryExpression_Works()
    {
        var globals = MakeGlobals(
            configureUser: u => u["department"] = "Finance",
            configureAction: a => a["name"] = "read");

        var result = await _compiler.EvaluateAsync(
            "user.department == \"Finance\" && action.name == \"read\"",
            globals);

        AssertRight(result).Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_CompilationError_ReturnsLeft()
    {
        var globals = MakeGlobals();

        var result = await _compiler.EvaluateAsync("invalid >>> syntax", globals);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_RuntimeError_ReturnsLeft()
    {
        var globals = MakeGlobals();

        // Accessing a property that doesn't exist on the dynamic object
        var result = await _compiler.EvaluateAsync("user.nonExistentProperty == \"test\"", globals);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NullExpression_ThrowsArgumentException()
    {
        var globals = MakeGlobals();
        var act = () => _compiler.EvaluateAsync(null!, globals).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EvaluateAsync_NullGlobals_ThrowsArgumentNullException()
    {
        var act = () => _compiler.EvaluateAsync("true", null!).AsTask();

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_LogicalOr_Works()
    {
        var globals = MakeGlobals(
            configureUser: u => u["role"] = "viewer");

        var result = await _compiler.EvaluateAsync(
            "user.role == \"admin\" || user.role == \"viewer\"",
            globals);

        AssertRight(result).Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_Negation_Works()
    {
        var globals = MakeGlobals(
            configureUser: u => u["isBlocked"] = false);

        var result = await _compiler.EvaluateAsync("!((bool)user.isBlocked)", globals);

        AssertRight(result).Should().BeTrue();
    }

    #endregion

    #region Caching Behavior

    [Fact]
    public async Task EvaluateAsync_SameExpressionDifferentGlobals_UsesCachedCompilation()
    {
        var globals1 = MakeGlobals(configureUser: u => u["department"] = "Finance");
        var globals2 = MakeGlobals(configureUser: u => u["department"] = "Engineering");

        var result1 = await _compiler.EvaluateAsync("user.department == \"Finance\"", globals1);
        var result2 = await _compiler.EvaluateAsync("user.department == \"Finance\"", globals2);

        AssertRight(result1).Should().BeTrue();
        AssertRight(result2).Should().BeFalse();
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task CompileAsync_ConcurrentCalls_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _compiler.CompileAsync($"{i} < 100").AsTask())
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r.IsRight.Should().BeTrue());
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var compiler = new EELCompiler();

        var act = () =>
        {
            compiler.Dispose();
            compiler.Dispose();
        };

        act.Should().NotThrow();
    }

    #endregion
}
