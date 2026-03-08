using System.Dynamic;
using System.Reflection;

using Encina.Security.ABAC.EEL;

using LanguageExt;

namespace Encina.Security.ABAC.Testing;

/// <summary>
/// Test helpers for validating and evaluating EEL (Encina Expression Language)
/// expressions in unit and integration tests.
/// </summary>
/// <remarks>
/// <para>
/// Provides static methods to compile, evaluate, and assert EEL expressions
/// without requiring full DI setup. Uses a shared <see cref="EELCompiler"/>
/// instance for efficient repeated compilations.
/// </para>
/// <para>
/// Use <see cref="ValidateAllExpressionsAsync"/> in a unit test to verify
/// that all <see cref="RequireConditionAttribute"/> expressions in an assembly
/// compile successfully — catching errors at test time rather than production.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Validate all expressions in an assembly
/// [Fact]
/// public async Task AllExpressions_ShouldCompile()
/// {
///     await EELTestHelper.ValidateAllExpressionsAsync(typeof(MyCommand).Assembly);
/// }
///
/// // Evaluate an expression
/// [Fact]
/// public async Task ShouldPermitFinanceDepartment()
/// {
///     var result = await EELTestHelper.EvaluateAsync(
///         "user.department == \"Finance\"",
///         new
///         {
///             user = new { department = "Finance" },
///             resource = new { },
///             environment = new { },
///             action = new { }
///         });
///
///     Assert.True(result.IsRight);
///     result.IfRight(value => Assert.True(value));
/// }
/// </code>
/// </example>
public static class EELTestHelper
{
    private static readonly Lazy<EELCompiler> SharedCompiler = new(
        () => new EELCompiler(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Validates that all EEL expressions declared in the specified assembly compile successfully.
    /// </summary>
    /// <param name="assembly">The assembly to scan for <see cref="RequireConditionAttribute"/> decorations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all expressions have been validated.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more expressions fail to compile, with details of each failure.
    /// </exception>
    /// <example>
    /// <code>
    /// [Fact]
    /// public async Task AllExpressions_ShouldCompile()
    /// {
    ///     await EELTestHelper.ValidateAllExpressionsAsync(typeof(MyCommand).Assembly);
    /// }
    /// </code>
    /// </example>
    public static async Task ValidateAllExpressionsAsync(
        Assembly assembly,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var expressions = EELExpressionDiscovery.Discover([assembly]);

        if (expressions.Count == 0)
        {
            return;
        }

        var failures = new List<(Type RequestType, string Expression, string ErrorMessage)>();

        foreach (var (requestType, expression) in expressions)
        {
            var result = await SharedCompiler.Value
                .CompileAsync(expression, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Left: error => failures.Add((requestType, expression, error.Message)),
                Right: _ => { });
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"EEL validation failed: {failures.Count} expression(s) could not be compiled.\n" +
                string.Join("\n", failures.Select(f =>
                    $"  [{f.RequestType.Name}] \"{f.Expression}\" → {f.ErrorMessage}")));
        }
    }

    /// <summary>
    /// Validates that all EEL expressions declared in the specified assembly compile successfully.
    /// </summary>
    /// <param name="assembly">The assembly to scan for <see cref="RequireConditionAttribute"/> decorations.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more expressions fail to compile, with details of each failure.
    /// </exception>
    /// <remarks>
    /// Synchronous wrapper for <see cref="ValidateAllExpressionsAsync"/>.
    /// Prefer the async version in test frameworks that support it.
    /// </remarks>
    public static void ValidateAllExpressions(Assembly assembly)
    {
        ValidateAllExpressionsAsync(assembly).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Compiles and evaluates an EEL expression against the provided anonymous globals.
    /// </summary>
    /// <param name="expression">The EEL expression to evaluate.</param>
    /// <param name="anonymousGlobals">
    /// An anonymous object with <c>user</c>, <c>resource</c>, <c>environment</c>, and <c>action</c>
    /// properties, each containing an anonymous object with attribute values.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right(result)</c> on success with the boolean evaluation result, or
    /// <c>Left(error)</c> if compilation or evaluation fails.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await EELTestHelper.EvaluateAsync(
    ///     "user.department == \"Finance\" &amp;&amp; resource.amount > 10000",
    ///     new
    ///     {
    ///         user = new { department = "Finance" },
    ///         resource = new { amount = 50000 },
    ///         environment = new { },
    ///         action = new { name = "approve" }
    ///     });
    /// </code>
    /// </example>
    public static async ValueTask<Either<EncinaError, bool>> EvaluateAsync(
        string expression,
        object anonymousGlobals,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(anonymousGlobals);

        var globals = CreateGlobals(anonymousGlobals);
        return await SharedCompiler.Value
            .EvaluateAsync(expression, globals, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that an EEL expression compiles successfully.
    /// </summary>
    /// <param name="expression">The EEL expression to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the expression has been validated.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression fails to compile.</exception>
    public static async Task AssertCompilesAsync(
        string expression,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        var result = await SharedCompiler.Value
            .CompileAsync(expression, cancellationToken)
            .ConfigureAwait(false);

        result.Match(
            Left: error => throw new InvalidOperationException(
                $"Expected expression to compile, but it failed: \"{expression}\" → {error.Message}"),
            Right: _ => { });
    }

    /// <summary>
    /// Asserts that an EEL expression fails to compile.
    /// </summary>
    /// <param name="expression">The EEL expression that should not compile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the expression has been validated.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression compiles successfully.</exception>
    public static async Task AssertDoesNotCompileAsync(
        string expression,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        var result = await SharedCompiler.Value
            .CompileAsync(expression, cancellationToken)
            .ConfigureAwait(false);

        result.Match(
            Left: _ => { },
            Right: _ => throw new InvalidOperationException(
                $"Expected expression to fail compilation, but it succeeded: \"{expression}\""));
    }

    /// <summary>
    /// Asserts that an EEL expression compiles successfully.
    /// </summary>
    /// <param name="expression">The EEL expression to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when the expression fails to compile.</exception>
    /// <remarks>
    /// Synchronous wrapper for <see cref="AssertCompilesAsync"/>.
    /// Prefer the async version in test frameworks that support it.
    /// </remarks>
    public static void AssertCompiles(string expression)
    {
        AssertCompilesAsync(expression).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asserts that an EEL expression fails to compile.
    /// </summary>
    /// <param name="expression">The EEL expression that should not compile.</param>
    /// <exception cref="InvalidOperationException">Thrown when the expression compiles successfully.</exception>
    /// <remarks>
    /// Synchronous wrapper for <see cref="AssertDoesNotCompileAsync"/>.
    /// Prefer the async version in test frameworks that support it.
    /// </remarks>
    public static void AssertDoesNotCompile(string expression)
    {
        AssertDoesNotCompileAsync(expression).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates <see cref="EELGlobals"/> from an anonymous object containing
    /// <c>user</c>, <c>resource</c>, <c>environment</c>, and <c>action</c> properties.
    /// </summary>
    private static EELGlobals CreateGlobals(object anonymousGlobals)
    {
        var type = anonymousGlobals.GetType();

        return new EELGlobals
        {
            user = ToExpandoObject(type.GetProperty("user")?.GetValue(anonymousGlobals)),
            resource = ToExpandoObject(type.GetProperty("resource")?.GetValue(anonymousGlobals)),
            environment = ToExpandoObject(type.GetProperty("environment")?.GetValue(anonymousGlobals)),
            action = ToExpandoObject(type.GetProperty("action")?.GetValue(anonymousGlobals))
        };
    }

    /// <summary>
    /// Converts an object (typically anonymous) to an <see cref="ExpandoObject"/>
    /// by copying all public properties.
    /// </summary>
    private static ExpandoObject ToExpandoObject(object? source)
    {
        var expando = new ExpandoObject();

        if (source is null)
        {
            return expando;
        }

        if (source is ExpandoObject existing)
        {
            return existing;
        }

        var dict = (IDictionary<string, object?>)expando;

        foreach (var prop in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            dict[prop.Name] = prop.GetValue(source);
        }

        return expando;
    }
}
