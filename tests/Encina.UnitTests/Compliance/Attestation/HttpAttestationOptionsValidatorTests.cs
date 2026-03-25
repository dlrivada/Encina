using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Validation;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Attestation;

public class HttpAttestationOptionsValidatorTests
{
    private readonly HttpAttestationOptionsValidator _sut = new();

    [Fact]
    public void Validate_NullAttestEndpoint_ReturnsFail()
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = null! };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AttestEndpointUrl");
    }

    [Fact]
    public void Validate_HttpsUrl_ReturnsSuccess()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://attestation.example.com/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_HttpUrl_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("http://attestation.example.com/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HTTPS");
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

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_LocalhostHttps_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://localhost:5001/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("localhost");
    }

    [Fact]
    public void Validate_LoopbackIPv4_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://127.0.0.1:5001/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("loopback");
    }

    [Fact]
    public void Validate_PrivateIPv4_10Network_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://10.0.0.1/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("private");
    }

    [Fact]
    public void Validate_PrivateIPv4_172Network_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://172.16.0.1/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("private");
    }

    [Fact]
    public void Validate_PrivateIPv4_192Network_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://192.168.1.1/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("private");
    }

    [Fact]
    public void Validate_LinkLocalIPv4_ReturnsFail()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://169.254.0.1/api/attest")
        };

        var result = _sut.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("link-local");
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

        result.Succeeded.Should().BeTrue();
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

        result.Failed.Should().BeTrue();
    }
}
