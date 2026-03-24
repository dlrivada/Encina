using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.DataProtection;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Messaging.Encryption.DataProtection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaMessageEncryptionDataProtection();

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        var result = services.AddEncinaMessageEncryptionDataProtection();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_RegistersIMessageEncryptionProvider()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IMessageEncryptionProvider));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be(typeof(DataProtectionMessageEncryptionProvider));
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection(o =>
        {
            o.Purpose = "Custom.Purpose";
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<DataProtectionEncryptionOptions>>().Value;

        options.Purpose.Should().Be("Custom.Purpose");
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_WithoutConfigure_UsesDefaultPurpose()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection();

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<DataProtectionEncryptionOptions>>().Value;

        options.Purpose.Should().Be("Encina.Messaging.Encryption");
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_WithConfigureEncryption_PassesOptions()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection(
            configure: null,
            configureEncryption: e =>
            {
                e.EncryptAllMessages = true;
                e.DefaultKeyId = "dp-key";
            });

        var sp = services.BuildServiceProvider();
        var encOptions = sp.GetRequiredService<IOptions<global::Encina.Messaging.Encryption.MessageEncryptionOptions>>().Value;

        encOptions.EncryptAllMessages.Should().BeTrue();
        encOptions.DefaultKeyId.Should().Be("dp-key");
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_ResolvesProvider()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddLogging();

        services.AddEncinaMessageEncryptionDataProtection();

        var sp = services.BuildServiceProvider();
        var provider = sp.GetService<IMessageEncryptionProvider>();

        provider.Should().NotBeNull();
        provider.Should().BeOfType<DataProtectionMessageEncryptionProvider>();
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_NullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        var act = () => services.AddEncinaMessageEncryptionDataProtection(configure: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_NullConfigureEncryption_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        var act = () => services.AddEncinaMessageEncryptionDataProtection(
            configure: o => o.Purpose = "Test",
            configureEncryption: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_CalledTwice_DoesNotDuplicateRegistration()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection();
        services.AddEncinaMessageEncryptionDataProtection();

        // TryAddSingleton should prevent duplicates
        var descriptors = services.Where(
            d => d.ServiceType == typeof(IMessageEncryptionProvider)).ToList();

        descriptors.Should().HaveCount(1);
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_RegistersDataProtectionOptions_EvenWithoutConfigure()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection();

        var sp = services.BuildServiceProvider();

        // Should resolve without error
        var act = () => sp.GetRequiredService<IOptions<DataProtectionEncryptionOptions>>();
        act.Should().NotThrow();
    }
}
