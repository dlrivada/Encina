using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.DataProtection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Messaging.Encryption.DataProtection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Action act = () => services.AddEncinaMessageEncryptionDataProtection();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        var result = services.AddEncinaMessageEncryptionDataProtection();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_RegistersIMessageEncryptionProvider()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IMessageEncryptionProvider));

        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(DataProtectionMessageEncryptionProvider));
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

        options.Purpose.ShouldBe("Custom.Purpose");
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_WithoutConfigure_UsesDefaultPurpose()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection();

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<DataProtectionEncryptionOptions>>().Value;

        options.Purpose.ShouldBe("Encina.Messaging.Encryption");
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

        encOptions.EncryptAllMessages.ShouldBeTrue();
        encOptions.DefaultKeyId.ShouldBe("dp-key");
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

        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<DataProtectionMessageEncryptionProvider>();
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_NullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        Action act = () => services.AddEncinaMessageEncryptionDataProtection(configure: null);

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_NullConfigureEncryption_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        Action act = () => services.AddEncinaMessageEncryptionDataProtection(
            configure: o => o.Purpose = "Test",
            configureEncryption: null);

        Should.NotThrow(act);
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

        descriptors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaMessageEncryptionDataProtection_RegistersDataProtectionOptions_EvenWithoutConfigure()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();

        services.AddEncinaMessageEncryptionDataProtection();

        var sp = services.BuildServiceProvider();

        // Should resolve without error
        Action act = () => sp.GetRequiredService<IOptions<DataProtectionEncryptionOptions>>();
        Should.NotThrow(act);
    }
}
