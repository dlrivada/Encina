#pragma warning disable CA2012 // ValueTask consumed by NSubstitute mock setup

using System.Collections.Immutable;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Health;
using Encina.Messaging.Encryption.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Encryption;

public class MessageEncryptionHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_SuccessfulRoundTrip_ReturnsHealthy()
    {
        // Arrange
        var provider = Substitute.For<IMessageEncryptionProvider>();
        var encryptedPayload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray.Create<byte>(1, 2, 3),
            KeyId = "health-key",
            Algorithm = "AES-256-GCM",
            Nonce = ImmutableArray<byte>.Empty,
            Tag = ImmutableArray<byte>.Empty,
            Version = 1
        };

        provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, EncryptedPayload>(encryptedPayload));

        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes("encina-message-health-probe");
        provider.DecryptAsync(
            Arg.Any<EncryptedPayload>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ImmutableArray<byte>>(ImmutableArray.Create(plaintextBytes)));

        var services = new ServiceCollection();
        services.AddSingleton(provider);
        var sp = services.BuildServiceProvider();

        var healthCheck = new MessageEncryptionHealthCheck(sp, NullLogger<MessageEncryptionHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("keyId");
        result.Data.ShouldContainKey("algorithm");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingProvider_ReturnsUnhealthy()
    {
        // Arrange — no IMessageEncryptionProvider registered
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var healthCheck = new MessageEncryptionHealthCheck(sp, NullLogger<MessageEncryptionHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IMessageEncryptionProvider");
    }

    [Fact]
    public async Task CheckHealthAsync_EncryptionFails_ReturnsUnhealthy()
    {
        // Arrange
        var provider = Substitute.For<IMessageEncryptionProvider>();
        var error = EncinaErrors.Create("test", "encryption failed");

        provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, EncryptedPayload>(error));

        var services = new ServiceCollection();
        services.AddSingleton(provider);
        var sp = services.BuildServiceProvider();

        var healthCheck = new MessageEncryptionHealthCheck(sp, NullLogger<MessageEncryptionHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("encrypt phase");
    }

    [Fact]
    public async Task CheckHealthAsync_DecryptionMismatch_ReturnsUnhealthy()
    {
        // Arrange
        var provider = Substitute.For<IMessageEncryptionProvider>();
        var encryptedPayload = new EncryptedPayload
        {
            Ciphertext = ImmutableArray.Create<byte>(1, 2, 3),
            KeyId = "health-key",
            Algorithm = "AES-256-GCM",
            Nonce = ImmutableArray<byte>.Empty,
            Tag = ImmutableArray<byte>.Empty,
            Version = 1
        };

        provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, EncryptedPayload>(encryptedPayload));

        var wrongBytes = System.Text.Encoding.UTF8.GetBytes("wrong-content");
        provider.DecryptAsync(
            Arg.Any<EncryptedPayload>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ImmutableArray<byte>>(ImmutableArray.Create(wrongBytes)));

        var services = new ServiceCollection();
        services.AddSingleton(provider);
        var sp = services.BuildServiceProvider();

        var healthCheck = new MessageEncryptionHealthCheck(sp, NullLogger<MessageEncryptionHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("decrypt phase");
    }

    [Fact]
    public async Task CheckHealthAsync_ProviderThrows_ReturnsUnhealthy()
    {
        // Arrange
        var provider = Substitute.For<IMessageEncryptionProvider>();
        provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, EncryptedPayload>>(
                Task.FromException<Either<EncinaError, EncryptedPayload>>(
                    new InvalidOperationException("provider crash"))));

        var services = new ServiceCollection();
        services.AddSingleton(provider);
        var sp = services.BuildServiceProvider();

        var healthCheck = new MessageEncryptionHealthCheck(sp, NullLogger<MessageEncryptionHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", healthCheck, null, null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        MessageEncryptionHealthCheck.DefaultName.ShouldBe("encina-message-encryption");
    }
}
