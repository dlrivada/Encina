#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern
using Encina.Compliance.DataSubjectRights;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

using Npgsql;

using NSubstitute.ExceptionExtensions;

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

    [Fact]
    public async Task GuidSubjectId_EncryptsUnderTheSubjectKey_AndErasureMakesTheFieldUnreadable()
    {
        // Arrange (#1174)
        var store = BuildCryptoShredderStore();
        var streamId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        const string email = "guid-subject@example.com";

        await using (var session = store.LightweightSession())
        {
            session.Events.Append(streamId, new TestGuidSubjectEvent { PatientId = patientId, Email = email });
            await session.SaveChangesAsync();
        }

        // Stored JSON holds the encrypted envelope, never the plaintext
        var storedJson = await ReadStoredEventJsonAsync(store, streamId);
        storedJson.ShouldContain("__enc");
        storedJson.ShouldNotContain(email);

        // Readable while the subject key exists
        (await ReadEmailAsync(store, streamId)).ShouldBe(email);

        // Act: erase the subject (the key is registered under the invariant string form of the Guid)
        var erased = await _keyProvider.DeleteSubjectKeysAsync(patientId.ToString("D"));
        erased.IsRight.ShouldBeTrue();

        // Assert: the field is unreadable after erasure
        var afterErasure = await ReadEmailAsync(store, streamId);
        afterErasure.ShouldNotBe(email);
        afterErasure.ShouldBe("[REDACTED]");

        store.Dispose();
    }

    [Fact]
    public async Task StronglyTypedSubjectId_EncryptsUnderTheWrappedValueKey_AndErasureMakesTheFieldUnreadable()
    {
        // Arrange (#1174)
        var store = BuildCryptoShredderStore();
        var streamId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        const string email = "typed-subject@example.com";

        await using (var session = store.LightweightSession())
        {
            session.Events.Append(streamId, new TestTypedSubjectEvent { PatientId = new TestPatientId(patientId), Email = email });
            await session.SaveChangesAsync();
        }

        (await ReadStoredEventJsonAsync(store, streamId)).ShouldNotContain(email);
        (await ReadEmailAsync(store, streamId)).ShouldBe(email);

        // Act
        var erased = await _keyProvider.DeleteSubjectKeysAsync(patientId.ToString("D"));
        erased.IsRight.ShouldBeTrue();

        // Assert
        (await ReadEmailAsync(store, streamId)).ShouldBe("[REDACTED]");

        store.Dispose();
    }

    [Fact]
    public async Task Append_KeyProviderFails_ThrowsAndStoresNoEvent()
    {
        // Arrange (#1646): the key store is down, so the PII field cannot be encrypted
        var failingKeys = Substitute.For<ISubjectKeyProvider>();
        failingKeys.GetOrCreateSubjectKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new NpgsqlException("key store unreachable"));
        var store = BuildCryptoShredderStore(failingKeys);
        await EnsureEventSchemaAsync(store);
        var streamId = Guid.NewGuid();

        // Act
        await using (var session = store.LightweightSession())
        {
            session.Events.Append(streamId, new TestPiiEvent { UserId = "user-down", Email = "down@example.com", OrderId = "1" });
            await Should.ThrowAsync<Exception>(() => session.SaveChangesAsync());
        }

        // Assert: nothing reached the event store, encrypted or not
        (await CountStoredEventsAsync(store, streamId)).ShouldBe(0);

        store.Dispose();
    }

    [Fact]
    public async Task Append_MissingSubjectId_ThrowsAndStoresNoEvent()
    {
        // Arrange (#1646)
        var store = BuildCryptoShredderStore();
        await EnsureEventSchemaAsync(store);
        var streamId = Guid.NewGuid();

        // Act
        await using (var session = store.LightweightSession())
        {
            session.Events.Append(streamId, new TestGuidSubjectEvent { PatientId = Guid.Empty, Email = "nobody@example.com" });
            await Should.ThrowAsync<Exception>(() => session.SaveChangesAsync());
        }

        // Assert
        (await CountStoredEventsAsync(store, streamId)).ShouldBe(0);

        store.Dispose();
    }

    #region Helpers

    /// <summary>Creates the event tables by appending a non-PII event to another stream.</summary>
    private static async Task EnsureEventSchemaAsync(DocumentStore store)
    {
        await using var session = store.LightweightSession();
        session.Events.Append(Guid.NewGuid(), new TestNonPiiEvent { EventName = "SchemaWarmUp", Timestamp = DateTimeOffset.UnixEpoch });
        await session.SaveChangesAsync();
    }

    private async Task<long> CountStoredEventsAsync(DocumentStore store, Guid streamId)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"select count(*) from {store.Options.DatabaseSchemaName}.mt_events where stream_id = @streamId";
        command.Parameters.AddWithValue("streamId", streamId);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string> ReadEmailAsync(DocumentStore store, Guid streamId)
    {
        await using var session = store.LightweightSession();
        var events = await session.Events.FetchStreamAsync(streamId);
        events.ShouldNotBeEmpty();

        return events[0].Data switch
        {
            TestGuidSubjectEvent guidEvent => guidEvent.Email,
            TestTypedSubjectEvent typedEvent => typedEvent.Email,
            var other => throw new InvalidOperationException($"Unexpected event type {other.GetType().Name}")
        };
    }

    private async Task<string> ReadStoredEventJsonAsync(DocumentStore store, Guid streamId)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"select data::text from {store.Options.DatabaseSchemaName}.mt_events where stream_id = @streamId";
        command.Parameters.AddWithValue("streamId", streamId);

        var json = await command.ExecuteScalarAsync() as string;
        json.ShouldNotBeNull();
        return json;
    }

    private DocumentStore BuildCryptoShredderStore(ISubjectKeyProvider? keyProvider = null)
    {
        return DocumentStore.For(opts =>
        {
            opts.Connection(_fixture.ConnectionString);
            opts.DatabaseSchemaName = $"crypto_test_{Guid.NewGuid():N}";

            // Apply crypto-shredder serializer
            CryptoShredderSerializerFactory.Apply(
                opts,
                keyProvider ?? _keyProvider,
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

    public sealed record TestPatientId(Guid Value);

    public class TestGuidSubjectEvent
    {
        public Guid PatientId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }

    public class TestTypedSubjectEvent
    {
        public TestPatientId PatientId { get; set; } = new(Guid.Empty);

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
        public string Email { get; set; } = string.Empty;
    }

    public class TestNonPiiEvent
    {
        public string EventName { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    #endregion
}
