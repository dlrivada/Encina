using Encina.Compliance.Attestation;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Attestation;

public class AttestationOptionsTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new AttestationOptions();

        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void UseInMemory_ShouldSetFlag()
    {
        var options = new AttestationOptions();

        var result = options.UseInMemory();

        result.Should().BeSameAs(options);
        options.UseInMemoryProvider.Should().BeTrue();
    }

    [Fact]
    public void UseHashChain_ShouldSetOptions()
    {
        var options = new AttestationOptions();

        var result = options.UseHashChain(o =>
        {
            o.StoragePath = "/tmp/chain";
        });

        result.Should().BeSameAs(options);
        options.HashChainOptions.Should().NotBeNull();
        options.HashChainOptions!.StoragePath.Should().Be("/tmp/chain");
    }

    [Fact]
    public void UseHashChain_WithoutConfigure_ShouldSetDefaults()
    {
        var options = new AttestationOptions();

        options.UseHashChain();

        options.HashChainOptions.Should().NotBeNull();
    }

    [Fact]
    public void UseHttp_ShouldSetOptions()
    {
        var options = new AttestationOptions();

        options.UseHttp(o =>
        {
            o.AttestEndpointUrl = new Uri("https://attestation.example.com/api/attest");
        });

        options.HttpOptions.Should().NotBeNull();
        options.HttpOptions!.AttestEndpointUrl.Should().NotBeNull();
    }

    [Fact]
    public void UseHttp_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new AttestationOptions();

        var act = () => options.UseHttp(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseInMemory_NullOptions_ThrowsArgumentNullException()
    {
        AttestationOptions options = null!;

        var act = () => options.UseInMemory();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseHashChain_NullOptions_ThrowsArgumentNullException()
    {
        AttestationOptions options = null!;

        var act = () => options.UseHashChain();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseHttp_NullOptions_ThrowsArgumentNullException()
    {
        AttestationOptions options = null!;

        var act = () => options.UseHttp(_ => { });

        act.Should().Throw<ArgumentNullException>();
    }
}
