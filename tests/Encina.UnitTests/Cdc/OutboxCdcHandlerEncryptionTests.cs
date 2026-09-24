using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Serialization;
using Encina.UnitTests.Messaging.Encryption;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// The CDC outbox handler reads outbox rows through the same <see cref="IMessageSerializer"/>
/// that wrote them, so a row encrypted by <see cref="EncryptingMessageSerializer"/> is decrypted
/// before it is republished (#1168, #1259 review).
/// </summary>
public sealed class OutboxCdcHandlerEncryptionTests
{
    private const string Diagnosis = "F41.1 generalized anxiety disorder";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleInsert_EncryptedContent_RepublishesDecryptedNotification(bool encryptionRegisteredFirst)
    {
        // Arrange
        var encina = new FakeEncina();
        using var provider = BuildProvider(encina, encryptionRegisteredFirst);
        using var scope = provider.CreateScope();

        var serializer = scope.ServiceProvider.GetRequiredService<IMessageSerializer>();
        serializer.ShouldBeOfType<EncryptingMessageSerializer>();

        var content = serializer.Serialize(new DiagnosisRecordedNotification(Diagnosis));
        content.ShouldStartWith("ENC:v1:");
        content.ShouldNotContain("F41.1");

        var handler = scope.ServiceProvider.GetRequiredService<IChangeEventHandler<JsonElement>>();

        // Act
        var result = await handler.HandleInsertAsync(CreateOutboxRow(content), CreateContext());

        // Assert
        result.IsRight.ShouldBeTrue();
        encina.PublishedNotifications.ShouldHaveSingleItem()
            .ShouldBeOfType<DiagnosisRecordedNotification>()
            .Diagnosis.ShouldBe(Diagnosis);
    }

    [Fact]
    public async Task HandleInsert_ContentEncryptedWithUnknownKey_ReturnsLeftAndPublishesNothing()
    {
        // Arrange: the row was encrypted with key material this process does not have, so
        // decryption fails (AES-GCM authentication) inside EncryptingMessageSerializer.
        var encina = new FakeEncina();
        using var provider = BuildProvider(encina, encryptionRegisteredFirst: false);
        using var scope = provider.CreateScope();

        var foreignContent = RealMessageEncryption.CreateSerializer()
            .Serialize(new DiagnosisRecordedNotification(Diagnosis));

        var handler = scope.ServiceProvider.GetRequiredService<IChangeEventHandler<JsonElement>>();

        // Act
        var result = await handler.HandleInsertAsync(CreateOutboxRow(foreignContent), CreateContext());

        // Assert
        result.IsLeft.ShouldBeTrue();
        encina.PublishedNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleInsert_WithoutEncryption_UsesDefaultJsonSerializer()
    {
        // Arrange: no encryption package registered; AddEncinaCdc registers JsonMessageSerializer.
        var encina = new FakeEncina();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEncina>(encina);
        services.AddEncinaCdc(config => config.UseOutboxCdc("OutboxMessages"));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMessageSerializer>().ShouldBeOfType<JsonMessageSerializer>();
        var content = new JsonMessageSerializer().Serialize(new DiagnosisRecordedNotification("plain"));
        var handler = scope.ServiceProvider.GetRequiredService<IChangeEventHandler<JsonElement>>();

        // Act
        var result = await handler.HandleInsertAsync(CreateOutboxRow(content), CreateContext());

        // Assert
        result.IsRight.ShouldBeTrue();
        encina.PublishedNotifications.ShouldHaveSingleItem()
            .ShouldBeOfType<DiagnosisRecordedNotification>()
            .Diagnosis.ShouldBe("plain");
    }

    private static ServiceProvider BuildProvider(FakeEncina encina, bool encryptionRegisteredFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEncina>(encina);
        services.AddKeyMaterial();

        if (encryptionRegisteredFirst)
        {
            services.AddEncinaMessageEncryption(o => o.EncryptAllMessages = true);
            services.AddEncinaCdc(config => config.UseOutboxCdc("OutboxMessages"));
        }
        else
        {
            services.AddEncinaCdc(config => config.UseOutboxCdc("OutboxMessages"));
            services.AddEncinaMessageEncryption(o => o.EncryptAllMessages = true);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static JsonElement CreateOutboxRow(string content) =>
        JsonSerializer.SerializeToElement(new
        {
            Id = Guid.NewGuid().ToString(),
            NotificationType = typeof(DiagnosisRecordedNotification).AssemblyQualifiedName,
            Content = content,
            ProcessedAtUtc = (string?)null
        });

    private static ChangeContext CreateContext() =>
        new(
            "OutboxMessages",
            new ChangeMetadata(new TestCdcPosition(1), new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc), null, null, null),
            CancellationToken.None);

    public sealed record DiagnosisRecordedNotification(string Diagnosis) : INotification;
}
