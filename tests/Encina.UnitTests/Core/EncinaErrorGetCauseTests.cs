using Shouldly;

namespace Encina.UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="EncinaErrorExtensions.GetCause(EncinaError)"/> (#1159 review).
/// </summary>
public sealed class EncinaErrorGetCauseTests
{
    [Fact]
    public void GetCause_CreatedWithoutException_ReturnsNone_EvenThoughExceptionIsTheMessageCarrier()
    {
        var error = EncinaErrors.Create("consent.missing", "Consent missing for subject 'patient-1'");

        // EncinaError.Exception exposes the internal carrier, whose message is the error message.
        error.Exception.IsSome.ShouldBeTrue();
        error.GetCause().IsNone.ShouldBeTrue();
    }

    [Fact]
    public void GetCause_CreatedWithException_ReturnsThatException()
    {
        var cause = new TimeoutException("db timeout");
        var error = EncinaErrors.Create("store.failure", "Store failed", cause);

        error.GetCause().IfNone(() => throw new InvalidOperationException("expected a cause")).ShouldBeSameAs(cause);
    }

    [Fact]
    public void GetCause_FromPlainException_ReturnsIt()
    {
        var cause = new InvalidOperationException("boom");
        var error = EncinaError.New(cause);

        error.GetCause().IfNone(() => throw new InvalidOperationException("expected a cause")).ShouldBeSameAs(cause);
    }

    [Fact]
    public void GetCause_MessageOnlyError_ReturnsNone()
    {
        EncinaError.New("plain message").GetCause().IsNone.ShouldBeTrue();
    }
}
