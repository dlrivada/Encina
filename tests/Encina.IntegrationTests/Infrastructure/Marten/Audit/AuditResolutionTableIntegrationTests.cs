using Encina.Audit.Marten;
using Encina.Audit.Marten.Projections;
using Encina.Audit.Marten.Resolution;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Npgsql;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Audit;

/// <summary>
/// Issue #3049 (phase 1): on a fresh Marten store configured by <see cref="ConfigureMartenOperationAuditProjections"/>,
/// the <see cref="AuditResolution"/> table exists after the configured schema changes are applied, before any
/// projection daemon reads it, and a document round-trips through its string id.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class AuditResolutionTableIntegrationTests(MartenFixture fixture)
{
    [Fact]
    public async Task FreshStore_HasTheResolutionTable_AndTheDocumentRoundTrips()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("PostgreSQL through Testcontainers is not available.");
        }

        var schema = $"audit_resolution_{Guid.NewGuid():N}"[..40];
        using var store = DocumentStore.For(options =>
        {
            options.Connection(fixture.ConnectionString);
            options.DatabaseSchemaName = schema;
            options.Events.StreamIdentity = JasperFx.Events.StreamIdentity.AsString;
            new ConfigureMartenOperationAuditProjections(
                Options.Create(new MartenOperationAuditOptions()), NullLoggerFactory.Instance).Configure(options);
        });

        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new NpgsqlCommand("select to_regclass(@name)::text", connection);
            command.Parameters.AddWithValue("name", $"{schema}.mt_doc_auditresolution");
            var table = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            table.ShouldNotBeNull().ShouldNotBeOfType<DBNull>();
        }

        var eventId = Guid.NewGuid();
        var id = AuditResolutionIds.Format(AuditResolutionTrail.Operation, eventId, AuditResolutionField.All);
        await using (var session = store.LightweightSession())
        {
            session.Store(new AuditResolution
            {
                Id = id,
                Trail = AuditResolutionTrail.Operation,
                EventId = eventId,
                Field = AuditResolutionField.All,
                Kind = AuditResolutionKind.Unreadable,
                ResolvedBy = "operator",
                ResolvedAtUtc = DateTimeOffset.UtcNow
            });
            await session.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var query = store.QuerySession();
        var loaded = await query.LoadAsync<AuditResolution>(id, TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.EventId.ShouldBe(eventId);
        loaded.Kind.ShouldBe(AuditResolutionKind.Unreadable);
    }
}
