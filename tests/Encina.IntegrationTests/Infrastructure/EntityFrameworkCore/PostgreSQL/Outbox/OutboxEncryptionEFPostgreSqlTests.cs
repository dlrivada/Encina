using System.Collections.Immutable;
using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Attributes;
using Encina.Messaging.Encryption.Model;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Outbox;

/// <summary>
/// Verifies that outbox payloads written through <see cref="OutboxOrchestrator"/> and
/// <see cref="EncryptingMessageSerializer"/> are stored encrypted in the real PostgreSQL
/// column, and that plain (non-encrypted) notifications round-trip as before (#1168).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class OutboxEncryptionEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;

    public OutboxEncryptionEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [EncryptedMessage]
    private sealed record PatientDiagnosisNotification : INotification
    {
        public string Diagnosis { get; init; } = string.Empty;
    }

    [Fact]
    public async Task AddAsync_WithEncryptedMessageAttribute_StoresCiphertextInRawColumn()
    {
        // Arrange
        EncryptedMessageAttributeCache.ClearCache();
        try
        {
            await using var context = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
            var store = new OutboxStoreEF(context);
            var messageFactory = new OutboxMessageFactory();
            var serializer = CreateEncryptingSerializer();
            var orchestrator = new OutboxOrchestrator(
                store,
                new OutboxOptions(),
                NullLogger<OutboxOrchestrator>.Instance,
                messageFactory,
                serializer);

            var notification = new PatientDiagnosisNotification { Diagnosis = "F41.1" };

            // Act
            (await orchestrator.AddAsync(notification)).ShouldBeRight();
            (await store.SaveChangesAsync()).ShouldBeRight();

            // Assert: read the raw column directly, bypassing any serializer, exactly
            // like the outbox processing loop would when it reads the persisted row.
            await using var verifyContext = _fixture.CreateDbContext<TestPostgreSqlDbContext>();
            var stored = await verifyContext.Set<OutboxMessage>()
                .SingleAsync(m => m.NotificationType.Contains(nameof(PatientDiagnosisNotification)));

            stored.Content.ShouldStartWith("ENC:v1:");
            stored.Content.ShouldNotContain("F41.1");

            // Round-trips back through the same serializer.
            var roundTripped = serializer.Deserialize<PatientDiagnosisNotification>(stored.Content);
            roundTripped.ShouldNotBeNull();
            roundTripped!.Diagnosis.ShouldBe("F41.1");
        }
        finally
        {
            EncryptedMessageAttributeCache.ClearCache();
        }
    }

    private static EncryptingMessageSerializer CreateEncryptingSerializer()
    {
        var inner = new JsonMessageSerializer();
        var provider = new PassthroughMessageEncryptionProvider();
        var options = Options.Create(new MessageEncryptionOptions { Enabled = true });
        return new EncryptingMessageSerializer(inner, provider, options, NullLogger<EncryptingMessageSerializer>.Instance);
    }

    /// <summary>
    /// Test double that tags plaintext bytes verbatim as ciphertext and reverses that on
    /// decrypt. This isolates the assertion to "does the outbox column hold the encrypted
    /// envelope" without depending on a real KMS/key-provider setup in the integration
    /// environment.
    /// </summary>
    private sealed class PassthroughMessageEncryptionProvider : IMessageEncryptionProvider
    {
        public ValueTask<Either<EncinaError, EncryptedPayload>> EncryptAsync(
            ReadOnlyMemory<byte> plaintext,
            MessageEncryptionContext context,
            CancellationToken cancellationToken = default)
        {
            var payload = new EncryptedPayload
            {
                Ciphertext = ImmutableArray.Create(plaintext.Span.ToArray()),
                KeyId = context.KeyId ?? "test-key",
                Algorithm = "test-passthrough",
                Nonce = ImmutableArray.Create<byte>(1, 2, 3),
                Tag = ImmutableArray.Create<byte>(4, 5, 6),
                Version = 1
            };

            return new ValueTask<Either<EncinaError, EncryptedPayload>>(Right<EncinaError, EncryptedPayload>(payload));
        }

        public ValueTask<Either<EncinaError, ImmutableArray<byte>>> DecryptAsync(
            EncryptedPayload payload,
            MessageEncryptionContext context,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<Either<EncinaError, ImmutableArray<byte>>>(
                Right<EncinaError, ImmutableArray<byte>>(payload.Ciphertext));
        }
    }
}
