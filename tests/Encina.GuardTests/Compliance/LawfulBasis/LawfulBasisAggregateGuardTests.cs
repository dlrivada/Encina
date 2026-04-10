using Encina.Compliance.LawfulBasis.Aggregates;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.GuardTests.Compliance.LawfulBasis;

/// <summary>
/// Guard tests for <see cref="LawfulBasisAggregate"/> verifying argument validation and state guards.
/// </summary>
public class LawfulBasisAggregateGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private static LawfulBasisAggregate CreateValid() =>
        LawfulBasisAggregate.Register(
            Guid.NewGuid(),
            "MyApp.Commands.Test",
            GDPRLawfulBasis.Contract,
            "purpose",
            null,
            null,
            "contract-ref",
            Now);

    // ================================================================
    // Register guards
    // ================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_NullOrEmptyRequestTypeName_Throws(string? requestTypeName)
    {
        var act = () => LawfulBasisAggregate.Register(
            Guid.NewGuid(),
            requestTypeName!,
            GDPRLawfulBasis.Contract,
            null, null, null, null,
            Now);

        Should.Throw<ArgumentException>(act);
    }

    // ================================================================
    // ChangeBasis state guards
    // ================================================================

    [Fact]
    public void ChangeBasis_WhenRevoked_ThrowsInvalidOperation()
    {
        var aggregate = CreateValid();
        aggregate.Revoke("Test revocation", Now.AddDays(1));

        Should.Throw<InvalidOperationException>(() =>
            aggregate.ChangeBasis(GDPRLawfulBasis.Consent, null, null, null, null, Now.AddDays(2)));
    }

    [Fact]
    public void ChangeBasis_SameBasis_ThrowsInvalidOperation()
    {
        var aggregate = CreateValid();

        Should.Throw<InvalidOperationException>(() =>
            aggregate.ChangeBasis(GDPRLawfulBasis.Contract, null, null, null, null, Now.AddDays(1)));
    }

    [Fact]
    public void ChangeBasis_DifferentBasis_Succeeds()
    {
        var aggregate = CreateValid();

        Should.NotThrow(() =>
            aggregate.ChangeBasis(GDPRLawfulBasis.Consent, "new purpose", null, null, null, Now.AddDays(1)));
    }

    // ================================================================
    // Revoke state guards
    // ================================================================

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsInvalidOperation()
    {
        var aggregate = CreateValid();
        aggregate.Revoke("First revocation", Now.AddDays(1));

        Should.Throw<InvalidOperationException>(() =>
            aggregate.Revoke("Second revocation", Now.AddDays(2)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Revoke_NullOrEmptyReason_ThrowsArgumentException(string? reason)
    {
        var aggregate = CreateValid();

        Should.Throw<ArgumentException>(() =>
            aggregate.Revoke(reason!, Now.AddDays(1)));
    }

    [Fact]
    public void Revoke_ValidReason_Succeeds()
    {
        var aggregate = CreateValid();

        Should.NotThrow(() => aggregate.Revoke("Valid reason", Now.AddDays(1)));
    }
}
