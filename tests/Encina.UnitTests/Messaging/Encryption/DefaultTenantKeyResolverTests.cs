using Encina.Messaging.Encryption;
using Shouldly;
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
        keyId.ShouldBe("tenant-acme-corp-key");
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
        keyId.ShouldBe("org-contoso-encryption-key");
    }

    [Fact]
    public void ResolveKeyId_NullTenantId_ThrowsArgumentException()
    {
        var options = Options.Create(new MessageEncryptionOptions());
        var resolver = new DefaultTenantKeyResolver(options);

        Action act = () => resolver.ResolveKeyId(null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("tenantId");
    }

    [Fact]
    public void ResolveKeyId_EmptyTenantId_ThrowsArgumentException()
    {
        var options = Options.Create(new MessageEncryptionOptions());
        var resolver = new DefaultTenantKeyResolver(options);

        Action act = () => resolver.ResolveKeyId(string.Empty);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("tenantId");
    }

    [Fact]
    public void ResolveKeyId_WhitespaceTenantId_ThrowsArgumentException()
    {
        var options = Options.Create(new MessageEncryptionOptions());
        var resolver = new DefaultTenantKeyResolver(options);

        Action act = () => resolver.ResolveKeyId("   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("tenantId");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Action act = () => new DefaultTenantKeyResolver(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }
}
