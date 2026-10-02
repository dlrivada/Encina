using System.Net;

using Encina.Validation;

using Shouldly;

namespace Encina.UnitTests.Validation.Endpoints;

/// <summary>
/// Unit tests for <see cref="EndpointValidator"/> (#852): host classification, normalisation and
/// the strict endpoint policy.
/// </summary>
public sealed class EndpointValidatorTests
{
    private static readonly EndpointPolicy StrictHttps = EndpointPolicy.ForHttps(allowInsecureHttp: false, allowLocalEndpoints: false);

    #region ClassifyHost

    [Theory]
    [InlineData("vault.example.com")]
    [InlineData("my_vault")]
    [InlineData("8.8.8.8")]
    [InlineData("2001:4860:4860::8888")]
    [InlineData("[2001:db8::1]")]
    [InlineData("ff80::1")] // multicast: must not be mistaken for fe80::/10
    [InlineData("100.100.100.201")]
    [InlineData("172.32.0.1")]
    [InlineData("::ffff:8.8.8.8")]
    public void ClassifyHost_PublicHosts_ArePublic(string host)
    {
        EndpointValidator.ClassifyHost(host).ShouldBe(EndpointHostKind.Public);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("localhost.")]
    [InlineData("api.localhost")]
    [InlineData("ip6-localhost")]
    [InlineData("ip6-loopback")]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.1.")]
    [InlineData("127.1.2.3")]
    [InlineData("2130706433")] // decimal 127.0.0.1
    [InlineData("0177.0.0.1")] // octal 127.0.0.1
    [InlineData("0x7f.0.0.1")] // hexadecimal 127.0.0.1
    [InlineData("::1")]
    [InlineData("[::1]")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::127.0.0.1")]
    [InlineData("64:ff9b::127.0.0.1")]
    [InlineData("ⓛⓞⓒⓐⓛⓗⓞⓢⓣ")] // circled letters map to "localhost"
    [InlineData("１２７.０.０.１")] // full-width digits map to 127.0.0.1
    public void ClassifyHost_LoopbackForms_AreLoopback(string host)
    {
        EndpointValidator.ClassifyHost(host).ShouldBe(EndpointHostKind.Loopback);
    }

    [Theory]
    [InlineData("169.254.0.1")]
    [InlineData("169.254.255.255")]
    [InlineData("fe80::1")]
    [InlineData("febf::1")]
    [InlineData("fe80::1%4")]
    [InlineData("::ffff:169.254.10.10")]
    public void ClassifyHost_LinkLocal_IsLinkLocal(string host)
    {
        EndpointValidator.ClassifyHost(host).ShouldBe(EndpointHostKind.LinkLocal);
    }

    [Theory]
    [InlineData("169.254.169.254")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("[::ffff:a9fe:a9fe]")]
    [InlineData("fd00:ec2::254")]
    [InlineData("100.100.100.200")]
    [InlineData("metadata.google.internal")]
    [InlineData("METADATA.GOOGLE.INTERNAL.")]
    [InlineData("metadata.goog")]
    [InlineData("instance-data")]
    [InlineData("instance-data.ec2.internal")]
    public void ClassifyHost_CloudMetadata_IsCloudMetadata(string host)
    {
        EndpointValidator.ClassifyHost(host).ShouldBe(EndpointHostKind.CloudMetadata);
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("0")]
    [InlineData("0.1.2.3")]
    [InlineData("::")]
    [InlineData("[::]")]
    [InlineData("::ffff:0.0.0.0")]
    public void ClassifyHost_Unspecified_IsUnspecified(string host)
    {
        EndpointValidator.ClassifyHost(host).ShouldBe(EndpointHostKind.Unspecified);
    }

    [Theory]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("::ffff:10.1.2.3")]
    public void ClassifyHost_PrivateRanges_ArePrivate(string host)
    {
        EndpointValidator.ClassifyHost(host).ShouldBe(EndpointHostKind.Private);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("[]")]
    [InlineData("localhost:8080")]
    [InlineData("evil.com/path")]
    [InlineData("user@host")]
    [InlineData("bad host")]
    public void ClassifyHost_MalformedHosts_AreInvalid(string? host)
    {
        EndpointValidator.ClassifyHost(host).ShouldBe(EndpointHostKind.Invalid);
    }

    [Fact]
    public void ClassifyAddress_NullAddress_Throws()
    {
        Should.Throw<ArgumentNullException>(() => EndpointValidator.ClassifyAddress(null!));
    }

    [Fact]
    public void ClassifyAddress_IPv4MappedLoopback_IsLoopback()
    {
        EndpointValidator.ClassifyAddress(IPAddress.Loopback.MapToIPv6()).ShouldBe(EndpointHostKind.Loopback);
    }

    #endregion

    #region ValidateUri

