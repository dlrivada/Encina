using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace Encina.Testing.Pact;

/// <summary>
/// Adapter that bridges xUnit's ITestOutputHelper to PactNet's IOutput interface.
/// </summary>
/// <remarks>
/// xUnit 2 does not capture console output, so this adapter is needed to route
/// PactNet's logging output to xUnit's test output mechanism.
/// </remarks>
internal sealed class XunitOutputAdapter : IOutput
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="XunitOutputAdapter"/> class.
    /// </summary>
    /// <param name="output">The xUnit test output helper.</param>
    public XunitOutputAdapter(ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(output);
        _output = output;
    }

    /// <summary>
    /// Writes a line to the xUnit test output.
    /// </summary>
    /// <param name="line">The line to write.</param>
    public void WriteLine(string line)
    {
        try
        {
            _output.WriteLine(line);
        }
        catch (InvalidOperationException ex) when (IsTestCompletedException(ex))
        {
            // xUnit throws InvalidOperationException if the test has already completed.
            // We silently ignore this to avoid test failures on cleanup.
        }
    }

    /// <summary>
    /// Determines whether the exception indicates that the xUnit test has already completed.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>True if this is the known "test completed" exception; otherwise false.</returns>
    private static bool IsTestCompletedException(InvalidOperationException ex)
    {
        // xUnit v2/v3 throws with message containing "no longer available" or similar
        // when attempting to write output after test completion.
        return ex.Message.Contains("no longer available", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("There is no currently active test", StringComparison.OrdinalIgnoreCase);
    }
}
