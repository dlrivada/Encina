using Encina.Compliance.Anonymization.InMemory;
using Encina.Compliance.Anonymization.Model;
using Shouldly;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Regression tests for #2135: entries that share a PerformedAtUtc come back in a total order (id descending).
/// </summary>
public class InMemoryAnonymizationAuditStoreOrderTests
{
    private static readonly DateTimeOffset Shared = new(2026, 10, 16, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetBySubjectIdAsync_EntriesSharingATimestamp_AreOrderedByIdDescending()
    {
        var store = await FilledStoreAsync();

        var result = await store.GetBySubjectIdAsync("subject-1");

        result.Match(
            Right: entries => entries.Select(e => e.Id).ToList().ShouldBe(["e", "d", "c", "b", "a"]),
            Left: _ => throw new ShouldAssertException("query failed"));
    }

    [Fact]
    public async Task GetAllAsync_EntriesSharingATimestamp_AreOrderedByIdDescending()
    {
        var store = await FilledStoreAsync();

        var result = await store.GetAllAsync();

        result.Match(
            Right: entries => entries.Select(e => e.Id).ToList().ShouldBe(["e", "d", "c", "b", "a"]),
            Left: _ => throw new ShouldAssertException("query failed"));
        store.GetAllEntries().Select(e => e.Id).ToList().ShouldBe(["e", "d", "c", "b", "a"]);
    }

    private static async Task<InMemoryAnonymizationAuditStore> FilledStoreAsync()
    {
        var store = new InMemoryAnonymizationAuditStore();
        foreach (var id in new[] { "c", "a", "e", "b", "d" })
        {
            var entry = AnonymizationAuditEntry.Create(AnonymizationOperation.Anonymized, subjectId: "subject-1") with
            {
                Id = id,
                PerformedAtUtc = Shared
            };
            (await store.AddEntryAsync(entry)).IsRight.ShouldBeTrue();
        }

        return store;
    }
}