    [Fact]
    public void ValidateUri_PublicHttps_ReturnsNull()
    {
        EndpointValidator.ValidateUri(new Uri("https://vault.example.com:8200"), "VaultUri", StrictHttps).ShouldBeNull();
    }

    [Fact]
    public void ValidateUri_Null_ReportsNotConfigured()
    {
        EndpointValidator.ValidateUri(null, "VaultUri", StrictHttps).ShouldBe("VaultUri must be configured.");
    }

    [Fact]
    public void ValidateUri_Relative_ReportsNotAbsolute()
    {
        EndpointValidator.ValidateUri(new Uri("/vault", UriKind.Relative), "VaultUri", StrictHttps)
            .ShouldBe("VaultUri must be an absolute URI.");
    }

    [Fact]
    public void ValidateUri_PlainHttp_NamesTheOptOut()
    {
        var error = EndpointValidator.ValidateUri(new Uri("http://vault.example.com"), "VaultUri", StrictHttps);

        error.ShouldNotBeNull();
        error.ShouldContain("must use HTTPS");
        error.ShouldContain("AllowInsecureHttp");
    }

    [Fact]
    public void ValidateUri_PlainHttpWithOptOut_ReturnsNull()
    {
        var policy = EndpointPolicy.ForHttps(allowInsecureHttp: true, allowLocalEndpoints: false);

        EndpointValidator.ValidateUri(new Uri("http://vault.example.com"), "VaultUri", policy).ShouldBeNull();
    }

    [Fact]
    public void ValidateUri_UnknownScheme_ListsAllowedSchemes()
    {
        var policy = new EndpointPolicy { AllowedSchemes = ["nats", "tls"] };

        var error = EndpointValidator.ValidateUri(new Uri("ftp://nats.example.com"), "Url", policy);

        error.ShouldBe("Url must use one of the allowed schemes (nats, tls).");
    }

