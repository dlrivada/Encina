using Encina.Messaging.Encryption;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Messaging.Encryption;

public class DefaultTenantKeyResolverTests
{
    [Fact]
    public void ResolveKeyId_DefaultPattern_ReturnsFormattedKeyId()
    {
        // Arrange
        var options = Options.Create(new MessageEncryptionOptions());
        var resolver = new DefaultTenantKeyResolver(options);

        // Act
        var keyId = resolver.ResolveKeyId("acme-corp");

        // Assert
        keyId.Should().Be("tenant-acme-corp-key");
    }

    [Fact]
    public void ResolveKeyId_CustomPattern_ReturnsFormattedKeyId()
    {
        // Arrange
        var options = Options.Create(new MessageEncryptionOptions
        {
            TenantKeyPattern = "org-{0}-encryption-key"
        });
        var resolver = new DefaultTenantKeyResolver(options);

        // Act
        var keyId = resolver.ResolveKeyId("contoso");

        // Assert
        keyId.Should().Be("org-contoso-encryption-key");
    }

    [Fact]
    public void ResolveKeyId_NullTenantId_ThrowsArgumentException()
    {
        var options = Options.Create(new MessageEncryptionOptions());
        var resolver = new DefaultTenantKeyResolver(options);

        var act = () => resolver.ResolveKeyId(null!);

        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void ResolveKeyId_EmptyTenantId_ThrowsArgumentException()
    {
        var options = Options.Create(new MessageEncryptionOptions());
        var resolver = new DefaultTenantKeyResolver(options);

        var act = () => resolver.ResolveKeyId(string.Empty);

        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void ResolveKeyId_WhitespaceTenantId_ThrowsArgumentException()
    {
        var options = Options.Create(new MessageEncryptionOptions());
        var resolver = new DefaultTenantKeyResolver(options);

        var act = () => resolver.ResolveKeyId("   ");

        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTenantKeyResolver(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }
}
