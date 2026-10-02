using Encina.Cdc.Debezium;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumCdcOptionsValidator"/> (#852): the listener prefix is checked
/// for format and scheme only, never for SSRF.
/// </summary>
public sealed class DebeziumCdcOptionsValidatorTests
{
    private readonly DebeziumCdcOptionsValidator _sut = new();

    [Theory]
    [InlineData("http://+")]
    [InlineData("http://*")]
    [InlineData("HTTP://localhost")]
    [InlineData("http://127.0.0.1")]
    [InlineData("https://cdc.example.com")]
    [InlineData("http://0.0.0.0")]
    [InlineData("http://[::1]")]
    public void Validate_ValidListenPrefixes_Succeed(string listenUrl)
    {
        _sut.Validate(null, new DebeziumCdcOptions { ListenUrl = listenUrl }).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://+")]
    [InlineData("+")]
    [InlineData("http://")]
    [InlineData("http://host:8080")]
    [InlineData("http://host/path")]
    [InlineData("http://bad host")]
    [InlineData("http://::1")]
    [InlineData("http://[not-ipv6]")]
    [InlineData("http://[")]
    [InlineData("http://[]")]
    public void Validate_InvalidListenPrefixes_Fail(string listenUrl)
    {
        var result = _sut.Validate(null, new DebeziumCdcOptions { ListenUrl = listenUrl });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("ListenUrl");
    }

    [Fact]
    public void Validate_NullListenUrl_Fails()
    {
        _sut.Validate(null, new DebeziumCdcOptions { ListenUrl = null! }).Failed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Validate_PortOutOfRange_Fails(int port)
    {
        _sut.Validate(null, new DebeziumCdcOptions { ListenPort = port }).FailureMessage!.ShouldContain("ListenPort");
    }

    [Theory]
    [InlineData("")]
    [InlineData("debezium")]
    public void Validate_PathWithoutLeadingSlash_Fails(string path)
    {
        _sut.Validate(null, new DebeziumCdcOptions { ListenPath = path }).FailureMessage!.ShouldContain("ListenPath");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ChannelCapacityBelowOne_Fails(int capacity)
    {
        _sut.Validate(null, new DebeziumCdcOptions { ChannelCapacity = capacity }).FailureMessage!.ShouldContain("ChannelCapacity");
    }

    [Fact]
    public void Validate_ChannelCapacityOne_Succeeds()
    {
        _sut.Validate(null, new DebeziumCdcOptions { ChannelCapacity = 1 }).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => _sut.Validate(null, null!));
    }
}
