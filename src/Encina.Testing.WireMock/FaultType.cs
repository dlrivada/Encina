namespace Encina.Testing.WireMock;

/// <summary>
/// Defines fault types for simulating HTTP failures in integration tests.
/// </summary>
/// <remarks>
/// WireMock.Net supports a limited set of fault types. The available faults are:
/// <list type="bullet">
/// <item><see cref="EmptyResponse"/>: Returns a completely empty response.</item>
/// <item><see cref="MalformedResponse"/>: Sends garbage data then closes the connection.</item>
/// <item><see cref="Timeout"/>: Delays response indefinitely (simulated via delay).</item>
/// </list>
/// </remarks>
public enum FaultType
{
    /// <summary>
    /// Server returns an empty response body.
    /// Useful for testing handling of unexpected empty responses.
    /// </summary>
    EmptyResponse,

    /// <summary>
    /// Server returns a malformed/invalid response (garbage data).
    /// Useful for testing JSON parsing error handling.
    /// </summary>
    MalformedResponse,

    /// <summary>
    /// Simulates a request timeout.
    /// The server delays indefinitely (5 minutes) until the client times out.
    /// </summary>
    Timeout
}
