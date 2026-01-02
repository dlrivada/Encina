namespace Encina.Testing.Mutations;

/// <summary>
/// Marks a test method as needing stronger assertions to improve mutation coverage.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to annotate tests that have been identified during Stryker mutation testing
/// as having surviving mutants. This serves as documentation and a reminder to strengthen the
/// test assertions.
/// </para>
/// <para>
/// Surviving mutants indicate that the test assertions are not specific enough to detect
/// certain code changes. Common causes include:
/// </para>
/// <list type="bullet">
///   <item><description>Missing boundary condition checks (e.g., &gt; vs &gt;=)</description></item>
///   <item><description>Incomplete value verification (e.g., not checking all properties)</description></item>
///   <item><description>Weak assertions that pass for multiple values</description></item>
///   <item><description>Missing null/empty state assertions</description></item>
/// </list>
/// <para>
/// <b>Workflow:</b>
/// </para>
/// <list type="number">
///   <item><description>Run Stryker mutation testing</description></item>
///   <item><description>Identify surviving mutants in the report</description></item>
///   <item><description>Add this attribute to the corresponding test with a description</description></item>
///   <item><description>Strengthen the assertions to kill the mutant</description></item>
///   <item><description>Remove the attribute once the mutant is killed</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [Fact]
/// [NeedsMutationCoverage("Boundary condition not verified - survived arithmetic mutation on line 45")]
/// public void Calculate_BoundaryValue_ShouldReturnExpectedResult()
/// {
///     // Arrange
///     var calculator = new Calculator();
///
///     // Act
///     var result = calculator.Calculate(100);
///
///     // Assert - TODO: Add boundary check for 100 vs 101
///     result.ShouldBe(200);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class NeedsMutationCoverageAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NeedsMutationCoverageAttribute"/> class.
    /// </summary>
    /// <param name="reason">
    /// A description of why the test needs stronger mutation coverage.
    /// Should include the mutation type and location if known (e.g., "Boundary condition on line 45").
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reason"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is empty or whitespace.</exception>
    public NeedsMutationCoverageAttribute(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Reason = reason;
    }

    /// <summary>
    /// Gets the reason why this test needs stronger mutation coverage.
    /// </summary>
    /// <value>
    /// A description of the mutation coverage gap, typically including the mutation type
    /// and location in the source code.
    /// </value>
    public string Reason { get; }

    /// <summary>
    /// Gets or sets the Stryker mutant ID, if known.
    /// </summary>
    /// <value>
    /// The mutant ID from the Stryker report (e.g., "280", "366"), or <c>null</c> if not specified.
    /// </value>
    /// <remarks>
    /// The mutant ID can be found in the Stryker HTML report or by running
    /// <c>dotnet run --file scripts/list_survivors.cs</c>.
    /// </remarks>
    public string? MutantId { get; init; }

    /// <summary>
    /// Gets or sets the source file where the mutation was applied.
    /// </summary>
    /// <value>
    /// The relative path to the source file, or <c>null</c> if not specified.
    /// </value>
    public string? SourceFile { get; init; }

    /// <summary>
    /// Gets or sets the line number where the mutation was applied.
    /// </summary>
    /// <value>
    /// The line number in the source file, or <c>null</c> if not specified.
    /// </value>
    public int? Line { get; init; }
}
