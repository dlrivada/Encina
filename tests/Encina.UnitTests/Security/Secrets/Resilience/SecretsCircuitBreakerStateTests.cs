using Encina.Security.Secrets.Resilience;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class SecretsCircuitBreakerStateTests
{
    [Fact]
    public void State_Should_DefaultToClosed()
    {
        var state = new SecretsCircuitBreakerState();

        state.State.ShouldBe(CircuitBreakerStateValue.Closed);
    }

    [Fact]
    public void SetOpened_Should_ChangeStateToOpened()
    {
        var state = new SecretsCircuitBreakerState();

        state.SetOpened();

        state.State.ShouldBe(CircuitBreakerStateValue.Opened);
    }

    [Fact]
    public void SetHalfOpen_Should_ChangeStateToHalfOpen()
    {
        var state = new SecretsCircuitBreakerState();

        state.SetHalfOpen();

        state.State.ShouldBe(CircuitBreakerStateValue.HalfOpen);
    }

    [Fact]
    public void SetClosed_Should_ChangeStateToClosed()
    {
        var state = new SecretsCircuitBreakerState();
        state.SetOpened(); // First change to opened

        state.SetClosed();

        state.State.ShouldBe(CircuitBreakerStateValue.Closed);
    }

    [Fact]
    public void SetOpened_ThenSetHalfOpen_Should_ChangeStateToHalfOpen()
    {
        var state = new SecretsCircuitBreakerState();

        state.SetOpened();
        state.SetHalfOpen();

        state.State.ShouldBe(CircuitBreakerStateValue.HalfOpen);
    }

    [Fact]
    public void SetOpened_ThenSetHalfOpen_ThenSetClosed_Should_ChangeStateToClosed()
    {
        var state = new SecretsCircuitBreakerState();

        state.SetOpened();
        state.SetHalfOpen();
        state.SetClosed();

        state.State.ShouldBe(CircuitBreakerStateValue.Closed);
    }
}
