namespace Encina.Security.Secrets.Resilience;

/// <summary>
/// Tracks the current state of the secrets resilience circuit breaker.
/// </summary>
/// <remarks>
/// Registered as a singleton and updated by the resilience pipeline's circuit breaker
/// callbacks. Read by <see cref="Health.SecretsHealthCheck"/> to report degraded status
/// when the circuit is open.
/// </remarks>
internal sealed class SecretsCircuitBreakerState
{
    private volatile CircuitBreakerStateValue _state = CircuitBreakerStateValue.Closed;

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    public CircuitBreakerStateValue State => _state;

    /// <summary>
    /// Transitions the circuit breaker to the <see cref="CircuitBreakerStateValue.Opened"/> state.
    /// </summary>
    public void SetOpened() => _state = CircuitBreakerStateValue.Opened;

    /// <summary>
    /// Transitions the circuit breaker to the <see cref="CircuitBreakerStateValue.HalfOpen"/> state.
    /// </summary>
    public void SetHalfOpen() => _state = CircuitBreakerStateValue.HalfOpen;

    /// <summary>
    /// Transitions the circuit breaker to the <see cref="CircuitBreakerStateValue.Closed"/> state.
    /// </summary>
    public void SetClosed() => _state = CircuitBreakerStateValue.Closed;
}

/// <summary>
/// Represents the possible states of the secrets circuit breaker.
/// </summary>
internal enum CircuitBreakerStateValue
{
    /// <summary>Circuit is closed — requests flow normally.</summary>
    Closed,

    /// <summary>Circuit is half-open — a single test request is allowed through.</summary>
    HalfOpen,

    /// <summary>Circuit is open — requests are rejected immediately.</summary>
    Opened
}
