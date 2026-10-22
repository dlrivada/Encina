using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Validation;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Attestation;

/// <summary>
/// Property-based tests for <see cref="HttpAttestationOptionsValidator"/>: scheme rules, private and
/// loopback ranges that <c>AllowInsecureHttp</c> opts into, and ranges that are never allowed.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "Attestation")]
public sealed class HttpAttestationOptionsValidatorPropertyTests
{
    private static ValidatorResult Validate(string attestUrl, bool allowInsecure, string? verifyUrl = null)
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri(attestUrl),
            VerifyEndpointUrl = verifyUrl is null ? null : new Uri(verifyUrl),
            AllowInsecureHttp = allowInsecure
        };

        var result = new HttpAttestationOptionsValidator().Validate(null, options);
        return new ValidatorResult(result.Succeeded, result.FailureMessage, result.Failures?.Count() ?? 0);
    }

    private sealed record ValidatorResult(bool Succeeded, string? Message, int FailureCount);

    // A public DNS name derived from the generated number, e.g. svc-17.attest.example.com.
    private static string PublicHost(PositiveInt n) => $"svc-{n.Get}.attest.example.com";

    // One address per private or loopback range: 10/8, 172.16/12, 192.168/16, 100.64/10 and 127/8.
    private static string PrivateOrLoopbackAddress(byte range, byte b, byte c)
        => (range % 5) switch
        {
            0 => $"10.{b}.{c}.7",
            1 => $"172.{16 + (b % 16)}.{c}.7",
            2 => $"192.168.{b}.{c}",
            3 => $"100.{64 + (b % 64)}.{c}.7",
            _ => $"127.{b}.{c}.1"
        };

    [Property(MaxTest = 50)]
    public bool Validate_HttpsPublicHost_AlwaysSucceeds(PositiveInt n, bool allowInsecure)
        => Validate($"https://{PublicHost(n)}/attest", allowInsecure).Succeeded;

    [Property(MaxTest = 50)]
    public bool Validate_HttpPublicHost_FailsUnlessInsecureHttpIsAllowed(PositiveInt n, bool allowInsecure)
        => Validate($"http://{PublicHost(n)}/attest", allowInsecure).Succeeded == allowInsecure;

    [Property(MaxTest = 50)]
    public bool Validate_HttpToPrivateOrLoopbackAddress_FailsUnlessInsecureHttpIsAllowed(byte range, byte b, byte c, bool allowInsecure)
        => Validate($"http://{PrivateOrLoopbackAddress(range, b, c)}/attest", allowInsecure).Succeeded == allowInsecure;

    [Property(MaxTest = 50)]
    public bool Validate_HttpsToPrivateOrLoopbackAddress_FailsUnlessInsecureHttpIsAllowed(byte range, byte b, byte c, bool allowInsecure)
        => Validate($"https://{PrivateOrLoopbackAddress(range, b, c)}/attest", allowInsecure).Succeeded == allowInsecure;

    [Property(MaxTest = 50)]
    public bool Validate_LinkLocalAddress_IsRejectedEvenWhenInsecureHttpIsAllowed(byte c, byte d, bool useHttp, bool allowInsecure)
    {
        var scheme = useHttp && allowInsecure ? "http" : "https";

        return !Validate($"{scheme}://169.254.{c}.{d}/attest", allowInsecure).Succeeded;
    }

    [Property(MaxTest = 50)]
    public bool Validate_UnspecifiedAddress_IsRejectedEvenWhenInsecureHttpIsAllowed(byte b, byte c, byte d, bool allowInsecure)
        => !Validate($"https://0.{b}.{c}.{d}/attest", allowInsecure).Succeeded;

    [Property(MaxTest = 50)]
    public bool Validate_CloudMetadataAddress_IsRejectedWhateverTheSchemeAndOptIn(bool useHttp, bool allowInsecure)
    {
        var scheme = useHttp ? "http" : "https";

        return !Validate($"{scheme}://169.254.169.254/latest/meta-data", allowInsecure).Succeeded;
    }

    [Property(MaxTest = 50)]
    public bool Validate_InvalidVerifyEndpoint_FailsEvenWhenAttestEndpointIsValid(PositiveInt n)
    {
        var result = Validate(
            $"https://{PublicHost(n)}/attest",
            allowInsecure: false,
            verifyUrl: $"http://{PublicHost(n)}/verify");

        return !result.Succeeded
            && result.Message!.Contains(nameof(HttpAttestationOptions.VerifyEndpointUrl), StringComparison.Ordinal);
    }

    [Property(MaxTest = 50)]
    public bool Validate_Failure_ReportsExactlyOneMessage(PositiveInt n)
    {
        var result = Validate($"http://{PublicHost(n)}/attest", allowInsecure: false);

        return !result.Succeeded && result.FailureCount == 1;
    }
}
