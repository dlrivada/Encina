using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Validation;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Attestation;

/// <summary>
/// Property-based tests for <see cref="HttpAttestationOptionsValidator"/>.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "Attestation")]
public sealed class HttpAttestationOptionsValidatorPropertyTests
{
    private static string Host(PositiveInt n) => $"attest{n.Get}.example.com";

    [Property(MaxTest = 50)]
    public bool Validate_HttpsPublicHost_AlwaysSucceeds(PositiveInt n)
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = new Uri($"https://{Host(n)}/attest") };

        return new HttpAttestationOptionsValidator().Validate(null, options).Succeeded;
    }

    [Property(MaxTest = 50)]
    public bool Validate_HttpPublicHost_FailsUnlessInsecureHttpIsAllowed(PositiveInt n, bool allowInsecure)
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri($"http://{Host(n)}/attest"),
            AllowInsecureHttp = allowInsecure
        };

        var result = new HttpAttestationOptionsValidator().Validate(null, options);

        return result.Succeeded == allowInsecure;
    }

    [Property(MaxTest = 50)]
    public bool Validate_InvalidVerifyEndpoint_FailsEvenWhenAttestEndpointIsValid(PositiveInt n)
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri($"https://{Host(n)}/attest"),
            VerifyEndpointUrl = new Uri($"http://{Host(n)}/verify")
        };

        var result = new HttpAttestationOptionsValidator().Validate(null, options);

        return result.Failed && result.FailureMessage!.Contains(nameof(HttpAttestationOptions.VerifyEndpointUrl), StringComparison.Ordinal);
    }

    [Property(MaxTest = 50)]
    public bool Validate_PrivateIpv4Address_FailsUnlessInsecureHttpIsAllowed(byte b, byte c, bool allowInsecure)
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri($"https://10.{b}.{c}.5/attest"),
            AllowInsecureHttp = allowInsecure
        };

        var result = new HttpAttestationOptionsValidator().Validate(null, options);

        return result.Succeeded == allowInsecure;
    }

    [Property(MaxTest = 50)]
    public bool Validate_Failure_ReportsExactlyOneMessage(PositiveInt n)
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = new Uri($"http://{Host(n)}/attest") };

        var result = new HttpAttestationOptionsValidator().Validate(null, options);

        return result.Failed && result.Failures!.Count() == 1;
    }
}