    [Fact]
    public void ValidateUri_FileScheme_IsRejected()
    {
        EndpointValidator.ValidateUri(new Uri("file:///etc/passwd"), "VaultUri", StrictHttps).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("https://localhost:8200")]
    [InlineData("https://LOCALHOST:8200")]
    [InlineData("https://localhost.:8200")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://2130706433/")]
    [InlineData("https://0177.0.0.1/")]
    [InlineData("https://[::1]/")]
    [InlineData("https://[::ffff:127.0.0.1]/")]
    [InlineData("https://evil.example.com@127.0.0.1/")]
    public void ValidateUri_Loopback_IsRejectedWithoutOptOut(string url)
    {
        var error = EndpointValidator.ValidateUri(new Uri(url), "VaultUri", StrictHttps);

        error.ShouldNotBeNull();
        error.ShouldContain("loopback");
        error.ShouldContain("AllowLocalEndpoints");
    }

    [Fact]
    public void ValidateUri_LoopbackWithOptOut_ReturnsNull()
    {
        var policy = EndpointPolicy.ForHttps(allowInsecureHttp: false, allowLocalEndpoints: true);

        EndpointValidator.ValidateUri(new Uri("https://localhost:8200"), "VaultUri", policy).ShouldBeNull();
    }

    [Fact]
    public void ValidateUri_UserInfoBeforePublicHost_UsesTheRealHost()
    {
        EndpointValidator.ValidateUri(new Uri("https://localhost@vault.example.com/"), "VaultUri", StrictHttps).ShouldBeNull();
    }

    [Theory]
    [InlineData("https://169.254.169.254/latest/meta-data", "metadata")]
    [InlineData("https://[::ffff:169.254.169.254]/", "metadata")]
    [InlineData("https://metadata.google.internal/", "metadata")]
    [InlineData("https://169.254.1.1/", "link-local")]
    [InlineData("https://[fe80::1]/", "link-local")]
    [InlineData("https://0.0.0.0/", "unspecified")]
    [InlineData("https://[::]/", "unspecified")]
    public void ValidateUri_ForbiddenHosts_AreRejectedEvenWithOptOuts(string url, string reason)
    {
        var lenient = EndpointPolicy.ForHttps(allowInsecureHttp: true, allowLocalEndpoints: true);

        var error = EndpointValidator.ValidateUri(new Uri(url), "VaultUri", lenient);

        error.ShouldNotBeNull();
        error.ShouldContain(reason);
    }

    [Fact]
    public void ValidateUri_PrivateHost_IsAllowedByDefault()
    {
        EndpointValidator.ValidateUri(new Uri("https://10.0.0.5:8200"), "VaultUri", StrictHttps).ShouldBeNull();
    }

    [Fact]
    public void ValidateUri_PrivateHostWithRejectPrivateNetworks_NamesTheConfiguredOptOut()
    {
        var policy = new EndpointPolicy
        {
            AllowedSchemes = ["https"],
            RejectPrivateNetworks = true,
            LocalEndpointsOptOutName = "AllowInsecureHttp",
        };

        var error = EndpointValidator.ValidateUri(new Uri("https://192.168.1.10"), "Endpoint", policy);

        error.ShouldNotBeNull();
        error.ShouldContain("private");
        error.ShouldContain("AllowInsecureHttp");
    }

    [Fact]
    public void ValidateUri_ErrorNeverEchoesUserInfo()
    {
        var error = EndpointValidator.ValidateUri(new Uri("https://admin:s3cr3t@127.0.0.1/"), "VaultUri", StrictHttps);

        error.ShouldNotBeNull();
        error.ShouldNotContain("s3cr3t");
    }

    [Fact]
    public void ValidateUri_NullArguments_Throw()
    {
        var uri = new Uri("https://vault.example.com");

        Should.Throw<ArgumentNullException>(() => EndpointValidator.ValidateUri(uri, null!, StrictHttps));
        Should.Throw<ArgumentNullException>(() => EndpointValidator.ValidateUri(uri, "VaultUri", null!));
    }

    #endregion

    #region ValidateUrl

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ValidateUrl_Missing_ReportsNotConfigured(string? url)
    {
        EndpointValidator.ValidateUrl(url, "VaultAddress", StrictHttps).ShouldBe("VaultAddress must be configured.");
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("https://")]
    public void ValidateUrl_Unparseable_ReportsNotAbsolute(string url)
    {
        EndpointValidator.ValidateUrl(url, "VaultAddress", StrictHttps).ShouldBe("VaultAddress must be an absolute URI.");
    }

    [Fact]
    public void ValidateUrl_HostWithoutScheme_IsRejectedByScheme()
    {
        // "localhost:8200" parses as scheme "localhost".
        EndpointValidator.ValidateUrl("localhost:8200", "VaultAddress", StrictHttps)!.ShouldContain("allowed schemes");
    }

    [Fact]
    public void ValidateUrl_SurroundingWhitespace_IsTrimmed()
    {
        EndpointValidator.ValidateUrl("  https://vault.example.com  ", "VaultAddress", StrictHttps).ShouldBeNull();
    }

    [Fact]
    public void ValidateUrl_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => EndpointValidator.ValidateUrl("https://a.example", null!, StrictHttps));
        Should.Throw<ArgumentNullException>(() => EndpointValidator.ValidateUrl("https://a.example", "Url", null!));
    }

    #endregion

    #region ValidateHost

    [Fact]
    public void ValidateHost_PublicHost_ReturnsNull()
    {
        EndpointValidator.ValidateHost("mysql.example.com", "Hostname", StrictHttps).ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateHost_Missing_ReportsNotConfigured(string? host)
    {
        EndpointValidator.ValidateHost(host, "Hostname", StrictHttps).ShouldBe("Hostname must be configured.");
    }

    [Fact]
    public void ValidateHost_Malformed_ReportsInvalidHost()
    {
        EndpointValidator.ValidateHost("db:3306", "Hostname", StrictHttps)!.ShouldContain("valid host");
    }

    [Fact]
    public void ValidateHost_Localhost_RequiresOptOut()
    {
        EndpointValidator.ValidateHost("localhost", "Hostname", StrictHttps)!.ShouldContain("AllowLocalEndpoints");
        EndpointValidator.ValidateHost("localhost", "Hostname", EndpointPolicy.ForHttps(false, true)).ShouldBeNull();
    }

    [Fact]
    public void ValidateHost_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => EndpointValidator.ValidateHost("db.example.com", null!, StrictHttps));
        Should.Throw<ArgumentNullException>(() => EndpointValidator.ValidateHost("db.example.com", "Hostname", null!));
    }

    #endregion

    #region EndpointPolicy

    [Fact]
    public void ForHttps_Strict_AllowsOnlyHttps()
    {
        var policy = EndpointPolicy.ForHttps(allowInsecureHttp: false, allowLocalEndpoints: false);

        string.Join(",", policy.AllowedSchemes).ShouldBe("https");
        policy.AllowLocalEndpoints.ShouldBeFalse();
        policy.RejectPrivateNetworks.ShouldBeFalse();
        policy.InsecureHttpOptOutName.ShouldBe("AllowInsecureHttp");
        policy.LocalEndpointsOptOutName.ShouldBe("AllowLocalEndpoints");
    }

    [Fact]
    public void ForHttps_Lenient_AllowsHttpAndLocal()
    {
        var policy = EndpointPolicy.ForHttps(allowInsecureHttp: true, allowLocalEndpoints: true);

        string.Join(",", policy.AllowedSchemes).ShouldBe("https,http");
        policy.AllowLocalEndpoints.ShouldBeTrue();
    }

    #endregion
}
