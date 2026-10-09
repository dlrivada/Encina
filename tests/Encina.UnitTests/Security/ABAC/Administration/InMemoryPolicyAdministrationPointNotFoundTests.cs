using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Administration;

/// <summary>
/// An administrative operation on a policy that does not exist reports the resource-style
/// <see cref="ABACErrors.PolicyNotFoundCode"/>; it is a lookup failure, not an authorization
/// denial (#1984).
/// </summary>
public sealed class InMemoryPolicyAdministrationPointNotFoundTests
{
    private readonly InMemoryPolicyAdministrationPoint _sut = new(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);

    [Fact]
    public async Task RemovePolicyAsync_UnknownPolicy_ReturnsAdministrativePolicyNotFound()
    {
        var result = await _sut.RemovePolicyAsync("p-ghost");

        CodeOf(result).ShouldBe(ABACErrors.PolicyNotFoundCode);
        CodeOf(result).ShouldNotStartWith(EncinaErrorCodes.AuthorizationPrefix);
    }

    [Fact]
    public async Task UpdatePolicyAsync_UnknownPolicy_ReturnsAdministrativePolicyNotFound()
    {
        var policy = new Policy
        {
            Id = "p-ghost",
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        var result = await _sut.UpdatePolicyAsync(policy);

        CodeOf(result).ShouldBe(ABACErrors.PolicyNotFoundCode);
        CodeOf(result).ShouldNotStartWith(EncinaErrorCodes.AuthorizationPrefix);
    }

    private static string CodeOf(LanguageExt.Either<EncinaError, LanguageExt.Unit> result) =>
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty));
}
