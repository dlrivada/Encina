using Encina.Compliance.DataSubjectRights;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.GDPR;

/// <summary>
/// End-to-end integration tests for the "forget subject" flow:
/// store event → forget subject → read event → verify [REDACTED].
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class ForgetSubjectIntegrationTests : IDisposable
{
    private readonly MartenFixture _fixture;
    private readonly InMemorySubjectKeyProvider _keyProvider;

    public ForgetSubjectIntegrationTests(MartenFixture fixture)
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
    public async Task ForgetFlow_StoreEvent_ForgetSubject_ReadEvent_ShowsRedacted()
    {
        // Arrange
        var store = BuildCryptoShredderStore();
        var streamId = Guid.NewGuid();
        var subjectId = "user-forget-test-1";

        // Step 1: Store an event with PII
        await using (var session = store.LightweightSession())
        {
            var evt = new TestForgetEvent
            {
                UserId = subjectId,
                Email = "sensitive@example.com",
                Action = "AccountCreated"
            };
            session.Events.Append(streamId, evt);
            await session.SaveChangesAsync();
        }

        // Step 2: Verify PII is readable before forgetting
        await using (var session = store.LightweightSession())
        {
            var events = await session.Events.FetchStreamAsync(streamId);
            var data = events[0].Data as TestForgetEvent;
            data.ShouldNotBeNull();
            data.Email.ShouldBe("sensitive@example.com",
                "PII should be readable before forgetting");
        }

        // Step 3: Forget the subject (delete encryption keys)
        var deleteResult = await _keyProvider.DeleteSubjectKeysAsync(subjectId);
        deleteResult.IsRight.ShouldBeTrue("Delete should succeed");

        // Step 4: Read back — PII should be replaced with placeholder
        await using (var session = store.LightweightSession())
        {
            var events = await session.Events.FetchStreamAsync(streamId);
            var data = events[0].Data as TestForgetEvent;
            data.ShouldNotBeNull();
            data.Email.ShouldBe("[REDACTED]",
                "PII should be replaced with [REDACTED] after subject is forgotten");
            data.Action.ShouldBe("AccountCreated",
                "Non-PII fields should remain intact");
        }

        store.Dispose();
    }

    [Fact]
    public async Task ForgetFlow_MultiplePiiFields_AllRedacted()
    {
        // Arrange
        var store = BuildCryptoShredderStore();
        var streamId = Guid.NewGuid();
        var subjectId = "user-forget-multi";

        // Step 1: Store event with multiple PII fields
        await using (var session = store.LightweightSession())
        {
            var evt = new TestMultiPiiEvent
            {
                UserId = subjectId,
                Email = "multi@example.com",
                Phone = "+1-555-1234"
            };
            session.Events.Append(streamId, evt);
            await session.SaveChangesAsync();
        }

        // Step 2: Forget
        await _keyProvider.DeleteSubjectKeysAsync(subjectId);

        // Step 3: Read back
        await using (var session = store.LightweightSession())
        {
            var events = await session.Events.FetchStreamAsync(streamId);
            var data = events[0].Data as TestMultiPiiEvent;
            data.ShouldNotBeNull();
            data.Email.ShouldBe("[REDACTED]");
            data.Phone.ShouldBe("[REDACTED]");
        }

        store.Dispose();
    }

    [Fact]
    public async Task ForgetFlow_OnlyAffectsTargetSubject()
    {
        // Arrange
        var store = BuildCryptoShredderStore();
        var streamId = Guid.NewGuid();

        // Store events for two different subjects
        await using (var session = store.LightweightSession())
        {
            session.Events.Append(streamId,
                new TestForgetEvent { UserId = "user-A", Email = "a@example.com", Action = "SignUp" },
                new TestForgetEvent { UserId = "user-B", Email = "b@example.com", Action = "SignUp" });
            await session.SaveChangesAsync();
        }

        // Forget only user-A
        await _keyProvider.DeleteSubjectKeysAsync("user-A");

        // Read back
        await using (var session = store.LightweightSession())
        {
            var events = await session.Events.FetchStreamAsync(streamId);
            events.Count.ShouldBe(2);

            var eventA = events[0].Data as TestForgetEvent;
            var eventB = events[1].Data as TestForgetEvent;
            eventA.ShouldNotBeNull();
            eventB.ShouldNotBeNull();

            eventA.Email.ShouldBe("[REDACTED]", "Forgotten user-A should be redacted");
            eventB.Email.ShouldBe("b@example.com", "Non-forgotten user-B should remain");
        }

        store.Dispose();
    }

    #region Helpers

    private DocumentStore BuildCryptoShredderStore()
    {
        return DocumentStore.For(opts =>
        {
            opts.Connection(_fixture.ConnectionString);
            opts.DatabaseSchemaName = $"forget_test_{Guid.NewGuid():N}";

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

    public class TestForgetEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;
    }

    public class TestMultiPiiEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Phone { get; set; } = string.Empty;
    }

    #endregion
}
