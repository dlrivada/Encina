using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Validation;

using Shouldly;

namespace Encina.UnitTests.Compliance.Attestation;

public class HttpAttestationOptionsValidatorTests
{
    private readonly HttpAttestationOptionsValidator _sut = new();

    [Fact]
    public void Validate_NullAttestEndpoint_ReturnsFail()
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = null! };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("AttestEndpointUrl");
    }

    [Fact]
    public void Validate_HttpsUrl_ReturnsSuccess()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://attestation.example.com/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_HttpUrl_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("http://attestation.example.com/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("HTTPS");
    }

    [Fact]
    public void Validate_HttpUrl_WithAllowInsecure_ReturnsSuccess()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("http://attestation.example.com/api/attest"),
            AllowInsecureHttp = true
        };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_LocalhostHttps_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://localhost:5001/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("localhost");
    }

    [Fact]
    public void Validate_LoopbackIPv4_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://127.0.0.1:5001/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("loopback");
    }

    [Fact]
    public void Validate_PrivateIPv4_10Network_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://10.0.0.1/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("private");
    }

    [Fact]
    public void Validate_PrivateIPv4_172Network_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://172.16.0.1/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("private");
    }

    [Fact]
    public void Validate_PrivateIPv4_192Network_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://192.168.1.1/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("private");
    }

    [Fact]
    public void Validate_LinkLocalIPv4_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://169.254.0.1/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("link-local");
    }

    [Fact]
    public void Validate_ValidVerifyUrl_ReturnsSuccess()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://attestation.example.com/api/attest"),
            VerifyEndpointUrl = new Uri("https://attestation.example.com/api/verify")
        };

        var result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://[::ffff:169.254.169.254]/api/attest")]
    [InlineData("https://[::ffff:127.0.0.1]/api/attest")]
    [InlineData("https://[::ffff:10.0.0.1]/api/attest")]
    [InlineData("https://0.0.0.0/api/attest")]
    public void Validate_IPv4MappedAndUnspecifiedForms_ReturnFail(string url)
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = new Uri(url) };

        _sut.Validate(null, options).Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MulticastFf80_IsNotMistakenForLinkLocal()
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://[ff80::1]/api/attest") };

        _sut.Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://localhost:5001/api/attest")]
    [InlineData("http://192.168.1.10/api/attest")]
    public void Validate_LocalOrPrivate_WithAllowInsecure_ReturnsSuccess(string url)
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = new Uri(url), AllowInsecureHttp = true };

        _sut.Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://169.254.0.1/api/attest")]
    public void Validate_MetadataOrLinkLocal_WithAllowInsecure_StillReturnsFail(string url)
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = new Uri(url), AllowInsecureHttp = true };

        _sut.Validate(null, options).Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_LocalhostHttps_MessageNamesAllowInsecureHttp()
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://localhost:5001/api/attest") };

        _sut.Validate(null, options).FailureMessage!.ShouldContain("AllowInsecureHttp");
    }

    [Fact]
    public void Validate_InvalidVerifyUrl_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://attestation.example.com/api/attest"),
            VerifyEndpointUrl = new Uri("http://attestation.example.com/api/verify")
        };

        var result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
    }
}
