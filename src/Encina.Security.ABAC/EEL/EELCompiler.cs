using System.Collections.Concurrent;
using System.Dynamic;
using System.Globalization;

using LanguageExt;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Encina.Security.ABAC.EEL;

/// <summary>
/// Compiles and caches EEL (Encina Expression Language) expressions using Roslyn's
/// C# scripting API. Each expression is compiled to a <see cref="ScriptRunner{T}"/>
/// and cached for subsequent evaluations.
/// </summary>
/// <remarks>
/// <para>
/// EEL expressions are standard C# boolean expressions that can reference the four
/// XACML attribute categories (<c>user</c>, <c>resource</c>, <c>environment</c>, <c>action</c>)
/// as dynamic objects. For example:
/// <c>user.department == "Finance" &amp;&amp; resource.amount > 10000</c>
/// </para>
/// <para>
/// Thread safety: compilation is protected by a <see cref="SemaphoreSlim"/> to prevent
/// duplicate compilation, and the cache uses <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// for thread-safe reads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var compiler = new EELCompiler();
///
/// var globals = new EELGlobals
/// {
///     user = new ExpandoObject(),
///     resource = new ExpandoObject(),
///     environment = new ExpandoObject(),
///     action = new ExpandoObject()
/// };
/// ((IDictionary&lt;string, object?&gt;)globals.user)["department"] = "Finance";
///
/// var result = await compiler.EvaluateAsync(
///     "user.department == \"Finance\"",
///     globals);
///
/// result.Match(
///     error => Console.WriteLine($"Error: {error.Message}"),
///     value => Console.WriteLine($"Result: {value}")); // true
/// </code>
/// </example>
public sealed class EELCompiler : IDisposable
{
    private readonly ConcurrentDictionary<string, ScriptRunner<bool>> _cache = new();
    private readonly SemaphoreSlim _compilationLock = new(1, 1);
    private readonly ScriptOptions _scriptOptions;

    /// <summary>
    /// Initializes a new <see cref="EELCompiler"/> with default script options.
    /// </summary>
    public EELCompiler()
    {
        _scriptOptions = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(ExpandoObject).Assembly,
                typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly)
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic");
    }

    /// <summary>
    /// Compiles an EEL expression and returns a cached <see cref="ScriptRunner{T}"/>.
    /// </summary>
    /// <param name="expression">The C# boolean expression to compile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right(runner)</c> on success with a compiled script runner, or
    /// <c>Left(error)</c> if compilation fails with diagnostic information.
    /// </returns>
    public async ValueTask<Either<EncinaError, ScriptRunner<bool>>> CompileAsync(
        string expression,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        // Fast path: cache hit
        if (_cache.TryGetValue(expression, out var cachedRunner))
        {
            return cachedRunner;
        }

        await _compilationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(expression, out cachedRunner))
            {
                return cachedRunner;
            }

            var script = CSharpScript.Create<bool>(
                expression,
                _scriptOptions,
                typeof(EELGlobals));

            var diagnostics = script.Compile(cancellationToken);

            var errors = diagnostics
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .ToList();

            if (errors.Count > 0)
            {
                var errorMessages = string.Join("; ", errors.Select(e =>
                {
                    var location = e.Location.GetLineSpan();
                    return $"({location.StartLinePosition.Line},{location.StartLinePosition.Character}): {e.GetMessage(CultureInfo.InvariantCulture)}";
                }));

                return ABACErrors.InvalidCondition(expression, errorMessages);
            }

            var runner = script.CreateDelegate(cancellationToken);
            _cache.TryAdd(expression, runner);
            return runner;
        }
        finally
        {
            _compilationLock.Release();
        }
    }

    /// <summary>
    /// Compiles (if needed) and evaluates an EEL expression against the provided globals.
    /// </summary>
    /// <param name="expression">The C# boolean expression to evaluate.</param>
    /// <param name="globals">The <see cref="EELGlobals"/> providing attribute category values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right(result)</c> on success with the boolean evaluation result, or
    /// <c>Left(error)</c> if compilation or evaluation fails.
    /// </returns>
    public async ValueTask<Either<EncinaError, bool>> EvaluateAsync(
        string expression,
        EELGlobals globals,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(globals);

        var compileResult = await CompileAsync(expression, cancellationToken).ConfigureAwait(false);

        return await compileResult.Match<ValueTask<Either<EncinaError, bool>>>(
            Left: error => new ValueTask<Either<EncinaError, bool>>(error),
            Right: async runner =>
            {
                try
                {
                    var result = await runner.Invoke(globals, cancellationToken).ConfigureAwait(false);
                    return result;
                }
                catch (Exception ex)
                {
                    return ABACErrors.InvalidCondition(expression, $"Evaluation failed: {ex.Message}");
                }
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the resources used by the <see cref="EELCompiler"/>.
    /// </summary>
    public void Dispose()
    {
        _compilationLock.Dispose();
    }
}
