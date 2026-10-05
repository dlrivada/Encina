using Encina.Compliance.DataSubjectRights;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.GDPR;

using JasperFx;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Npgsql;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.GDPR;

/// <summary>
/// Nested crypto-shredding on a real Marten/PostgreSQL store wired by <c>AddEncinaMartenGdpr</c> with the
/// PostgreSQL key store (#1698): nested objects, collection elements and dictionary values are stored as
/// <c>cs2</c> tokens, read back as plaintext, located by path and erased through the DSR executor.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class CryptoShreddingNestedIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;
    private ServiceProvider _provider = null!;
    private IDocumentStore _store = null!;
    private string _schema = null!;

    public CryptoShreddingNestedIntegrationTests(MartenFixture fixture) => _fixture = fixture;

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");

        _schema = $"nested_crypto_{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarten(opts =>
        {
            opts.Connection(_fixture.ConnectionString);
            opts.DatabaseSchemaName = _schema;
            opts.AutoCreateSchemaObjects = AutoCreate.All;
        }).UseLightweightSessions();
        services.AddEncinaDataSubjectRights();
        services.AddEncinaMartenGdpr(o =>
        {
            o.UsePostgreSqlKeyStore = true;
            o.ValidateOnStartup = false;
        });

        _provider = services.BuildServiceProvider();
        _store = _provider.GetRequiredService<IDocumentStore>();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task NestedCollectionAndDictionary_AreStoredAsTokens_AndReadBackAsPlaintext()
    {
        var subject = $"patient-{Guid.NewGuid():N}";
        var streamId = Guid.NewGuid();
        var evt = NestedVisit(subject, therapist: $"therapist-{Guid.NewGuid():N}");

        await AppendAsync(streamId, evt);

        var raw = await ReadRawAsync(streamId);
        raw.ShouldContain("cs2:");
        raw.ShouldNotContain("contact@example.com");
        raw.ShouldNotContain("note-1");
        raw.ShouldNotContain("dictionary-note");
        raw.ShouldNotContain("therapist@example.com");

        var read = await FetchAsync(streamId);
        read.Contact!.Email.ShouldBe("contact@example.com");
        read.Notes[0].Text.ShouldBe("note-1");
        read.ByTopic["topic"].Text.ShouldBe("dictionary-note");
        read.Therapist!.Email.ShouldBe("therapist@example.com");
    }

    [Fact]
    public async Task Locator_ReportsNestedPaths_AndErasureShredsOnlyThatSubject()
    {
        var subject = $"patient-{Guid.NewGuid():N}";
        var therapist = $"therapist-{Guid.NewGuid():N}";
        var streamId = Guid.NewGuid();
        await AppendAsync(streamId, NestedVisit(subject, therapist));

        await using (var scope = _provider.CreateAsyncScope())
        {
            var locator = scope.ServiceProvider.GetRequiredService<IPersonalDataLocator>();
            var located = await locator.LocateAllDataAsync(subject);
            located.IsRight.ShouldBeTrue();
            located.IfRight(locations => locations.Select(l => l.FieldName).Order(StringComparer.Ordinal).ShouldBe(
                ["ByTopic{}.Text", "Contact.Email", "Notes[].Text"]));

            var executor = scope.ServiceProvider.GetRequiredService<IDataErasureExecutor>();
            var erased = await executor.EraseAsync(subject, new ErasureScope { Reason = ErasureReason.ConsentWithdrawn });
            erased.IsRight.ShouldBeTrue();
            erased.IfRight(r =>
            {
                r.FieldsErased.ShouldBe(3);
                r.FieldsFailed.ShouldBe(0);
            });
        }

        var read = await FetchAsync(streamId);
        read.Contact!.Email.ShouldBe("[REDACTED]");
        read.Notes[0].Text.ShouldBe("[REDACTED]");
        read.ByTopic["topic"].Text.ShouldBe("[REDACTED]");
        read.Therapist!.Email.ShouldBe("therapist@example.com");
    }

    [Fact]
    public async Task MissingNestedSubjectId_StoresNoEvent()
    {
        var streamId = Guid.NewGuid();
        await AppendAsync(Guid.NewGuid(), new SchemaWarmUp());
        var evt = NestedVisit($"patient-{Guid.NewGuid():N}", null);
        evt.Notes[0].SubjectId = null;

        await using (var session = _store.LightweightSession())
        {
            session.Events.Append(streamId, evt);
            await Should.ThrowAsync<Exception>(() => session.SaveChangesAsync());
        }

        (await CountAsync(streamId)).ShouldBe(0);
    }

    [Fact]
    public async Task ReadModelOfAForgottenSubject_IsSavedAgainWithTheTombstone()
    {
        var subject = $"patient-{Guid.NewGuid():N}";
        var document = new VisitReadModel { Id = Guid.NewGuid(), Contact = new ContactBlock { SubjectId = subject, Email = "rm@example.com" } };
        await using (var session = _store.LightweightSession())
        {
            session.Store(document);
            await session.SaveChangesAsync();
        }

        await using (var scope = _provider.CreateAsyncScope())
        {
            var keys = scope.ServiceProvider.GetRequiredService<global::Encina.Marten.GDPR.Abstractions.ISubjectKeyProvider>();
            (await keys.DeleteSubjectKeysAsync(subject)).IsRight.ShouldBeTrue();
        }

        await using (var session = _store.LightweightSession())
        {
            var loaded = await session.LoadAsync<VisitReadModel>(document.Id);
            loaded!.Contact!.Email.ShouldBe("[REDACTED]");
            session.Store(loaded);
            await session.SaveChangesAsync();
        }

        await using (var session = _store.QuerySession())
        {
            (await session.LoadAsync<VisitReadModel>(document.Id))!.Contact!.Email.ShouldBe("[REDACTED]");
        }
    }

    private static NestedVisitRecorded NestedVisit(string subject, string? therapist) => new()
    {
        VisitId = Guid.NewGuid(),
        Contact = new ContactBlock { SubjectId = subject, Email = "contact@example.com" },
        Notes = [new NoteBlock { SubjectId = subject, Text = "note-1" }],
        ByTopic = new Dictionary<string, NoteBlock> { ["topic"] = new() { SubjectId = subject, Text = "dictionary-note" } },
        Therapist = therapist is null ? null : new ContactBlock { SubjectId = therapist, Email = "therapist@example.com" },
    };

    private async Task AppendAsync(Guid streamId, object evt)
    {
        await using var session = _store.LightweightSession();
        session.Events.Append(streamId, evt);
        await session.SaveChangesAsync();
    }

    private async Task<NestedVisitRecorded> FetchAsync(Guid streamId)
    {
        await using var session = _store.QuerySession();
        var events = await session.Events.FetchStreamAsync(streamId);
        return events.Single().Data.ShouldBeOfType<NestedVisitRecorded>();
    }

    private async Task<string> ReadRawAsync(Guid streamId)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"select data::text from {_schema}.mt_events where stream_id = @streamId";
        command.Parameters.AddWithValue("streamId", streamId);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> CountAsync(Guid streamId)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"select count(*) from {_schema}.mt_events where stream_id = @streamId";
        command.Parameters.AddWithValue("streamId", streamId);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    public sealed class SchemaWarmUp
    {
        public string Name { get; set; } = "warm-up";
    }

    public sealed class ContactBlock
    {
        public string? SubjectId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true, Portable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
        public string? Email { get; set; }
    }

    public sealed class NoteBlock
    {
        public string? SubjectId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Health, Erasable = true, Portable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
        public string? Text { get; set; }
    }

    public sealed class NestedVisitRecorded
    {
        public Guid VisitId { get; set; }

        public ContactBlock? Contact { get; set; }

        public List<NoteBlock> Notes { get; set; } = [];

        public Dictionary<string, NoteBlock> ByTopic { get; set; } = [];

        public ContactBlock? Therapist { get; set; }
    }

    public sealed class VisitReadModel
    {
        public Guid Id { get; set; }

        public ContactBlock? Contact { get; set; }
    }
}
