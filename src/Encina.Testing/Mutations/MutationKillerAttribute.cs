namespace Encina.Testing.Mutations;

/// <summary>
/// Marks a test method as explicitly designed to kill a specific type of mutation.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to document tests that were specifically written to improve mutation coverage.
/// This helps maintainers understand why certain edge case tests exist and prevents accidental removal.
/// </para>
/// <para>
/// Common mutation types that require explicit killer tests:
/// </para>
/// <list type="bullet">
///   <item><description><b>Arithmetic</b>: +→-, *→/, etc.</description></item>
///   <item><description><b>Equality</b>: ==→!=, &lt;→&lt;=, etc.</description></item>
///   <item><description><b>Boolean</b>: true→false, &amp;&amp;→||</description></item>
///   <item><description><b>Unary</b>: -x→x, !x→x, ++→--</description></item>
///   <item><description><b>Null-check</b>: x==null→x!=null</description></item>
///   <item><description><b>String</b>: ""→"Stryker was here!"</description></item>
///   <item><description><b>Linq</b>: First()→Last(), Any()→All()</description></item>
///   <item><description><b>Block removal</b>: Removing entire statements</description></item>
/// </list>
/// <para>
/// <b>Best practices:</b>
/// </para>
/// <list type="bullet">
///   <item><description>One mutation killer test per specific mutation type</description></item>
///   <item><description>Use precise assertions that detect the exact change</description></item>
///   <item><description>Document the mutation location in the attribute</description></item>
///   <item><description>Include boundary values when testing arithmetic/equality mutations</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [Fact]
/// [MutationKiller("EqualityMutation", Description = "Verifies >= is not mutated to >")]
/// public void IsAdult_ExactlyEighteen_ShouldReturnTrue()
/// {
///     // Arrange
///     var person = new Person { Age = 18 };
///
///     // Act
///     var result = person.IsAdult(); // Uses age >= 18
///
///     // Assert - This test kills the >= to > mutation
///     result.ShouldBeTrue();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class MutationKillerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MutationKillerAttribute"/> class.
    /// </summary>
    /// <param name="mutationType">
    /// The type of mutation this test is designed to kill.
    /// Use descriptive names like "EqualityMutation", "ArithmeticMutation", "BooleanMutation", etc.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mutationType"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="mutationType"/> is empty or whitespace.</exception>
    public MutationKillerAttribute(string mutationType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutationType);
        MutationType = mutationType;
    }

    /// <summary>
    /// Gets the type of mutation this test is designed to kill.
    /// </summary>
    /// <value>
    /// A descriptive name for the mutation type, such as "EqualityMutation", "ArithmeticMutation",
    /// "BooleanMutation", "NullCheckMutation", etc.
    /// </value>
    public string MutationType { get; }

    /// <summary>
    /// Gets or sets additional description of what specific mutation this test kills.
    /// </summary>
    /// <value>
    /// A detailed description, such as "Verifies &gt;= is not mutated to &gt;" or
    /// "Ensures null check is not inverted".
    /// </value>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the source file containing the code that can be mutated.
    /// </summary>
    /// <value>
    /// The relative path to the source file, or <c>null</c> if not specified.
    /// </value>
    public string? SourceFile { get; init; }

    /// <summary>
    /// Gets or sets the method name where the mutation applies.
    /// </summary>
    /// <value>
    /// The name of the method being tested for mutations, or <c>null</c> if not specified.
    /// </value>
    public string? TargetMethod { get; init; }

    /// <summary>
    /// Gets or sets the line number where the mutation applies.
    /// </summary>
    /// <value>
    /// The line number in the source file, or <c>null</c> if not specified.
    /// </value>
    public int? Line { get; init; }
}
