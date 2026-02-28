using Encina.Compliance.Anonymization.InMemory;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="InMemoryAnonymizationAuditStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryAnonymizationAuditStoreGuardTests
{
    private readonly InMemoryAnonymizationAuditStore _store = new();

    /// <summary>
    /// Verifies that AddEntryAsync throws ArgumentNullException when entry is null.
    /// </summary>
    [Fact]
    public async Task AddEntryAsync_NullEntry_ThrowsArgumentNullException()
    {
        var act = async () => await _store.AddEntryAsync(null!, CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("entry");
    }

    /// <summary>
    /// Verifies that GetBySubjectIdAsync throws ArgumentNullException when subjectId is null.
    /// </summary>
    [Fact]
    public async Task GetBySubjectIdAsync_NullSubjectId_ThrowsArgumentNullException()
    {
        var act = async () => await _store.GetBySubjectIdAsync(null!, CancellationToken.None);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("subjectId");
    }
}
