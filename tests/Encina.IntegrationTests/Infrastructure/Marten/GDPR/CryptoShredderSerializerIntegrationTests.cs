using Encina.Compliance.DataSubjectRights;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.GDPR;

/// <summary>
/// Integration tests for <see cref="CryptoShredderSerializer"/> with a real Marten event store.
/// Verifies full encryption/decryption roundtrip using real AES-256-GCM via InMemorySubjectKeyProvider.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class CryptoShredderSerializerIntegrationTests : IDisposable
{
    private readonly MartenFixture _fixture;
    private readonly InMemorySubjectKeyProvider _keyProvider;

    public CryptoShredderSerializerIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
        _keyProvider = new InMemorySubjectKeyProvider(
            TimeProvider.System,
            NullLogger<InMemorySubjectKeyProvider>.Instance);
    }

    public void Dispose()
    {
        _keyProvider.Clear();
        CryptoShreddedPropertyCache.ClearCache();
    }

    [Fact]
    public async Task Roundtrip_StoreAndRetrieveEvent_PiiIsTransparentlyEncrypted()
    {
        // Arrange — build a Marten store with crypto-shredder serializer
        var store = BuildCryptoShredderStore();
        var streamId = Guid.NewGuid();
        var originalEmail = "test@example.com";

        // Act — store event with PII
        await using (var session = store.LightweightSession())
        {
            var evt = new TestPiiEvent
            {
                UserId = "user-integration-1",
                Email = originalEmail,
                OrderId = Guid.NewGuid().ToString()
            };
            session.Events.Append(streamId, evt);
            await session.SaveChangesAsync();
        }

        // Read it back
        string retrievedEmail;
        await using (var session = store.LightweightSession())
        {
            var events = await session.Events.FetchStreamAsync(streamId);
            events.ShouldNotBeEmpty("Should have stored the event");

            var data = events[0].Data as TestPiiEvent;
            data.ShouldNotBeNull();
            retrievedEmail = data.Email;
        }

        // Assert — PII was decrypted transparently
        retrievedEmail.ShouldBe(originalEmail,
            "Email should be transparently decrypted when read back");

        store.Dispose();
    }

    [Fact]
    public async Task NonPiiEvent_IsStoredWithoutEncryption()
    {
        // Arrange
        var store = BuildCryptoShredderStore();
        var streamId = Guid.NewGuid();

        // Act
        await using (var session = store.LightweightSession())
        {
            var evt = new TestNonPiiEvent
            {
                EventName = "OrderShipped",
                Timestamp = DateTimeOffset.UtcNow
            };
            session.Events.Append(streamId, evt);
            await session.SaveChangesAsync();
        }

        await using (var session = store.LightweightSession())
        {
            var events = await session.Events.FetchStreamAsync(streamId);
            events.ShouldNotBeEmpty();

            var data = events[0].Data as TestNonPiiEvent;
            data.ShouldNotBeNull();
            data.EventName.ShouldBe("OrderShipped");
        }

        store.Dispose();
    }

    [Fact]
    public async Task MultipleEventsWithDifferentSubjects_EachEncryptedIndependently()
    {
        // Arrange
        var store = BuildCryptoShredderStore();
        var streamId = Guid.NewGuid();

        // Act — store two events with different user IDs
        await using (var session = store.LightweightSession())
        {
            session.Events.Append(streamId,
                new TestPiiEvent { UserId = "user-A", Email = "a@example.com", OrderId = "1" },
                new TestPiiEvent { UserId = "user-B", Email = "b@example.com", OrderId = "2" });
            await session.SaveChangesAsync();
        }

        // Read back
        await using (var session = store.LightweightSession())
        {
            var events = await session.Events.FetchStreamAsync(streamId);
            events.Count.ShouldBe(2);

            var eventA = events[0].Data as TestPiiEvent;
            var eventB = events[1].Data as TestPiiEvent;
            eventA.ShouldNotBeNull();
            eventB.ShouldNotBeNull();

            eventA.Email.ShouldBe("a@example.com");
            eventB.Email.ShouldBe("b@example.com");
        }

        store.Dispose();
    }

    #region Helpers

    private DocumentStore BuildCryptoShredderStore()
    {
        return DocumentStore.For(opts =>
        {
            opts.Connection(_fixture.ConnectionString);
            opts.DatabaseSchemaName = $"crypto_test_{Guid.NewGuid():N}";

            // Apply crypto-shredder serializer
            CryptoShredderSerializerFactory.Apply(
                opts,
                _keyProvider,
                new DefaultForgottenSubjectHandler(
                    NullLogger<DefaultForgottenSubjectHandler>.Instance),
                NullLogger<CryptoShredderSerializer>.Instance);
        });
    }

    #endregion

    #region Test Events

    public class TestPiiEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;

        public string OrderId { get; set; } = string.Empty;
    }

    public class TestNonPiiEvent
    {
        public string EventName { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    #endregion
}
