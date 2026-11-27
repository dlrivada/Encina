using LanguageExt;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for the access elevation model (<see cref="AccessElevation"/>, <see cref="AccessElevationDenials"/>
/// and the internal no-op gate): they pin what the entry points accept and what they refuse.
/// </summary>
public class AccessElevationGuardTests
{
    #region AccessElevation(Guid, string, DateTimeOffset)

    /// <summary>
    /// The positional constructor checks no argument: the scope is stored as given.
    /// </summary>
    [Fact]
    public void AccessElevation_EmptyScope_IsStoredUnchanged()
    {
        var expires = TimeProvider.System.GetUtcNow();
        var id = Guid.NewGuid();

        var elevation = new AccessElevation(id, string.Empty, expires);

        elevation.ElevationId.ShouldBe(id);
        elevation.Scope.ShouldBeEmpty();
        elevation.ExpiresAtUtc.ShouldBe(expires);
    }

    /// <summary>
    /// A with-expression copies the elevation and keeps value equality on the unchanged members.
    /// </summary>
    [Fact]
    public void AccessElevation_WithScope_KeepsOtherMembers()
    {
        var original = new AccessElevation(Guid.NewGuid(), "orders", TimeProvider.System.GetUtcNow());

        var copy = original with { Scope = "payments" };

        copy.Scope.ShouldBe("payments");
        copy.ElevationId.ShouldBe(original.ElevationId);
        copy.ShouldNotBe(original);
    }

    #endregion

    #region AccessElevationDenials.IsOverridable(EncinaError)

    /// <summary>
    /// An authorization denial the gate may override is reported as overridable.
    /// </summary>
    [Fact]
    public void IsOverridable_ForbiddenDenial_ReturnsTrue()
    {
        var denial = EncinaErrors.Create(EncinaErrorCodes.AuthorizationForbidden, ErrorCategory.Forbidden, "denied");

        AccessElevationDenials.IsOverridable(denial).ShouldBeTrue();
    }

    /// <summary>
    /// The ABAC codes, kept as literals, are overridable too.
    /// </summary>
    [Fact]
    public void IsOverridable_AbacAccessDenied_ReturnsTrue()
    {
        var denial = EncinaErrors.Create("encina.authorization.abac_access_denied", ErrorCategory.Forbidden, "denied");

        AccessElevationDenials.IsOverridable(denial).ShouldBeTrue();
    }

    /// <summary>
    /// A denial with an unrelated code is not overridable.
    /// </summary>
    [Fact]
    public void IsOverridable_UnrelatedCode_ReturnsFalse()
    {
        var denial = EncinaErrors.Create("encina.unrelated", ErrorCategory.Internal, "boom");

        AccessElevationDenials.IsOverridable(denial).ShouldBeFalse();
    }

    /// <summary>
    /// An error that carries no code at all is never overridable (fail closed).
    /// </summary>
    [Fact]
    public void IsOverridable_ErrorWithoutCode_ReturnsFalse()
    {
        var denial = EncinaError.New("plain message");

        AccessElevationDenials.IsOverridable(denial).ShouldBeFalse();
    }

    #endregion

    #region NoAccessElevationGate.TryElevateAsync

    /// <summary>
    /// The no-op gate validates no argument and always answers Right(None), even for a null request and context.
    /// </summary>
    [Fact]
    public async Task TryElevateAsync_NullRequestAndContext_ReturnsRightNone()
    {
        var gate = new NoAccessElevationGate();
        var denial = EncinaErrors.Create(EncinaErrorCodes.AuthorizationForbidden, ErrorCategory.Forbidden, "denied");

        var result = await gate.TryElevateAsync<object>(null!, null!, denial, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((Option<AccessElevation>)result).IsNone.ShouldBeTrue();
    }

    #endregion
}
